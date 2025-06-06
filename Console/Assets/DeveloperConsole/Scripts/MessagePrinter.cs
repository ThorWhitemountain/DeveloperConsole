﻿using System.Collections.Generic;
using System.Threading;
using System.Text;
using UnityEngine;
using System;

namespace Anarkila.DeveloperConsole
{
#pragma warning disable 1998
    public static class MessagePrinter
    {
        private static Dictionary<LogType, string> LogTypes = new();
        private static List<TempMessage> messagesBeforeInitDone = new(32);
        private static ConsoleGUIStyle currentGUIStyle = ConsoleGUIStyle.Large;
        private static ConsoleSettings settings = new();
        private static StringBuilder sb = new();
        private static bool printMessageTimestamps = true;
        private static bool consoleInitialized;
        private static Color textColor = Color.white;
        private static bool initDone;
        private static long messageCount;

        /// <summary>
        /// Init Message Printer
        /// </summary>
        public static void Init()
        {
            if (initDone)
            {
                return;
            }

            foreach (LogType logType in Enum.GetValues(typeof(LogType)))
            {
                LogTypes.Add(logType, logType.ToString());
            }

            Application.logMessageReceived += UnityLogEvent;
            Application.quitting += OnDestroy;

            ConsoleEvents.RegisterConsoleInitializedEvent += ConsoleIsInitialized;
            ConsoleEvents.RegisterConsoleColorsChangedEvent += ColorsChanged;
            ConsoleEvents.RegisterGUIStyleChangeEvent += GUIStyleChanged;
            ConsoleEvents.RegisterSettingsChangedEvent += GetSettings;
            ConsoleEvents.RegisterDestroyEvent += ConsoleDestroyed;

            initDone = true;
        }

        private static void ColorsChanged()
        {
            textColor = settings.interfaceStyle == ConsoleGUIStyle.Large
                ? settings.consoleColors.largeGUITextColor
                : settings.consoleColors.minimalGUITextColor;
        }

        private static void ConsoleIsInitialized()
        {
            consoleInitialized = ConsoleManager.IsConsoleInitialized();
            GetSettings();
            if (messagesBeforeInitDone.Count != 0)
            {
                for (int i = 0; i < messagesBeforeInitDone.Count; i++)
                {
                    ConsoleEvents.Log(messagesBeforeInitDone[i].message, messagesBeforeInitDone[i].messageColor, true,
                        LogType.Log);
                }

                messagesBeforeInitDone.Clear();
            }

            textColor = settings.interfaceStyle == ConsoleGUIStyle.Large
                ? settings.consoleColors.largeGUITextColor
                : settings.consoleColors.minimalGUITextColor;
        }

        private static void GetSettings()
        {
            settings = ConsoleManager.GetSettings();
            printMessageTimestamps = settings.printMessageTimestamps;
        }

        private static void ConsoleDestroyed(float time)
        {
            OnDestroy();
        }

        /// <summary>
        /// OnDestroy callback from Unity Engine.
        /// </summary>
        private static void OnDestroy()
        {
            Application.logMessageReceived -= UnityLogEvent;
            Application.quitting -= OnDestroy;

            ConsoleEvents.RegisterConsoleInitializedEvent -= ConsoleIsInitialized;
            ConsoleEvents.RegisterConsoleColorsChangedEvent -= ColorsChanged;
            ConsoleEvents.RegisterGUIStyleChangeEvent -= GUIStyleChanged;
            ConsoleEvents.RegisterSettingsChangedEvent -= GetSettings;
            ConsoleEvents.RegisterDestroyEvent -= ConsoleDestroyed;

#if UNITY_EDITOR
            // for domain reload purposes

            if (settings != null && settings.printMessageCountOnStopPlay)
            {
                if (messageCount != 0)
                {
                    Debug.Log($"Debug.Log and Debug.LogError were called {messageCount} times.");
                }
            }

            LogTypes.Clear();
            messagesBeforeInitDone.Clear();
            currentGUIStyle = ConsoleGUIStyle.Large;
            sb = new StringBuilder();
            printMessageTimestamps = true;
            consoleInitialized = false;
            initDone = false;
            messageCount = 0;
#endif
        }

