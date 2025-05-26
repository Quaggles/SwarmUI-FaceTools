using SwarmUI.Core;
using System.IO;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json.Linq;
using SwarmUI.Backends;
using SwarmUI.Builtin_ComfyUIBackend;
using SwarmUI.Text2Image;
using SwarmUI.Utils;
using SearchOption = System.IO.SearchOption;

namespace Quaggles.Extensions.FaceTools;

public class FaceToolsExtension : Extension
{
    public static float StepInjectPriority = 9.1f;
    public const string ExtensionPrefix = "[FaceTools] ";
    public const string Prefix = "[ReActor] ";
    public const string FeatureId = "reactor";
    public static string ModelHashCacheLocation;
    public const int FaceToolsNodeIndex = 51000; // If you are copying this code pick a different index to prevents nodes trying to use the same ids

    public static T2IRegisteredParam<Image> FaceImage;
    public static T2IRegisteredParam<double> FaceRestoreVisibility, CodeFormerWeight;
    public static T2IRegisteredParam<string>FaceRestoreModel,
        SecondFaceRestoreModel,
        FaceSwapModel,
        FaceModel,
        FaceMaskModel,
        FaceDetectionModel,
        InputFacesIndex,
        SourceFacesIndex,
        InputGenderDetect,
        SourceGenderDetect,
        InputFacesOrder,
        SourceFacesOrder;

    public static T2IRegisteredParam<bool> FaceBoost, FaceBoostRestoreAfterMain, RemoveParamsIfDefault;
    
    // Prepopulated with options that should always exist
    public static List<string> FaceRestoreModels = ["none", "codeformer-v0.1.0.pth", "GFPGANv1.3.pth", "GFPGANv1.4.pth", "GPEN-BFR-512.onnx", "GPEN-BFR-1024.onnx", "GPEN-BFR-2048.onnx"];
    public static List<string> FaceSwapModels = ["inswapper_128.onnx", "reswapper_128.onnx", "reswapper_256.onnx"];
    public static List<string> FaceModels = ["none"];
    public static List<string> FaceDetectionModels = ["retinaface_resnet50", "retinaface_mobile0.25", "YOLOv5l", "YOLOv5n"];
    public static List<string> GenderDetectOptions = ["no", "female", "male"];
    public const string GenderDetectOptionsDefault = "no";
    public static List<string> FacesOrderOptions = ["left-right", "right-left", "top-bottom", "bottom-top", "small-large", "large-small"];
    public const string FacesOrderOptionsDefault = "large-small";
    public static List<string> YoloModels = ["none"];
    
    // A list of swap parameters that should be removed if swapping is not used
    private static HashSet<T2IParamType> faceSwapParams = [];
    
