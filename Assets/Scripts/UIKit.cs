using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Tiny code-driven uGUI toolkit: canvas, rounded panels, labels, buttons, sliders, progress bars.
/// Uses Unity's built-in font so no font asset or TMP import is required.
/// </summary>
public static class UIKit
{
    static Sprite roundSprite, circleSprite, whiteSprite;
    static Font font;

    public static readonly Color Gold = new Color(1f, 0.82f, 0.2f);
    public static readonly Color Green = new Color(0.2f, 0.72f, 0.3f);
    public static readonly Color Blue = new Color(0.2f, 0.5f, 0.95f);
    public static readonly Color Red = new Color(0.9f, 0.25f, 0.2f);
    public static readonly Color Dark = new Color(0.08f, 0.1f, 0.18f, 0.92f);

    public static Font Font
    {
        get
        {
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font;
        }
    }

    // ------------------------------------------------------------------ sprites

    static Sprite MakeRound(int size, int radius, bool sliced)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color32[] px = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                float a = Mathf.Clamp01(radius - d + 0.5f);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        Vector4 border = sliced ? new Vector4(radius, radius, radius, radius) : Vector4.zero;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
    }

    public static Sprite RoundSprite { get { if (roundSprite == null) roundSprite = MakeRound(48, 16, true); return roundSprite; } }
    public static Sprite CircleSprite { get { if (circleSprite == null) circleSprite = MakeRound(64, 32, false); return circleSprite; } }

    public static Sprite WhiteSprite
    {
        get
        {
            if (whiteSprite == null)
            {
                Texture2D t = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                Color32[] px = new Color32[16];
                for (int i = 0; i < 16; i++) px[i] = new Color32(255, 255, 255, 255);
                t.SetPixels32(px);
                t.Apply();
                whiteSprite = Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
            }
            return whiteSprite;
        }
    }

    // ------------------------------------------------------------------ canvas

    public static Canvas MakeCanvas(string name, int sortingOrder = 0)
    {
        GameObject go = new GameObject(name);
        Canvas c = go.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = sortingOrder;
        CanvasScaler s = go.AddComponent<CanvasScaler>();
        s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        s.referenceResolution = new Vector2(1920f, 1080f);
        s.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        s.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();
        return c;
    }

    public static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;
        GameObject go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        UnityEngine.InputSystem.UI.InputSystemUIInputModule module = go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        module.AssignDefaultActions();
#else
        go.AddComponent<StandaloneInputModule>();