        private static void GUIStyleChanged(ConsoleGUIStyle newStyle)
        {
            currentGUIStyle = newStyle;
        }

        private static void UnityLogEvent(string input, string stackTrace, LogType type)
        {
            ConsoleEvents.UnityLog(input, stackTrace, type, textColor);
        }

        /// <summary>
        /// Add message prefix
        /// </summary>
        public static string AddMessagePrefix(string prefixMessage, string msg)
        {
            sb.Clear();
            sb.Append(ConsoleConstants.OPENBRACKET);
            sb.Append(prefixMessage);
            sb.Append(ConsoleConstants.CLOSEDBRACKET);
            sb.Append(msg);

            return sb.ToString();
        }

        /// <summary>
        /// Append stack trace into string depending on print setting
        /// </summary>
        public static string AppendStrackTrace(string message, string stackTrace, ConsoleLogOptions printOption)
        {
            switch (printOption)
            {
                case ConsoleLogOptions.LogWithoutExceptions:
                    return message;

                case ConsoleLogOptions.LogExceptionWithStackTrace:
                    return $"{ConsoleConstants.COLOR_RED_START}{message} {stackTrace} {ConsoleConstants.COLOR_END}";

                case ConsoleLogOptions.LogWithExceptions:
                    return $"{ConsoleConstants.COLOR_RED_START}{message} {ConsoleConstants.COLOR_END}";

                case ConsoleLogOptions.LogExceptionsWithStackTraceEditorOnly:
#if UNITY_EDITOR
                    return $"{ConsoleConstants.COLOR_RED_START}{message} {stackTrace} {ConsoleConstants.COLOR_END}";
#else
                    return null;
#endif
                case ConsoleLogOptions.LogWithExceptionsEditorOnly:
#if UNITY_EDITOR
                    return $"{ConsoleConstants.COLOR_RED_START}{message} {ConsoleConstants.COLOR_END}";
#else
                    return null;
#endif
                default:
                    return message;
            }
        }

        /// <summary>
        /// Print message to Developer Console
        /// </summary>
        public static void PrintLog(string text, Action<string, Color?> subscribers, Color? textColor = null,
            bool appendStackTrace = false, LogType logType = LogType.Error, string stackTrace = "",
            bool forceIgnoreTimestamp = false)
        {
            if (!ConsoleManager.IsRunningOnMainThread(Thread.CurrentThread)
                || !Application.isPlaying
                || currentGUIStyle == ConsoleGUIStyle.Minimal
                || settings.UnityLogOption == ConsoleLogOptions.DontPrintLogs)
            {
                return;
            }

            if (settings.UnityLogOption == ConsoleLogOptions.LogInfoOnly && logType != LogType.Log)
            {
                return;
            }

            if (appendStackTrace)
            {
                if (logType is LogType.Error or LogType.Exception)
                {
                    text = AppendStrackTrace(text, stackTrace, settings.UnityLogOption);
                }

                if (settings.printLogType && Debug.isDebugBuild)
                {
                    text = AddMessagePrefix(LogTypes[logType], text);
                }
            }

            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (!forceIgnoreTimestamp && printMessageTimestamps)
            {
                text = AddMessagePrefix(DateTime.Now.ToString(ConsoleConstants.DATETIMEFORMAT), text);
            }

            if (!consoleInitialized)
            {
                TempMessage temp = new();
                temp.message = text;
                temp.messageColor = textColor;
                messagesBeforeInitDone.Add(temp);
                return;
            }

            if (subscribers != null && Application.isPlaying)
            {
                subscribers.Invoke(text, textColor);
            }
        }
    }
}