    public override void OnInit()
    {
        ModelHashCacheLocation = Path.Combine(Environment.CurrentDirectory, FilePath, "ModelHashCache.json");
        ScriptFiles.Add("assets/facetools.js");
        
        // Define required nodes
        ComfyUIBackendExtension.NodeToFeatureMap["ReActorFaceSwapOpt"] = FeatureId;
        string oldNodePath = Utilities.CombinePathWithAbsolute(Environment.CurrentDirectory, $"{ComfyUIBackendExtension.Folder}/DLNodes/comfyui-reactor-node");
        string newNodePath = Utilities.CombinePathWithAbsolute(Environment.CurrentDirectory, $"{ComfyUIBackendExtension.Folder}/DLNodes/ComfyUI-ReActor");
        string nodeUrl = "https://github.com/Gourieff/ComfyUI-ReActor";
        
        // Add required custom node as installable feature
        InstallableFeatures.RegisterInstallableFeature(new("ReActor", FeatureId, nodeUrl, "Gourieff", $"This will install the ComfyUI-ReActor node developed by Gourieff from {nodeUrl}.\nDo you wish to install?"));
        ComfyUISelfStartBackend.ComfyNodeGitPins["ComfyUI-ReActor"] = "d901609a1d5d1942a6b069b2f8f3778fee3a7134";
        
        // If the old repository is installed remove it as it's unsupported and will pop up Github login windows
        if (Directory.Exists(oldNodePath))
        {
            Logs.Init($"{ExtensionPrefix}Moving deprecated ReActor repository to recycle bin '{oldNodePath}', click the 'Install ReActor' button in the parameter list to install its replacement");
            bool success = false;
            try
            {
                FileSystem.DeleteDirectory(oldNodePath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin, UICancelOption.ThrowException);
                success = true;
            }
            catch (Exception ex)
            {
                Logs.Error($"{ExtensionPrefix}Failed to move '{oldNodePath}' folder to recycle bin, will try to delete permanently -- error was {ex.ReadableString()}");
            }

            try
            {
                // If the previous operation was unsuccessful and the folder still exists try an alternative method to delete
                if (!success && Directory.Exists(oldNodePath))
                {
                    // This is required as git objects are marked readonly and prevent Directory.Delete from working
                    Logs.Init($"{ExtensionPrefix}Removing ReadOnly attribute from all files/directories in '{oldNodePath}'");
                    var directory = new DirectoryInfo(oldNodePath);
                    foreach (var info in directory.GetFileSystemInfos("*", SearchOption.AllDirectories))
                        info.Attributes = FileAttributes.Normal;
                    Logs.Init($"{ExtensionPrefix}Deleting directory '{oldNodePath}'");
                    directory.Delete(true);
                    Logs.Init($"{ExtensionPrefix}Successfully deleted directory '{oldNodePath}'");
                }
            } catch (Exception ex)
            {
                Logs.Error($"{ExtensionPrefix}Could not delete '{oldNodePath}', please remove this folder manually then reboot SwarmUI -- error was {ex.ReadableString()}");
            }
        }
        
        // Prevents install button from being shown during backend load if it looks like it was installed
        // it will appear if the backend loads and the backend reports it's not installed
        if (Directory.Exists(newNodePath))
        {
            ComfyUIBackendExtension.FeaturesSupported.UnionWith([FeatureId]);
            ComfyUIBackendExtension.FeaturesDiscardIfNotFound.UnionWith([FeatureId]);
        }
        
        // Callbacks when ComfyUI is ready to get node and model information
        ComfyUIBackendExtension.RawObjectInfoParsers.Add(rawObjectInfo =>
        {
            if (rawObjectInfo.TryGetValue("ReActorFaceSwapOpt", out JToken nodeReActor))
            {
                T2IParamTypes.ConcatDropdownValsClean(ref FaceRestoreModels, nodeReActor["input"]?["required"]?["face_restore_model"]?.FirstOrDefault()?.Select(m => $"{m}") ?? []);
                T2IParamTypes.ConcatDropdownValsClean(ref FaceSwapModels, nodeReActor["input"]?["required"]?["swap_model"]?.FirstOrDefault()?.Select(m => $"{m}"));
            }
            if (rawObjectInfo.TryGetValue("ReActorLoadFaceModel", out JToken nodeReActorLoadFaceModel))
            {
                T2IParamTypes.ConcatDropdownValsClean(ref FaceModels, nodeReActorLoadFaceModel["input"]?["required"]?["face_model"]?.FirstOrDefault()?.Select(m => $"{m}") ?? []);
            }
            if (rawObjectInfo.TryGetValue("SwarmYoloDetection", out JToken nodeYoloDetection))
            {
                T2IParamTypes.ConcatDropdownValsClean(ref YoloModels, nodeYoloDetection["input"]?["required"]?["model_name"]?.FirstOrDefault()?.Select(m => $"{m}") ?? []);
            }
        });
        
        // We can find yolo models before backends load since they are in modelroot
        T2IParamTypes.ConcatDropdownValsClean(ref YoloModels, ComfyUIBackendExtension.InternalListModelsFor("yolov8", false));

        // Setup parameters
        T2IParamGroup reactorGroup = new("ReActor", Toggles: true, Open: false, IsAdvanced: false, OrderPriority: 9);
        int orderCounter = 0;
        var modelRoot = Utilities.CombinePathWithAbsolute(Environment.CurrentDirectory, Program.ServerSettings.Paths.ModelRoot);
        FaceImage = T2IParamTypes.Register<Image>(new($"{Prefix}Face Image",
            "[Optional] The source image containing a face you want to swap, leave empty/turn off to only run face restore.",
            null,
            Toggleable: true,
            Group: reactorGroup,
            ChangeWeight: 2,
            FeatureFlag: FeatureId,
            OrderPriority: orderCounter++
        ));
        FaceModel = T2IParamTypes.Register<string>(new($"{Prefix}Face Model",
            "[Optional] The model containing a face you want to swap, this is skipped if a 'Face Image' is provided and enabled.\n" +
            "Face Models can be created in the Comfy Workflow tab using the 'Save Face Model' node.\n" +
            "To add new models put them in <i><b>'SwarmUI/dlbackend/comfy/ComfyUI/models/reactor/faces'</b></i>",
            "none",
            GetValues: _ => FaceModels,
            Group: reactorGroup,
            IgnoreIf: "none",
            FeatureFlag: FeatureId,
            ChangeWeight: 2,
            OrderPriority: orderCounter++
        ));
        FaceRestoreVisibility = T2IParamTypes.Register<double>(new($"{Prefix}Face Restore Visibility",
            "How visible the face restore is against the original image (Higher is stronger).",
            "1",
            Min: 0.1, Max: 1, Step: 0.01,
            ViewType: ParamViewType.SLIDER,
            Group: reactorGroup,
            FeatureFlag: FeatureId,
            OrderPriority: orderCounter++
        ));
        FaceRestoreModel = T2IParamTypes.Register<string>(new($"{Prefix}Face Restore Model",
            "[Optional] Model to use for face restoration.\n" +
            "To add new models put them in <i><b>'SwarmUI/dlbackend/comfy/ComfyUI/models/facerestore_models'</b></i>.\n" +
            "Download from <a href=\"https://huggingface.co/datasets/Gourieff/ReActor/tree/main/models/facerestore_models\">https://huggingface.co/datasets/Gourieff/ReActor/tree/main/models/facerestore_models</a>",
            "codeformer-v0.1.0.pth",
            GetValues: _ => FaceRestoreModels,
            Group: reactorGroup,
            FeatureFlag: FeatureId,
            OrderPriority: orderCounter++
        ));
        CodeFormerWeight = T2IParamTypes.Register<double>(new($"{Prefix}CodeFormer Weight",
            "Face restoration weight with CodeFormer model (Lower is stronger).",
            "0.5",
            Min: 0, Max: 1, Step: 0.01,
            ViewType: ParamViewType.SLIDER,
            Group: reactorGroup,
            FeatureFlag: FeatureId,
            OrderPriority: orderCounter++
        ));
        SecondFaceRestoreModel = T2IParamTypes.Register<string>(new($"{Prefix}Second Face Restore Model",
            "Runs a second face restoration model after the main swap/restoration.",
            "GPEN-BFR-512.onnx",
            IgnoreIf: "none",
            GetValues: _ => FaceRestoreModels,
            Group: reactorGroup,
            FeatureFlag: FeatureId,
            IsAdvanced: true,
            Toggleable: true,
            OrderPriority: orderCounter++
        ));
        FaceMaskModel = T2IParamTypes.Register<string>(new($"{Prefix}Face Mask Model", 
            "Masks out the area around faces to avoid the swap/restoration affecting other nearby elements like hair.\n" +
            $"To add new models put them in <i><b>'{modelRoot}/yolov8'</b></i>.\n" +
            "Download from <a href=\"https://github.com/hben35096/assets/releases/\">https://github.com/hben35096/assets/releases/</a>",
            "face_yolov8m-seg_60.pt",
            IgnoreIf: "none",
            FeatureFlag: FeatureId, 
            Group: reactorGroup, 
            GetValues: _ => YoloModels, 
            Toggleable: true,
            OrderPriority: orderCounter++
        ));
        FaceBoost = T2IParamTypes.Register<bool>(new($"{Prefix}Face Boost",
            "Restores the face after the face swap but before transplanting on the generated image, can increase quality but results may vary.\n" +
            "A side effect of enabling this with <b>Restore After Main</b> disabled with is that only one face in the image will be restored or swapped",
            "false",
            IgnoreIf: "false",
            Group: reactorGroup,
            FeatureFlag: FeatureId,
            IsAdvanced: true,
            ChangeWeight: 1,
            OrderPriority: orderCounter++
        ));
        FaceBoostRestoreAfterMain = T2IParamTypes.Register<bool>(new($"{Prefix}Face Boost Restore After Main",
            "Restores the face again after transplanting on the generated image",
            "false",
            IgnoreIf: "false",
            Group: reactorGroup,
            FeatureFlag: FeatureId,
            IsAdvanced: true,
            OrderPriority: orderCounter++
        ));
        InputFacesOrder = T2IParamTypes.Register<string>(new($"{Prefix}Input Faces Order",
            "Sorting order for faces in the generated image.",
            FacesOrderOptionsDefault,
            GetValues: _ => FacesOrderOptions,
            Group: reactorGroup,
            FeatureFlag: FeatureId,
            IsAdvanced: true,
            OrderPriority: orderCounter++
        ));
        InputFacesIndex = T2IParamTypes.Register<string>(new($"{Prefix}Input Faces Index",
            "Changes the indexes of faces on the generated image for swapping.",
            "0",
            Examples: ["0,1,2", "1,0,2"],
            Group: reactorGroup,
            FeatureFlag: FeatureId,
            IsAdvanced: true,
            OrderPriority: orderCounter++
        ));
        InputGenderDetect = T2IParamTypes.Register<string>(new($"{Prefix}Input Gender Detect",
            "Only swap faces in the generated image that match this gender.",
            GenderDetectOptionsDefault,
            GetValues: _ => GenderDetectOptions,
            Group: reactorGroup,
            FeatureFlag: FeatureId,
            IsAdvanced: true,
            OrderPriority: orderCounter++
        ));
        SourceFacesOrder = T2IParamTypes.Register<string>(new($"{Prefix}Source Faces Order",
            "Sorting order for faces in the source face image.",
            FacesOrderOptionsDefault,
            GetValues: _ => FacesOrderOptions,
            Group: reactorGroup,
            FeatureFlag: FeatureId,
            IsAdvanced: true,
            OrderPriority: orderCounter++
        ));
        SourceFacesIndex = T2IParamTypes.Register<string>(new($"{Prefix}Source Faces Index",
            "Changes the indexes of faces on the source face image for swapping.",
            "0",
            Examples: ["0,1,2", "1,0,2"],
            Group: reactorGroup,
            FeatureFlag: FeatureId,
            IsAdvanced: true,
            OrderPriority: orderCounter++
        ));
        SourceGenderDetect = T2IParamTypes.Register<string>(new($"{Prefix}Source Gender Detect",
            "Only swap faces in the source face image that match this gender.",
            GenderDetectOptionsDefault,
            GetValues: _ => GenderDetectOptions,
            Group: reactorGroup,
            FeatureFlag: FeatureId,
            IsAdvanced: true,
            OrderPriority: orderCounter++
        ));
        FaceDetectionModel = T2IParamTypes.Register<string>(new($"{Prefix}Face Detection Model",
            "Model to use for face detection.",
            FaceDetectionModels.First(),
            GetValues: _ => FaceDetectionModels,
            Group: reactorGroup,
            FeatureFlag: FeatureId,
            IsAdvanced: true,
            OrderPriority: orderCounter++
        ));
        FaceSwapModel = T2IParamTypes.Register<string>(new($"{Prefix}Face Swap Model",
            "Model to use for face swap.\n" +
            "To add new models put them in <i><b>'SwarmUI/dlbackend/comfy/ComfyUI/models/insightface'</b></i>.\n" +
            "Download from <a href=\"https://huggingface.co/datasets/Gourieff/ReActor/tree/main/models\">https://huggingface.co/datasets/Gourieff/ReActor/tree/main/models</a>",
            "inswapper_128.onnx",
            GetValues: _ => FaceSwapModels,
            Group: reactorGroup,
            FeatureFlag: FeatureId,
            IsAdvanced: true,
            OrderPriority: orderCounter++
        ));
        RemoveParamsIfDefault = T2IParamTypes.Register<bool>(new($"{Prefix}Remove Params If Default",
            "Removes certain parameters if they use the default value for a cleaner parameter list in the UI",
            "true",
            IgnoreIf: "true",
            DoNotSave: true,
            DoNotPreview: true,
            Group: reactorGroup,
            FeatureFlag: FeatureId,
            IsAdvanced: true,
            OrderPriority: orderCounter++
        ));

        // List of parameters that are only used if face swapping
        faceSwapParams =
        [
            FaceBoost.Type,
            FaceBoostRestoreAfterMain.Type,
            FaceSwapModel.Type,
            InputFacesIndex.Type,
            SourceFacesIndex.Type,
            InputGenderDetect.Type,
            SourceGenderDetect.Type,
            InputFacesOrder.Type,
            SourceFacesOrder.Type,
        ];

        // Add into workflow
        WorkflowGenerator.AddStep(g =>
        {
            // Get these parameters first to determine if we should run
            bool hasFaceRestoreModel = g.UserInput.TryGet(FaceRestoreModel, out string faceRestoreModel) && faceRestoreModel != "none";
            var hasFaceRestoreModelExtra = g.UserInput.TryGet(SecondFaceRestoreModel, out string faceRestoreModelExtra) && faceRestoreModelExtra != "none";
            bool hasImage = g.UserInput.TryGet(FaceImage, out Image inputImage);
            bool hasModel = g.UserInput.TryGet(FaceModel, out string faceModel);
            var faceSwapModel = g.UserInput.Get(FaceSwapModel);

            // Only work if either of these are passed
            if (!hasFaceRestoreModel && !hasImage && !hasModel)
                return;
            
            if (!g.Features.Contains(FeatureId))
                throw new SwarmUserErrorException("ReActor parameters specified, but feature isn't installed");
            
            // Get parameters used in multiple branches below
            double faceRestoreVisibility = g.UserInput.GetAndRemoveIfDefault(FaceRestoreVisibility);
            double codeFormerWeight = g.UserInput.GetAndRemoveIfDefault(CodeFormerWeight);
            string faceDetectionModel = g.UserInput.GetAndRemoveIfDefault(FaceDetectionModel);
            
            // If we are not running a checkpoint model load check the integrity of models
            // This can be detected when 0 or 1 steps, doNotSave is true and prompt is "(load the model please)"
            // This is done because exceptions thrown during a checkpoint model load get replaced with a generic failed to load model message
            var isLoadingCheckpoint = g.UserInput.TryGet(T2IParamTypes.Steps, out int steps) && steps <= 1 ||
                                      g.UserInput.TryGet(T2IParamTypes.DoNotSave, out bool doNotSave) && doNotSave &&
                                      g.UserInput.TryGet(T2IParamTypes.Prompt, out string prompt) && prompt == "(load the model please)";
            if (!isLoadingCheckpoint)
            {
                try
                {
                    if (hasFaceRestoreModel)
                        Utils.DownloadOrValidateModel(g,$"models/facerestore_models/{faceRestoreModel}");
                    if (hasFaceRestoreModelExtra)
                        Utils.DownloadOrValidateModel(g, $"models/facerestore_models/{faceRestoreModelExtra}");
                    if ((hasImage || hasModel) && !string.IsNullOrWhiteSpace(faceSwapModel))
                    {
                        if (faceSwapModel.StartsWith("reswapper_"))
                            Utils.DownloadOrValidateModel(g, $"models/reswapper/{faceSwapModel}");
                        else
                            Utils.DownloadOrValidateModel(g, $"models/insightface/{faceSwapModel}");
                        Utils.DownloadOrValidateModel(g, $"models/insightface/models/buffalo_l/1k3d68.onnx");
                        Utils.DownloadOrValidateModel(g, $"models/insightface/models/buffalo_l/2d106det.onnx");
                        Utils.DownloadOrValidateModel(g, $"models/insightface/models/buffalo_l/det_10g.onnx");
                        Utils.DownloadOrValidateModel(g, $"models/insightface/models/buffalo_l/genderage.onnx");
                        Utils.DownloadOrValidateModel(g, $"models/insightface/models/buffalo_l/w600k_r50.onnx");
                        Utils.DownloadOrValidateModel(g, $"models/nsfw_detector/vit-base-nsfw-detector/model.safetensors");
                    }
                }
                finally
                {
                    Utils.SaveHashCache();
                }
            }

            JArray reactorOutput = null;
            if (hasImage || hasModel) // If user passed in an image/model do face swap
            {
                string sourceNode = null;
                if (hasImage)
                {
                    sourceNode = g.CreateLoadImageNode(inputImage, $"${{{FaceImage.Type.ID}}}", false, g.GetStableDynamicID(FaceToolsNodeIndex, 0));
                    // Image has priority over model if both provided
                    hasModel = false;
                    if (g.UserInput.TryRemove(FaceModel.Type))
                        Logs.Verbose($"{ExtensionPrefix}Removed redundant param '{FaceModel.Type.ID}' as it was skipped due to {FaceImage.Type.ID} being provided");
                }

                if (hasModel)
                {
                    sourceNode = g.CreateNode("ReActorLoadFaceModel", new JObject
                    {
                        ["face_model"] = faceModel
                    }, g.GetStableDynamicID(FaceToolsNodeIndex, 1));
                }

                string faceBoostNode = null;
                if (hasFaceRestoreModel && g.UserInput.TryGet(FaceBoost, out bool faceBoost) && faceBoost)
                {
                    faceBoostNode = g.CreateNode("ReActorFaceBoost", new JObject
                    {
                        ["enabled"] = true,
                        ["boost_model"] = faceRestoreModel,
                        ["interpolation"] = "Bicubic",
                        ["visibility"] = faceRestoreVisibility,
                        ["codeformer_weight"] = codeFormerWeight,
                        ["restore_with_main_after"] = g.UserInput.Get(FaceBoostRestoreAfterMain)
                    }, g.GetStableDynamicID(FaceToolsNodeIndex, 2));
                }
                else
                {
                    if (g.UserInput.TryRemove(FaceBoostRestoreAfterMain.Type)) 
                        Logs.Verbose($"{ExtensionPrefix}Removed redundant param '{FaceBoostRestoreAfterMain.Type.ID}' as it wasn't used");
                }

                string optionsNode = g.CreateNode("ReActorOptions", new JObject
                {
                    ["input_faces_order"] = g.UserInput.GetAndRemoveIfDefault(InputFacesOrder),
                    ["input_faces_index"] = g.UserInput.GetAndRemoveIfDefault(InputFacesIndex),
                    ["detect_gender_input"] = g.UserInput.GetAndRemoveIfDefault(InputGenderDetect),
                    ["source_faces_order"] = g.UserInput.GetAndRemoveIfDefault(SourceFacesOrder),
                    ["source_faces_index"] = g.UserInput.GetAndRemoveIfDefault(SourceFacesIndex),
                    ["detect_gender_source"] = g.UserInput.GetAndRemoveIfDefault(SourceGenderDetect),
                    ["console_log_level"] = 1,
                }, g.GetStableDynamicID(FaceToolsNodeIndex, 3));

                string reactorNode = g.CreateNode("ReActorFaceSwapOpt", new JObject
                {
                    ["input_image"] = g.FinalImageOut,
                    ["options"] = new JArray { optionsNode, 0 },
                    ["source_image"] = hasImage ? new JArray { sourceNode, 0 } : null,
                    ["face_model"] = hasModel ? new JArray { sourceNode, 0 } : null,
                    ["face_boost"] = string.IsNullOrEmpty(faceBoostNode) ? null : new JArray { faceBoostNode, 0 },
                    ["enabled"] = true,
                    ["swap_model"] = faceSwapModel,
                    ["facedetection"] = faceDetectionModel,
                    ["face_restore_model"] = faceRestoreModel,
                    ["face_restore_visibility"] = faceRestoreVisibility,
                    ["codeformer_weight"] = codeFormerWeight,
                }, g.GetStableDynamicID(FaceToolsNodeIndex, 4));
                reactorOutput = [reactorNode, 0];
            }
            else // If a face restore model was provided just do that
            {
                string restoreFace = g.CreateNode("ReActorRestoreFace", new JObject
                {
                    ["image"] = g.FinalImageOut,
                    ["facedetection"] = faceDetectionModel,
                    ["model"] = faceRestoreModel,
                    ["visibility"] = faceRestoreVisibility,
                    ["codeformer_weight"] = codeFormerWeight
                }, g.GetStableDynamicID(FaceToolsNodeIndex, 5));
                reactorOutput = [restoreFace, 0];
                
                // Remove parameters from the user input that were not utilised to keep things clean
                foreach (var param in faceSwapParams)
                    if (g.UserInput.TryRemove(param))
                        Logs.Verbose($"{ExtensionPrefix}Removed redundant param '{param.ID}' as face swap was not used");
            }

            // Inserts a second face restore model into the chain
            if (hasFaceRestoreModelExtra)
            {
                string restoreFace = g.CreateNode("ReActorRestoreFace", new JObject
                {
                    ["image"] = reactorOutput,
                    ["facedetection"] = faceDetectionModel,
                    ["model"] = faceRestoreModelExtra,
                    ["visibility"] = faceRestoreVisibility,
                    ["codeformer_weight"] = codeFormerWeight
                }, g.GetStableDynamicID(FaceToolsNodeIndex, 6));
                reactorOutput = [restoreFace, 0];
            }

            // Masks the restored face result by the generated images face
            if (g.UserInput.TryGet(FaceMaskModel, out string faceMaskModel))
            {
                string swarmYoloMask = g.CreateNode("SwarmYoloDetection", new JObject
                {
                    ["image"] = g.FinalImageOut,
                    ["model_name"] = faceMaskModel,
                    ["index"] = 0,
                }, g.GetStableDynamicID(FaceToolsNodeIndex, 7));
                string maskNode = g.CreateNode("ReActorMaskHelper", new JObject
                {
                    ["image"] = g.FinalImageOut,
                    ["swapped_image"] = reactorOutput,
                    ["bbox_model_name"] = "",
                    ["bbox_threshold"] = 0.5,
                    ["bbox_dilation"] = 10,
                    ["bbox_crop_factor"] = 3,
                    ["bbox_drop_size"] = 10,
                    ["sam_model_name"] = "",
                    ["sam_dilation"] = 0,
                    ["sam_threshold"] = 0.93,
                    ["bbox_expansion"] = 0,
                    ["mask_hint_threshold"] = 0.7,
                    ["mask_hint_use_negative"] = "False",
                    ["morphology_operation"] = "dilate",
                    ["morphology_distance"] = 0,
                    ["blur_radius"] = 9,
                    ["sigma_factor"] = 1,
                    ["mask_optional"] = new JArray { swarmYoloMask, 0 },
                }, g.GetStableDynamicID(FaceToolsNodeIndex, 8));
                reactorOutput = [maskNode, 0];
            }

            g.FinalImageOut = reactorOutput;
        }, StepInjectPriority);
    }
}