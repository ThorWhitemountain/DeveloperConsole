using UnityEngine;
using TMPro;

namespace Anarkila.DeveloperConsole
{
    public class ConsoleTextColor : MonoBehaviour
    {
        [SerializeField] private ConsoleGUIStyle style = ConsoleGUIStyle.Large;

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

            if (TryGetComponent(out TMP_Text textComponent))
            {
                Color color = style == ConsoleGUIStyle.Large
                    ? settings.consoleColors.largeGUITextColor
                    : settings.consoleColors.minimalGUITextColor;
                textComponent.color = color;
            }
#if UNITY_EDITOR
            else
            {
                Debug.Log($"Gameobject {gameObject.name} doesn't have TMP_text component!");
                ConsoleEvents.RegisterConsoleColorsChangedEvent -= SetColors;
                enabled = false;
                return;
            }
#endif
        }
    }
}