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
            // Table header background for column labels.
            public static readonly Color TableHeaderBackground = new Color(0.22f, 0.22f, 0.22f, 1f);
            // Alternating row background for table readability.
            public static readonly Color TableRowEvenBackground = new Color(0.18f, 0.18f, 0.18f, 0.35f);
            // Alternating row background for table readability.
            public static readonly Color TableRowOddBackground = new Color(0.16f, 0.16f, 0.16f, 0.2f);
            // Table border and column separator color.
            public static readonly Color TableBorder = new Color(0.12f, 0.12f, 0.12f, 0.9f);
            // Warning highlight for data that needs attention.
            public static readonly Color WarningText = new Color(0.95f, 0.8f, 0.2f, 1f);
        }

        public static class Typography
        {
            // Typography settings for foldout headers.
            public const int FoldoutHeaderFontSizeDelta = 2;
            public const FontStyle FoldoutHeaderFontStyle = FontStyle.Bold;
            // Typography settings for sub-foldout headers.
            public const int SubFoldoutHeaderFontSizeDelta = 1;
            public const FontStyle SubFoldoutHeaderFontStyle = FontStyle.Bold;
            // Typography settings for table headers.
            public const int TableHeaderFontSizeDelta = 1;
            public const FontStyle TableHeaderFontStyle = FontStyle.Bold;
        }

        public static class Spacing
        {
            // Padding for foldout header content (left, right, top, bottom).
            public static readonly RectOffset FoldoutHeaderPadding = new RectOffset(8, 8, 4, 4);
            // Vertical spacing above and below foldout headers.
            public const float FoldoutMarginTop = 1f;
            public const float FoldoutMarginBottom = 1f;
            // Vertical padding inside expanded foldouts.
            public const float FoldoutContentPadding = 3f;
            // Padding for sub-foldout header content (left, right, top, bottom).
            public static readonly RectOffset SubFoldoutHeaderPadding = new RectOffset(10, 8, 3, 3);
            // Vertical spacing above and below sub-foldout headers.
            public const float SubFoldoutMarginTop = 1f;
            public const float SubFoldoutMarginBottom = 1f;
            // Padding inside table cells (left, right, top, bottom).
            public static readonly RectOffset TableCellPadding = new RectOffset(6, 6, 3, 3);
            // Padding inside the table container (left, right, top, bottom).
            public static readonly RectOffset TableContainerPadding = new RectOffset(2, 2, 2, 2);
            // Vertical spacing between table rows.
            public const float TableRowGap = 1f;
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
            // Minimum height for table header rows.
            public const float TableHeaderMinHeight = 27f;
            // Minimum height for table data rows.
            public const float TableRowMinHeight = 29f;
            // Thickness for table borders and column separators.
            public const float TableBorderWidth = 1f;
            // Square size for small icon-style buttons.
            public const float IconButtonSize = 22f;
            // Square size for table row icons.
            public const float TableIconSize = 19f;
        }

        private static GUIStyle _foldoutHeader;
        private static GUIStyle _subFoldoutHeader;
        private static GUIStyle _tableHeaderLabel;
        private static GUIStyle _tableCellLabel;
        private static GUIStyle _tableHeaderRow;
        private static GUIStyle _tableRowEven;
        private static GUIStyle _tableRowOdd;
        private static GUIStyle _tableContainer;
        private static Texture2D _tableBorderTexture;
        private static Texture2D _tableHeaderTexture;
        private static Texture2D _tableRowEvenTexture;
        private static Texture2D _tableRowOddTexture;

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

        public static GUIStyle TableHeaderLabel
        {
            get
            {
                if (_tableHeaderLabel == null)
                {
                    _tableHeaderLabel = new GUIStyle(EditorStyles.label)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        fontSize = EditorStyles.label.fontSize + Typography.TableHeaderFontSizeDelta,
                        fontStyle = Typography.TableHeaderFontStyle
                    };
                }

                return _tableHeaderLabel;
            }
        }

        public static GUIStyle TableCellLabel
        {
            get
            {
                if (_tableCellLabel == null)
                {
                    _tableCellLabel = new GUIStyle(EditorStyles.label)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        fontSize = EditorStyles.label.fontSize + 1
                    };
                }

                return _tableCellLabel;
            }
        }

        public static GUIStyle TableHeaderRow
        {
            get
            {
                if (_tableHeaderRow == null)
                {
                    _tableHeaderRow = new GUIStyle
                    {
                        normal = { background = GetTableHeaderTexture() },
                        padding = Spacing.TableCellPadding
                    };
                }

                return _tableHeaderRow;
            }
        }

        public static GUIStyle TableRowEven
        {
            get
            {
                if (_tableRowEven == null)
                {
                    _tableRowEven = new GUIStyle
                    {
                        normal = { background = GetTableRowEvenTexture() },
                        padding = Spacing.TableCellPadding
                    };
                }

                return _tableRowEven;
            }
        }

        public static GUIStyle TableRowOdd
        {
            get
            {
                if (_tableRowOdd == null)
                {
                    _tableRowOdd = new GUIStyle
                    {
                        normal = { background = GetTableRowOddTexture() },
                        padding = Spacing.TableCellPadding
                    };
                }

                return _tableRowOdd;
            }
        }

        public static GUIStyle TableContainer
        {
            get
            {
                if (_tableContainer == null)
                {
                    _tableContainer = new GUIStyle(GUI.skin.box)
                    {
                        normal = { background = GetTableBorderTexture() },
                        padding = Spacing.TableContainerPadding
                    };
                }

                return _tableContainer;
            }
        }

        private static Texture2D GetTableHeaderTexture()
        {
            if (_tableHeaderTexture == null)
            {
                _tableHeaderTexture = CreateSolidTexture(Colors.TableHeaderBackground);
            }

            return _tableHeaderTexture;
        }

        private static Texture2D GetTableRowEvenTexture()
        {
            if (_tableRowEvenTexture == null)
            {
                _tableRowEvenTexture = CreateSolidTexture(Colors.TableRowEvenBackground);
            }

            return _tableRowEvenTexture;
        }

        private static Texture2D GetTableRowOddTexture()
        {
            if (_tableRowOddTexture == null)
            {
                _tableRowOddTexture = CreateSolidTexture(Colors.TableRowOddBackground);
            }

            return _tableRowOddTexture;
        }

        private static Texture2D GetTableBorderTexture()
        {
            if (_tableBorderTexture == null)
            {
                _tableBorderTexture = CreateSolidTexture(Colors.TableBorder);
            }

            return _tableBorderTexture;
        }

        private static Texture2D CreateSolidTexture(Color color)
        {
            var texture = new Texture2D(1, 1)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
}
