using UnityEngine;
using UnityEngine.UI;

namespace HangarGame.UI
{
    public static class UITheme
    {
        public static readonly Color BgDark = new Color(0.055f, 0.075f, 0.105f, 0.96f);
        public static readonly Color BgHeader = new Color(0.085f, 0.115f, 0.155f, 0.98f);
        public static readonly Color BgCard = new Color(0.095f, 0.130f, 0.175f, 0.96f);
        public static readonly Color BgCardActive = new Color(0.125f, 0.195f, 0.255f, 0.98f);
        public static readonly Color BgRecessed = new Color(0.040f, 0.055f, 0.080f, 1.0f);
        public static readonly Color Border = new Color(0.20f, 0.29f, 0.38f, 0.95f);
        public static readonly Color TextMain = new Color(0.91f, 0.94f, 0.97f, 1.0f);
        public static readonly Color TextMuted = new Color(0.56f, 0.65f, 0.74f, 1.0f);
        public static readonly Color AccentTeal = new Color(0.22f, 0.58f, 0.72f, 1.0f);
        public static readonly Color AccentAmber = new Color(0.84f, 0.54f, 0.18f, 1.0f);
        public static readonly Color AccentGreen = new Color(0.18f, 0.52f, 0.34f, 1.0f);
        public static readonly Color AccentRed = new Color(0.68f, 0.23f, 0.23f, 1.0f);
        public static readonly Color RadarGrid = new Color(0.13f, 0.22f, 0.29f, 0.75f);
    }

    public static class UIBuilder
    {
        private static Font cachedFont;

        public static Font GetFont(int size = 16)
        {
            if (cachedFont != null) return cachedFont;
            cachedFont = Font.CreateDynamicFontFromOSFont("Arial", size);
            if (cachedFont == null) cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return cachedFont;
        }

        public static GameObject Panel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size, Color bg, Color? borderCol = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = anchorMin == anchorMax ? new Vector2(0.5f, 0.5f) : Vector2.zero;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.color = bg;

            if (borderCol.HasValue)
            {
                var outline = go.AddComponent<Outline>();
                outline.effectColor = borderCol.Value;
                outline.effectDistance = new Vector2(1.5f, 1.5f);
            }
            return go;
        }

        public static Text Label(Transform parent, string name, string text, Vector2 anchor, Vector2 pos, Vector2 size, int fontSize, Color color, Font font = null, FontStyle style = FontStyle.Normal, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var t = go.AddComponent<Text>();
            t.font = font != null ? font : GetFont(fontSize);
            t.text = text;
            t.fontSize = fontSize;
            t.fontStyle = style;
            t.alignment = align;
            t.color = color;
            t.raycastTarget = false;
            return t;
        }

        public static Button Button(Transform parent, string name, string label, Vector2 anchor, Vector2 pos, Vector2 size, Color bg, Color? borderCol = null, int fontSize = 15, Font font = null, FontStyle style = FontStyle.Bold)
        {
            var go = Panel(parent, name, anchor, anchor, pos, size, bg, borderCol);
            var btn = go.AddComponent<Button>();
            var txt = Label(go.transform, "Text", label, new Vector2(0.5f, 0.5f), Vector2.zero, size - new Vector2(10f, 4f), fontSize, UITheme.TextMain, font, style);
            txt.raycastTarget = false;
            return btn;
        }

        public static Image BarFill(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color bgCol, Color fillCol)
        {
            var bg = Panel(parent, name, anchor, anchor, pos, size, bgCol);
            bg.GetComponent<Image>().raycastTarget = false;

            var fill = new GameObject("Fill");
            fill.transform.SetParent(bg.transform, false);
            var rt = fill.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.offsetMin = new Vector2(1f, 1f);
            rt.offsetMax = new Vector2(-1f, -1f);

            var img = fill.AddComponent<Image>();
            img.color = fillCol;
            img.raycastTarget = false;
            return img;
        }

        public static void SetBar(Image fill, float ratio)
        {
            if (fill == null) return;
            float r = Mathf.Clamp01(ratio);
            var rt = fill.rectTransform;
            rt.anchorMax = new Vector2(r, 1f);
            fill.enabled = r > 0.005f;
        }

        public static void AddLine(Transform parent, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject("GridLine");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }
    }
}
