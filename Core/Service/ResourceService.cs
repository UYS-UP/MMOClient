using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

public class ResourceService : SingletonMono<ResourceService>
{
    private Dictionary<string, UnityEngine.Object> resourceCache = new Dictionary<string, UnityEngine.Object>();
    private Dictionary<string, AssetBundle> bundleCache = new Dictionary<string, AssetBundle>();
    private AssetBundleManifest manifest = null;
    private const string ManifestBundleName = "AssetBundleManifest";
    
    /// <summary>
    /// 加载资源包
    /// </summary>
    public T LoadResource<T>(string path) where T : Object
    {
        if (resourceCache.TryGetValue(path, out var cachedAsset)) return cachedAsset as T;
        T asset = Resources.Load<T>(path);
        if (asset != null)
        {
            resourceCache[path] = asset;
        }
        else
        {
            Debug.LogWarning($"加载资源失败: {path}");
        }

        return asset;
    }

    public void LoadResourceAsync<T>(string path, Action<T> onComplete) where T : Object
    {
        StartCoroutine(LoadResourceRoutine(path, onComplete));
    }

    /// <summary>
    /// 异步加载资源包
    /// </summary>
    private IEnumerator LoadResourceRoutine<T>(string path, Action<T> onComplete) where T : Object
    {
        if (resourceCache.TryGetValue(path, out var cachedAsset))
        {
            onComplete?.Invoke(cachedAsset as T);
            yield break;
        }

        ResourceRequest request = Resources.LoadAsync<T>(path);
        yield return request;

        if (request.asset != null)
        {
            resourceCache[path] = request.asset;
            onComplete?.Invoke(request.asset as T);
        }
        else
        {
            Debug.LogWarning($"异步加载资源失败: {path}");
            onComplete?.Invoke(null);
        }
    }
    
    public void LoadAssetBundleResourceAsync<T>(string bundleName, string assetName, Action<T> onComplete) where T : Object
    {
        StartCoroutine(LoadAssetBundleResourceRoutine(bundleName, assetName, onComplete));
    }

    /// <summary>
    /// 异步加载AB包
    /// </summary>
    private IEnumerator LoadAssetBundleResourceRoutine<T>(string bundleName, string assetName, Action<T> onComplete)
        where T : Object
    {
        string cacheKey = $"{bundleName}_{assetName}";
        if (resourceCache.TryGetValue(cacheKey, out var cachedAsset))
        {
            onComplete?.Invoke(cachedAsset as T);
            yield break;
        }

        AssetBundle bundle = GetAssetBundleWithDependencies(bundleName);
        if (bundle == null)
        {
            yield return StartCoroutine(LoadAssetBundleWithDependenciesRoutine(bundleName, null));
        }
    }
    
    /// <summary>
    /// 同步加载AB包
    /// </summary>
    public T LoadAssetBundleResource<T>(string bundleName, string assetName) where T : Object
    {
        string cacheKey = $"{bundleName}/{assetName}";
        if (resourceCache.TryGetValue(cacheKey, out var cachedAsset))
        {
            return cachedAsset as T;
        }

        AssetBundle bundle = GetAssetBundleWithDependencies(bundleName);
        if (bundle == null)
        {
            Debug.LogWarning($"加载AB包失败: {bundleName}");
            return null;
        }

        T asset = bundle.LoadAsset<T>(assetName);
        if (asset != null)
        {
            resourceCache[cacheKey] = asset;
        }
        else
        {
            Debug.LogWarning($"{bundleName}加载资源{assetName}失败");
        }

        return asset;
    }


