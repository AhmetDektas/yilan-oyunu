using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Procedurally draws a small silhouette-style guest portrait (skin tone
/// circle face + hair/hat shape + eyes, "shifty" narrow eyes for
/// suspicious guests) straight into a Texture2D at runtime — no imported
/// art assets needed. Variation (skin tone, hair color/style, glasses)
/// comes from a hash of the guest's name, so the same guest always shows
/// the same face. Only ever reads visible traits (name, IsSuspicious) —
/// never IsTrouble — so it can't leak the hidden ground truth the
/// Papers-Please guessing game depends on hiding. Generated sprites are
/// cached by (name, suspicious) since the name pool is small and finite.
/// </summary>
public static class PortraitGenerator
{
    const int Size = 64;

    static readonly Color32[] SkinTones =
    {
        new Color32(255, 224, 189, 255),
        new Color32(224, 172, 105, 255),
        new Color32(141, 85, 36, 255),
        new Color32(255, 205, 148, 255),
    };

    static readonly Color32[] HairColors =
    {
        new Color32(40, 30, 20, 255),
        new Color32(90, 60, 30, 255),
        new Color32(20, 20, 20, 255),
        new Color32(180, 180, 180, 255),
        new Color32(150, 40, 25, 255),
    };

    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    public static Sprite Generate(string guestName, bool suspicious)
    {
        string key = guestName + "|" + suspicious;
        if (cache.TryGetValue(key, out var cached)) return cached;

        int hash = Mathf.Abs(guestName.GetHashCode());
        Color32 skin = SkinTones[hash % SkinTones.Length];
        Color32 hair = HairColors[(hash / SkinTones.Length) % HairColors.Length];
        int hairStyle = (hash / (SkinTones.Length * HairColors.Length)) % 3; // 0=full, 1=short, 2=hat
        bool glasses = (hash % 7) == 0;

        var pixels = new Color32[Size * Size];
        Color32 clear = new Color32(0, 0, 0, 0);
        for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

        Vector2 center = new Vector2(Size * 0.5f, Size * 0.47f);
        float faceR = Size * 0.30f;

        DrawHair(pixels, center, faceR, hairStyle, hair);
        FillCircle(pixels, center, faceR, skin);
        DrawEyes(pixels, center, faceR, suspicious);
        DrawMouth(pixels, center, faceR);
        if (glasses) DrawGlasses(pixels, center, faceR);

        var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.SetPixels32(pixels);
        tex.Apply();

        var sprite = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
        cache[key] = sprite;
        return sprite;
    }

    static void DrawHair(Color32[] px, Vector2 center, float faceR, int style, Color32 color)
    {
        switch (style)
        {
            case 0: // full/bushy — big circle offset upward, face drawn on top leaves a rim
                FillCircle(px, center + new Vector2(0, faceR * 0.35f), faceR * 1.18f, color);
                break;
            case 1: // short — thin cap, mostly hidden behind the face
                FillCircle(px, center + new Vector2(0, faceR * 0.65f), faceR * 1.05f, color);
                break;
            case 2: // hat — brim + crown
                FillEllipse(px, center + new Vector2(0, -faceR * 0.55f), faceR * 1.35f, faceR * 0.35f, color);
                FillEllipse(px, center + new Vector2(0, -faceR * 0.95f), faceR * 0.7f, faceR * 0.55f, color);
                break;
        }
    }

    static void DrawEyes(Color32[] px, Vector2 center, float faceR, bool suspicious)
    {
        Color32 dark = new Color32(30, 25, 20, 255);
        Vector2 left = center + new Vector2(-faceR * 0.42f, faceR * 0.05f);
        Vector2 right = center + new Vector2(faceR * 0.42f, faceR * 0.05f);

        if (suspicious)
        {
            // narrow "shifty" slit eyes
            FillEllipse(px, left, faceR * 0.16f, faceR * 0.05f, dark);
            FillEllipse(px, right, faceR * 0.16f, faceR * 0.05f, dark);
        }
        else
        {
            FillCircle(px, left, faceR * 0.11f, dark);
            FillCircle(px, right, faceR * 0.11f, dark);
        }
    }

    static void DrawMouth(Color32[] px, Vector2 center, float faceR)
    {
        Color32 dark = new Color32(120, 60, 50, 255);
        FillEllipse(px, center + new Vector2(0, -faceR * 0.45f), faceR * 0.22f, faceR * 0.06f, dark);
    }

    static void DrawGlasses(Color32[] px, Vector2 center, float faceR)
    {
        Color32 dark = new Color32(15, 15, 15, 230);
        Vector2 barCenter = center + new Vector2(0, faceR * 0.05f);
        FillEllipse(px, barCenter, faceR * 0.75f, faceR * 0.14f, dark);
    }

    static void FillCircle(Color32[] px, Vector2 center, float r, Color32 color)
        => FillEllipse(px, center, r, r, color);

    static void FillEllipse(Color32[] px, Vector2 center, float rx, float ry, Color32 color)
    {
        int minX = Mathf.Max(0, Mathf.FloorToInt(center.x - rx));
        int maxX = Mathf.Min(Size - 1, Mathf.CeilToInt(center.x + rx));
        int minY = Mathf.Max(0, Mathf.FloorToInt(center.y - ry));
        int maxY = Mathf.Min(Size - 1, Mathf.CeilToInt(center.y + ry));

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                float nx = (x + 0.5f - center.x) / rx;
                float ny = (y + 0.5f - center.y) / ry;
                if (nx * nx + ny * ny <= 1f) px[y * Size + x] = color;
            }
        }
    }
}
