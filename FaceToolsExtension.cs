using SwarmUI.Core;

namespace Quaggles.Extensions.FaceTools;

public class FaceToolsExtension : Extension
{
    public override void OnInit()
    {
        base.OnInit();
        FaceRestoreCFParams.Initialise();
        ReactorParams.Initialise();
    }
}