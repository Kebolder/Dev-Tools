using UnityEditor;
using UnityEngine;
using Kebolder.DevTools.Editor.Modules;

namespace Kebolder.DevTools.Editor
{
    public sealed class DevToolsWindow : EditorWindow
    {
        [MenuItem("Jax's Tools/Dev Tools")]
        public static void ShowWindow()
        {
            var window = GetWindow<DevToolsWindow>();
            window.titleContent = new GUIContent("Dev Tools");
            window.Show();
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
    }
}
