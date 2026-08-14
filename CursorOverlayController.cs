using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace NevarisCursorCustomizer;

internal static class CursorOverlayController
{
    internal const string DefaultStyle = "Halo";
    internal const string DefaultColor = "High Contrast White";
    internal const int MinimumSize = 24;
    internal const int MaximumSize = 96;
    internal const int MinimumThickness = 1;
    internal const int MaximumThickness = 8;
    internal static readonly string[] Styles =
    {
        "Halo",
        "High-Contrast Arrow",
        "Crosshair",
        "Target",
        "Dot"
    };
    internal static readonly string[] Colors =
    {
        "High Contrast White",
        "Gold",
        "Cyan",
        "Lime",
        "Magenta",
        "Red"
    };

    private const int TextureResolution = 128;
    private const float PulseDuration = 1.1f;
    private static GameObject _canvasObject;
    private static GameObject _cursorObject;
    private static GameObject _pulseObject;
    private static RectTransform _cursorRect;
    private static RectTransform _pulseRect;
    private static RawImage _cursorImage;
    private static RawImage _pulseImage;
    private static Texture2D _cursorTexture;
    private static Texture2D _pulseTexture;
    private static bool _worldReady;
    private static bool _settingsDirty = true;
    private static int _lastTickFrame = -1;
    private static float _pulseStartedAt = -10f;

    internal static void Initialize() => NormalizeSettings();

    internal static void EnterWorld()
    {
        _worldReady = true;
        EnsureUi();
        ApplySettings();
    }

    internal static void LeaveWorld()
    {
        _worldReady = false;
        DestroyUi();
    }

    internal static void Shutdown()
    {
        _worldReady = false;
        DestroyUi();
    }

    internal static void ApplySettings()
    {
        NormalizeSettings();
        _settingsDirty = true;
        if (_worldReady)
        {
            EnsureUi();
            RebuildVisuals();
        }
    }

    internal static void PreviewPulse()
    {
        if (!_worldReady)
            return;
        EnsureUi();
        _pulseStartedAt = Time.unscaledTime;
        if (_pulseObject is not null)
            _pulseObject.SetActive(true);
    }

    internal static void Tick()
    {
        if (!_worldReady || _lastTickFrame == Time.frameCount)
            return;
        _lastTickFrame = Time.frameCount;

        EnsureUi();
        if (_settingsDirty)
            RebuildVisuals();

        bool shouldDisplay = Plugin.Settings.Enabled.Value && ShouldShowForLockState();
        if (_cursorObject is not null && _cursorObject.activeSelf != shouldDisplay)
            _cursorObject.SetActive(shouldDisplay);

        Vector2 mouse = Input.mousePosition;
        if (shouldDisplay && _cursorRect is not null)
            PositionMarker(mouse);

        if (!IsTextInputFocused() && Plugin.Settings.FindCursorHotkey.Value.IsDown())
            PreviewPulse();

        UpdatePulse(mouse, shouldDisplay);
    }

    private static void EnsureUi()
    {
        if (!_worldReady || _canvasObject is not null)
            return;

        _canvasObject = new GameObject("NevarisCursorCustomizer.Canvas");
        Object.DontDestroyOnLoad(_canvasObject);
        Canvas canvas = _canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;

        _cursorObject = CreateRawImage("CursorMarker", _canvasObject.transform,
            out _cursorRect, out _cursorImage);
        _pulseObject = CreateRawImage("LocatorPulse", _canvasObject.transform,
            out _pulseRect, out _pulseImage);
        _pulseRect.pivot = new Vector2(0.5f, 0.5f);
        _pulseObject.SetActive(false);
        _settingsDirty = true;
    }

