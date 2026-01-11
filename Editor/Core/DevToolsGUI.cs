using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Kebolder.DevTools.Editor.Core
{
    public static class DevToolsGUI
    {
        private static readonly Dictionary<string, bool> FoldoutStates = new Dictionary<string, bool>();

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
            }

            return newExpanded;
        }

        public static void EndFoldout(bool expanded, bool boxed = true)
        {
            if (expanded)
            {
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
