using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace SuperliminalTAS.Components
{
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
            return LegacyNotoMonoAssetLoader.GetFontOrDefault();
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
    }
}
