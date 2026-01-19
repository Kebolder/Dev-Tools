using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Kebolder.DevTools.Editor.Core;

namespace Kebolder.DevTools.Editor.Modules
{
    public static class ExpressionAdder
    {
        // Lower values render higher in the Dev Tools window.
        public const int Order = 30;

        private const string VrcExpressionsMenuTypeName = "VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu";
        private const string VrcExpressionParametersTypeName = "VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters";
        private static MethodInfo _getActiveFolderPathMethod;

        private static string _assetName = string.Empty;
        private static string _folderPath = "Assets";

        public static void Draw()
        {
            using (var foldout = DevToolsGUI.BeginFoldout("modules.expression_adder", "Expression Creator"))
            {
                if (!foldout.Expanded)
                {
                    return;
                }

                var contextFolder = GetFolderContext();
                _folderPath = contextFolder ?? _folderPath;

                EditorGUILayout.LabelField(
                    "Creates a basic FX AnimatorController plus VRC Expressions Menu + Parameters assets.",
                    EditorStyles.wordWrappedLabel);

                EditorGUILayout.Space(DevToolsStyles.Spacing.FieldGroupGap);

                _assetName = EditorGUILayout.TextField("Name", _assetName ?? string.Empty);
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField("Folder", _folderPath);
                }

                EditorGUILayout.Space(DevToolsStyles.Spacing.ButtonGroupGap);

                var canCreate = !string.IsNullOrWhiteSpace(_assetName);
                using (new EditorGUI.DisabledScope(!canCreate))
                {
                    if (GUILayout.Button("Create Expressions"))
                    {
                        if (TryPickFolder(out var folder))
                        {
                            _folderPath = folder;
                            CreateAssets(_folderPath, _assetName);
                        }
                    }
                }
            }
        }

        private static bool TryPickFolder(out string projectRelative)
        {
            projectRelative = _folderPath;
            var defaultFolder = GetFolderContext() ?? "Assets";

            var absoluteDefault = ProjectPathToAbsolute(defaultFolder);
            var absolute = EditorUtility.OpenFolderPanel("Choose Directory", absoluteDefault, string.Empty);
            if (string.IsNullOrEmpty(absolute))
            {
                return false;
            }

            var dataPath = Application.dataPath.Replace('\\', '/');
            var normalized = absolute.Replace('\\', '/');
            if (!normalized.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("Invalid Folder", "Please pick a folder inside this project's Assets folder.", "OK");
                return false;
            }

            projectRelative = "Assets" + normalized.Substring(dataPath.Length);
            if (!AssetDatabase.IsValidFolder(projectRelative))
            {
                EditorUtility.DisplayDialog("Invalid Folder", "Selected folder is not a valid Unity project folder.", "OK");
                return false;
            }

            return true;
        }

        private static string GetFolderContext()
        {
            var active = GetActiveProjectFolder();
            if (!string.IsNullOrEmpty(active))
            {
                return active;
            }

            var selectionFolder = GetSelectedProjectFolder();
            if (!string.IsNullOrEmpty(selectionFolder))
            {
                return selectionFolder;
            }

            return AssetDatabase.IsValidFolder(_folderPath) ? _folderPath : "Assets";
        }

        private static string GetActiveProjectFolder()
        {
            try
            {
                if (_getActiveFolderPathMethod == null)
                {
                    _getActiveFolderPathMethod = typeof(ProjectWindowUtil).GetMethod(
                        "GetActiveFolderPath",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                }

                if (_getActiveFolderPathMethod == null)
                {
                    return null;
                }

                var path = _getActiveFolderPathMethod.Invoke(null, null) as string;
                if (string.IsNullOrEmpty(path))
                {
                    return null;
                }

                path = path.Replace('\\', '/');
                return AssetDatabase.IsValidFolder(path) ? path : null;
            }
            catch
            {
                return null;
            }
        }

        private static string GetSelectedProjectFolder()
        {
            var obj = Selection.activeObject;
            if (obj == null) return null;

            var path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path)) return null;
            path = path.Replace('\\', '/');

            if (AssetDatabase.IsValidFolder(path))
            {
                return path;
            }

            var dir = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(dir)) return null;
            dir = dir.Replace('\\', '/');

            return AssetDatabase.IsValidFolder(dir) ? dir : null;
        }

        private static string ProjectPathToAbsolute(string projectRelative)
        {
            projectRelative = (projectRelative ?? string.Empty).Replace('\\', '/').Trim();
            if (string.IsNullOrEmpty(projectRelative)) return Application.dataPath;

            if (string.Equals(projectRelative, "Assets", StringComparison.OrdinalIgnoreCase))
            {
                return Application.dataPath;
            }

            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            if (projectRelative.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                return Path.GetFullPath(Path.Combine(projectRoot, projectRelative));
            }

            return projectRoot;
        }

        private static void CreateAssets(string selectedPath, string assetName)
        {
            assetName = (assetName ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(assetName))
            {
                EditorUtility.DisplayDialog("Error", "Please enter a component name.", "OK");
                return;
            }

            if (!AssetDatabase.IsValidFolder(selectedPath))
            {
                EditorUtility.DisplayDialog("Error", "Please select a valid folder inside Assets.", "OK");
                return;
            }

            if (assetName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                EditorUtility.DisplayDialog("Error", "Name contains invalid file name characters.", "OK");
                return;
            }

            var menuType = FindTypeByFullName(VrcExpressionsMenuTypeName);
            var parametersType = FindTypeByFullName(VrcExpressionParametersTypeName);
            if (menuType == null || parametersType == null)
            {
                EditorUtility.DisplayDialog(
                    "Missing VRChat SDK",
                    "VRC SDK types not found.\n\n" +
                    "- VRCExpressionsMenu\n" +
                    "- VRCExpressionParameters\n\n" +
                    "Import the VRChat SDK3 (Avatars) to enable this module.",
                    "OK");
                return;
            }

            var controllerPath = AssetDatabase.GenerateUniqueAssetPath($"{selectedPath}/{assetName} FX.controller");
            var menuPath = AssetDatabase.GenerateUniqueAssetPath($"{selectedPath}/{assetName} Menu.asset");
            var parametersPath = AssetDatabase.GenerateUniqueAssetPath($"{selectedPath}/{assetName} Parameters.asset");

            AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

            var menu = ScriptableObject.CreateInstance(menuType);
            var parameters = ScriptableObject.CreateInstance(parametersType);

            AssetDatabase.CreateAsset(menu, menuPath);
            AssetDatabase.CreateAsset(parameters, parametersPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Success",
                "Created assets:\n" +
                $"- {controllerPath}\n" +
                $"- {menuPath}\n" +
                $"- {parametersPath}",
                "OK");
        }

        private static Type FindTypeByFullName(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var type = asm.GetType(fullName, throwOnError: false);
                    if (type != null) return type;
                }
                catch
                {
                }
            }

            return null;
        }
    }
}
