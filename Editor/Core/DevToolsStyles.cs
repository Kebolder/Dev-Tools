using UnityEditor;
using UnityEngine;

namespace Kebolder.DevTools.Editor.Core
{
    public static class DevToolsStyles
    {
        public static class Colors
        {
            // Light neutral for tab-style headers.
            public static readonly Color FoldoutHeaderBackground = new Color(0.345f, 0.345f, 0.345f, 1f);
            // Slightly darker to distinguish nested sections.
            public static readonly Color SubFoldoutHeaderBackground = new Color(0.28f, 0.28f, 0.28f, 1f);
        }

        public static class Typography
        {
            // Typography settings for foldout headers.
            public const int FoldoutHeaderFontSizeDelta = 2;
            public const FontStyle FoldoutHeaderFontStyle = FontStyle.Bold;
            // Typography settings for sub-foldout headers.
            public const int SubFoldoutHeaderFontSizeDelta = 1;
            public const FontStyle SubFoldoutHeaderFontStyle = FontStyle.Bold;
        }

        public static class Spacing
        {
            // Padding for foldout header content (left, right, top, bottom).
            public static readonly RectOffset FoldoutHeaderPadding = new RectOffset(8, 8, 4, 4);
            // Vertical spacing above and below foldout headers.
            public const float FoldoutMarginTop = 1f;
            public const float FoldoutMarginBottom = 1f;
            // Padding for sub-foldout header content (left, right, top, bottom).
            public static readonly RectOffset SubFoldoutHeaderPadding = new RectOffset(10, 8, 3, 3);
            // Vertical spacing above and below sub-foldout headers.
            public const float SubFoldoutMarginTop = 1f;
            public const float SubFoldoutMarginBottom = 1f;
            // General layout gaps inside modules.
            public const float SectionGap = 1f;
            public const float SubSectionGap = 1f;
            public const float FieldGroupGap = 1f;
            public const float ButtonGroupGap = 6f;
        }

        public static class Sizes
        {
            // Minimum height for foldout headers.
            public const float FoldoutHeaderMinHeight = 30f;
            // Minimum height for sub-foldout headers.
            public const float SubFoldoutHeaderMinHeight = 25f;
            // Square size for small icon-style buttons.
            public const float IconButtonSize = 22f;
        }

        private static GUIStyle _foldoutHeader;
        private static GUIStyle _subFoldoutHeader;

        public static GUIStyle FoldoutHeader
        {
            get
            {
                if (_foldoutHeader == null)
                {
                    _foldoutHeader = new GUIStyle(EditorStyles.boldLabel)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        fontSize = EditorStyles.boldLabel.fontSize + Typography.FoldoutHeaderFontSizeDelta,
                        fontStyle = Typography.FoldoutHeaderFontStyle,
                        padding = Spacing.FoldoutHeaderPadding
                    };
                }

                return _foldoutHeader;
            }
        }

        public static GUIStyle SubFoldoutHeader
        {
            get
            {
                if (_subFoldoutHeader == null)
                {
                    _subFoldoutHeader = new GUIStyle(EditorStyles.boldLabel)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        fontSize = EditorStyles.boldLabel.fontSize + Typography.SubFoldoutHeaderFontSizeDelta,
                        fontStyle = Typography.SubFoldoutHeaderFontStyle,
                        padding = Spacing.SubFoldoutHeaderPadding
                    };
                }

                return _subFoldoutHeader;
            }
        }
    }
}
