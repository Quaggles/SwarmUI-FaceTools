# SwarmUI-FaceTools

A [SwarmUI](https://github.com/mcmonkeyprojects/SwarmUI/) extension that adds parameters for [ReActor](https://github.com/Gourieff/comfyui-reactor-node) and [FaceRestoreCF](https://github.com/mav-rik/facerestore_cf) nodes to the the generate tab.

![image](https://github.com/user-attachments/assets/61be3d04-88f2-4f21-b84a-f47435dfefd1)

## Changelog
<details>
  <summary>4 November 2024</summary>

* ReActor install button will now not show up if you load the page before the backend has loaded
* Dropdown parameters are not prepopulated so they now show up if you load the page before the backend has loaded
* Expanded the readme to give some possible solutions to common issues people have been running into and flesh out some things
* Parameters that are not relevant to the current ReActor workflow are now automatically removed from the image parameter list to keep the UI cleaner, for example you won't see parameters like 'Input Faces Order' filling up the list unless you changed it from the default, see the comparison below, to disable this behaviour you can disable the 'Remove Params If Default' param

![Untitled](https://github.com/user-attachments/assets/412b96f6-85d1-43e4-88ec-65b156b1c772)

</details>
<details>
  <summary>13 October 2024</summary>

* Much better install process for dependencies with no need to use ComfyUI Manager, if dependencies aren't installed a button to install them will appear in the parameter group, see the new [Installation steps](https://github.com/Quaggles/SwarmUI-FaceTools/?tab=readme-ov-file#installation-simple) for details
* Previously ReActor and FaceRestoreCF were 2 extension classes, they've been merged so it's simpler to manage in the extension tab
* Model dropdowns now read from ComfyUI model folder, no need to install models into both the SwarmUI model folder and the ComfyUI model folder anymore. ***Warning:*** Deleted models do not get removed from the list when refreshing, you'll need to restart SwarmUI for them to disappear
</details>

## Installation (Simple)

1. In SwarmUI go to the Server/Extensions tab
2. Find FaceTools in the list and click the 'Install' button
3. Refresh the page and go back to the generate tab, if you see the parameters then the required ComfyUI dependencies are installed and you can start using the extension, otherwise continue below.
4. If dependencies are not installed buttons will be shown in the parameter group to install them. The 'Install IP Adapter' button will be shown first if it's not installed, install it and once the backend restarts refresh the page.

![image](https://github.com/user-attachments/assets/fe396a47-6f62-453c-976e-fe99e2d3e15d)

5. If ReActor is not installed a button will be shown, click install and monitor the progress by going to the `Server/Logs` tab and setting the view to `Debug`, it will automatically start downloading models so this might take a while, if this takes too long SwarmUI might say `Some backends have errored on the server. Check the server logs for details` just leave it until the downloads finish and then restart SwarmUI.

![image](https://github.com/user-attachments/assets/048df53e-57bf-4758-8f09-ec22b53e1263)

6. Check that `SwarmUI/dlbackend/comfy/ComfyUI/models/insightface/inswapper_128.onnx` exists and if it doesn't download it from https://huggingface.co/datasets/Gourieff/ReActor/tree/main/models and put it there

7. If you run into issues check the [Troubleshooting section](#troubleshooting)

## Installation (Manual)

1. Shutdown SwarmUI
2. Open a cmd/terminal window in `SwarmUI\src\Extensions`
3. Run `git clone https://github.com/Quaggles/SwarmUI-FaceTools.git`
4. Run `SwarmUI\update-windows.bat` to recompile SwarmUI
5. Launch SwarmUI and follow on from [Step 4 of Installation (Simple)](#installation-simple)

## Updating
1. Update SwarmUI first, updates to this extension might require the latest version of SwarmUI to function
2. In SwarmUI go to the Server/Extensions tab
3. Click the update button for 'FaceToolsExtension'

## Usage

### ReActor

These parameters inject different nodes to the workflow based on the options selected.

If `Face Image` is provided then face swap will be performed after image generation using the face/s in this image.

If `Face Model` is set a saved Face Model is used to perform the face swap. Face Models can be created in the Comfy Workflow tab using the 'Save Face Model' node. `Face Image` takes priority over `Face Model` if both are set.

If `Face Restore Model` is set then face restoration will be run after the face swap, if `Face Image` or `Face Model` were not provided it runs directly on the generated image similar to FaceRestoreCF. If this option is 'None' then face restoration is skipped.

If `Second Face Restore Model` (Advanced option) is set then a second face restore model will be run after the first. I found good results with codeformer-v0.1.0 first to fix distortions then GPEN-BFR-1024 as the second model to sharpen the result.

If `Face Mask Model` is set the face swap and face restore will be masked so it doesn't effect overlapping features like hair. If the dropdown is empty download models from here: [YOLOv8 Segmentation models](https://github.com/hben35096/assets/releases/) and put them in `{SwarmUIModelRoot}/yolov8`

`Face Boost` is described [here](https://github.com/Gourieff/comfyui-reactor-node?tab=readme-ov-file#051-alpha1)

Many more parameters are available if you enable 'Advanced Options' at the bottom of the panel. All parameters have tooltips or read through the [ReActor readme](https://github.com/Gourieff/comfyui-reactor-node) for more info, you can also use 'Comfy Workflow/Import From Generate Tab' feature to see what the parameters are doing in the ComfyUI workflow.

### FaceRestoreCF
Unfortunately FaceRestoreCF cannot be installed with the 1 click button yet as it is awaiting a pull request to fix it. You can do basic face restoration with ReActor in the meantime, check the usage section above. If you really want FaceRestoreCF you can install it with ComfyUI manager manually.

FaceRestoreCF does not automatically download the model, if the dropdown is empty you will need to download it manually [from here](https://github.com/sczhou/CodeFormer/releases/download/v0.1.0/codeformer.pth) rename it to `codeformer-v0.1.0.pth` and place it in `"SwarmUI/dlbackend/comfy/ComfyUI/models/facerestore_models"`

Just make sure the parameter group is enabled and that `Face Restore Model` is set and it should work, you can also use 'Comfy Workflow/Import From Generate Tab' feature to see what the parameters are doing in the workflow.

ReActor can do all the face restoration actions that FaceRestoreCF can but FaceRestoreCF caches the face restore model and runs much faster (And doesn't automatically download 2gb of models) so I've left support in for those who prefer it.

## Model Paths for Face Restore and Fask Models

The ReActor and FaceRestoreCF custom nodes do not follow the 'ComfyUI extra paths' config to allow them to exist in the SwarmUI ModelRoot folder.

* `Face Swap` models must be installed in:

`"SwarmUI/dlbackend/comfy/ComfyUI/models/insightface"`

* `Face Restore Model` models must be installed in:

`"SwarmUI/dlbackend/comfy/ComfyUI/models/facerestore_models"`

* Saved `Face Model` models must be installed in:

`"SwarmUI/dlbackend/comfy/ComfyUI/models/reactor/faces"`

After installing a model make sure to click the model refresh button in SwarmUI so the extension can scan again.

## Troubleshooting

### ModuleNotFoundError: No module named 'insightface'

If you installed SwarmUI fresh after October 22nd it's likely you have ComfyUI v0.2.4 or greater which might require some extra steps to get working

1. Ensure SwarmUI is shutdown
2. Start a terminal/cmd in `SwarmUI/dlbackend/comfy/python_embeded`
3. Run the command `python.exe -m pip install numpy==1.26.4`, if using Powershell you might need to prepend commands with `./` to ensure it doesn't find your global python install on PATH
4. Then run the command `python.exe -m pip install https://github.com/Gourieff/Assets/raw/main/Insightface/insightface-0.7.3-cp312-cp312-win_amd64.whl`
5. Launch SwarmUI and hopefully the error is gone, for more information about this issue [read here](https://github.com/Gourieff/comfyui-reactor-node/issues/471)

If that still doesn't solve the issue you can try:

1. Install Visual Studio 2022 Community version OR only VS C++ Build Tools and select "Desktop Development with C++" under "Workloads -> Desktop & Mobile"
2. Find the python version of your ComfyUI backend by looking at the file properties of `SwarmUI\dlbackend\comfy\python_embeded\python.exe`, for this example I have Python 3.12.7
3. Install the exact same python version from https://www.python.org/downloads/ and manually copy over the `include` and `libs` folders from `C:\Users\<User>\AppData\Local\Programs\Python\Python312` to  `SwarmUI\dlbackend\comfy\python_embeded`
4. Start a terminal/cmd in `SwarmUI/dlbackend/comfy/python_embeded`
5. Run the command `python.exe -m pip install insightface`, if using Powershell you might need to prepend commands with `./` to ensure it doesn't find your global python install on PATH
6. If all goes well this should compile insightface successfully and you can continue

### ComfyUI execution error: `[ONNXRuntimeError] : 7 : INVALID_PROTOBUF : Load model from .../inswapper_128.onnx failed:Protobuf parsing failed`

Try going to the folder in the error message and deleting the `inswapper_128.onnx` model and replacing it with a version downloaded directly from [here](https://huggingface.co/datasets/Gourieff/ReActor/tree/main/models)

### An error occurs when clicking 'Generate' when using ReActor

Read through the [ReActor Troubleshooting Guide](https://github.com/Gourieff/comfyui-reactor-node#troubleshooting) and see if any match your error.

### I get 'ComfyUI execution error CUDA_PATH is set but CUDA wasn't able to be loaded'

I had this happen even when CUDA_PATH pointed to a valid CUDA toolkit installation, try going to 'System Properties/Environment Variables' and removing `CUDA_PATH` to see if it fixes your problem, this may effect other software finding CUDA.

### I can't see the ReActor or CodeFormerCF parameter groups

Open the 'Comfy Workflow' tab and check that the relevant nodes can be added there, if you cannot add the nodes in the ComfyUI workflow then they are not installed correctly and you might need to ask for help installing them on their respective pages [ReActor](https://github.com/Gourieff/comfyui-reactor-node), [FaceRestoreCF](https://github.com/mav-rik/facerestore_cf) or the [SwarmUI discord](https://discord.gg/swarmui-1243166023859961988).

### If all else fails
Ask for help on the [SwarmUI discord](https://discord.gg/swarmui-1243166023859961988)
