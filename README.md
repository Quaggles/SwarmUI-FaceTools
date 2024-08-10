# SwarmUI-FaceTools

A [SwarmUI](https://github.com/mcmonkeyprojects/SwarmUI/) extension that adds parameters for [ReActor](https://github.com/Gourieff/comfyui-reactor-node) and [FaceRestoreCF](https://github.com/mav-rik/facerestore_cf) nodes to the the generate tab.

***If the nodes are not installed in the ComfyUI backend the parameters for that node won't show up in the generate tab.***

![image](https://github.com/user-attachments/assets/61be3d04-88f2-4f21-b84a-f47435dfefd1)

## Installation

1. Shutdown SwarmUI
2. Open a cmd/terminal window in `SwarmUI\src\Extensions`
3. Run `git clone https://github.com/Quaggles/SwarmUI-FaceTools.git`
4. Run `SwarmUI\update-windows.bat` to recompile SwarmUI
5. Launch SwarmUI as usual, if ReActor or CodeFormerCF nodes are installed you should see parameter groups for them in the generate tab, if not follow the steps below.

## Installing ReActor and CodeFormerCF Custom Nodes into SwarmUI
1. Drag any image into the prompt bar, in the parameters section click the "Install IP Adapter" button, this must be done before any other steps, if the button does not appear it's already installed ![image](https://github.com/user-attachments/assets/fcd63537-87e5-4ec1-af04-591b7566e684)
2. Install [ComfyUI-Manager](https://github.com/ltdrdata/ComfyUI-Manager) if not already installed by opening a cmd/terminal window in `SwarmUI/dlbackend/comfy/ComfyUI/custom_nodes` and running `git clone https://github.com/ltdrdata/ComfyUI-Manager.git`
3. Restart SwarmUI
4. Open the 'Comfy Workflow' tab and click on the manager button and then 'Custom Nodes Manager': ![image](https://github.com/user-attachments/assets/878878c1-e498-4e3c-922b-72efe382fb12)
5. In the Custom Nodes Manager find and install 'ReActor Node for ComfyUI' and/or 'Facerestore CF (Code Former)'
6. Click the red restart button at the bottom of the window then go to your SwarmUI Server/Logs tab and set the view to ComfyUI to view the download/install progress, **this will take a while**
7. When the logs show that the downloads have finished restart SwarmUI and the parameter groups should appear

## Updating
1. Shutdown SwarmUI
2. Open a cmd/terminal window in `SwarmUI\src\Extensions\SwarmUI-FaceTools`
3. Run `git pull`
4. Run `SwarmUI\update-windows.bat` to recompile SwarmUI

## Usage

### ReActor

These parameters inject different nodes to the workflow based on the options selected.

If `Face Image` is provided then face swap will be performed after image generation using the face/s in this image.

If `Face Model` is set a saved Face Model is used to perform the face swap. Face Models can be created in the Comfy Workflow tab using the 'Save Face Model' node. Once the model is saved read this to ensure saved face models show up in the dropdown: [Model Paths for Face Restore and Fask Models](#model-paths-for-face-restore-and-fask-models), `Face Image` takes priority over `Face Model` if both are set.

If `Face Restore Model` is set then face restoration will be run after the face swap, if `Face Image` or `Face Model` were not provided it runs directly on the generated image similar to FaceRestoreCF. If this option is 'None' then face restoration is skipped.

If `Second Face Restore Model` (Advanced option) is set then a second face restore model will be run after the first. I found good results with codeformer-v0.1.0 first to fix distortions then GPEN-BFR-1024 as the second model to sharpen the result.

If `Face Mask Model` is set the face swap and face restore will be masked so it doesn't effect overlapping features like hair. If the dropdown is empty download models from here: [YOLOv8 Segmentation models](https://github.com/hben35096/assets/releases/) and put them in `{SwarmUIModelRoot}/yolov8`

`Face Boost` is described [here](https://github.com/Gourieff/comfyui-reactor-node?tab=readme-ov-file#051-alpha1)

Many more parameters are available if you enable 'Advanced Options' at the bottom of the panel. All parameters have tooltips or read through the [ReActor readme](https://github.com/Gourieff/comfyui-reactor-node) for more info, you can also use 'Comfy Workflow/Import From Generate Tab' feature to see what the parameters are doing in the ComfyUI workflow.

### FaceRestoreCF

FaceRestoreCF does not automatically download the model, you will need to download it manually [from here](https://github.com/sczhou/CodeFormer/releases/download/v0.1.0/codeformer.pth) rename it to `codeformer-v0.1.0.pth` and then read this about where to place it: [Model Paths for Face Restore and Fask Models](#model-paths-for-face-restore-and-fask-models)

Just make sure the parameter group is enabled and that `Face Restore Model` is set and it should work, you can also use 'Comfy Workflow/Import From Generate Tab' feature to see what the parameters are doing in the workflow.

## Model Paths for Face Restore and Fask Models

* `Face Restore Model` models must be installed in BOTH:

`"{SwarmUIModelRoot}/facerestore_models"` AND `"dlbackend/comfy/ComfyUI/models/facerestore_models"`

* Saved `Face Model` models must be installed in BOTH:

`"{SwarmUIModelRoot}/reactor/faces"` AND `"dlbackend/comfy/ComfyUI/models/reactor/faces"`

The ReActor and FaceRestoreCF custom nodes do not follow the 'ComfyUI extra paths' config to allow them to exist only in the SwarmUI ModelRoot folder, this means some models will need to be copied so they exist both in the SwarmUI ModelRoot folder and the ComfyUI model folder. If the model exists only in SwarmUI model folder it will show be visible in the dropdowns but ComfyUI will throw an error when you generate, if they exist only in the ComfyUI model folder they will not show up in the dropdowns in SwarmUI but will be visible on the nodes if you place them in the 'Comfy Workflow' tab.

If you installed ReActor a set of Face Restore Models (Such as codeformer-v0.1.0.pth) and a Face Swap Model (inswapper_128.onnx) should have downloaded automatically into the `ComfyUI/models` folder, because they are automatically downloaded this the extension assumes these exist in the ComfyUI model folder and will add them to the dropdowns even if they don't exist in the SwarmUI ModelRoot folder so you don't need to copy them there. 

After installing a model make sure to click the model refresh button in SwarmUI so the extension can scan again.

## Troubleshooting

### I can't see the ReActor or CodeFormerCF parameter groups

Open the 'Comfy Workflow' tab and check that the relevant nodes can be added there, if you cannot add the nodes in the ComfyUI workflow then they are not installed correctly and you'll need to ask for help installing them on their respective pages [(ReActor)](https://github.com/Gourieff/comfyui-reactor-node), [(FaceRestoreCF)](https://github.com/mav-rik/facerestore_cf) or try to use [ComfyUI Manager](https://github.com/ltdrdata/ComfyUI-Manager) to install them.

### I get 'ComfyUI execution error CUDA_PATH is set but CUDA wasnt able to be loaded'

I had this happen even when CUDA_PATH pointed to a valid CUDA toolkit installation, try going to 'System Properties/Environment Variables' and removing `CUDA_PATH` to see if it fixes your problem, this may effect other software finding CUDA.
