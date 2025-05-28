using UnityEngine;
using UnityEngine.UI;

namespace Anarkila.DeveloperConsole
{
    public class ConsoleColorSetter : MonoBehaviour
    {
        [SerializeField] private ColorTarget style = ColorTarget.LargeGUIBackground;


        private enum ColorTarget
        {
            LargeGUIBorder,
            LargeGUIBackground,
            MinimalGUIBackground,
            ControlColor,
            LargeGUIScrollbarHandle,
            LargeGUIScrollbarBackground
        }

        private void Awake()
        {
            ConsoleEvents.RegisterConsoleColorsChangedEvent += SetColors;
        }

        private void Start()
        {
            SetColors();
        }

        private void OnDestroy()
        {
            ConsoleEvents.RegisterConsoleColorsChangedEvent -= SetColors;
        }

        private void SetColors()
        {
            ConsoleSettings settings = ConsoleManager.GetSettings();

            if (settings == null)
            {
                return;
            }

            if (TryGetComponent(out Image image))
            {
                switch (style)
                {
                    case ColorTarget.LargeGUIBorder:
                        image.color = settings.consoleColors.largeGUIBorderColor;
                        break;

                    case ColorTarget.LargeGUIBackground:
                        image.color = settings.consoleColors.largeGUIBackgroundColor;
                        break;

                    case ColorTarget.MinimalGUIBackground:
                        image.color = settings.consoleColors.minimalGUIBackgroundColor;
                        break;

                    case ColorTarget.ControlColor:
                        image.color = settings.consoleColors.largeGUIControlsColor;
                        break;

                    case ColorTarget.LargeGUIScrollbarHandle:
                        image.color = settings.consoleColors.largeGUIScrollbarHandleColor;
                        break;

                    case ColorTarget.LargeGUIScrollbarBackground:
                        image.color = settings.consoleColors.largeGUIScrollbarBackgroundColor;
                        break;
                }
            }
#if UNITY_EDITOR
            else
            {
                Debug.Log(string.Format("Gameobject {0} doesn't have Image component!", gameObject.name));
                ConsoleEvents.RegisterConsoleColorsChangedEvent -= SetColors;
                enabled = false;
            }
#endif
        }
    }
}