    /// <summary>
    /// 加载AB包及其依赖包
    /// </summary>
    private AssetBundle GetAssetBundleWithDependencies(string bundleName)
    {
        if (bundleCache.TryGetValue(bundleName, out var bundle))
        {
            return bundle;
        }

        // 加载 Manifest
        if (manifest == null)
        {
            AssetBundle manifestBundle =
                AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, ManifestBundleName));
            if (manifestBundle != null)
            {
                manifest = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                bundleCache[ManifestBundleName] = manifestBundle;
            }
            else
            {
                Debug.LogWarning($"加载主包失败: {Path.Combine(Application.streamingAssetsPath, ManifestBundleName)}");
                return null;
            }
        }

        // 加载依赖
        string[] dependencies = manifest.GetAllDependencies(bundleName);
        foreach (var dep in dependencies)
        {
            if (!bundleCache.ContainsKey(dep))
            {
                string depPath = Path.Combine(Application.streamingAssetsPath, dep);
                AssetBundle depBundle = AssetBundle.LoadFromFile(depPath);
                if (depBundle != null)
                {
                    bundleCache[dep] = depBundle;
                }
                else
                {
                    Debug.LogWarning($"加载依赖包失败: {dep}");
                }
            }
        }

        // 加载
        string bundlePath = Path.Combine(Application.streamingAssetsPath, bundleName);
        bundle = AssetBundle.LoadFromFile(bundlePath);
        if (bundle != null)
        {
            bundleCache[bundleName] = bundle;
        }

        return bundle;
    }
    
    
    // 异步加载AB包及其依赖
    private IEnumerator LoadAssetBundleWithDependenciesRoutine(string bundleName, Action<AssetBundle> onComplete)
    {
        if (bundleCache.TryGetValue(bundleName, out var bundle))
        {
            onComplete?.Invoke(bundle);
            yield break;
        }

        // 加载 Manifest
        if (manifest == null)
        {
            string manifestPath = Path.Combine(Application.streamingAssetsPath, ManifestBundleName);
            UnityWebRequest manifestRequest = UnityWebRequestAssetBundle.GetAssetBundle(manifestPath);
            yield return manifestRequest.SendWebRequest();

            if (manifestRequest.result == UnityWebRequest.Result.Success)
            {
                AssetBundle manifestBundle = DownloadHandlerAssetBundle.GetContent(manifestRequest);
                if (manifestBundle != null)
                {
                    manifest = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                    bundleCache[ManifestBundleName] = manifestBundle;
                }
                else
                {
                    Debug.LogWarning($"异步加载主包失败: {manifestPath}");
                    onComplete?.Invoke(null);
                    yield break;
                }
            }
            else
            {
                Debug.LogWarning($"下载主包失败: {manifestRequest.error}");
                onComplete?.Invoke(null);
                yield break;
            }
        }

        // 加载依赖
        string[] dependencies = manifest.GetAllDependencies(bundleName);
        foreach (var dep in dependencies)
        {
            if (!bundleCache.ContainsKey(dep))
            {
                yield return StartCoroutine(LoadAssetBundleRoutine(dep, null));
            }
        }

        // 加载目标 Bundle
        yield return StartCoroutine(LoadAssetBundleRoutine(bundleName, onComplete));
    }

    // 异步加载单个 AssetBundle
    private IEnumerator LoadAssetBundleRoutine(string bundleName, Action<AssetBundle> onComplete)
    {
        if (bundleCache.TryGetValue(bundleName, out var bundle))
        {
            onComplete?.Invoke(bundle);
            yield break;
        }

        string bundlePath = Path.Combine(Application.streamingAssetsPath, bundleName);
        UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(bundlePath);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            bundle = DownloadHandlerAssetBundle.GetContent(request);
            if (bundle != null)
            {
                bundleCache[bundleName] = bundle;
                onComplete?.Invoke(bundle);
            }
            else
            {
                Debug.LogWarning($"Failed to load AssetBundle async: {bundleName}");
                onComplete?.Invoke(null);
            }
        }
        else
        {
            Debug.LogWarning($"Failed to download AssetBundle {bundleName}: {request.error}");
            onComplete?.Invoke(null);
        }
    }

    // 卸载资源
    public void UnloadResource(string path)
    {
        if (resourceCache.TryGetValue(path, out var asset))
        {
            if (asset is not GameObject)
            {
                Resources.UnloadAsset(asset);
            }
            resourceCache.Remove(path);
        }
    }

    // 卸载AB包及其依赖
    public void UnloadAssetBundle(string bundleName, bool unloadAllLoadedObjects = false)
    {
        if (bundleCache.TryGetValue(bundleName, out var bundle))
        {
            bundle.Unload(unloadAllLoadedObjects);
            bundleCache.Remove(bundleName);

            // 移除相关缓存
            var keysToRemove = new List<string>();
            foreach (var key in resourceCache.Keys)
            {
                if (key.StartsWith(bundleName + "/"))
                {
                    keysToRemove.Add(key);
                }
            }
            foreach (var key in keysToRemove)
            {
                resourceCache.Remove(key);
            }

            // 检查依赖是否仍被其他AB包使用
            if (manifest != null)
            {
                foreach (var cachedBundleName in bundleCache.Keys)
                {
                    if (cachedBundleName == ManifestBundleName) continue;
                    var dependencies = manifest.GetAllDependencies(cachedBundleName);
                    foreach (var dep in dependencies)
                    {
                        bundleCache.TryGetValue(dep, out var depBundle);
                        if (depBundle != null)
                        {
                            return;
                        }
                    }
                }

                // 如果没有其他 AB包使用依赖，卸载Manifest
                if (bundleCache.TryGetValue(ManifestBundleName, out var manifestBundle))
                {
                    manifestBundle.Unload(unloadAllLoadedObjects);
                    bundleCache.Remove(ManifestBundleName);
                    manifest = null;
                }
            }
        }
    }

    // 卸载所有资源和 AssetBundle
    public void UnloadAll()
    {
        foreach (var bundle in bundleCache.Values)
        {
            bundle.Unload(true);
        }
        bundleCache.Clear();
        resourceCache.Clear();
        manifest = null;
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
    }
}
