using UnityEngine;
using UnityEditor;

namespace Anarkila.DeveloperConsole
{
    public class ConsoleEditorWindow : EditorWindow
    {
        private bool writingTextFile = false;

        [MenuItem("Tools/DeveloperConsole")]
        public static void Open()
        {
            ConsoleEditorWindow window = GetWindow<ConsoleEditorWindow>();
            GUIContent titleContent = new("Developer Console");
            window.titleContent = titleContent;
        }

        private void OnGUI()
        {
            DrawLayout();
        }

        private void DrawLayout()
        {
            GUILayout.Space(20);
            if (GUILayout.Button("Generate Command List", GUILayout.Height(30)))
            {
                if (writingTextFile)
                {
                    return;
                }

                writingTextFile = CreateTextFileUtility.GenerateCommandList();
            }
        }
    }
}