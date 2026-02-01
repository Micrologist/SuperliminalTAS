using SuperliminalTools.PracticeMod;
using SuperliminalTools.TASMod;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using SuperliminalTools.Components.Visual;
using SuperliminalTools.Components.Control;
using SuperliminalTools.Components.UI;

#if LEGACY
using Il2CppInterop.Runtime.Injection;
#endif

namespace SuperliminalTools.Components;

public static class Utility
{
    public static Material GetTransparentMaterial(Color color)
    {
        Material mat = new(Shader.Find("Standard"));

        // Set rendering mode to transparent
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;

        mat.color = color;

        return mat;
    }

    public static Font GetNotoSansMonoFont()
    {
#if LEGACY
        return Components.UI.LegacyNotoMonoAssetLoader.GetFontOrDefault();
#else
        return Resources.FindObjectsOfTypeAll<Font>().FirstOrDefault(f => f != null && f.name == "NotoMono-Regular");
#endif
    }

    public static Text CreateHUD(
        Transform parent,
        Font font,
        int fontSize,
        Vector2 anchoredPosition,
        Vector2 size)
    {
        var root = new GameObject($"{parent.name}_HUD");
        GameObject.DontDestroyOnLoad(root);
        root.hideFlags = HideFlags.HideAndDontSave;

        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9;

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        var child = new GameObject($"{parent.name}_HUD_StatusText");
        GameObject.DontDestroyOnLoad(child);
        child.hideFlags = HideFlags.HideAndDontSave;
        child.transform.SetParent(root.transform);
        child.AddComponent<CanvasGroup>().blocksRaycasts = false;

        var text = child.AddComponent<Text>();
        text.fontSize = fontSize;

        text.font = font;

        var rect = text.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.pivot = new Vector2(0f, 1f);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;

        return text;
    }

#if LEGACY
    public static void RegisterAllIL2CPPTypes()
    {
        // Register custom MonoBehaviour types with IL2CPP before they can be used
        ClassInjector.RegisterTypeInIl2Cpp<TASMod.Demo.DemoRecorder>();
        ClassInjector.RegisterTypeInIl2Cpp<UI.HUDController>();
        ClassInjector.RegisterTypeInIl2Cpp<TASModController>();
        ClassInjector.RegisterTypeInIl2Cpp<PracticeModController>();
        ClassInjector.RegisterTypeInIl2Cpp<PathProjector>();
        ClassInjector.RegisterTypeInIl2Cpp<RenderDistanceController>();
        ClassInjector.RegisterTypeInIl2Cpp<NoClipController>();
        ClassInjector.RegisterTypeInIl2Cpp<GizmoVisibilityController>();
        ClassInjector.RegisterTypeInIl2Cpp<ColliderVisualizer>();
        ClassInjector.RegisterTypeInIl2Cpp<ColliderVisualizerController>();
        ClassInjector.RegisterTypeInIl2Cpp<FadeController>();
        ClassInjector.RegisterTypeInIl2Cpp<FlashlightController>();
        ClassInjector.RegisterTypeInIl2Cpp<PathProjectorController>();
        ClassInjector.RegisterTypeInIl2Cpp<TeleportAndScaleController>();
        ClassInjector.RegisterTypeInIl2Cpp<ObjectScaleController>();
    }
#endif

    public static void AddAllSharedComponents(GameObject gameObject)
    {
        if (ColliderVisualizerController.Instance == null)
        {
            gameObject.AddComponent<ColliderVisualizerController>();
        }

        if (FadeController.Instance == null)
        {
            gameObject.AddComponent<FadeController>();
        }

        if (FlashlightController.Instance == null)
        {
            gameObject.AddComponent<FlashlightController>();
        }

        if (GizmoVisibilityController.Instance == null)
        {
            gameObject.AddComponent<GizmoVisibilityController>();
        }

        if (HUDController.Instance == null)
        {
            gameObject.AddComponent<HUDController>();
        }

        if (NoClipController.Instance == null)
        {
            gameObject.AddComponent<NoClipController>();
        }

        if (PathProjectorController.Instance == null)
        {
            gameObject.AddComponent<PathProjectorController>();
        }

        if (RenderDistanceController.Instance == null)
        {
            gameObject.AddComponent<RenderDistanceController>();
        }

        if (TeleportAndScaleController.Instance == null)
        {
            gameObject.AddComponent<TeleportAndScaleController>();
        }

        if (ObjectScaleController.Instance == null)
        {
            gameObject.AddComponent<ObjectScaleController>();
        }
    }

}