using Newtonsoft.Json.Linq;
using SwarmUI.Builtin_ComfyUIBackend;
using SwarmUI.Core;
using SwarmUI.Text2Image;
using SwarmUI.Utils;

namespace Quaggles.Extensions.FaceTools;

public static class ReactorParams
{
    public static float StepInjectPriority = 9.1f;
    private const string Prefix = "[ReActor] ";
    private const string FeatureId = "reactor";

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

    public static T2IRegisteredParam<bool> FaceBoost, FaceBoostRestoreAfterMain;
    
    public static List<string> FaceRestoreModels = [];
    public static List<string> FaceSwapModels = [];
    public static List<string> FaceModels = [];
    public static List<string> FaceDetectionModels = [];
    public static List<string> YoloModels = [];
    public static List<string> GenderDetectOptions = [];
    public static List<string> FacesOrderOptions = [];

    public static void Initialise()
    {
        // Define required nodes
        ComfyUIBackendExtension.NodeToFeatureMap["ReActorFaceSwapOpt"] = FeatureId;
        
        // Add required custom node as installable feature
        InstallableFeatures.RegisterInstallableFeature(new("ReActor", FeatureId, "https://github.com/Gourieff/comfyui-reactor-node", "Gourieff", "This will install the ReActor ComfyUI node developed by Gourieff.\nDo you wish to install?"));
        
        ComfyUIBackendExtension.RawObjectInfoParsers.Add(rawObjectInfo =>
        {
            if (rawObjectInfo.TryGetValue("ReActorFaceSwapOpt", out JToken nodeReActor))
            {
                T2IParamTypes.ConcatDropdownValsClean(ref FaceRestoreModels, nodeReActor["input"]?["required"]?["face_restore_model"]?.FirstOrDefault()?.Select(m => $"{m}") ?? []);
                T2IParamTypes.ConcatDropdownValsClean(ref FaceSwapModels, nodeReActor["input"]?["required"]?["swap_model"]?.FirstOrDefault()?.Select(m => $"{m}"));
                T2IParamTypes.ConcatDropdownValsClean(ref FaceDetectionModels, nodeReActor["input"]?["required"]?["facedetection"]?.FirstOrDefault()?.Select(m => $"{m}") ?? []);
            }
            if (rawObjectInfo.TryGetValue("ReActorOptions", out JToken nodeReActorOptions))
            {
                T2IParamTypes.ConcatDropdownValsClean(ref GenderDetectOptions, nodeReActorOptions["input"]?["required"]?["detect_gender_input"]?.FirstOrDefault()?.Select(m => $"{m}") ?? []);
                T2IParamTypes.ConcatDropdownValsClean(ref FacesOrderOptions, nodeReActorOptions["input"]?["required"]?["input_faces_order"]?.FirstOrDefault()?.Select(m => $"{m}") ?? []);
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

        // Setup parameters
        T2IParamGroup reactorGroup = new("ReActor", Toggles: true, Open: false, IsAdvanced: false, OrderPriority: 9);
        int orderCounter = 0;
        var modelRoot = Utilities.CombinePathWithAbsolute(Environment.CurrentDirectory, Program.ServerSettings.Paths.ModelRoot);
        FaceImage = T2IParamTypes.Register<Image>(new($"{Prefix}Face Image",
            $"The source image containing a face you want to swap, leave empty/turn off to only run face restore.",
            null,
            Toggleable: true,
            Group: reactorGroup,
            ChangeWeight: 2,
            FeatureFlag: FeatureId,
            OrderPriority: orderCounter++
        ));
        FaceModel = T2IParamTypes.Register<string>(new($"{Prefix}Face Model",
            $"The model containing a face you want to swap, this is skipped if a 'Face Image' is provided and enabled.\n" +
            $"To add new models put them in <i><b>'ComfyUI/models/reactor/faces'</b></i>",
            "none",
            GetValues: _ => FaceModels,
            Group: reactorGroup,
            IgnoreIf: "none",
            FeatureFlag: FeatureId,
            ChangeWeight: 2,
            OrderPriority: orderCounter++
        ));
        FaceRestoreVisibility = T2IParamTypes.Register<double>(new($"{Prefix}Face Restore Visibility",
            $"How visible the face restore is against the original image (Higher is stronger).",
            "1",
            Min: 0.1, Max: 1, Step: 0.01,
            ViewType: ParamViewType.SLIDER,
            Group: reactorGroup,
            FeatureFlag: FeatureId,
            OrderPriority: orderCounter++
        ));
        FaceRestoreModel = T2IParamTypes.Register<string>(new($"{Prefix}Face Restore Model",
            $"Model to use for face restoration.\n" +
            $"To add new models put them in <i><b>'ComfyUI/models/facerestore_model'</b></i>.\n" +
            "Download from <a href=\"https://huggingface.co/datasets/Gourieff/ReActor/tree/main/models/facerestore_models\">https://huggingface.co/datasets/Gourieff/ReActor/tree/main/models/facerestore_models</a>",
            "codeformer-v0.1.0.pth",
            GetValues: _ => FaceRestoreModels,
            Group: reactorGroup,
            FeatureFlag: FeatureId,
            OrderPriority: orderCounter++
        ));
        CodeFormerWeight = T2IParamTypes.Register<double>(new($"{Prefix}CodeFormer Weight",
            $"Face restoration weight with CodeFormer model (Lower is stronger).",
            "0.5",
            Min: 0, Max: 1, Step: 0.01,
            ViewType: ParamViewType.SLIDER,
            Group: reactorGroup,
            FeatureFlag: FeatureId,
            OrderPriority: orderCounter++
        ));
        SecondFaceRestoreModel = T2IParamTypes.Register<string>(new($"{Prefix}Second Face Restore Model",
            $"Runs a second face restoration model after the main swap/restoration.",
            "GPEN-BFR-1024.onnx",
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
            FeatureFlag: FeatureId, 
            Group: reactorGroup, 
            GetValues: _ => YoloModels, 
            Toggleable: true,
            OrderPriority: orderCounter++
        ));
        FaceBoost = T2IParamTypes.Register<bool>(new($"{Prefix}Face Boost",
            $"Restores the face after insightface but before transplanting on the generated image\n" +
            $"see <a href=\"https://github.com/Gourieff/comfyui-reactor-node/pull/321\">https://github.com/Gourieff/comfyui-reactor-node/pull/321</a>.",
            "false",
            Group: reactorGroup,
            FeatureFlag: FeatureId,
            IsAdvanced: true,
            ChangeWeight: 1,
            OrderPriority: orderCounter++
        ));
        FaceBoostRestoreAfterMain = T2IParamTypes.Register<bool>(new($"{Prefix}Face Boost Restore After Main",
            $"Restores the face again after transplanting on the generated image",
            "false",
            Group: reactorGroup,
            FeatureFlag: FeatureId,
            IsAdvanced: true,
            OrderPriority: orderCounter++
        ));
        InputFacesOrder = T2IParamTypes.Register<string>(new($"{Prefix}Input Faces Order",
            $"Sorting order for faces in the generated image.",
            "large-small",
            GetValues: _ => FacesOrderOptions,
            Group: reactorGroup,
            FeatureFlag: FeatureId,
            IsAdvanced: true,
            OrderPriority: orderCounter++
        ));
        InputFacesIndex = T2IParamTypes.Register<string>(new($"{Prefix}Input Faces Index",
            $"Changes the indexes of faces on the generated image for swapping.",
            "0",
            Examples: ["0,1,2", "1,0,2"],
            Group: reactorGroup,
            FeatureFlag: FeatureId,
            IsAdvanced: true,
            OrderPriority: orderCounter++
        ));
        InputGenderDetect = T2IParamTypes.Register<string>(new($"{Prefix}Input Gender Detect",
            $"Only swap faces in the generated image that match this gender.",
            "no",
            GetValues: _ => GenderDetectOptions,
            Group: reactorGroup,
            FeatureFlag: FeatureId,
            IsAdvanced: true,
            OrderPriority: orderCounter++
        ));
        SourceFacesOrder = T2IParamTypes.Register<string>(new($"{Prefix}Source Faces Order",
            $"Sorting order for faces in the source face image.",
            "large-small",
            GetValues: _ => FacesOrderOptions,
            Group: reactorGroup,
            FeatureFlag: FeatureId,
            IsAdvanced: true,
            OrderPriority: orderCounter++
        ));
        SourceFacesIndex = T2IParamTypes.Register<string>(new($"{Prefix}Source Faces Index",
            $"Changes the indexes of faces on the source face image for swapping.",
            "0",
            Examples: ["0,1,2", "1,0,2"],
            Group: reactorGroup,
            FeatureFlag: FeatureId,
            IsAdvanced: true,
            OrderPriority: orderCounter++
        ));
        SourceGenderDetect = T2IParamTypes.Register<string>(new($"{Prefix}Source Gender Detect",
            $"Only swap faces in the source face image that match this gender.",
            "no",
            GetValues: _ => GenderDetectOptions,
            Group: reactorGroup,
            FeatureFlag: FeatureId,
            IsAdvanced: true,
            OrderPriority: orderCounter++
        ));
        FaceDetectionModel = T2IParamTypes.Register<string>(new($"{Prefix}Face Detection Model",
            $"Model to use for face detection.",
            "retinaface_resnet50",
            GetValues: _ => FaceDetectionModels,
            Group: reactorGroup,
            FeatureFlag: FeatureId,
            IsAdvanced: true,
            OrderPriority: orderCounter++
        ));
        FaceSwapModel = T2IParamTypes.Register<string>(new($"{Prefix}Face Swap Model",
            $"Model to use for face swap.\n" +
            $"To add new models put them in <i><b>'ComfyUI/models/insightface'</b></i>.",
            "inswapper_128.onnx",
            GetValues: _ => FaceSwapModels,
            Group: reactorGroup,
            FeatureFlag: FeatureId,
            IsAdvanced: true,
            OrderPriority: orderCounter++
        ));

        // Add into workflow
        WorkflowGenerator.AddStep(g =>
        {
            // Get these parameters first to determine if we should run
            bool hasRestoreModel = g.UserInput.TryGet(FaceRestoreModel, out string faceRestoreModel);
            bool hasImage = g.UserInput.TryGet(FaceImage, out Image inputImage);
            bool hasModel = g.UserInput.TryGet(FaceModel, out string faceModel);
            
            // Only work if either of these are passed
            if (!hasRestoreModel && !hasImage && !hasModel)
                return;
            
            if (!g.Features.Contains(FeatureId))
                throw new SwarmUserErrorException("ReActor parameters specified, but feature isn't installed");
            
            // Get parameters used in multiple branches below
            double faceRestoreVisibility = g.UserInput.Get(FaceRestoreVisibility);
            double codeFormerWeight = g.UserInput.Get(CodeFormerWeight);
            string faceDetectionModel = g.UserInput.Get(FaceDetectionModel);
            JArray reactorOutput = null;
            if (hasImage || hasModel) // If user passed in an image/model do face swap
            {
                string sourceNode = null;
                if (hasImage)
                {
                    sourceNode = g.CreateLoadImageNode(inputImage, "image", true);
                    // Image has priority over model if both provided
                    hasModel = false;
                }

                if (hasModel)
                {
                    sourceNode = g.CreateNode("ReActorLoadFaceModel", new JObject
                    {
                        ["face_model"] = faceModel
                    });
                }

                string faceBoostNode = null;
                if (g.UserInput.TryGet(FaceBoost, out bool faceBoost) && faceBoost && g.UserInput.TryGet(FaceBoostRestoreAfterMain, out bool faceBoostRestoreAfterMain))
                {
                    faceBoostNode = g.CreateNode("ReActorFaceBoost", new JObject
                    {
                        ["enabled"] = true,
                        ["boost_model"] = faceRestoreModel,
                        ["interpolation"] = "Bicubic",
                        ["visibility"] = faceRestoreVisibility,
                        ["codeformer_weight"] = codeFormerWeight,
                        ["restore_with_main_after"] = faceBoostRestoreAfterMain,
                    });
                }

                string optionsNode = g.CreateNode("ReActorOptions", new JObject
                {
                    ["input_faces_order"] = g.UserInput.Get(InputFacesOrder),
                    ["input_faces_index"] = g.UserInput.Get(InputFacesIndex),
                    ["detect_gender_input"] = g.UserInput.Get(InputGenderDetect),
                    ["source_faces_order"] = g.UserInput.Get(SourceFacesOrder),
                    ["source_faces_index"] = g.UserInput.Get(SourceFacesIndex),
                    ["detect_gender_source"] = g.UserInput.Get(SourceGenderDetect),
                    ["console_log_level"] = 1,
                });

                string reactorNode = g.CreateNode("ReActorFaceSwapOpt", new JObject
                {
                    ["input_image"] = g.FinalImageOut,
                    ["options"] = new JArray { optionsNode, 0 },
                    ["source_image"] = hasImage ? new JArray { sourceNode, 0 } : null,
                    ["face_model"] = hasModel ? new JArray { sourceNode, 0 } : null,
                    ["face_boost"] = string.IsNullOrEmpty(faceBoostNode) ? null : new JArray { faceBoostNode, 0 },
                    ["enabled"] = true,
                    ["swap_model"] = g.UserInput.Get(FaceSwapModel),
                    ["facedetection"] = faceDetectionModel,
                    ["face_restore_model"] = faceRestoreModel,
                    ["face_restore_visibility"] = faceRestoreVisibility,
                    ["codeformer_weight"] = codeFormerWeight,
                });
                reactorOutput = [reactorNode, 0];
            }
            else if (hasRestoreModel && faceRestoreModel != "none") // If a face restore model was provided just do that
            {
                string restoreFace = g.CreateNode("ReActorRestoreFace", new JObject
                {
                    ["image"] = g.FinalImageOut,
                    ["facedetection"] = faceDetectionModel,
                    ["model"] = faceRestoreModel,
                    ["visibility"] = faceRestoreVisibility,
                    ["codeformer_weight"] = codeFormerWeight
                });
                reactorOutput = [restoreFace, 0];
            }
            else // Nothing valid was provided so stop
            {
                return;
            }

            // Inserts a second face restore model into the chain
            if (g.UserInput.TryGet(SecondFaceRestoreModel, out string faceRestoreModelExtra))
            {
                string restoreFace = g.CreateNode("ReActorRestoreFace", new JObject
                {
                    ["image"] = reactorOutput,
                    ["facedetection"] = faceDetectionModel,
                    ["model"] = faceRestoreModelExtra,
                    ["visibility"] = faceRestoreVisibility,
                    ["codeformer_weight"] = codeFormerWeight
                });
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
                });
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
                });
                reactorOutput = [maskNode, 0];
            }

            g.FinalImageOut = reactorOutput;
        }, StepInjectPriority);
    }
}