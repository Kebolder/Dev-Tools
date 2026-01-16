using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Kebolder.DevTools.Editor.Core;

namespace Kebolder.DevTools.Editor.Modules
{
    public static class AnimRepath
    {
        // Lower values render higher in the Dev Tools window.
        public const int Order = 20;

        private static GameObject _targetObject;
        private static AnimatorController _controller;
        private static GUIStyle _warningTableLabelStyle;
        private static int _clipSelection;
        private static bool _showOnlyMissing;
        private static bool _bulkOnlyMissing;
        private static string _bulkOriginal = string.Empty;
        private static string _bulkReplacement = string.Empty;
        private static Vector2 _tableScroll;
        private static readonly DevToolsGUI.TableColumn[] TableColumns =
        {
            DevToolsGUI.TableColumn.Fixed("Clip", 180f),
            DevToolsGUI.TableColumn.Fixed("Property", 220f),
            DevToolsGUI.TableColumn.Flexible("Path"),
            DevToolsGUI.TableColumn.Fixed("Actions", 80f, TextAnchor.MiddleCenter)
        };
        private static readonly Dictionary<string, string> EditablePaths = new Dictionary<string, string>();

        public static void Draw()
        {
            using (var foldout = DevToolsGUI.BeginFoldout("modules.anim_repath", "Anim Repath"))
            {
                if (!foldout.Expanded)
                {
                    return;
                }

                _targetObject = (GameObject)EditorGUILayout.ObjectField(
                    "Animator Object",
                    _targetObject,
                    typeof(GameObject),
                    true);
                if (_targetObject == null)
                {
                    _showOnlyMissing = false;
                    _bulkOnlyMissing = false;
                }

                _controller = (AnimatorController)EditorGUILayout.ObjectField(
                    "Animator Controller",
                    _controller,
                    typeof(AnimatorController),
                    false);

                var activeController = ResolveController();
                if (activeController == null)
                {
                    return;
                }

                var clips = GetControllerClips(activeController);
                if (clips.Count == 0)
                {
                    EditorGUILayout.LabelField("No animation clips found.");
                    return;
                }

                DrawClipSelector(clips);

                using (new EditorGUI.DisabledScope(_targetObject == null))
                {
                    _showOnlyMissing = EditorGUILayout.Toggle("Show only missing", _showOnlyMissing);
                }

                var selectedClips = GetSelectedClips(clips).ToList();

                EditorGUILayout.Space(DevToolsStyles.Spacing.SectionGap);
                DrawClipsTable(selectedClips);

                EditorGUILayout.Space(DevToolsStyles.Spacing.SectionGap);
                DrawBulkReplace(selectedClips);
            }
        }

        private static AnimatorController ResolveController()
        {
            if (_targetObject != null)
            {
                var animator = _targetObject.GetComponent<Animator>();
                if (animator == null)
                {
                    EditorGUILayout.HelpBox("Selected object has no Animator component. Falling back to Animator Controller field.", MessageType.Info);
                    return _controller;
                }

                var runtime = animator.runtimeAnimatorController;
                if (runtime is AnimatorOverrideController overrideController)
                {
                    runtime = overrideController.runtimeAnimatorController;
                }

                var controller = runtime as AnimatorController;
                if (controller == null)
                {
                    EditorGUILayout.HelpBox("Animator does not use an AnimatorController. Falling back to Animator Controller field.", MessageType.Info);
                    return _controller;
                }

                return controller;
            }

            return _controller;
        }

        private static List<AnimationClip> GetControllerClips(AnimatorController controller)
        {
            return controller.animationClips
                .Where(clip => clip != null)
                .Distinct()
                .OrderBy(clip => clip.name)
                .ToList();
        }

