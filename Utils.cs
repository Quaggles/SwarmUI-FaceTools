using SwarmUI.Text2Image;
using SwarmUI.Utils;

namespace Quaggles.Extensions.FaceTools;

public static class Utils
{
    // Cleans up parameters that are left as default
    public static string GetAndRemoveIfDefault(this T2IParamInput input, T2IRegisteredParam<string> param)
    {
        if (input.TryGet(FaceToolsExtension.RemoveParamsIfDefault, out var removeParams) && !removeParams) return input.Get(param);
        if (!input.TryGet(param, out var value)) return param.Type.Default;
        if (value == param.Type.Default)
            if (input.ValuesInput.Remove(param.Type.ID))
                Logs.Verbose($"{FaceToolsExtension.ExtensionPrefix}Removed redundant param '{param.Type.ID}' as it was set to the default");
        return value;
    }
    
    // Cleans up parameters that are left as default
    public static double GetAndRemoveIfDefault(this T2IParamInput input, T2IRegisteredParam<double> param)
    {
        if (input.TryGet(FaceToolsExtension.RemoveParamsIfDefault, out var removeParams) && !removeParams) return input.Get(param);
        var defaultVal = double.Parse(param.Type.Default);
        if (!input.TryGet(param, out var value)) return defaultVal;
        if (Math.Abs(value - defaultVal) < param.Type.Step)
            if (input.ValuesInput.Remove(param.Type.ID))
                Logs.Verbose($"{FaceToolsExtension.ExtensionPrefix}Removed redundant param '{param.Type.ID}' as it was set to the default");
        return value;
    }
}