    private static GameObject CreateRawImage(string name, Transform parent,
        out RectTransform rect, out RawImage image)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        image = obj.AddComponent<RawImage>();
        image.raycastTarget = false;
        return obj;
    }

    private static void RebuildVisuals()
    {
        if (_cursorImage is null || _pulseImage is null)
            return;

        DestroyTexture(ref _cursorTexture);
        DestroyTexture(ref _pulseTexture);

        Color32 accent = ResolveColor(Plugin.Settings.Color.Value);
        int size = Mathf.Clamp(Plugin.Settings.Size.Value, MinimumSize, MaximumSize);
        int thickness = Mathf.Clamp(
            Plugin.Settings.Thickness.Value, MinimumThickness, MaximumThickness);
        bool outline = Plugin.Settings.DarkOutline.Value;
        string style = ResolveChoice(Plugin.Settings.Style.Value, Styles, DefaultStyle);

        _cursorTexture = CreateMarkerTexture(style, accent, size, thickness, outline);
        _pulseTexture = CreateMarkerTexture(DefaultStyle, accent, size, thickness, outline);
        _cursorImage.texture = _cursorTexture;
        _pulseImage.texture = _pulseTexture;
        _cursorImage.color = new Color(1f, 1f, 1f, Mathf.Clamp(Plugin.Settings.Opacity.Value, 0.4f, 1f));
        _pulseImage.color = new Color(1f, 1f, 1f, 0f);
        _cursorRect.sizeDelta = new Vector2(size, size);
        _cursorRect.pivot = style == "High-Contrast Arrow"
            ? new Vector2(0f, 1f)
            : new Vector2(0.5f, 0.5f);
        _settingsDirty = false;
    }

    private static void PositionMarker(Vector2 mouse)
    {
        string style = Plugin.Settings.Style.Value;
        if (style == "High-Contrast Arrow")
        {
            float size = Plugin.Settings.Size.Value;
            _cursorRect.anchoredPosition = mouse + new Vector2(-size * 0.04f, size * 0.04f);
        }
        else
        {
            _cursorRect.anchoredPosition = mouse;
        }
    }

    private static void UpdatePulse(Vector2 mouse, bool markerVisible)
    {
        if (_pulseObject is null || _pulseRect is null || _pulseImage is null)
            return;

        float elapsed = Time.unscaledTime - _pulseStartedAt;
        bool active = markerVisible && elapsed >= 0f && elapsed < PulseDuration;
        if (_pulseObject.activeSelf != active)
            _pulseObject.SetActive(active);
        if (!active)
            return;

        float progress = Mathf.Clamp01(elapsed / PulseDuration);
        float eased = 1f - Mathf.Pow(1f - progress, 4f);
        float baseSize = Mathf.Clamp(Plugin.Settings.Size.Value, MinimumSize, MaximumSize);
        float pulseSize = baseSize * Mathf.Lerp(1.15f, 3.2f, eased);
        _pulseRect.anchoredPosition = mouse;
        _pulseRect.sizeDelta = new Vector2(pulseSize, pulseSize);
        float opacity = Mathf.Clamp(Plugin.Settings.Opacity.Value, 0.4f, 1f)
            * (1f - progress);
        _pulseImage.color = new Color(1f, 1f, 1f, opacity);
    }

    private static bool ShouldShowForLockState()
    {
        if (!Plugin.Settings.HideWhenLocked.Value)
            return true;
        try { return Cursor.lockState != CursorLockMode.Locked; }
        catch { return true; }
    }

    private static bool IsTextInputFocused()
    {
        try
        {
            GameObject selected = EventSystem.current?.currentSelectedGameObject;
            TMP_InputField input = selected?.GetComponentInParent<TMP_InputField>();
            return input is not null && input.isFocused;
        }
        catch
        {
            return false;
        }
    }

    private static void NormalizeSettings()
    {
        Plugin.Settings.Style.Value = ResolveChoice(
            Plugin.Settings.Style.Value, Styles, DefaultStyle);
        Plugin.Settings.Color.Value = ResolveChoice(
            Plugin.Settings.Color.Value, Colors, DefaultColor);
        Plugin.Settings.Size.Value = Mathf.Clamp(
            Plugin.Settings.Size.Value, MinimumSize, MaximumSize);
        Plugin.Settings.Thickness.Value = Mathf.Clamp(
            Plugin.Settings.Thickness.Value, MinimumThickness, MaximumThickness);
        Plugin.Settings.Opacity.Value = Mathf.Clamp(Plugin.Settings.Opacity.Value, 0.4f, 1f);
    }

    private static string ResolveChoice(string value, string[] choices, string fallback)
    {
        foreach (string choice in choices)
            if (string.Equals(value, choice, StringComparison.OrdinalIgnoreCase))
                return choice;
        return fallback;
    }

    private static Color32 ResolveColor(string name) => name switch
    {
        "Gold" => new Color32(255, 202, 62, 255),
        "Cyan" => new Color32(58, 224, 255, 255),
        "Lime" => new Color32(119, 245, 89, 255),
        "Magenta" => new Color32(255, 92, 213, 255),
        "Red" => new Color32(255, 91, 79, 255),
        _ => new Color32(244, 247, 255, 255)
    };

    private static Texture2D CreateMarkerTexture(string style, Color32 accent,
        int displaySize, int thickness, bool darkOutline)
    {
        bool[] mask = BuildMask(style, displaySize, thickness);
        var pixels = new Color32[TextureResolution * TextureResolution];
        var transparent = new Color32(0, 0, 0, 0);
        var outline = new Color32(12, 15, 20, 245);
        int outlineRadius = darkOutline
            ? Mathf.Clamp(Mathf.RoundToInt(thickness * TextureResolution / (float)displaySize * 0.75f), 1, 8)
            : 0;

        for (int y = 0; y < TextureResolution; y++)
        for (int x = 0; x < TextureResolution; x++)
        {
            int index = y * TextureResolution + x;
            if (mask[index])
                pixels[index] = accent;
            else if (outlineRadius > 0 && HasMaskedNeighbor(mask, x, y, outlineRadius))
                pixels[index] = outline;
            else
                pixels[index] = transparent;
        }

        var texture = new Texture2D(TextureResolution, TextureResolution, TextureFormat.RGBA32, false);
        texture.name = $"NevarisCursorCustomizer.{style}";
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return texture;
    }

    private static bool[] BuildMask(string style, int displaySize, int thickness)
    {
        var mask = new bool[TextureResolution * TextureResolution];
        float stroke = Mathf.Max(1f / TextureResolution, thickness / (float)displaySize);
        Vector2[] arrow =
        {
            new(0.04f, 0.04f), new(0.04f, 0.84f), new(0.25f, 0.65f),
            new(0.41f, 0.95f), new(0.58f, 0.86f), new(0.42f, 0.58f),
            new(0.75f, 0.58f)
        };

        for (int y = 0; y < TextureResolution; y++)
        for (int x = 0; x < TextureResolution; x++)
        {
            float nx = (x + 0.5f) / TextureResolution;
            float ny = 1f - (y + 0.5f) / TextureResolution;
            float dx = nx - 0.5f;
            float dy = ny - 0.5f;
            float radius = Mathf.Sqrt(dx * dx + dy * dy);
            bool filled = style switch
            {
                "High-Contrast Arrow" => PointInPolygon(new Vector2(nx, ny), arrow),
                "Crosshair" => CrosshairMask(nx, ny, stroke),
                "Target" => Mathf.Abs(radius - 0.31f) <= stroke * 0.5f
                    || radius <= Mathf.Max(0.035f, stroke * 0.8f),
                "Dot" => radius <= 0.13f,
                _ => Mathf.Abs(radius - 0.34f) <= stroke * 0.5f
            };
            mask[y * TextureResolution + x] = filled;
        }
        return mask;
    }

    private static bool CrosshairMask(float x, float y, float stroke)
    {
        float dx = Mathf.Abs(x - 0.5f);
        float dy = Mathf.Abs(y - 0.5f);
        bool horizontal = dy <= stroke * 0.5f && dx >= 0.11f && dx <= 0.39f;
        bool vertical = dx <= stroke * 0.5f && dy >= 0.11f && dy <= 0.39f;
        return horizontal || vertical;
    }

    private static bool PointInPolygon(Vector2 point, Vector2[] polygon)
    {
        bool inside = false;
        for (int current = 0, previous = polygon.Length - 1;
             current < polygon.Length;
             previous = current++)
        {
            Vector2 a = polygon[current];
            Vector2 b = polygon[previous];
            bool crosses = (a.y > point.y) != (b.y > point.y)
                && point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x;
            if (crosses)
                inside = !inside;
        }
        return inside;
    }

    private static bool HasMaskedNeighbor(bool[] mask, int x, int y, int radius)
    {
        int minY = Math.Max(0, y - radius);
        int maxY = Math.Min(TextureResolution - 1, y + radius);
        int minX = Math.Max(0, x - radius);
        int maxX = Math.Min(TextureResolution - 1, x + radius);
        int radiusSquared = radius * radius;
        for (int sampleY = minY; sampleY <= maxY; sampleY++)
        for (int sampleX = minX; sampleX <= maxX; sampleX++)
        {
            int dx = sampleX - x;
            int dy = sampleY - y;
            if (dx * dx + dy * dy <= radiusSquared
                && mask[sampleY * TextureResolution + sampleX])
                return true;
        }
        return false;
    }

    private static void DestroyUi()
    {
        if (_canvasObject is not null)
            Object.Destroy(_canvasObject);
        DestroyTexture(ref _cursorTexture);
        DestroyTexture(ref _pulseTexture);
        _canvasObject = null;
        _cursorObject = null;
        _pulseObject = null;
        _cursorRect = null;
        _pulseRect = null;
        _cursorImage = null;
        _pulseImage = null;
        _pulseStartedAt = -10f;
        _settingsDirty = true;
    }

    private static void DestroyTexture(ref Texture2D texture)
    {
        if (texture is not null)
            Object.Destroy(texture);
        texture = null;
    }
}
