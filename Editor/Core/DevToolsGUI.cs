using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Kebolder.DevTools.Editor.Core
{
    public static class DevToolsGUI
    {
        private static readonly Dictionary<string, bool> FoldoutStates = new Dictionary<string, bool>();
        private static readonly Dictionary<TextAnchor, GUIStyle> TableHeaderLabelStyles = new Dictionary<TextAnchor, GUIStyle>();
        private static readonly Dictionary<TextAnchor, GUIStyle> TableCellLabelStyles = new Dictionary<TextAnchor, GUIStyle>();

        public static bool Foldout(string key, string label, bool defaultExpanded = true, bool boxed = true)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Foldout key must be non-empty.", nameof(key));
            }

            if (!FoldoutStates.TryGetValue(key, out var expanded))
            {
                expanded = EditorPrefs.GetBool(GetPrefsKey(key), defaultExpanded);
                FoldoutStates[key] = expanded;
            }

            var headerStyle = DevToolsStyles.FoldoutHeader;
            GUILayout.Space(DevToolsStyles.Spacing.FoldoutMarginTop);
            var headerRect = GUILayoutUtility.GetRect(
                GUIContent.none,
                headerStyle,
                GUILayout.ExpandWidth(true),
                GUILayout.MinHeight(DevToolsStyles.Sizes.FoldoutHeaderMinHeight));
            EditorGUI.DrawRect(headerRect, DevToolsStyles.Colors.FoldoutHeaderBackground);

            var newExpanded = DrawHeaderToggle(headerRect, label, headerStyle, expanded);
            GUILayout.Space(DevToolsStyles.Spacing.FoldoutMarginBottom);

            if (newExpanded != expanded)
            {
                FoldoutStates[key] = newExpanded;
                EditorPrefs.SetBool(GetPrefsKey(key), newExpanded);
            }

            if (newExpanded)
            {
                EditorGUI.indentLevel++;
                GUILayout.Space(DevToolsStyles.Spacing.FoldoutContentPadding);
            }

            return newExpanded;
        }

        public static void EndFoldout(bool expanded, bool boxed = true)
        {
            if (expanded)
            {
                GUILayout.Space(DevToolsStyles.Spacing.FoldoutContentPadding);
                EditorGUI.indentLevel--;
            }
        }

        public static FoldoutScope BeginFoldout(string key, string label, bool defaultExpanded = true, bool boxed = true)
        {
            return new FoldoutScope(Foldout(key, label, defaultExpanded, boxed), boxed);
        }

        public static bool SubFoldout(string key, string label, bool defaultExpanded = true)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Foldout key must be non-empty.", nameof(key));
            }

            if (!FoldoutStates.TryGetValue(key, out var expanded))
            {
                expanded = EditorPrefs.GetBool(GetPrefsKey(key), defaultExpanded);
                FoldoutStates[key] = expanded;
            }

            var headerStyle = DevToolsStyles.SubFoldoutHeader;
            GUILayout.Space(DevToolsStyles.Spacing.SubFoldoutMarginTop);
            var headerRect = GUILayoutUtility.GetRect(
                GUIContent.none,
                headerStyle,
                GUILayout.ExpandWidth(true),
                GUILayout.MinHeight(DevToolsStyles.Sizes.SubFoldoutHeaderMinHeight));
            EditorGUI.DrawRect(headerRect, DevToolsStyles.Colors.SubFoldoutHeaderBackground);

            var newExpanded = DrawHeaderToggle(headerRect, label, headerStyle, expanded);
            GUILayout.Space(DevToolsStyles.Spacing.SubFoldoutMarginBottom);

            if (newExpanded != expanded)
            {
                FoldoutStates[key] = newExpanded;
                EditorPrefs.SetBool(GetPrefsKey(key), newExpanded);
            }

            if (newExpanded)
            {
                EditorGUI.indentLevel++;
            }

            return newExpanded;
        }

        public static FoldoutScope BeginSubFoldout(string key, string label, bool defaultExpanded = true)
        {
            return new FoldoutScope(SubFoldout(key, label, defaultExpanded), boxed: false);
        }

        public static TableScope BeginTable(string key, IReadOnlyList<TableColumn> columns, bool drawHeader = true)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Table key must be non-empty.", nameof(key));
            }

            if (columns == null || columns.Count == 0)
            {
                throw new ArgumentException("Table must have at least one column.", nameof(columns));
            }

            var context = new TableContext(key, columns, drawHeader);
            return new TableScope(context);
        }

        private static string GetPrefsKey(string key)
        {
            return $"Kebolder.DevTools.Foldout.{key}";
        }

        private static bool DrawHeaderToggle(Rect headerRect, string label, GUIStyle headerStyle, bool expanded)
        {
            var newExpanded = expanded;
            if (GUI.Button(headerRect, GUIContent.none, GUIStyle.none))
            {
                newExpanded = !expanded;
            }
            GUI.Label(headerRect, label, headerStyle);
            return newExpanded;
        }

        private static GUIStyle GetAlignedStyle(GUIStyle baseStyle, TextAnchor alignment, Dictionary<TextAnchor, GUIStyle> cache)
        {
            if (baseStyle.alignment == alignment)
            {
                return baseStyle;
            }

            if (!cache.TryGetValue(alignment, out var style))
            {
                style = new GUIStyle(baseStyle)
                {
                    alignment = alignment
                };
                cache[alignment] = style;
            }

            return style;
        }

        // Vertically center any control within its cell regardless of row height.
        private static void DrawCentered(Action draw)
        {
            GUILayout.BeginVertical(GUILayout.ExpandHeight(true));
            GUILayout.FlexibleSpace();
            draw?.Invoke();
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }

        public readonly struct TableColumn
        {
            public readonly string Header;
            public readonly float Width;
            public readonly TextAnchor Alignment;

            public TableColumn(string header, float width = 0f, TextAnchor alignment = TextAnchor.MiddleLeft)
            {
                Header = header;
                Width = width;
                Alignment = alignment;
            }

            public static TableColumn Fixed(string header, float width, TextAnchor alignment = TextAnchor.MiddleLeft)
            {
                return new TableColumn(header, width, alignment);
            }

            public static TableColumn Flexible(string header, TextAnchor alignment = TextAnchor.MiddleLeft)
            {
                return new TableColumn(header, 0f, alignment);
            }
        }

        public readonly struct TableScope : IDisposable
        {
            private readonly TableContext _context;

            internal TableScope(TableContext context)
            {
                _context = context;
            }

            public void Row(Action<TableRow> drawCells)
            {
                if (drawCells == null)
                {
                    return;
                }

                _context.BeginRow();
                var row = new TableRow(_context);
                drawCells(row);
                _context.EndRow();
            }

            public void Dispose()
            {
                _context.Dispose();
            }
        }

        public readonly struct TableRow
        {
            private readonly TableContext _context;

            internal TableRow(TableContext context)
            {
                _context = context;
            }

            public void Cell(Action draw)
            {
                _context.BeginCell();
                DrawCentered(draw);
                _context.EndCell();
            }

            public void Label(string text)
            {
                _context.BeginCell();
                var column = _context.CurrentColumn;
                var style = GetAlignedStyle(DevToolsStyles.TableCellLabel, column.Alignment, TableCellLabelStyles);
                DrawCentered(() => GUILayout.Label(text ?? string.Empty, style));
                _context.EndCell();
            }
        }

        internal sealed class TableContext : IDisposable
        {
            private readonly IReadOnlyList<TableColumn> _columns;
            private readonly string _key;
            private readonly int _indentLevel;
            private int _rowIndex;
            private int _cellIndex;

            public TableContext(string key, IReadOnlyList<TableColumn> columns, bool drawHeader)
            {
                _key = key;
                _columns = columns;
                _rowIndex = 0;
                _cellIndex = 0;
                _indentLevel = EditorGUI.indentLevel;

                EditorGUI.indentLevel = 0;
                GUILayout.BeginVertical(DevToolsStyles.TableContainer);
                if (drawHeader)
                {
                    DrawHeader();
                }
            }

            public TableColumn CurrentColumn
            {
                get
                {
                    if (_cellIndex >= _columns.Count)
                    {
                        return _columns[_columns.Count - 1];
                    }

                    return _columns[_cellIndex];
                }
            }

            public void BeginRow()
            {
                var rowStyle = (_rowIndex % 2 == 0)
                    ? DevToolsStyles.TableRowEven
                    : DevToolsStyles.TableRowOdd;
                BeginRowInternal(rowStyle, DevToolsStyles.Sizes.TableRowMinHeight);
            }

            public void EndRow()
            {
                EndRowInternal();
                _rowIndex++;
            }

            public void BeginCell()
            {
                if (_cellIndex >= _columns.Count)
                {
                    throw new InvalidOperationException($"Too many cells drawn for table '{_key}'.");
                }

                var column = _columns[_cellIndex];
                if (column.Width > 0f)
                {
                    GUILayout.BeginVertical(GUILayout.Width(column.Width), GUILayout.ExpandHeight(true));
                }
                else
                {
                    GUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                }
            }

            public void EndCell()
            {
                GUILayout.EndVertical();
                _cellIndex++;
                if (_cellIndex < _columns.Count)
                {
                    DrawColumnSeparator();
                }
            }

            public void Dispose()
            {
                GUILayout.EndVertical();
                EditorGUI.indentLevel = _indentLevel;
            }

            private void DrawHeader()
            {
                BeginRowInternal(DevToolsStyles.TableHeaderRow, DevToolsStyles.Sizes.TableHeaderMinHeight);
                for (var i = 0; i < _columns.Count; i++)
                {
                    var column = _columns[i];
                    BeginCell();
                    var style = GetAlignedStyle(DevToolsStyles.TableHeaderLabel, column.Alignment, TableHeaderLabelStyles);
                    DrawCentered(() => GUILayout.Label(column.Header ?? string.Empty, style));
                    EndCell();
                }
                EndRowInternal();
            }

            private void BeginRowInternal(GUIStyle rowStyle, float minHeight)
            {
                GUILayout.BeginHorizontal(rowStyle, GUILayout.MinHeight(minHeight), GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                _cellIndex = 0;
            }

            private void EndRowInternal()
            {
                GUILayout.EndHorizontal();
                GUILayout.Space(DevToolsStyles.Spacing.TableRowGap);
            }

            private void DrawColumnSeparator()
            {
                var separatorRect = GUILayoutUtility.GetRect(
                    DevToolsStyles.Sizes.TableBorderWidth,
                    DevToolsStyles.Sizes.TableBorderWidth,
                    GUILayout.Width(DevToolsStyles.Sizes.TableBorderWidth),
                    GUILayout.ExpandHeight(true));
                EditorGUI.DrawRect(separatorRect, DevToolsStyles.Colors.TableBorder);
            }
        }

        public readonly struct FoldoutScope : IDisposable
        {
            private readonly bool _expanded;
            private readonly bool _boxed;

            public FoldoutScope(bool expanded, bool boxed)
            {
                _expanded = expanded;
                _boxed = boxed;
            }

            public bool Expanded => _expanded;

            public void Dispose()
            {
                EndFoldout(_expanded, _boxed);
            }
        }
    }
}
