﻿using UnityEngine.UI;
using UnityEngine;

namespace Anarkila.DeveloperConsole
{
    public class ScrollRectMover : MonoBehaviour
    {
        private Vector2 cachedVector = Vector2.zero;
        private ScrollRect scrollRect;
        private bool scrollToBottom;

        private void Awake()
        {
            if (TryGetComponent(out ScrollRect rect))
            {
                scrollRect = rect;

                ConsoleSettings settings = ConsoleManager.GetSettings();
                if (settings != null)
                {
                    scrollRect.verticalScrollbarVisibility = settings.ScrollRectVisibility;
                }

                ConsoleEvents.RegisterConsoleScrollMoveEvent += ScrollToBottom;
            }
            else
            {
#if UNITY_EDITOR
                Debug.Log($"Gameobject {gameObject.name} doesn't have ScrollRect component!");
#endif
                ConsoleEvents.RegisterConsoleScrollMoveEvent -= ScrollToBottom;
                enabled = false;
                return;
            }
        }

        private void OnDestroy()
        {
            ConsoleEvents.RegisterConsoleScrollMoveEvent -= ScrollToBottom;
        }

        private void Start()
        {
            ConsoleSettings settings = ConsoleManager.GetSettings();

            if (settings != null)
            {
                scrollToBottom = settings.scrollToBottomOnEnable;
                scrollRect.scrollSensitivity = settings.scrollSensitivity;
                ScrollToBottom();
            }
        }

        private void OnDisable()
        {
            if (scrollToBottom)
            {
                ScrollToBottom();
            }
        }

        private void ScrollToBottom()
        {
            scrollRect.normalizedPosition = cachedVector;
        }
    }
}