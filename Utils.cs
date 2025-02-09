using System.IO;
using System.Security.Cryptography;
using FreneticUtilities.FreneticToolkit;
using Newtonsoft.Json;
using SwarmUI.Text2Image;
using SwarmUI.Utils;

namespace Quaggles.Extensions.FaceTools;

public static class Utils
{
    private static readonly Dictionary<string, string> ModelSha256Checksums = new()
    {
        {"dlbackend/comfy/ComfyUI/models/facerestore_models/codeformer-v0.1.0.pth", "1009e537e0c2a07d4cabce6355f53cb66767cd4b4297ec7a4a64ca4b8a5684b7" },
        {"dlbackend/comfy/ComfyUI/models/facerestore_models/GFPGANv1.3.pth", "c953a88f2727c85c3d9ae72e2bd4846bbaf59fe6972ad94130e23e7017524a70" },
        {"dlbackend/comfy/ComfyUI/models/facerestore_models/GFPGANv1.4.pth", "e2cd4703ab14f4d01fd1383a8a8b266f9a5833dacee8e6a79d3bf21a1b6be5ad" },
        {"dlbackend/comfy/ComfyUI/models/facerestore_models/GPEN-BFR-512.onnx", "bf80acb8e91ba8852e3f012505be2c3b6cd6b3eed5ec605e3db87863c4e74d4e" },
        {"dlbackend/comfy/ComfyUI/models/facerestore_models/GPEN-BFR-1024.onnx", "cec8892093d7b99828acde97bf231fb0964d3fb11b43f3b0951e36ef1e192a3e" },
        {"dlbackend/comfy/ComfyUI/models/facerestore_models/GPEN-BFR-2048.onnx", "d0229ff43f979c360bd19daa9cd0ce893722d59f41a41822b9223ebbe4f89b3e" },
        {"dlbackend/comfy/ComfyUI/models/insightface/inswapper_128.onnx", "e4a3f08c753cb72d04e10aa0f7dbe3deebbf39567d4ead6dce08e98aa49e16af" },
        {"dlbackend/comfy/ComfyUI/models/insightface/models/buffalo_l/1k3d68.onnx", "df5c06b8a0c12e422b2ed8947b8869faa4105387f199c477af038aa01f9a45cc" },
        {"dlbackend/comfy/ComfyUI/models/insightface/models/buffalo_l/2d106det.onnx", "f001b856447c413801ef5c42091ed0cd516fcd21f2d6b79635b1e733a7109dbf" },
        {"dlbackend/comfy/ComfyUI/models/insightface/models/buffalo_l/det_10g.onnx", "5838f7fe053675b1c7a08b633df49e7af5495cee0493c7dcf6697200b85b5b91" },
        {"dlbackend/comfy/ComfyUI/models/insightface/models/buffalo_l/genderage.onnx", "4fde69b1c810857b88c64a335084f1c3fe8f01246c9a191b48c7bb756d6652fb" },
        {"dlbackend/comfy/ComfyUI/models/insightface/models/buffalo_l/w600k_r50.onnx", "4c06341c33c2ca1f86781dab0e829f88ad5b64be9fba56e56bc9ebdefc619e43" },
        {"dlbackend/comfy/ComfyUI/models/nsfw_detector/vit-base-nsfw-detector/model.safetensors", "266efb8bf67c1e865a577222fbbd6ddb149b9e00ba0d2b50466a034837f026a4" },
    };
    
    private static ConcurrentDictionary<string, CacheData> modelHashCache;
    private static bool cacheDirty = false;

    private record struct CacheData
    {
        public long LastWriteTime;
        public long FileSize;
        public string HashSha256;
    }

    private record struct SerializedCache
    {
        public int CacheFormatVersion;
        public ConcurrentDictionary<string, CacheData> ModelHashCache;
    }

    private static readonly LockObject ModelHashCacheFileLock = new();

