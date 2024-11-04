using System.IO;
using Newtonsoft.Json.Linq;
using SwarmUI.Builtin_ComfyUIBackend;
using SwarmUI.Core;
using SwarmUI.Text2Image;
using SwarmUI.Utils;

namespace Quaggles.Extensions.FaceTools;

public static class FaceRestoreCFParams
{
    public static float StepInjectPriority = 9;
    private const string Prefix = "[FR] ";
    private const string FeatureId = "facerestorecf";

    public static T2IRegisteredParam<double> Fidelity;
    public static T2IRegisteredParam<string> FaceRestoreModel, FaceDetectionModel;
    
    public static List<string> FaceRestoreModels = [];
    public static List<string> FaceDetectionModels = [];

    public static void Initialise()
    {
        // Define required nodes
        ComfyUIBackendExtension.NodeToFeatureMap["FaceRestoreCFWithModel"] = FeatureId;
        
        // Add required custom node as installable feature
        InstallableFeatures.RegisterInstallableFeature(new("FaceRestoreCF", FeatureId, "https://github.com/mav-rik/facerestore_cf", "mav-rik", "This will install the FaceRestoreCF ComfyUI node developed by mav-rik.\nDo you wish to install?"));
        
        // Prevents install button from being shown during backend load if it looks like it was installed
        // it will appear if the backend loads and the backend reports it's not installed
        if (Directory.Exists(Utilities.CombinePathWithAbsolute(Environment.CurrentDirectory, $"{ComfyUIBackendExtension.Folder}/DLNodes/facerestore_cf")))
        {
            ComfyUIBackendExtension.FeaturesSupported.UnionWith([FeatureId]);
            ComfyUIBackendExtension.FeaturesDiscardIfNotFound.UnionWith([FeatureId]);
        }
        
        ComfyUIBackendExtension.RawObjectInfoParsers.Add(rawObjectInfo =>
        {
            if (rawObjectInfo.TryGetValue("FaceRestoreModelLoader", out JToken nodeLoader))
            {
                T2IParamTypes.ConcatDropdownValsClean(ref FaceRestoreModels, nodeLoader["input"]?["required"]?["model_name"]?.FirstOrDefault()?.Select(m => $"{m}") ?? []);
            }
            if (rawObjectInfo.TryGetValue("FaceRestoreCFWithModel", out JToken nodeRestore))
            {
                T2IParamTypes.ConcatDropdownValsClean(ref FaceDetectionModels, nodeRestore["input"]?["required"]?["facedetection"]?.FirstOrDefault()?.Select(m => $"{m}") ?? []);
            }
        });

        // Setup parameters
        T2IParamGroup faceRestorationGroup = new("FaceRestoreCF", Toggles: true, Open: false, IsAdvanced: false, OrderPriority: 9);
        int orderCounter = 0;
        Fidelity = T2IParamTypes.Register<double>(new($"{Prefix}Fidelity",
            $"Face restoration strength (Lower is stronger).",
            "0.5",
            Min: 0, Max: 1, Step: 0.01,
            ViewType: ParamViewType.SLIDER,
            Group: faceRestorationGroup,
            FeatureFlag: FeatureId,
            OrderPriority: orderCounter++
        ));
        FaceRestoreModel = T2IParamTypes.Register<string>(new($"{Prefix}Face Restore Model",
            $"Model to use for face restoration.\n" +
            $"To add new models put them in <i><b>'ComfyUI/models/facerestore_models'</b></i>",
            "codeformer-v0.1.0.pth",
            GetValues: _ => FaceRestoreModels,
            Group: faceRestorationGroup,
            FeatureFlag: FeatureId,
            ChangeWeight: 2,
            OrderPriority: orderCounter++
        ));
        FaceDetectionModel = T2IParamTypes.Register<string>(new($"{Prefix}Face Detection Model",
            $"Model to use for face detection.",
            "retinaface_resnet50",
            GetValues: _ => FaceDetectionModels,
            Group: faceRestorationGroup,
            FeatureFlag: FeatureId,
            IsAdvanced: true,
            ChangeWeight: 1,
            OrderPriority: orderCounter++
        ));

        // Add into workflow
        WorkflowGenerator.AddStep(g =>
        {
            // Require at least FaceRestoreModel param
            if (!g.UserInput.TryGet(FaceRestoreModel, out string faceRestoreModel)) return;
            if (!g.Features.Contains(FeatureId))
                throw new SwarmUserErrorException("FaceRestoreCF parameters specified, but feature isn't installed");
            string loaderNode = g.CreateNode("FaceRestoreModelLoader", new JObject
            {
                ["model_name"] = faceRestoreModel,
            });
            string restoreNode = g.CreateNode("FaceRestoreCFWithModel", new JObject
            {
                ["facerestore_model"] = new JArray { loaderNode, 0 },
                ["image"] = g.FinalImageOut,
                ["facedetection"] = g.UserInput.Get(FaceDetectionModel),
                ["codeformer_fidelity"] = g.UserInput.Get(Fidelity),
            });
            g.FinalImageOut = [restoreNode, 0];
        }, StepInjectPriority);
    }
}