        private static void DrawClipsTable(IReadOnlyList<AnimationClip> selectedClips)
        {
            if (_targetObject == null)
            {
                EditorGUILayout.HelpBox("Assign an Animator Object to validate missing paths/components.", MessageType.Info);
            }

            var entries = BuildEntries(selectedClips, _targetObject);
            _tableScroll = EditorGUILayout.BeginScrollView(_tableScroll, GUILayout.Height(300f));
            using (var table = DevToolsGUI.BeginTable("modules.anim_repath.table", TableColumns))
            {
                foreach (var entry in entries)
                {
                    table.Row(row =>
                    {
                        row.Cell(() =>
                        {
                            var style = entry.IsMissing ? WarningTableLabelStyle : DevToolsStyles.TableCellLabel;
                            GUILayout.Label(entry.Clip.name, style);
                        });
                        row.Cell(() =>
                        {
                            var style = entry.IsMissing ? WarningTableLabelStyle : DevToolsStyles.TableCellLabel;
                            GUILayout.BeginHorizontal();
                            if (entry.Icon != null)
                            {
                                GUILayout.Label(
                                    entry.Icon,
                                    GUILayout.Width(DevToolsStyles.Sizes.TableIconSize),
                                    GUILayout.Height(DevToolsStyles.Sizes.TableIconSize));
                                GUILayout.Space(4f);
                            }
                            GUILayout.Label(entry.Property, style);
                            GUILayout.EndHorizontal();
                        });
                        row.Cell(() =>
                        {
                            var edited = GetEditablePath(entry.Clip, entry.Path);
                            edited = EditorGUILayout.TextField(edited);
                            SetEditablePath(entry.Clip, entry.Path, edited);
                        });
                        row.Cell(() =>
                        {
                            var edited = GetEditablePath(entry.Clip, entry.Path);
                            var canApply = !string.Equals(edited, entry.Path, System.StringComparison.Ordinal);
                            using (new EditorGUI.DisabledScope(!canApply))
                            {
                                if (GUILayout.Button("Apply", GUILayout.Width(60f)))
                                {
                                    ApplyPathChange(entry.Clip, entry.Path, edited);
                                }
                            }
                        });
                    });
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private static void DrawClipSelector(IReadOnlyList<AnimationClip> clips)
        {
            var labels = new string[clips.Count + 1];
            labels[0] = "All animations";
            for (var i = 0; i < clips.Count; i++)
            {
                labels[i + 1] = clips[i].name;
            }

            if (_clipSelection >= labels.Length)
            {
                _clipSelection = 0;
            }

            _clipSelection = EditorGUILayout.Popup("Animation Filter", _clipSelection, labels);
        }

        private static IEnumerable<AnimationClip> GetSelectedClips(IReadOnlyList<AnimationClip> clips)
        {
            if (_clipSelection <= 0)
            {
                return clips;
            }

            var index = _clipSelection - 1;
            if (index < 0 || index >= clips.Count)
            {
                return clips;
            }

            return new[] { clips[index] };
        }

        private static void DrawBulkReplace(IReadOnlyList<AnimationClip> selectedClips)
        {
            using (var bulkFoldout = DevToolsGUI.BeginSubFoldout("modules.anim_repath.bulk", "Bulk Replace"))
            {
                if (!bulkFoldout.Expanded)
                {
                    return;
                }

                EditorGUILayout.HelpBox("Wildcard supported: use * to match any substring (example: path/old/* -> path/new/*).", MessageType.Info);

                _bulkOriginal = EditorGUILayout.TextField("Original", _bulkOriginal);
                _bulkReplacement = EditorGUILayout.TextField("Replacement", _bulkReplacement);

                using (new EditorGUI.DisabledScope(_targetObject == null))
                {
                    _bulkOnlyMissing = EditorGUILayout.Toggle("Apply only to missing", _bulkOnlyMissing);
                }

                var canApply = selectedClips.Count > 0 && !string.IsNullOrEmpty(_bulkOriginal);
                if (_bulkOnlyMissing && _targetObject == null)
                {
                    canApply = false;
                }

                using (new EditorGUI.DisabledScope(!canApply))
                {
                    if (GUILayout.Button("Apply Bulk"))
                    {
                        ApplyBulkReplace(selectedClips, _bulkOriginal, _bulkReplacement, _targetObject, _bulkOnlyMissing);
                    }
                }
            }
        }

        private static void ApplyBulkReplace(
            IReadOnlyList<AnimationClip> clips,
            string original,
            string replacement,
            GameObject root,
            bool onlyMissing)
        {
            foreach (var clip in clips)
            {
                if (clip == null)
                {
                    continue;
                }

                Undo.RecordObject(clip, "Bulk Repath Animation Clip");

                foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                {
                    ApplyBulkReplaceBinding(clip, binding, original, replacement, root, onlyMissing);
                }

                foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                {
                    ApplyBulkReplaceBinding(clip, binding, original, replacement, root, onlyMissing);
                }

                EditorUtility.SetDirty(clip);
            }
        }

        private static void ApplyBulkReplaceBinding(
            AnimationClip clip,
            EditorCurveBinding binding,
            string original,
            string replacement,
            GameObject root,
            bool onlyMissing)
        {
            var path = binding.path ?? string.Empty;
            if (onlyMissing && root != null && !IsBindingMissing(root, path, binding.type))
            {
                return;
            }

            if (!TryGetReplacementPath(path, original, replacement, out var newPath))
            {
                return;
            }

            if (string.Equals(path, newPath, System.StringComparison.Ordinal))
            {
                return;
            }

            if (binding.isPPtrCurve)
            {
                var curve = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                var updated = binding;
                updated.path = newPath;
                AnimationUtility.SetObjectReferenceCurve(clip, updated, curve);
                AnimationUtility.SetObjectReferenceCurve(clip, binding, null);
            }
            else
            {
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                var updated = binding;
                updated.path = newPath;
                AnimationUtility.SetEditorCurve(clip, updated, curve);
                AnimationUtility.SetEditorCurve(clip, binding, null);
            }

            UpdateEditablePathKey(clip, path, newPath);
        }

        private static bool TryGetReplacementPath(string path, string original, string replacement, out string newPath)
        {
            newPath = path;
            if (string.IsNullOrEmpty(original))
            {
                return false;
            }

            var starIndex = original.IndexOf('*');
            if (starIndex >= 0)
            {
                var prefix = original.Substring(0, starIndex);
                var suffix = original.Substring(starIndex + 1);
                if (!path.StartsWith(prefix, System.StringComparison.Ordinal) ||
                    !path.EndsWith(suffix, System.StringComparison.Ordinal))
                {
                    return false;
                }

                var middleLength = path.Length - prefix.Length - suffix.Length;
                if (middleLength < 0)
                {
                    return false;
                }

                var middle = path.Substring(prefix.Length, middleLength);
                newPath = replacement.IndexOf('*') >= 0
                    ? replacement.Replace("*", middle)
                    : replacement;
                return true;
            }

            if (!string.Equals(path, original, System.StringComparison.Ordinal))
            {
                return false;
            }

            newPath = replacement ?? string.Empty;
            return true;
        }

        private static IEnumerable<TableEntry> BuildEntries(IEnumerable<AnimationClip> clips, GameObject root)
        {
            var entries = new List<TableEntry>();
            foreach (var clip in clips)
            {
                if (clip == null)
                {
                    continue;
                }

                var seen = new HashSet<string>();
                var hasBindings = false;

                foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                {
                    hasBindings = true;
                    AddEntry(entries, seen, clip, binding, root);
                }

                foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                {
                    hasBindings = true;
                    AddEntry(entries, seen, clip, binding, root);
                }

                if (!hasBindings)
                {
                    entries.Add(new TableEntry(clip, "(none)", string.Empty, false, null));
                }
            }

            return entries;
        }

        private static void AddEntry(List<TableEntry> entries, HashSet<string> seen, AnimationClip clip, EditorCurveBinding binding, GameObject root)
        {
            var path = binding.path ?? string.Empty;
            var property = FormatProperty(path, binding.type);
            var icon = GetTypeIcon(binding.type);
            var key = $"{path}|{property}";
            if (!seen.Add(key))
            {
                return;
            }

            var isMissing = root != null && IsBindingMissing(root, path, binding.type);
            if (_showOnlyMissing && !isMissing)
            {
                return;
            }

            entries.Add(new TableEntry(clip, property, path, isMissing, icon));
        }

        private static bool IsBindingMissing(GameObject root, string path, System.Type componentType)
        {
            var rootTransform = root.transform;
            var targetTransform = string.IsNullOrEmpty(path) ? rootTransform : rootTransform.Find(path);
            if (targetTransform == null)
            {
                return true;
            }

            if (componentType == null)
            {
                return true;
            }

            if (componentType == typeof(GameObject) || componentType == typeof(Transform))
            {
                return false;
            }

            return targetTransform.GetComponent(componentType) == null;
        }

        private static string FormatProperty(string path, System.Type componentType)
        {
            var objectName = string.IsNullOrEmpty(path)
                ? "(root)"
                : path.Split('/').Last();
            return objectName;
        }

        private static Texture2D GetTypeIcon(System.Type componentType)
        {
            if (componentType == null)
            {
                return null;
            }

            return EditorGUIUtility.ObjectContent(null, componentType).image as Texture2D;
        }

        private static string GetEditablePath(AnimationClip clip, string path)
        {
            var key = $"{clip.GetInstanceID()}|{path}";
            if (!EditablePaths.TryGetValue(key, out var value))
            {
                value = path;
                EditablePaths[key] = value;
            }

            return value;
        }

        private static void SetEditablePath(AnimationClip clip, string path, string value)
        {
            var key = $"{clip.GetInstanceID()}|{path}";
            EditablePaths[key] = value ?? string.Empty;
        }

        private static void ApplyPathChange(AnimationClip clip, string oldPath, string newPath)
        {
            if (clip == null)
            {
                return;
            }

            oldPath = oldPath ?? string.Empty;
            newPath = newPath ?? string.Empty;
            if (string.Equals(oldPath, newPath, System.StringComparison.Ordinal))
            {
                return;
            }

            Undo.RecordObject(clip, "Repath Animation Clip");

            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (!string.Equals(binding.path ?? string.Empty, oldPath, System.StringComparison.Ordinal))
                {
                    continue;
                }

                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                var updated = binding;
                updated.path = newPath;
                AnimationUtility.SetEditorCurve(clip, updated, curve);
                AnimationUtility.SetEditorCurve(clip, binding, null);
            }

            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
            {
                if (!string.Equals(binding.path ?? string.Empty, oldPath, System.StringComparison.Ordinal))
                {
                    continue;
                }

                var curve = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                var updated = binding;
                updated.path = newPath;
                AnimationUtility.SetObjectReferenceCurve(clip, updated, curve);
                AnimationUtility.SetObjectReferenceCurve(clip, binding, null);
            }

            var oldKey = $"{clip.GetInstanceID()}|{oldPath}";
            var newKey = $"{clip.GetInstanceID()}|{newPath}";
            EditablePaths.Remove(oldKey);
            EditablePaths[newKey] = newPath;

            EditorUtility.SetDirty(clip);
        }

        private static void UpdateEditablePathKey(AnimationClip clip, string oldPath, string newPath)
        {
            var oldKey = $"{clip.GetInstanceID()}|{oldPath}";
            var newKey = $"{clip.GetInstanceID()}|{newPath}";
            EditablePaths.Remove(oldKey);
            EditablePaths[newKey] = newPath ?? string.Empty;
        }

        private readonly struct TableEntry
        {
            public readonly AnimationClip Clip;
            public readonly string Property;
            public readonly string Path;
            public readonly bool IsMissing;
            public readonly Texture2D Icon;

            public TableEntry(AnimationClip clip, string property, string path, bool isMissing, Texture2D icon)
            {
                Clip = clip;
                Property = property;
                Path = path;
                IsMissing = isMissing;
                Icon = icon;
            }
        }

        private static GUIStyle WarningTableLabelStyle
        {
            get
            {
                if (_warningTableLabelStyle == null)
                {
                    _warningTableLabelStyle = new GUIStyle(DevToolsStyles.TableCellLabel);
                    _warningTableLabelStyle.normal.textColor = DevToolsStyles.Colors.WarningText;
                }

                return _warningTableLabelStyle;
            }
        }
    }
}
