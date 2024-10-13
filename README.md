# SwarmUI-FaceTools

A [SwarmUI](https://github.com/mcmonkeyprojects/SwarmUI/) extension that adds parameters for [ReActor](https://github.com/Gourieff/comfyui-reactor-node) and [FaceRestoreCF](https://github.com/mav-rik/facerestore_cf) nodes to the the generate tab.

![image](https://github.com/user-attachments/assets/61be3d04-88f2-4f21-b84a-f47435dfefd1)

## Changelog
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

5. If ReActor/CodeFormerCF are not installed a button will be shown, install it and after the backend restarts the parameters should appear and you are good to go.

![image](https://github.com/user-attachments/assets/048df53e-57bf-4758-8f09-ec22b53e1263)

Unfortunately FaceRestoreCF cannot be installed this way yet as it is awaiting a pull request to fix it. You can do basic face restoration with ReActor in the meantime, check the usage section below. If you really want FaceRestoreCF you can install it with ComfyUI manager manually.

## Installation (Advanced)

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

FaceRestoreCF does not automatically download the model, if the dropdown is empty you will need to download it manually [from here](https://github.com/sczhou/CodeFormer/releases/download/v0.1.0/codeformer.pth) rename it to `codeformer-v0.1.0.pth` and place it in `"SwarmUI/dlbackend/comfy/ComfyUI/models/facerestore_models"`

Just make sure the parameter group is enabled and that `Face Restore Model` is set and it should work, you can also use 'Comfy Workflow/Import From Generate Tab' feature to see what the parameters are doing in the workflow.

ReActor can do all the face restoration actions that FaceRestoreCF can but FaceRestoreCF caches the face restore model and runs much faster (And doesn't automatically download 2gb of models) so I've left support in for those who prefer it.

## Model Paths for Face Restore and Fask Models

The ReActor and FaceRestoreCF custom nodes do not follow the 'ComfyUI extra paths' config to allow them to exist in the SwarmUI ModelRoot folder.

* `Face Restore Model` models must be installed in:

`"SwarmUI/dlbackend/comfy/ComfyUI/models/facerestore_models"`

* Saved `Face Model` models must be installed in:

`"SwarmUI/dlbackend/comfy/ComfyUI/models/reactor/faces"`

After installing a model make sure to click the model refresh button in SwarmUI so the extension can scan again.

## Troubleshooting

### An error occurs when clicking 'Generate' when using ReActor

Read through the [ReActor Troubleshooting Guide](https://github.com/Gourieff/comfyui-reactor-node#troubleshooting) and see if any match your error.

### I can't see the ReActor or CodeFormerCF parameter groups

Open the 'Comfy Workflow' tab and check that the relevant nodes can be added there, if you cannot add the nodes in the ComfyUI workflow then they are not installed correctly and you'll need to ask for help installing them on their respective pages [ReActor](https://github.com/Gourieff/comfyui-reactor-node), [FaceRestoreCF](https://github.com/mav-rik/facerestore_cf) or try to use [ComfyUI Manager](https://github.com/ltdrdata/ComfyUI-Manager) to install them.

### I get 'ComfyUI execution error CUDA_PATH is set but CUDA wasnt able to be loaded'

I had this happen even when CUDA_PATH pointed to a valid CUDA toolkit installation, try going to 'System Properties/Environment Variables' and removing `CUDA_PATH` to see if it fixes your problem, this may effect other software finding CUDA.
