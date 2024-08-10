using System.IO;
using Newtonsoft.Json.Linq;
using SwarmUI.Builtin_ComfyUIBackend;
using SwarmUI.Core;
using SwarmUI.Text2Image;
using SwarmUI.Utils;

namespace Quaggles.Extensions.FaceTools;

public class ReactorExtension : Extension
{
    public static float StepInjectPriority = 9.1f;
    private const string Prefix = "[ReActor] ";
    private const string FeatureReactor = "ReActor";
    private const string NodeNameReactor = "ReActorFaceSwapOpt";

    public static readonly List<string> GenderDetect = ["no", "female", "male"];
    public static readonly List<string> FacesOrder = ["left-right", "right-left", "top-bottom", "bottom-top", "small-large", "large-small"];
    public static readonly List<string> FaceDetectionModels = ["retinaface_resnet50", "retinaface_mobile0.25", "YOLOv5l", "YOLOv5n"];

    public static T2IRegisteredParam<Image> FaceImage;
    public static T2IRegisteredParam<double> FaceRestoreVisibility, CodeFormerWeight;

    public static T2IRegisteredParam<string> FaceRestoreModel,
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

    private static ModelHelper faceRestoreModelHelper = new("facerestore_models")
    {
        // These get automatically downloaded by the ReActor installer so assume they exist in the ComfyUI model folder without requiring them in ModelRoot
        AlwaysInclude = { "codeformer-v0.1.0.pth", "GFPGANv1.3.pth", "GFPGANv1.4.pth", "GPEN-BFR-512.onnx", "GPEN-BFR-1024.onnx", "GPEN-BFR-2048.onnx" },
        Default = "codeformer-v0.1.0.pth",
        SearchOption = SearchOption.TopDirectoryOnly, // Node doesn't accept models of this type in subdirectories
        NullValue = "none",
    };

    private static ModelHelper faceSwapModelHelper = new("insightface")
    {
        // This gets automatically downloaded by the ReActor installer so assume it exists in the ComfyUI model folder without requiring them in ModelRoot
        AlwaysInclude = { "inswapper_128.onnx" },
        Default = "inswapper_128.onnx",
        SearchOption = SearchOption.TopDirectoryOnly, // Node doesn't accept models of this type in subdirectories
    };

    private static ModelHelper faceMaskModelHelper = new("yolov8")
    {
        Default = "face_yolov8m-seg_60.pt", // Pick this if it exists
    };

    private static ModelHelper faceModelHelper = new("reactor/faces")
    {
        SearchOption = SearchOption.TopDirectoryOnly, // Node doesn't accept models of this type in subdirectories
    };

