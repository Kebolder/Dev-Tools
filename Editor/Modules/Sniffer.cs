using System;
using UnityEditor;
using UnityEngine;
using Kebolder.DevTools.Editor.Core;

namespace Kebolder.DevTools.Editor.Modules
{
    public static class Sniffer
    {
        // Lower values render higher in the Dev Tools window.
        public const int Order = 99;

        private static GameObject _targetGO;
        private static int _componentIndex;
        private static Component[] _components = Array.Empty<Component>();
        private static bool _includeHidden;

        public static void Draw()
        {
            // Main foldout container for the module UI.
            using (var foldout = DevToolsGUI.BeginFoldout("modules.sniffer", "Component Sniffer"))
            {
                if (!foldout.Expanded)
                {
                    return;
                }

                EditorGUILayout.LabelField(
                    "Pick a GameObject, then choose a Component to dump.",
                    EditorStyles.wordWrappedLabel);
                EditorGUILayout.Space(8);

                _targetGO = (GameObject)EditorGUILayout.ObjectField(
                    "GameObject",
                    _targetGO,
                    typeof(GameObject),
                    true);

                if (GUILayout.Button("Use Active Selection"))
                {
                    _targetGO = Selection.activeGameObject;
                    _componentIndex = 0;
                }

                EditorGUILayout.Space(8);

                if (_targetGO == null)
                {
                    // Early exit when no target is selected.
                    EditorGUILayout.HelpBox("Assign a GameObject to continue.", MessageType.Info);
                    return;
                }

                _components = _targetGO.GetComponents<Component>();
                if (_components == null) _components = Array.Empty<Component>();
                if (_components.Length == 0)
                {
                    EditorGUILayout.HelpBox("No components found on this GameObject.", MessageType.Warning);
                    return;
                }

                var names = new string[_components.Length];
                for (var i = 0; i < _components.Length; i++)
                {
                    var c = _components[i];
                    names[i] = c == null ? "(Missing Script)" : c.GetType().FullName;
                }

                _componentIndex = Mathf.Clamp(_componentIndex, 0, _components.Length - 1);
                _componentIndex = EditorGUILayout.Popup("Component", _componentIndex, names);

                EditorGUILayout.Space(10);

                _includeHidden = EditorGUILayout.Toggle("Include Hidden Properties", _includeHidden);
                EditorGUILayout.Space(6);

                using (new EditorGUI.DisabledScope(_components[_componentIndex] == null))
                {
                    if (GUILayout.Button("Dump Serialized Info to Console"))
                    {
                        DumpComponent(_components[_componentIndex], _includeHidden);
                    }
                }

                EditorGUILayout.Space(6);
                EditorGUILayout.HelpBox(
                    "This prints all SerializedProperty paths, types, and values (where readable).\n" +
                    "Use the propertyPath strings as exact field names for editor scripts.",
                    MessageType.Info);
            }
        }

        private static void DumpComponent(Component comp, bool includeHidden)
        {
            if (comp == null) return;

            Debug.Log($"[ComponentSniffer] Dumping: {comp.GetType().FullName} on '{comp.gameObject.name}'");

            var so = new SerializedObject(comp);
            var it = so.GetIterator();

            var enterChildren = true;
            var count = 0;

            // Iterate all serialized properties (includes hidden Unity fields too).
            while (includeHidden ? it.Next(enterChildren) : it.NextVisible(enterChildren))
            {
                enterChildren = true;
                count++;

                var path = it.propertyPath;
                var type = it.propertyType.ToString();
                var value = ReadValue(it);
                var arrayInfo = it.isArray && it.propertyType != SerializedPropertyType.String
                    ? $" arraySize={it.arraySize}"
                    : string.Empty;

                Debug.Log($"[ComponentSniffer] {path} | {type}{arrayInfo} | {value}");
            }

            Debug.Log($"[ComponentSniffer] Done. Properties dumped: {count}");
        }

        private static string ReadValue(SerializedProperty p)
        {
            try
            {
                switch (p.propertyType)
                {
                    case SerializedPropertyType.Integer:
                        return p.intValue.ToString();
                    case SerializedPropertyType.Boolean:
                        return p.boolValue.ToString();
                    case SerializedPropertyType.Float:
                        return p.floatValue.ToString("G");
                    case SerializedPropertyType.String:
                        return p.stringValue ?? "";
                    case SerializedPropertyType.Color:
                        return p.colorValue.ToString();
                    case SerializedPropertyType.ObjectReference:
                        return p.objectReferenceValue
                            ? $"{p.objectReferenceValue.name} ({p.objectReferenceValue.GetType().Name})"
                            : "null";
                    case SerializedPropertyType.LayerMask:
                        return p.intValue.ToString();
                    case SerializedPropertyType.Enum:
                        return p.enumDisplayNames != null &&
                               p.enumValueIndex >= 0 &&
                               p.enumValueIndex < p.enumDisplayNames.Length
                            ? p.enumDisplayNames[p.enumValueIndex]
                            : p.enumValueIndex.ToString();
                    case SerializedPropertyType.Vector2:
                        return p.vector2Value.ToString("G");
                    case SerializedPropertyType.Vector3:
                        return p.vector3Value.ToString("G");
                    case SerializedPropertyType.Vector4:
                        return p.vector4Value.ToString("G");
                    case SerializedPropertyType.Rect:
                        return p.rectValue.ToString();
                    case SerializedPropertyType.ArraySize:
                        return p.intValue.ToString();
                    case SerializedPropertyType.Character:
                        return ((char)p.intValue).ToString();
                    case SerializedPropertyType.AnimationCurve:
                        return p.animationCurveValue != null ? "AnimationCurve" : "null";
                    case SerializedPropertyType.Bounds:
                        return p.boundsValue.ToString();
                    case SerializedPropertyType.Quaternion:
                        return p.quaternionValue.eulerAngles.ToString("G");
                    case SerializedPropertyType.ExposedReference:
                        return p.exposedReferenceValue ? p.exposedReferenceValue.name : "null";
                    case SerializedPropertyType.FixedBufferSize:
                        return p.fixedBufferSize.ToString();
                    case SerializedPropertyType.Vector2Int:
                        return p.vector2IntValue.ToString();
                    case SerializedPropertyType.Vector3Int:
                        return p.vector3IntValue.ToString();
                    case SerializedPropertyType.RectInt:
                        return p.rectIntValue.ToString();
                    case SerializedPropertyType.BoundsInt:
                        return p.boundsIntValue.ToString();
                    case SerializedPropertyType.ManagedReference:
                        return string.IsNullOrEmpty(p.managedReferenceFullTypename)
                            ? "null"
                            : p.managedReferenceFullTypename;

                    // Generic / unsupported: show summary.
                case SerializedPropertyType.Generic:
                default:
                    if (p.isArray && p.propertyType != SerializedPropertyType.String)
                        return $"Array (size={p.arraySize})";
                    return p.hasVisibleChildren ? "{...}" : "";
                }
            }
            catch (Exception e)
            {
                return $"<error reading: {e.Message}>";
            }
        }
    }
}
