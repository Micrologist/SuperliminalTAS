using System;
using System.IO;
using UnityEngine;

#if LEGACY
using AssetBundle = UniverseLib.AssetBundle;
#else
using AssetBundle = UnityEngine.AssetBundle;
#endif

namespace SuperliminalTools.Components.UI;

/// <summary>
/// Static class for loading the Noto-Mono font via UniverseLib asset loading.
/// </summary>
internal static class FontAssetLoader
{
    private static UnityEngine.Font _cached;

    public static UnityEngine.Font GetFontOrDefault()

    {
        if (_cached != null)
            return _cached;

        try
        {
            var modDir = Path.GetDirectoryName(typeof(FontAssetLoader).Assembly.Location);
            var bundlePath = Path.Combine(modDir, "notomono");
            bundlePath = Path.GetFullPath(bundlePath);

            if (!File.Exists(bundlePath))
            {
                Debug.LogError($"[Font] Bundle not found: {bundlePath}");
                return GetBuiltinArial();
            }

           
            var bundle = AssetBundle.LoadFromFile(bundlePath);
            if (bundle == null)
            {
                Debug.LogError($"[Font] Failed to load bundle: {bundlePath}");
                return GetBuiltinArial();
            }

            // Unity 2019 safe: enumerate assets and pick first Font
            var assets = bundle.LoadAllAssets(); // returns Il2CppReferenceArray<Il2CppUnityEngine.Object> in many setups
            Font font = null;

            for (int i = 0; i < assets.Length; i++)
            {
#if LEGACY
                font = assets[i]?.TryCast<Font>();
#else
                font = assets[i] as Font;
#endif
                if (font != null && font.name == "NotoMono-Regular")
                    break;
            }

            if (font == null)
            {
                Debug.LogError("[Font] No Font asset found in bundle.");
                bundle.Unload(false);
                return GetBuiltinArial();
            }

            _cached = font;
            bundle.Unload(false);

            Debug.Log($"[Font] Loaded font: {_cached.name}");
            return _cached;

        }
        catch (Exception e)
        {
            Debug.LogError($"[Font] Exception loading font bundle: {e}");
            return GetBuiltinArial();
        }
    }

    private static Font GetBuiltinArial()
    {
        return Resources.GetBuiltinResource<Font>("Arial.ttf");
    }
}
