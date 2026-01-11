using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Kebolder.DevTools.Editor.Core;

namespace Kebolder.DevTools.Editor.Modules
{
    public static class ConstraintTool
    {
        // Lower values render higher in the Dev Tools window.
        public const int Order = 10;

        private static GameObject _constrainedGO;
        private static float _globalWeight = 1f;
        private static readonly List<GameObject> SourceGOs = new List<GameObject>();
        private static ReorderableList _sourcesList;

        public static void Draw()
        {
            using (var foldout = DevToolsGUI.BeginFoldout("modules.constraint_tool", "Constraint Tool"))
            {
                if (!foldout.Expanded)
                {
                    return;
                }

                EditorGUILayout.LabelField("Target", EditorStyles.boldLabel);
                _constrainedGO = (GameObject)EditorGUILayout.ObjectField(
                    "Target",
                    _constrainedGO,
                    typeof(GameObject),
                    true);

                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField("Constraint Type", EditorStyles.boldLabel);
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.Popup("Type", 0, new[] { "VRCParentConstraint" });
                }

                EditorGUILayout.Space(DevToolsStyles.Spacing.SectionGap);
                using (var settingsFoldout = DevToolsGUI.BeginSubFoldout("modules.constraint_tool.settings", "Settings"))
                {
                    if (settingsFoldout.Expanded)
                    {
                        _globalWeight = EditorGUILayout.Slider("Weight", _globalWeight, 0f, 1f);
                    }
                }

                EditorGUILayout.Space(DevToolsStyles.Spacing.SubSectionGap);
                using (var sourcesFoldout = DevToolsGUI.BeginSubFoldout("modules.constraint_tool.sources", "Sources"))
                {
                    if (sourcesFoldout.Expanded)
                    {
                        EnsureSourcesList();
                        _sourcesList.DoLayoutList();
                    }
                }

                EditorGUILayout.Space(DevToolsStyles.Spacing.ButtonGroupGap);

                var canRun = _constrainedGO != null && SourceGOs.Any(s => s != null);
                using (new EditorGUI.DisabledScope(!canRun))
                {
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Add Constraint"))
                    {
                        AddConstraint(withOffset: false);
                    }

                    if (GUILayout.Button("Add Constraint w/ Offset"))
                    {
                        AddConstraint(withOffset: true);
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        private static void EnsureSourcesList()
        {
            if (_sourcesList != null) return;

            _sourcesList = new ReorderableList(SourceGOs, typeof(GameObject), true, true, true, true);
            _sourcesList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Sources");
            _sourcesList.elementHeight = EditorGUIUtility.singleLineHeight + 4f;
            _sourcesList.drawElementCallback = (rect, index, _, __) =>
            {
                rect.y += 2f;
                var element = SourceGOs[index];
                SourceGOs[index] = (GameObject)EditorGUI.ObjectField(rect, $"Source {index}", element, typeof(GameObject), true);
            };
            _sourcesList.onAddCallback = _ => SourceGOs.Add(null);
            _sourcesList.onRemoveCallback = list =>
            {
                if (list.index >= 0 && list.index < SourceGOs.Count)
                {
                    SourceGOs.RemoveAt(list.index);
                }
            };
        }

        private static void AddConstraint(bool withOffset)
        {
            var constraintType = FindTypeByFullName("VRC.SDK3.Dynamics.Constraint.Components.VRCParentConstraint")
                ?? FindTypeByName("VRCParentConstraint");

            if (constraintType == null)
            {
                EditorUtility.DisplayDialog(
                    "Missing VRCParentConstraint",
                    "Could not find VRCParentConstraint type. Ensure VRChat SDK is imported.",
                    "OK");
                return;
            }

            var validSources = SourceGOs.Where(s => s != null).ToList();
            if (validSources.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "No Sources",
                    "Add at least one source before creating the constraint.",
                    "OK");
                return;
            }

            Undo.IncrementCurrentGroup();
            var group = Undo.GetCurrentGroup();

            if (_constrainedGO.GetComponent(constraintType) != null)
            {
                EditorUtility.DisplayDialog(
                    "Constraint Already Exists",
                    "A constraint component already exists on the target. Remove it first if you want to add a new one.",
                    "OK");
                return;
            }

            Component comp = Undo.AddComponent(_constrainedGO, constraintType);

            var so = new SerializedObject(comp);

            // Disable while authoring to avoid snapping.
            SetBool(so, "IsActive", false);
            SetBool(so, "Locked", false);

            SetFloat(so, "GlobalWeight", _globalWeight);
            SetObject(so, "TargetTransform", _constrainedGO.transform);

            SetInt(so, "Sources.totalLength", validSources.Count);
            SetInt(so, "Sources.sourceCount", validSources.Count);
            SetInt(so, "sources.sourceCount", validSources.Count);
            SetInt(so, "SourceCount", validSources.Count);

            var overflow = so.FindProperty("Sources.overflowList") ?? so.FindProperty("sources.overflowList");
            if (overflow != null && overflow.isArray)
            {
                overflow.arraySize = 0;
            }

            var constrainedTr = _constrainedGO.transform;

            for (var i = 0; i < validSources.Count; i++)
            {
                var srcTr = validSources[i].transform;

                var sourceEl = GetVrcSourceElement(so, i);
                if (sourceEl == null)
                {
                    EditorUtility.DisplayDialog(
                        "Error",
                        $"Could not access source slot {i}. Expected Sources.source0..source15 and Sources.overflowList beyond that.",
                        "OK");
                    Undo.RevertAllDownToGroup(group);
                    return;
                }

                SetRelObject(sourceEl, "SourceTransform", srcTr);
                SetRelFloat(sourceEl, "Weight", 1f);

                if (withOffset)
                {
                    var posOff = -srcTr.localPosition;
                    var delta = Quaternion.Inverse(srcTr.rotation) * constrainedTr.rotation;
                    var rotOffEuler = delta.eulerAngles;

                    SetRelVector3(sourceEl, "ParentPositionOffset", posOff);
                    SetRelVector3(sourceEl, "ParentRotationOffset", rotOffEuler);
                }
                else
                {
                    // Clear offsets so sources attach directly when no offset is requested.
                    SetRelVector3(sourceEl, "ParentPositionOffset", Vector3.zero);
                    SetRelVector3(sourceEl, "ParentRotationOffset", Vector3.zero);
                }
            }

            SetBool(so, "Locked", true);

            SetBool(so, "IsActive", true);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(comp);

            Undo.CollapseUndoOperations(group);
        }

        private static SerializedProperty GetVrcSourceElement(SerializedObject so, int i)
        {
            if (i >= 0 && i <= 15)
            {
                return so.FindProperty($"Sources.source{i}") ?? so.FindProperty($"sources.source{i}");
            }

            var overflow = so.FindProperty("Sources.overflowList") ?? so.FindProperty("sources.overflowList");
            if (overflow == null || !overflow.isArray) return null;

            var idx = i - 16;
            if (idx < 0) return null;

            if (overflow.arraySize <= idx)
            {
                overflow.arraySize = idx + 1;
            }

            return overflow.GetArrayElementAtIndex(idx);
        }

        private static void SetBool(SerializedObject so, string name, bool v)
        {
            var p = so.FindProperty(name);
            if (p != null && p.propertyType == SerializedPropertyType.Boolean) p.boolValue = v;
        }

        private static void SetFloat(SerializedObject so, string name, float v)
        {
            var p = so.FindProperty(name);
            if (p != null && p.propertyType == SerializedPropertyType.Float) p.floatValue = v;
        }

        private static void SetInt(SerializedObject so, string name, int v)
        {
            var p = so.FindProperty(name);
            if (p != null && p.propertyType == SerializedPropertyType.Integer) p.intValue = v;
        }

        private static void SetObject(SerializedObject so, string name, UnityEngine.Object v)
        {
            var p = so.FindProperty(name);
            if (p != null && p.propertyType == SerializedPropertyType.ObjectReference) p.objectReferenceValue = v;
        }

        private static void SetRelObject(SerializedProperty parent, string name, UnityEngine.Object v)
        {
            var p = FindRelative(parent, name);
            if (p != null && p.propertyType == SerializedPropertyType.ObjectReference) p.objectReferenceValue = v;
        }

        private static void SetRelFloat(SerializedProperty parent, string name, float v)
        {
            var p = FindRelative(parent, name);
            if (p != null && p.propertyType == SerializedPropertyType.Float) p.floatValue = v;
        }

        private static void SetRelVector3(SerializedProperty parent, string name, Vector3 v)
        {
            var p = FindRelative(parent, name);
            if (p != null && p.propertyType == SerializedPropertyType.Vector3) p.vector3Value = v;
        }

        private static SerializedProperty FindRelative(SerializedProperty parent, string name)
        {
            var p = parent.FindPropertyRelative(name);
            if (p != null) return p;
            return parent.FindPropertyRelative(ToLowerCamel(name));
        }

        private static string ToLowerCamel(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            if (char.IsLower(name[0])) return name;
            return char.ToLowerInvariant(name[0]) + name.Substring(1);
        }

        private static Type FindTypeByFullName(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var t = asm.GetType(fullName, throwOnError: false);
                    if (t != null) return t;
                }
                catch
                {
                }
            }
            return null;
        }

        private static Type FindTypeByName(string typeName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var t = asm.GetTypes().FirstOrDefault(x => x.Name == typeName);
                    if (t != null) return t;
                }
                catch
                {
                }
            }
            return null;
        }
    }
}
