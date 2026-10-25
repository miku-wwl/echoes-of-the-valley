using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Echoes.EditorTools
{
    /// <summary>Small helpers for building uGUI hierarchies from code: one warm, readable visual style.</summary>
    internal static class UiKit
    {
        public static readonly Color PanelColor = new Color(0.08f, 0.11f, 0.18f, 0.74f);
        public static readonly Color Cream = new Color(1f, 0.95f, 0.84f, 1f);
        public static readonly Color Gold = new Color(1f, 0.82f, 0.46f, 1f);
        public static readonly Color Ink = new Color(0.22f, 0.15f, 0.11f, 1f);

        public static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        public static readonly Vector2 TopCenter = new Vector2(0.5f, 1f);
        public static readonly Vector2 BottomLeft = new Vector2(0f, 0f);
        public static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);
        public static readonly Vector2 BottomRight = new Vector2(1f, 0f);
        public static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

        static Font font;
        static Sprite rounded;

        static Font DefaultFont => font != null ? font : (font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        static Sprite Rounded => rounded != null ? rounded : (rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"));

        public static Canvas CreateCanvas(string name, int sortingOrder)
        {
            GameObject go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static RectTransform NewRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        /// <summary>Fixed-size placement relative to a single anchor point.</summary>
        public static RectTransform Place(this RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        public static RectTransform Fill(this RectTransform rect, float padding = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = Center;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
            return rect;
        }

        public static Image Panel(Transform parent, string name, Color color, bool rounded = true)
        {
            Image image = NewRect(name, parent).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            if (rounded)
            {
                image.sprite = Rounded;
                image.type = Image.Type.Sliced;
            }
            return image;
        }

        public static Text Label(Transform parent, string name, string text, int size, Color color, TextAnchor align, FontStyle style = FontStyle.Normal, bool outline = true)
        {
            Text label = NewRect(name, parent).gameObject.AddComponent<Text>();
            label.font = DefaultFont;
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.color = color;
            label.alignment = align;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.raycastTarget = false;
            if (outline)
            {
                Shadow shadow = label.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0.04f, 0.05f, 0.09f, 0.7f);
                shadow.effectDistance = new Vector2(2f, -2f);
            }
            return label;
        }

        public static Button MakeButton(Transform parent, string name, string caption, Vector2 anchor, Vector2 position, Vector2 size)
        {
            RectTransform rect = NewRect(name, parent).Place(anchor, Center, position, size);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = Rounded;
            image.type = Image.Type.Sliced;

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(1f, 0.93f, 0.78f, 0.96f);
            colors.highlightedColor = new Color(1f, 1f, 0.93f, 1f);
            colors.selectedColor = new Color(1f, 0.98f, 0.86f, 1f);
            colors.pressedColor = new Color(0.95f, 0.76f, 0.45f, 1f);
            colors.disabledColor = new Color(0.7f, 0.7f, 0.7f, 0.5f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            Text text = Label(rect, "Caption", caption, 34, Ink, TextAnchor.MiddleCenter, FontStyle.Bold, false);
            text.rectTransform.Fill(4f);
            return button;
        }
    }
}
