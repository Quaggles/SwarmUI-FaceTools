# SwarmUI-FaceTools

A [SwarmUI](https://github.com/mcmonkeyprojects/SwarmUI/) extension that adds parameters for the [ReActor (with unsafe content filter)](https://github.com/Gourieff/ComfyUI-ReActor) node to the generate tab.

![image](https://github.com/user-attachments/assets/60c38f11-2b61-4841-8705-3709efb884e8)

## Changelog
<details>
  <summary>23 March 2025</summary>
  
* Workflow generator now uses StableDynamicIDs to prevent breaking workflow caching (Being forced to regenerate the entire image) when FaceTools parameters are changed.
  
* Pinned ReActor at version 0.6.0-a1, this prevents SwarmUI from automatically updating ReActor to a future version that might break compatibility with the extension until it has been manually checked as compatible.
</details>
<details>
  <summary>9 February 2025</summary>

* Added checksum validation for all models that ReActor autodownloads to warn users of corruption that cause cryptic errors, if you have a corrupted model you will see an error like this instructing you what to do:

![image](https://github.com/user-attachments/assets/1baa33a3-c65a-4b8a-a868-42fb4bff879a)

Technical details:
* The validation runs when you generate an image, if you have a corrupted model it will interrupt image generation and show the error shown above, it only validates the models currently being used by your workflow. 
* The first time you generate an image the initial checksum generation will take a few seconds and it will log to the console as it's working:
```
[Info] [FaceTools] Generated hash for 'dlbackend/comfy/ComfyUI/models/facerestore_models/codeformer-v0.1.0.pth' 1009e537 (Took 1.7 seconds)
[Info] [FaceTools] Generated hash for 'dlbackend/comfy/ComfyUI/models/insightface/inswapper_128.onnx' e4a3f08c (Took 2.4 seconds)
[Info] [FaceTools] Generated hash for 'dlbackend/comfy/ComfyUI/models/insightface/models/buffalo_l/1k3d68.onnx' df5c06b8 (Took 0.6 seconds)
[Info] [FaceTools] Generated hash for 'dlbackend/comfy/ComfyUI/models/insightface/models/buffalo_l/2d106det.onnx' f001b856 (Took 0.0 seconds)
[Info] [FaceTools] Generated hash for 'dlbackend/comfy/ComfyUI/models/insightface/models/buffalo_l/det_10g.onnx' 5838f7fe (Took 0.1 seconds)
[Info] [FaceTools] Generated hash for 'dlbackend/comfy/ComfyUI/models/insightface/models/buffalo_l/genderage.onnx' 4fde69b1 (Took 0.0 seconds)
[Info] [FaceTools] Generated hash for 'dlbackend/comfy/ComfyUI/models/insightface/models/buffalo_l/w600k_r50.onnx' 4c06341c (Took 0.8 seconds)
[Info] [FaceTools] Generated hash for 'dlbackend/comfy/ComfyUI/models/nsfw_detector/vit-base-nsfw-detector/model.safetensors' 266efb8b (Took 1.5 seconds)
```
* Generated checksums are cached to `'SwarmUI/src/Extensions/SwarmUI-FaceTools/ModelHashCache.json'` and are only recalculated if the file is modified.
* Models are only validated if you are using the built in ComfyUI backend, if ComdyUI is not at `SwarmUI/dlbackend/comfy/ComfyUI/` validation will be skipped for users with external ComfyUI installs.

</details>
<details>
  <summary>24 January 2025</summary>

* **[Notice]** The old ReActor repository (https://github.com/Gourieff/comfyui-reactor-node) was removed from GitHub, an updated version with a filter for unsafe content that is compliant with [GitHub rules](https://docs.github.com/en/site-policy/acceptable-use-policies/github-misinformation-and-disinformation#synthetic--manipulated-media-tools) has been made: https://github.com/Gourieff/ComfyUI-ReActor. If you have the old node installed (You installed prior to 17-01-2024) you will see the following message on SwarmUI startup as it removes the old node so you can install the new one:

`[Init] [FaceTools] Moving deprecated ReActor repository to recycle bin 'SwarmUI/src/BuiltinExtensions/ComfyUIBackend/DLNodes/comfyui-reactor-node', click the 'Install ReActor' button in the parameter list to install its replacement`
* Removed [FaceRestoreCF](https://github.com/mav-rik/facerestore_cf) support as it is no longer being maintained and ReActor can do face restoration with more options and models supported
</details>
<details>
  <summary>4 November 2024</summary>

* ReActor install button will now not show up if you load the page before the backend has loaded
* Dropdown parameters are now prepopulated where possible so they will show up if you load the page before the backend has loaded
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

6. Download `inswapper_128.onnx` from https://huggingface.co/datasets/Gourieff/ReActor/tree/main/models and put it here: `SwarmUI/dlbackend/comfy/ComfyUI/models/insightface/inswapper_128.onnx`

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

These parameters inject different nodes to the workflow based on the options selected.

If `Face Image` is provided then face swap will be performed after image generation using the face/s in this image.

If `Face Model` is set a saved Face Model is used to perform the face swap. Face Models can be created in the Comfy Workflow tab using the 'Save Face Model' node. `Face Image` takes priority over `Face Model` if both are set.

If `Face Restore Model` is set then face restoration will be run after the face swap, if `Face Image` or `Face Model` were not provided it runs directly on the generated image. If this option is 'None' then face restoration is skipped.

If `Second Face Restore Model` (Advanced option) is set then a second face restore model will be run after the first. I found good results with codeformer-v0.1.0 first to fix distortions then GPEN-BFR-1024 as the second model to sharpen the result.

If `Face Mask Model` is set the face swap and face restore will be masked so it doesn't effect overlapping features like hair. If the dropdown is empty download models from here: [YOLOv8 Segmentation models](https://github.com/hben35096/assets/releases/) and put them in `{SwarmUIModelRoot}/yolov8`

If `Face Boost` is set itr estores the face after the face swap but before transplanting on the generated image, can increase quality but results may vary. A side effect of enabling this with <b>Restore After Main</b> disabled with is that only one face in the image will be restored or swapped

Many more parameters are available if you enable 'Advanced Options' at the bottom of the panel. All parameters have tooltips or read through the [ReActor readme](https://github.com/Gourieff/ComfyUI-ReActor) for more info, you can also use 'Comfy Workflow/Import From Generate Tab' feature to see what the parameters are doing in the ComfyUI workflow.

## Model Paths for Face Restore and Fask Models

The ReActor custom nodes do not follow the 'ComfyUI extra paths' config to allow them to exist in the SwarmUI ModelRoot folder.

* `Face Swap` models can be downloaded from [here](https://huggingface.co/datasets/Gourieff/ReActor/tree/main/models) and must be installed in:

`"SwarmUI/dlbackend/comfy/ComfyUI/models/insightface"`

* `Face Restore Model` models can be downloaded from [here](https://huggingface.co/datasets/Gourieff/ReActor/tree/main/models/facerestore_models) and must be installed in:

`"SwarmUI/dlbackend/comfy/ComfyUI/models/facerestore_models"`

* Saved `Face Model` models must be installed in:

`"SwarmUI/dlbackend/comfy/ComfyUI/models/reactor/faces"`

After installing a model make sure to click the model refresh button in SwarmUI so the extension can scan again.

## Troubleshooting

SwarmUI will update ReActor automatically if you have AutoUpdate enabled for the ComfyUI backend. If you need to update the node manually or do manual troubleshooting steps it can be found in this folder `SwarmUI\src\BuiltinExtensions\ComfyUIBackend\DLNodes\ComfyUI-ReActor`.

### I see a black image result from the face swap

This is not a bug, ReActor returns a black image when the NSFW detector detects unsafe content in the source image to comply with [GitHub rules](https://docs.github.com/en/site-policy/acceptable-use-policies/github-misinformation-and-disinformation#synthetic--manipulated-media-tools).

### ModuleNotFoundError: No module named 'insightface'

SwarmUI should install a precompiled insightface wheel when you click the 'Install IP Adapter' button but if that didn't work you can compile it manually with the following steps:

1. Install Visual Studio 2022 Community version OR only VS C++ Build Tools and select "Desktop Development with C++" under "Workloads -> Desktop & Mobile"
2. Find the python version of your ComfyUI backend by looking at the file properties of `SwarmUI\dlbackend\comfy\python_embeded\python.exe`, for this example I have Python 3.12.7
3. Install the exact same python version from https://www.python.org/downloads/ and manually copy over the `include` and `libs` folders from `C:\Users\<User>\AppData\Local\Programs\Python\Python312` to  `SwarmUI\dlbackend\comfy\python_embeded`
4. Start a terminal/cmd in `SwarmUI/dlbackend/comfy/python_embeded`
5. Run the command `python.exe -m pip install insightface`, if using Powershell you might need to prepend commands with `./` to ensure it doesn't find your global python install on PATH
6. If all goes well this should compile insightface successfully and you can continue

### ComfyUI execution error: `[ONNXRuntimeError] : 7 : INVALID_PROTOBUF : Load model from .../inswapper_128.onnx failed:Protobuf parsing failed`

Try going to the folder in the error message and deleting the `inswapper_128.onnx` model and replacing it with a version downloaded directly from [here](https://huggingface.co/datasets/Gourieff/ReActor/tree/main/models)

### An error occurs when clicking 'Generate' when using ReActor

Read through the [ReActor Troubleshooting Guide](https://github.com/Gourieff/ComfyUI-ReActor#troubleshooting) and see if any match your error.

### I get 'ComfyUI execution error CUDA_PATH is set but CUDA wasn't able to be loaded'

I had this happen even when CUDA_PATH pointed to a valid CUDA toolkit installation, try going to 'System Properties/Environment Variables' and removing `CUDA_PATH` to see if it fixes your problem, this may effect other software finding CUDA.

### I can't see the ReActor parameter groups

Open the 'Comfy Workflow' tab and check that the relevant nodes can be added there, if you cannot add the nodes in the ComfyUI workflow then they are not installed correctly and you might need look through the [ReActor Github](https://github.com/Gourieff/ComfyUI-ReActor) or the [SwarmUI Discord](https://discord.gg/swarmui-1243166023859961988).

### If all else fails
Ask for help on the [SwarmUI discord](https://discord.gg/swarmui-1243166023859961988)