#endif
    }

    // ------------------------------------------------------------------ rect helpers

    public static RectTransform Rect(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    public static RectTransform Stretch(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }

    public static Image FullImage(Transform parent, string name, Color color)
    {
        RectTransform rt = Stretch(parent, name);
        Image img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    // ------------------------------------------------------------------ widgets

    static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

    public static Image Panel(Transform parent, string name, Color color, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        RectTransform rt = Rect(parent, name, anchor, pivot, pos, size);
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = RoundSprite;
        img.type = Image.Type.Sliced;
        img.color = color;
        return img;
    }

    public static Image Dot(Transform parent, Color color, Vector2 anchor, Vector2 pos, float size)
    {
        RectTransform rt = Rect(parent, "Dot", anchor, Center, pos, new Vector2(size, size));
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = CircleSprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    public static Text Label(Transform parent, string text, int size, Color color, Vector2 anchor, Vector2 pivot,
        Vector2 pos, Vector2 box, TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Bold)
    {
        RectTransform rt = Rect(parent, "Label", anchor, pivot, pos, box);
        Text t = rt.gameObject.AddComponent<Text>();
        t.font = Font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.fontStyle = style;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        Shadow sh = rt.gameObject.AddComponent<Shadow>();
        sh.effectColor = new Color(0f, 0f, 0f, 0.6f);
        sh.effectDistance = new Vector2(2f, -2f);
        return t;
    }

    public static Button MakeButton(Transform parent, string label, Color color, Vector2 anchor, Vector2 pos, Vector2 size,
        UnityAction onClick, int fontSize = 40)
    {
        Image img = Panel(parent, label + "Button", color, anchor, Center, pos, size);
        Button b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        ColorBlock cb = b.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(0.9f, 0.9f, 0.9f);
        cb.pressedColor = new Color(0.7f, 0.7f, 0.7f);
        cb.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.8f);
        b.colors = cb;
        Label(img.transform, label, fontSize, Color.white, Center, Center, Vector2.zero, size);
        b.onClick.AddListener(() => AudioManager.Play("click"));
        if (onClick != null) b.onClick.AddListener(onClick);
        return b;
    }

    public static Slider MakeSlider(Transform parent, string label, Vector2 anchor, Vector2 pos, float width, float value,
        UnityAction<float> onChange)
    {
        Label(parent, label, 28, Color.white, anchor, Center, pos + new Vector2(0f, 34f), new Vector2(width, 36f));
        RectTransform root = Rect(parent, label + "Slider", anchor, Center, pos, new Vector2(width, 28f));
        Image bg = root.gameObject.AddComponent<Image>();
        bg.sprite = RoundSprite;
        bg.type = Image.Type.Sliced;
        bg.color = new Color(1f, 1f, 1f, 0.22f);

        RectTransform fillArea = Stretch(root, "Fill Area");
        fillArea.offsetMin = new Vector2(4f, 4f);
        fillArea.offsetMax = new Vector2(-4f, -4f);
        RectTransform fill = Stretch(fillArea, "Fill");
        Image fillImg = fill.gameObject.AddComponent<Image>();
        fillImg.sprite = RoundSprite;
        fillImg.type = Image.Type.Sliced;
        fillImg.color = Gold;

        RectTransform handleArea = Stretch(root, "Handle Slide Area");
        handleArea.offsetMin = new Vector2(14f, 0f);
        handleArea.offsetMax = new Vector2(-14f, 0f);
        RectTransform handle = Rect(handleArea, "Handle", new Vector2(0f, 0.5f), Center, Vector2.zero, new Vector2(38f, 38f));
        Image handleImg = handle.gameObject.AddComponent<Image>();
        handleImg.sprite = CircleSprite;
        handleImg.color = Color.white;

        Slider s = root.gameObject.AddComponent<Slider>();
        s.fillRect = fill;
        s.handleRect = handle;
        s.targetGraphic = handleImg;
        s.direction = Slider.Direction.LeftToRight;
        s.minValue = 0f;
        s.maxValue = 1f;
        s.value = value;
        if (onChange != null) s.onValueChanged.AddListener(onChange);
        return s;
    }

    public static void AudioSliders(Transform parent, Vector2 anchor, Vector2 pos, float width, float spacing)
    {
        MakeSlider(parent, "MASTER VOLUME", anchor, pos, width, AudioManager.Master, v => AudioManager.Master = v);
        MakeSlider(parent, "MUSIC VOLUME", anchor, pos + new Vector2(0f, spacing), width, AudioManager.MusicVolume, v => AudioManager.MusicVolume = v);
        MakeSlider(parent, "SFX VOLUME", anchor, pos + new Vector2(0f, spacing * 2f), width, AudioManager.SfxVolume, v => AudioManager.SfxVolume = v);
    }

    public static Image FillBar(Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color fillColor)
    {
        Image back = Panel(parent, "BarBack", new Color(0f, 0f, 0f, 0.5f), anchor, Center, pos, size);
        RectTransform fr = Stretch(back.transform, "BarFill");
        fr.offsetMin = new Vector2(4f, 4f);
        fr.offsetMax = new Vector2(-4f, -4f);
        Image fill = fr.gameObject.AddComponent<Image>();
        fill.sprite = WhiteSprite;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        fill.fillAmount = 0f;
        fill.color = fillColor;
        fill.raycastTarget = false;
        return fill;
    }
}
