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
    private const string FeatureId = "face_restoration";
    private const string NodeIdFaceRestore = "FaceRestoreCFWithModel";
    public static readonly List<string> FaceDetectionModels = ["retinaface_resnet50", "retinaface_mobile0.25", "YOLOv5l", "YOLOv5n"];

    public static T2IRegisteredParam<double> Fidelity;
    public static T2IRegisteredParam<string> FaceRestoreModel, FaceDetectionModel;

    private static ModelHelper faceRestoreModelHelper = new("facerestore_models")
    {
        Default = "codeformer-v0.1.0.pth",
        Filter = model => string.Equals(Path.GetExtension(model), ".pth", StringComparison.OrdinalIgnoreCase)
    };

    public static void Initialise()
    {
        // Define required nodes
        ComfyUIBackendExtension.NodeToFeatureMap[NodeIdFaceRestore] = FeatureId;

        // Setup parameters
        T2IParamGroup faceRestorationGroup = new("FaceRestoreCF", Toggles: true, Open: false, IsAdvanced: false, OrderPriority: 9);
        int orderCounter = 0;
        var modelRoot = Utilities.CombinePathWithAbsolute(Environment.CurrentDirectory, Program.ServerSettings.Paths.ModelRoot);
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
            $"To add new models put them in both <i><b>'{modelRoot}/{faceRestoreModelHelper.Subfolder}'</b></i> AND <i><b>'ComfyUI/models/{faceRestoreModelHelper.Subfolder}'</b></i>",
            faceRestoreModelHelper.GetDefault(),
            IgnoreIf: "None",
            GetValues: _ => faceRestoreModelHelper.GetValues(),
            Group: faceRestorationGroup,
            FeatureFlag: FeatureId,
            ChangeWeight: 2,
            OrderPriority: orderCounter++
        ));
        FaceDetectionModel = T2IParamTypes.Register<string>(new($"{Prefix}Face Detection Model",
            $"Model to use for face detection.",
            FaceDetectionModels.FirstOrDefault(),
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
            if (!ComfyUIBackendExtension.FeaturesSupported.Contains(FeatureId))
                throw new SwarmUserErrorException("FaceRestoreCF parameters specified, but feature isn't installed");
            string loaderNode = g.CreateNode("FaceRestoreModelLoader", new JObject
            {
                ["model_name"] = faceRestoreModel,
            });
            string restoreNode = g.CreateNode(NodeIdFaceRestore, new JObject
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