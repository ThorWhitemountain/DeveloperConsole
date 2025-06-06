﻿#if UNITY_EDITOR

using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEngine;
using System;

namespace Anarkila.DeveloperConsole
{
    /// <summary>
    /// This class measures Unity Editor play button click to playable scene time,
    /// if consoleSettings printEditorDebugInfo is set to true.
    /// </summary>
    [ExecuteInEditMode]
    public class DebugEditorPlayTime : MonoBehaviour
    {
        public long playButtonTime;
        public long startTime;
        public bool called;

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        private void OnPlayModeChanged(PlayModeStateChange stateChange)
        {
            switch (stateChange)
            {
                case PlayModeStateChange.ExitingEditMode:
                    playButtonTime = DateTime.Now.Ticks;

                    // this needs to be called because this script is attached to Prefab
                    // Otherwise playButtonTime will reset on Play
                    PrefabUtility.RecordPrefabInstancePropertyModifications(this);
                    break;

                case PlayModeStateChange.EnteredPlayMode:
                    if (!ConsoleManager.GetSettings().printPlayButtonToSceneTime || called)
                    {
                        return;
                    }

                    startTime = DateTime.Now.Ticks;
                    long difference = (startTime - playButtonTime) / TimeSpan.TicksPerMillisecond;
                    string sceneName = SceneManager.GetActiveScene().name;

                    Console.Log($"Scene [{sceneName}] loaded in {difference} ms.");

                    called = true;
                    break;
            }
        }
    }
}

#endif