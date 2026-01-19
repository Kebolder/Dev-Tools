using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Kebolder.DevTools.Editor.Modules;

namespace Kebolder.DevTools.Editor
{
    public sealed class DevToolsWindow : EditorWindow
    {
        private const double PollIntervalSeconds = 0.5;
        private static MethodInfo _getActiveFolderPathMethod;
        private double _nextPollTime;
        private string _lastActiveProjectFolder;

        [MenuItem("Jax's Tools/Dev Tools")]
        public static void ShowWindow()
        {
            var window = GetWindow<DevToolsWindow>();
            window.titleContent = new GUIContent("Dev Tools");
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.update += Tick;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Tick;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Dev Tools", EditorStyles.boldLabel);
            DevToolsModules.DrawAll();
        }

        private void OnSelectionChange()
        {
            Repaint();
        }

        private void Tick()
        {
            if (EditorApplication.timeSinceStartup < _nextPollTime)
            {
                return;
            }

            _nextPollTime = EditorApplication.timeSinceStartup + PollIntervalSeconds;

            var activeFolder = GetActiveProjectFolder();
            if (activeFolder == null)
            {
                return;
            }

            if (string.Equals(activeFolder, _lastActiveProjectFolder, StringComparison.Ordinal))
            {
                return;
            }

            _lastActiveProjectFolder = activeFolder;
            Repaint();
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
    }
}
