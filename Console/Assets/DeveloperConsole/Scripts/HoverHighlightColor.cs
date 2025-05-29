using UnityEngine.UI;
using UnityEngine;

namespace Anarkila.DeveloperConsole
{
    public class HoverHighlightColor : MonoBehaviour
    {
        private void Awake()
        {
            ConsoleEvents.RegisterConsoleColorsChangedEvent += SetColors;
        }

        private void OnDestroy()
        {
            ConsoleEvents.RegisterConsoleColorsChangedEvent -= SetColors;
        }

        private void Start()
        {
            SetColors();
        }

        private void SetColors()
        {
            ConsoleSettings settings = ConsoleManager.GetSettings();
            if (settings == null)
            {
                return;
            }

            if (TryGetComponent(out Button button))
            {
                Color highlightColor = settings.consoleColors.largeGUIHighlightColor;
                ColorBlock colorVar = button.colors;
                colorVar.highlightedColor = highlightColor;
                colorVar.pressedColor = highlightColor;
                button.colors = colorVar;
            }
#if UNITY_EDITOR
            else
            {
                Debug.Log($"Gameobject {gameObject.name} doesn't have Button component!");
                ConsoleEvents.RegisterConsoleColorsChangedEvent -= SetColors;
                enabled = false;
                return;
            }
#endif
        }
    }
}