    public override void OnInit()
    {
        // Define required nodes
        ComfyUIBackendExtension.NodeToFeatureMap[NodeNameReactor] = FeatureReactor;

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
            FeatureFlag: FeatureReactor,
            OrderPriority: orderCounter++
        ));
        FaceModel = T2IParamTypes.Register<string>(new($"{Prefix}Face Model",
            $"The model containing a face you want to swap, this is skipped if a 'Face Image' is provided and enabled.\n" +
            $"To add new models put them in both <i><b>'{modelRoot}/{faceModelHelper.Subfolder}'</b></i> AND <i><b>'ComfyUI/models/{faceModelHelper.Subfolder}'</b></i>",
            faceModelHelper.GetDefault(),
            GetValues: _ => faceModelHelper.GetValues(),
            Group: reactorGroup,
            IgnoreIf: faceModelHelper.NullValue,
            FeatureFlag: FeatureReactor,
            ChangeWeight: 2,
            OrderPriority: orderCounter++
        ));
        FaceRestoreVisibility = T2IParamTypes.Register<double>(new($"{Prefix}Face Restore Visibility",
            $"How visible the face restore is against the original image (Higher is stronger).",
            "1",
            Min: 0.1, Max: 1, Step: 0.01,
            ViewType: ParamViewType.SLIDER,
            Group: reactorGroup,
            FeatureFlag: FeatureReactor,
            OrderPriority: orderCounter++
        ));
        FaceRestoreModel = T2IParamTypes.Register<string>(new($"{Prefix}Face Restore Model",
            $"Model to use for face restoration.\n" +
            $"To add new models put them in both <i><b>'{modelRoot}/{faceRestoreModelHelper.Subfolder}'</b></i> AND <i><b>'ComfyUI/models/{faceRestoreModelHelper.Subfolder}'</b></i>.\n" +
            "Download from <a href=\"https://huggingface.co/datasets/Gourieff/ReActor/tree/main/models/facerestore_models\">https://huggingface.co/datasets/Gourieff/ReActor/tree/main/models/facerestore_models</a>",
            faceRestoreModelHelper.GetDefault(),
            GetValues: _ => faceRestoreModelHelper.GetValues(),
            Group: reactorGroup,
            FeatureFlag: FeatureReactor,
            OrderPriority: orderCounter++
        ));
        CodeFormerWeight = T2IParamTypes.Register<double>(new($"{Prefix}CodeFormer Weight",
            $"Face restoration weight with CodeFormer model (Lower is stronger).",
            "0.5",
            Min: 0, Max: 1, Step: 0.01,
            ViewType: ParamViewType.SLIDER,
            Group: reactorGroup,
            FeatureFlag: FeatureReactor,
            OrderPriority: orderCounter++
        ));
        SecondFaceRestoreModel = T2IParamTypes.Register<string>(new($"{Prefix}Second Face Restore Model",
            $"Runs a second face restoration model after the main swap/restoration.",
            faceRestoreModelHelper.GetDefault("GPEN-BFR-1024.onnx"),
            IgnoreIf: faceRestoreModelHelper.NullValue,
            GetValues: _ => faceRestoreModelHelper.GetValues(),
            Group: reactorGroup,
            FeatureFlag: FeatureReactor,
            IsAdvanced: true,
            Toggleable: true,
            OrderPriority: orderCounter++
        ));
        FaceMaskModel = T2IParamTypes.Register<string>(new($"{Prefix}Face Mask Model", 
            "Masks out the area around faces to avoid the swap/restoration affecting other nearby elements like hair.\n" +
            $"To add new models put them in <i><b>'{modelRoot}/yolov8'</b></i>.\n" +
            "Download from <a href=\"https://github.com/hben35096/assets/releases/\">https://github.com/hben35096/assets/releases/</a>",
            faceMaskModelHelper.GetDefault(),
            IgnoreIf: faceMaskModelHelper.NullValue, 
            FeatureFlag: "yolov8", 
            Group: reactorGroup, 
            GetValues: _ => faceMaskModelHelper.GetValues(), 
            OrderPriority: orderCounter++
        ));
        FaceBoost = T2IParamTypes.Register<bool>(new($"{Prefix}Face Boost",
            $"Restores the face after insightface but before transplanting on the generated image\n" +
            $"see <a href=\"https://github.com/Gourieff/comfyui-reactor-node/pull/321\">https://github.com/Gourieff/comfyui-reactor-node/pull/321</a>.",
            "false",
            Group: reactorGroup,
            FeatureFlag: FeatureReactor,
            IsAdvanced: true,
            ChangeWeight: 1,
            OrderPriority: orderCounter++
        ));
        FaceBoostRestoreAfterMain = T2IParamTypes.Register<bool>(new($"{Prefix}Face Boost Restore After Main",
            $"Restores the face again after transplanting on the generated image",
            "false",
            Group: reactorGroup,
            FeatureFlag: FeatureReactor,
            IsAdvanced: true,
            OrderPriority: orderCounter++
        ));
        InputFacesOrder = T2IParamTypes.Register<string>(new($"{Prefix}Input Faces Order",
            $"Sorting order for faces in the generated image.",
            FacesOrder.Last(),
            GetValues: _ => FacesOrder,
            Group: reactorGroup,
            FeatureFlag: FeatureReactor,
            IsAdvanced: true,
            OrderPriority: orderCounter++
        ));
        InputFacesIndex = T2IParamTypes.Register<string>(new($"{Prefix}Input Faces Index",
            $"Changes the indexes of faces on the generated image for swapping.",
            "0",
            Examples: ["0,1,2", "1,0,2"],
            Group: reactorGroup,
            FeatureFlag: FeatureReactor,
            IsAdvanced: true,
            OrderPriority: orderCounter++
        ));
        InputGenderDetect = T2IParamTypes.Register<string>(new($"{Prefix}Input Gender Detect",
            $"Only swap faces in the generated image that match this gender.",
            GenderDetect.FirstOrDefault(),
            GetValues: _ => GenderDetect,
            Group: reactorGroup,
            FeatureFlag: FeatureReactor,
            IsAdvanced: true,
            OrderPriority: orderCounter++
        ));
        SourceFacesOrder = T2IParamTypes.Register<string>(new($"{Prefix}Source Faces Order",
            $"Sorting order for faces in the source face image.",
            FacesOrder.Last(),
            GetValues: _ => FacesOrder,
            Group: reactorGroup,
            FeatureFlag: FeatureReactor,
            IsAdvanced: true,
            OrderPriority: orderCounter++
        ));
        SourceFacesIndex = T2IParamTypes.Register<string>(new($"{Prefix}Source Faces Index",
            $"Changes the indexes of faces on the source face image for swapping.",
            "0",
            Examples: ["0,1,2", "1,0,2"],
            Group: reactorGroup,
            FeatureFlag: FeatureReactor,
            IsAdvanced: true,
            OrderPriority: orderCounter++
        ));
        SourceGenderDetect = T2IParamTypes.Register<string>(new($"{Prefix}Source Gender Detect",
            $"Only swap faces in the source face image that match this gender.",
            GenderDetect.FirstOrDefault(),
            GetValues: _ => GenderDetect,
            Group: reactorGroup,
            FeatureFlag: FeatureReactor,
            IsAdvanced: true,
            OrderPriority: orderCounter++
        ));
        FaceDetectionModel = T2IParamTypes.Register<string>(new($"{Prefix}Face Detection Model",
            $"Model to use for face detection.",
            FaceDetectionModels.FirstOrDefault(),
            GetValues: _ => FaceDetectionModels,
            Group: reactorGroup,
            FeatureFlag: FeatureReactor,
            IsAdvanced: true,
            OrderPriority: orderCounter++
        ));
        FaceSwapModel = T2IParamTypes.Register<string>(new($"{Prefix}Face Swap Model",
            $"Model to use for face swap.\n" +
            $"To add new models put them in both <i><b>'{modelRoot}/{faceSwapModelHelper.Subfolder}'</b></i> AND <i><b>'ComfyUI/models/{faceSwapModelHelper.Subfolder}'</b></i>.",
            faceSwapModelHelper.GetDefault(),
            IgnoreIf: faceSwapModelHelper.NullValue,
            GetValues: _ => faceSwapModelHelper.GetValues(),
            Group: reactorGroup,
            FeatureFlag: FeatureReactor,
            IsAdvanced: true,
            OrderPriority: orderCounter++
        ));

        // Add into workflow
        WorkflowGenerator.AddStep(g =>
        {
            if (ComfyUIBackendExtension.FeaturesSupported.Contains(FeatureReactor) &&
                g.UserInput.TryGet(FaceSwapModel, out string faceSwapModel) &&
                g.UserInput.TryGet(FaceRestoreVisibility, out double faceRestoreVisibility) &&
                g.UserInput.TryGet(CodeFormerWeight, out double codeFormerWeight) &&
                g.UserInput.TryGet(FaceDetectionModel, out string faceDetectionModel) &&
                g.UserInput.TryGet(InputFacesOrder, out string inputFacesOrder) &&
                g.UserInput.TryGet(InputFacesIndex, out string inputFacesIndex) &&
                g.UserInput.TryGet(InputGenderDetect, out string inputGenderDetect) &&
                g.UserInput.TryGet(SourceFacesOrder, out string sourceFacesOrder) &&
                g.UserInput.TryGet(SourceFacesIndex, out string sourceFacesIndex) &&
                g.UserInput.TryGet(SourceGenderDetect, out string sourceGenderDetect))
            {
                JArray reactorOutput = null;
                // Get name of restore model early as we use it in multiple places
                bool hasRestoreModel = g.UserInput.TryGet(FaceRestoreModel, out string faceRestoreModel);
                bool hasImage = g.UserInput.TryGet(FaceImage, out Image inputImage);
                bool hasModel = g.UserInput.TryGet(FaceModel, out string faceModel);
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
                        ["input_faces_order"] = inputFacesOrder,
                        ["input_faces_index"] = inputFacesIndex,
                        ["detect_gender_input"] = inputGenderDetect,
                        ["source_faces_order"] = sourceFacesOrder,
                        ["source_faces_index"] = sourceFacesIndex,
                        ["detect_gender_source"] = sourceGenderDetect,
                        ["console_log_level"] = 1,
                    });

                    string reactorNode = g.CreateNode(NodeNameReactor, new JObject
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
                    });
                    reactorOutput = [reactorNode, 0];
                }
                else if (hasRestoreModel && faceRestoreModel != faceRestoreModelHelper.NullValue) // If a face restore model was provided just do that
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
                if (g.UserInput.TryGet(FaceMaskModel, out string faceMaskmodel))
                {
                    string swarmYoloMask = g.CreateNode("SwarmYoloDetection", new JObject
                    {
                        ["image"] = g.FinalImageOut,
                        ["model_name"] = faceMaskmodel,
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
            }
        }, StepInjectPriority);
    }
}