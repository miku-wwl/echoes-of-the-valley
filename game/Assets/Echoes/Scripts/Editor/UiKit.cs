using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Echoes.EditorTools
{
    /// <summary>
    /// Small helpers for building uGUI hierarchies from code: one warm, readable storybook style. Panels are twilight plum with
    /// a thin gold edge, text is cream and gold, buttons are cream with a brown edge and a soft shadow and swell when hovered.
    /// </summary>
    internal static class UiKit
    {
        public static readonly Color PanelColor = new Color(0.145f, 0.227f, 0.235f, 0.92f);
        public static readonly Color ModalColor = new Color(0.105f, 0.18f, 0.19f, 0.97f);
        public static readonly Color Cream = new Color(1f, 0.95f, 0.84f, 1f);
        public static readonly Color Gold = new Color(.90f, 0.75f, 0.47f, 1f);
        public static readonly Color Ink = new Color(0.22f, 0.15f, 0.11f, 1f);
        public static readonly Color EdgeGold = new Color(1f, 0.82f, 0.46f, 0.40f);
        public static readonly Color EdgeBrown = new Color(0.50f, 0.32f, 0.16f, 0.85f);

        public static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        public static readonly Vector2 TopCenter = new Vector2(0.5f, 1f);
        public static readonly Vector2 TopRight = new Vector2(1f, 1f);
        public static readonly Vector2 MiddleRight = new Vector2(1f, 0.5f);
        public static readonly Vector2 BottomLeft = new Vector2(0f, 0f);
        public static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);
        public static readonly Vector2 BottomRight = new Vector2(1f, 0f);
        public static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

        static Font font;
        static Sprite rounded;
        static Sprite knob;

        static Font DefaultFont => font != null ? font : (font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        static Sprite Rounded => rounded != null ? rounded : (rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"));

        /// <summary>A round sprite for coins, icons and slider handles.</summary>
        public static Sprite Knob => knob != null ? knob : (knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"));

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

        /// <summary>A rounded (or flat) coloured rectangle; <paramref name="edge"/> adds a thin border drawn over it.</summary>
        public static Image Panel(Transform parent, string name, Color color, bool rounded = true, bool edge = false)
        {
            Image image = NewRect(name, parent).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            if (rounded)
            {
                image.sprite = Rounded;
                image.type = Image.Type.Sliced;
                if (edge) Edge(image.transform, EdgeGold);
            }
            return image;
        }

        /// <summary>A hollow rounded outline that fills its parent (the middle of the sprite is not drawn).</summary>
        public static Image Edge(Transform parent, Color color)
        {
            Image border = NewRect("Edge", parent).gameObject.AddComponent<Image>();
            border.sprite = Rounded;
            border.type = Image.Type.Sliced;
            border.fillCenter = false;
            border.color = color;
            border.raycastTarget = false;
            border.rectTransform.Fill();
            return border;
        }

        /// <summary>A small solid bar (an accent stripe, a divider) with no sprite.</summary>
        public static Image Bar(Transform parent, string name, Color color)
        {
            Image bar = NewRect(name, parent).gameObject.AddComponent<Image>();
            bar.color = color;
            bar.raycastTarget = false;
            return bar;
        }

        /// <summary>A round coloured dot (a coin, a status light).</summary>
        public static Image Dot(Transform parent, string name, Color color)
        {
            Image dot = NewRect(name, parent).gameObject.AddComponent<Image>();
            dot.sprite = Knob;
            dot.color = color;
            dot.raycastTarget = false;
            return dot;
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
                shadow.effectColor = new Color(0.05f, 0.03f, 0.09f, 0.7f);
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
            colors.normalColor = new Color(1f, 0.93f, 0.78f, 0.97f);
            colors.highlightedColor = new Color(1f, 1f, 0.93f, 1f);
            colors.selectedColor = new Color(1f, 0.98f, 0.86f, 1f);
            colors.pressedColor = new Color(0.95f, 0.76f, 0.45f, 1f);
            colors.disabledColor = new Color(0.7f, 0.66f, 0.62f, 0.5f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            Shadow shadow = rect.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.04f, 0.02f, 0.08f, 0.45f);
            shadow.effectDistance = new Vector2(0f, -5f);
            Edge(rect, EdgeBrown);
            rect.gameObject.AddComponent<UiButtonFx>();

            Text text = Label(rect, "Caption", caption, 34, Ink, TextAnchor.MiddleCenter, FontStyle.Bold, false);
            text.rectTransform.Fill(4f);
            return button;
        }

        /// <summary>The Music and Sound sliders, bound to GameSettings by a VolumeControl (520 x 112, centred on <paramref name="position"/>).</summary>
        public static VolumeControl VolumeSliders(Transform parent, Vector2 anchor, Vector2 position)
        {
            RectTransform box = NewRect("Volume", parent).Place(anchor, Center, position, new Vector2(520f, 112f));
            VolumeControl control = box.gameObject.AddComponent<VolumeControl>();
            Wire.Set(control, "music", VolumeRow(box, "Music", new Vector2(0f, 28f), GameSettings.DefaultMusic));
            Wire.Set(control, "sound", VolumeRow(box, "Sound", new Vector2(0f, -28f), GameSettings.DefaultSound));
            return control;
        }

        static Slider VolumeRow(Transform parent, string caption, Vector2 position, float value)
        {
            RectTransform row = NewRect(caption + "Row", parent).Place(Center, Center, position, new Vector2(520f, 52f));
            Text label = Label(row, "Label", caption, 28, Cream, TextAnchor.MiddleLeft);
            label.rectTransform.Place(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(150f, 44f));
            return MakeSlider(row, caption + "Slider", new Vector2(1f, 0.5f), new Vector2(-180f, 0f), new Vector2(360f, 44f), value);
        }

        /// <summary>A labelled 0..1 slider: a dark track, a gold fill and a round cream handle.</summary>
        public static Slider MakeSlider(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, float value)
        {
            RectTransform root = NewRect(name, parent).Place(anchor, Center, position, size);
            Slider slider = root.gameObject.AddComponent<Slider>();

            Image background = Panel(root, "Background", new Color(0.04f, 0.03f, 0.09f, 0.75f));
            background.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            background.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            background.rectTransform.sizeDelta = new Vector2(0f, 14f);

            RectTransform fillArea = NewRect("Fill Area", root);
            fillArea.anchorMin = new Vector2(0f, 0.5f);
            fillArea.anchorMax = new Vector2(1f, 0.5f);
            fillArea.sizeDelta = new Vector2(-24f, 14f);
            Image fill = Panel(fillArea, "Fill", Gold);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.sizeDelta = new Vector2(12f, 0f);

            RectTransform handleArea = NewRect("Handle Slide Area", root);
            handleArea.anchorMin = Vector2.zero;
            handleArea.anchorMax = Vector2.one;
            handleArea.offsetMin = new Vector2(12f, 0f);
            handleArea.offsetMax = new Vector2(-12f, 0f);
            Image handle = Dot(handleArea, "Handle", Cream);
            handle.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            handle.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            handle.rectTransform.sizeDelta = new Vector2(34f, 34f);
            handle.raycastTarget = true;
            Shadow handleShadow = handle.gameObject.AddComponent<Shadow>();
            handleShadow.effectColor = new Color(0.04f, 0.02f, 0.08f, 0.5f);
            handleShadow.effectDistance = new Vector2(0f, -3f);

            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.value = value;
            ColorBlock colors = slider.colors;
            colors.highlightedColor = Color.white;
            colors.selectedColor = new Color(1f, 0.95f, 0.8f, 1f);
            colors.pressedColor = new Color(0.95f, 0.76f, 0.45f, 1f);
            slider.colors = colors;
            return slider;
        }
    }
}
