using SwarmUI.Core;

namespace Quaggles.Extensions.FaceTools;

public class FaceToolsExtension : Extension
{
    public override void OnInit()
    {
        base.OnInit();
        
        // Add the JS file, which manages the install buttons for the comfy nodes
        ScriptFiles.Add("assets/facetools.js");
        
        // These are split into different classes to organise things a bit better
        // and so I don't need to worry about variables conflicting
        FaceRestoreCFParams.Initialise();
        ReactorParams.Initialise();
    }
}