    public static void ValidateModel(string relativePath)
    {
        var absolutePath = Path.Combine(Environment.CurrentDirectory, relativePath).Replace("\\", "/");
        // If we can't find it skip since the user may not be using the built in ComfyUI instance
        if (!File.Exists(absolutePath))
        {
            Logs.Verbose($"{FaceToolsExtension.ExtensionPrefix} {relativePath} could not be found, skipping hash validation...");
            return;
        }

        var name = Path.GetFileName(absolutePath);
        if (!ModelSha256Checksums.TryGetValue(relativePath, out var expectedHash))
            return;
        var hash = GetOrGenerateHashSha256(relativePath);
        if (expectedHash != hash)
        {
            string fixMessage;
            if (relativePath.Contains("vit-base-nsfw-detector"))
                fixMessage = $"this can usually be fixed by deleting the model at:\n\n{absolutePath} and restarting SwarmUI, it should try to download again";
            else if (relativePath.Contains("buffalo_l"))
                fixMessage = $"this can usually be fixed by downloading buffalo_l.zip from:\n\nhttps://huggingface.co/datasets/Gourieff/ReActor/tree/main/models/\n\nand then extracting and replacing the model at:\n\n{absolutePath}";
            else if (relativePath.Contains("facerestore_models"))
                fixMessage = $"this can usually be fixed by downloading {name} from:\n\nhttps://huggingface.co/datasets/Gourieff/ReActor/tree/main/models/facerestore_models/\n\nand replacing the model at:\n\n{absolutePath}";
            else
                fixMessage = $"this can usually be fixed by downloading {name} from:\n\nhttps://huggingface.co/datasets/Gourieff/ReActor/tree/main/models/\n\nand replacing the model at:\n\n{absolutePath}";

            var message = $"The model file '{name}' is corrupted! Expected SHA256 hash {expectedHash.Substring(0, 8)} but found {hash.Substring(0, 8)}, this can happen when the ReActor autodownloader fails to complete downloading the file, {fixMessage}";
            Logs.Error($"{FaceToolsExtension.ExtensionPrefix}{message}");
            throw new SwarmUserErrorException($"FaceTools Error\n{message}");
        }
    }

    public static string GetOrGenerateHashSha256(string relativePath)
    {
        var absolutePath = Path.Combine(Environment.CurrentDirectory, relativePath);
        if (!File.Exists(absolutePath))
            throw new FileNotFoundException(absolutePath);
        if (modelHashCache == null) // Import from the file cache if this is the first time
        {
            if (File.Exists(FaceToolsExtension.ModelHashCacheLocation))
            {
                try
                {
                    lock (ModelHashCacheFileLock)
                    {
                        var cacheJson = File.ReadAllText(FaceToolsExtension.ModelHashCacheLocation);
                        var cacheObject = JsonConvert.DeserializeObject<SerializedCache>(cacheJson);
                        modelHashCache = cacheObject.ModelHashCache;
                    }
                }
                catch (Exception ex)
                {
                    Logs.Error($"{FaceToolsExtension.ExtensionPrefix}Error reading from hash cache: {ex}");
                    modelHashCache = new();
                }
            }
            else
            {
                modelHashCache = new();
            }
        }
        var info = new FileInfo(absolutePath);
        
        // Return the cached value if it's valid
        bool inCache = modelHashCache.TryGetValue(relativePath, out var cacheData);
        if (inCache)
        {
            // Check if the cache entry is valid
            if (info.Length == cacheData.FileSize && info.LastWriteTime.ToFileTimeUtc() == cacheData.LastWriteTime)
            {
                Logs.Verbose($"{FaceToolsExtension.ExtensionPrefix}Found hash for {relativePath} in cache: {cacheData.HashSha256}");
                return cacheData.HashSha256;
            }
            Logs.Info($"{FaceToolsExtension.ExtensionPrefix}Hash for '{relativePath}' will be recalculated as a file change was detected on disk");
        }

        Logs.Verbose($"{FaceToolsExtension.ExtensionPrefix}Generating hash for '{relativePath}'");
        
        // Hashing
        var startGenerateTime = DateTime.UtcNow;
        using FileStream reader = File.OpenRead(absolutePath);
        var hash = Utilities.BytesToHex(SHA256.HashData(reader));
        Logs.Info($"{FaceToolsExtension.ExtensionPrefix}Generated hash for '{relativePath}' {hash.Substring(0, 8)} (Took {(DateTime.UtcNow - startGenerateTime).TotalSeconds:F1} seconds)");
        
        // Update hash cache
        var newCacheData = new CacheData { HashSha256 = hash, LastWriteTime = info.LastWriteTime.ToFileTimeUtc(), FileSize = info.Length };
        if (!inCache)
            modelHashCache.TryAdd(relativePath, newCacheData);
        else
            modelHashCache.TryUpdate(relativePath, newCacheData, cacheData);
        cacheDirty = true;

        return hash;
    }

    public static void SaveHashCache(bool force = false)
    {
        if (cacheDirty || force)
        {
            try
            {
                var json = JsonConvert.SerializeObject(new SerializedCache { ModelHashCache = modelHashCache, CacheFormatVersion = 1 }, Formatting.Indented);
                lock (ModelHashCacheFileLock)
                {
                    Logs.Verbose($"{FaceToolsExtension.ExtensionPrefix}Updating hash cache file");
                    File.WriteAllText(FaceToolsExtension.ModelHashCacheLocation, json);
                    cacheDirty = false;
                }
            }
            catch (Exception ex)
            {
                Logs.Error($"{FaceToolsExtension.ExtensionPrefix}Error writing to the hash cache: {ex}");
            }
        }
    }
    
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