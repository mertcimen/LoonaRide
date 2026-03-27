#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using _Main.Scripts.Containers;
using UnityEditor;
using UnityEngine;

namespace _Main.Scripts.LevelEditor
{
    [CustomPropertyDrawer(typeof(ColorType))]
    public class ColorTypeDrawer : PropertyDrawer
    {
        // Tekstür cache (renge göre 1x1 texture)
        private static readonly Dictionary<int, Texture2D> TexCache = new Dictionary<int, Texture2D>();

        // Popup style (değer değiştikçe background texture’ları güncellenecek)
        private static GUIStyle _popupStyle;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Label varsa PrefixLabel kullan, yoksa tüm alanı kullan
            Rect fieldRect;
            if (label != null && label != GUIContent.none && !string.IsNullOrEmpty(label.text))
                fieldRect = EditorGUI.PrefixLabel(position, label);
            else
                fieldRect = position;

            EnsureStyle();

            var names = property.enumDisplayNames;
            var idx = Mathf.Clamp(property.enumValueIndex, 0, names.Length - 1);

            var colorType = GetEnumValueSafe<ColorType>(property);
            ApplyPopupBackgroundColor(_popupStyle, colorType);

            EditorGUI.BeginChangeCheck();
            idx = EditorGUI.Popup(fieldRect, idx, names, _popupStyle);
            if (EditorGUI.EndChangeCheck())
                property.enumValueIndex = idx;

            EditorGUI.EndProperty();
        }

        private static void EnsureStyle()
        {
            if (_popupStyle != null) return;

            // EditorStyles.popup -> ok işareti/padding vs Unity ile uyumlu
            _popupStyle = new GUIStyle(EditorStyles.popup);

            // Unity 6000.x’te state textColor bazen saçmalayabiliyor; sabitleyelim
            var tc = EditorStyles.label.normal.textColor;
            _popupStyle.normal.textColor = tc;
            _popupStyle.hover.textColor = tc;
            _popupStyle.active.textColor = tc;
            _popupStyle.focused.textColor = tc;

            _popupStyle.onNormal.textColor = tc;
            _popupStyle.onHover.textColor = tc;
            _popupStyle.onActive.textColor = tc;
            _popupStyle.onFocused.textColor = tc;
        }

        private static void ApplyPopupBackgroundColor(GUIStyle style, ColorType t)
        {
            // Baz renk (alpha dahil)
            var baseCol = GetColor(t);

            // Hover / Active için ufak varyasyon
            var hoverCol = Tint(baseCol, +0.08f);
            var activeCol = Tint(baseCol, -0.08f);
            var focusedCol = Tint(baseCol, +0.05f);

            // Popup’ın kendi background’unu override ediyoruz
            style.normal.background   = GetTex(baseCol);
            style.hover.background    = GetTex(hoverCol);
            style.active.background   = GetTex(activeCol);
            style.focused.background  = GetTex(focusedCol);

            style.onNormal.background  = style.normal.background;
            style.onHover.background   = style.hover.background;
            style.onActive.background  = style.active.background;
            style.onFocused.background = style.focused.background;
        }

        private static Texture2D GetTex(Color c)
        {
            // Color32’ü int key’e çevir (cache)
            var c32 = (Color32)c;
            int key = (c32.a << 24) | (c32.r << 16) | (c32.g << 8) | c32.b;

            if (TexCache.TryGetValue(key, out var tex) && tex != null)
                return tex;

            tex = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point
            };
            tex.SetPixel(0, 0, c);
            tex.Apply();

            TexCache[key] = tex;
            return tex;
        }

        private static Color Tint(Color c, float delta)
        {
            // Hue sabit, RGB küçükçe oynasın
            return new Color(
                Mathf.Clamp01(c.r + delta),
                Mathf.Clamp01(c.g + delta),
                Mathf.Clamp01(c.b + delta),
                c.a
            );
        }

        private static TEnum GetEnumValueSafe<TEnum>(SerializedProperty enumProp) where TEnum : struct
        {
            if (enumProp == null) return default;

            int idx = Mathf.Clamp(enumProp.enumValueIndex, 0, enumProp.enumNames.Length - 1);
            string enumName = enumProp.enumNames[idx];

            if (Enum.TryParse(enumName, out TEnum parsed))
                return parsed;

            try { return (TEnum)Enum.ToObject(typeof(TEnum), enumProp.intValue); }
            catch { return default; }
        }

        private static Color GetColor(ColorType t)
        {
            return t switch
            {
                ColorType.None   => new Color(0.25f, 0.25f, 0.25f, 1.00f),
                ColorType.Red    => new Color(0.90f, 0.15f, 0.15f, 1.00f),
                ColorType.Orange => new Color(1.00f, 0.50f, 0.00f, 1.00f),
                ColorType.Yellow => new Color(1.00f, 0.90f, 0.10f, 1.00f),
                ColorType.Green  => new Color(0.15f, 0.75f, 0.20f, 1.00f),
                ColorType.Blue   => new Color(0.15f, 0.45f, 0.95f, 1.00f),
                ColorType.Purple => new Color(0.55f, 0.20f, 0.85f, 1.00f),
                ColorType.Pink   => new Color(1.00f, 0.20f, 0.60f, 1.00f),
                ColorType.Cyan   => new Color(0.00f, 0.85f, 0.95f, 1.00f),
                ColorType.Brown  => new Color(0.50f, 0.30f, 0.12f, 1.00f),
                _ => new Color(0.25f, 0.25f, 0.25f, 1.00f)
            };
        }

    }
}
#endif
