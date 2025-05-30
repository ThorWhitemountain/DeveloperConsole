using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace Anarkila.DeveloperConsole
{
    [CustomEditor(typeof(DeveloperConsole))]
    public class DeveloperConsoleEditor : Editor
    {
        private const string VERSION = "Developer Console 1.0.2";

        private string[] tabs = new string[]
            { "All Settings", "GUI Settings", "General Settings", "Keybindings", "Debug Settings" };

        private SerializedObject sTarget;
        private int toolbarTab;

        private List<SerializedProperty> guiSettings = new();
        private List<SerializedProperty> generalSettings = new();
        private List<SerializedProperty> keybindings = new();
        private List<SerializedProperty> debugSettings = new();

        private DeveloperConsole console;
        private bool renderCustomGUI = true;
        private int theme = 0;
        private string input;

        private void OnEnable()
        {
            console = (DeveloperConsole)target;
            sTarget = new SerializedObject(target);

            RegisterSerializedProperties();
        }

        private void RegisterSerializedProperties()
        {
            generalSettings.Clear();
            debugSettings.Clear();
            guiSettings.Clear();
            keybindings.Clear();

            List<string> fieldValues = console.settings.GetType().GetFields().Select(f => f.Name).ToList();
            string setting = "settings.";
            for (int i = 0; i < fieldValues.Count; i++)
            {
                string name = setting + fieldValues[i];
                if (0 <= i && i <= 8)
                {
                    AppendToList(guiSettings, name);
                }
                else if (9 <= i && i <= 34)
                {
                    AppendToList(generalSettings, name);
                }
                else if (35 <= i && i <= 38)
                {
                    AppendToList(keybindings, name);
                }
                else
                {
                    AppendToList(debugSettings, name);
                }
            }
        }

        private void AppendToList(List<SerializedProperty> list, string name)
        {
            list.Add(sTarget.FindProperty(name));
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField(VERSION, EditorStyles.boldLabel);

            renderCustomGUI = EditorGUILayout.Toggle("Custom GUI", renderCustomGUI);
            GUILayout.Space(20);

            if (!renderCustomGUI)
            {
                DrawDefaultInspector();
                return;
            }

            sTarget.Update();
            EditorGUI.BeginChangeCheck();

            toolbarTab = GUILayout.Toolbar(toolbarTab, tabs);
            GUILayout.Space(10);

            RenderTab();

            if (EditorGUI.EndChangeCheck())
            {
                sTarget.ApplyModifiedProperties();
            }

            RenderBottomButtons();
        }

        private void RenderTab()
        {
            switch (toolbarTab)
            {
                case 0: RenderAll(); break;
                case 1: RenderGUISettings(); break;
                case 2: RenderGeneralSettings(); break;
                case 3: RenderKeybindingSettings(); break;
                case 4: RenderDebugSettings(); break;
            }
        }

        private void RenderBottomButtons()
        {
            GUILayout.Space(20);
            if (GUILayout.Button("Reset Settings", GUILayout.Height(30)))
            {
                console.settings = new ConsoleSettings();
                EditorUtility.SetDirty(console.gameObject);
                Debug.Log("Settings reset to default.");
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Generate Command List", GUILayout.Height(30)))
            {
                CreateTextFileUtility.GenerateCommandList();
            }

            GUILayout.Space(10);
            input = GUILayout.TextField(input, 1);
            GUILayout.Space(5);
            if (GUILayout.Button("Get keycode from character", GUILayout.Height(30)))
            {
                if (string.IsNullOrEmpty(input))
                {
                    Debug.Log("Input is empty.");
                    input = "";
                }

                PrintKeyCodeEquivalent(input);
            }


            // Show 'Next GUI theme button' when playing in Editor
            if (Application.isPlaying)
            {
                GUILayout.Space(10);
                if (GUILayout.Button("Next GUI Theme", GUILayout.Height(30)))
                {
                    ++theme;
                    if (theme == 3)
                    {
                        theme = 0;
                    }

                    Console.SetGUITheme((ConsoleGUITheme)theme);
                }

                GUILayout.Space(10);
                if (GUILayout.Button("Write messages to file", GUILayout.Height(30)))
                {
                    string[] msgs = Console.GetConsoleMessagesArray();
                    TextFileWriter.WriteToFile(msgs);
                }
            }
        }

        private static void PrintKeyCodeEquivalent(string input)
        {
            Debug.Log($"Input: '{input}'");
            if (string.IsNullOrEmpty(input))
            {
                return;
            }

            char key = input[0];
            KeyCode code = (KeyCode)Enum.Parse(typeof(KeyCode), ((int)key).ToString(), true);
            Debug.Log(
                $"Keycode is : 'Keycode.{code}' - If you see a number it means the Keycode enum is missing that key");
        }

        private void RenderAll()
        {
            RenderGUISettings();
            RenderGeneralSettings();
            RenderKeybindingSettings();
            RenderDebugSettings();
        }

        private void RenderGUISettings()
        {
            bool customTheme = console.settings.interfaceTheme == ConsoleGUITheme.Custom;
            for (int i = 0; i < guiSettings.Count; i++)
            {
                if (!customTheme && i == 8)
                {
                    continue;
                }

                EditorGUILayout.PropertyField(guiSettings[i]);
            }
        }

        private void RenderGeneralSettings()
        {
            Render(generalSettings);
        }

        private void RenderKeybindingSettings()
        {
            Render(keybindings);
        }

        private void RenderDebugSettings()
        {
            Render(debugSettings);
        }

        private void Render(List<SerializedProperty> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == null)
                {
                    continue;
                }

                EditorGUILayout.PropertyField(list[i]);
            }
        }
    }
}