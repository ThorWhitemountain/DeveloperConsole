using UnityEngine.EventSystems;
using UnityEngine;
using TMPro;

namespace Anarkila.DeveloperConsole
{

    [RequireComponent(typeof(TMP_Text))]
    [DefaultExecutionOrder(-9990)]
    public class ConsoleMessage : MonoBehaviour, IPointerClickHandler
    {
        private TMP_Text textComponent;
        private Color defaultColor = Color.white;

        private void Awake()
        {
            textComponent = GetComponent<TMP_Text>();

            ConsoleSettings settings = ConsoleManager.GetSettings();
            defaultColor = settings.interfaceStyle == ConsoleGUIStyle.Large
                ? settings.consoleColors.largeGUITextColor
                : settings.consoleColors.minimalGUITextColor;
        }

        public void SetMessage(string text, Color? textColor = null)
        {
            textComponent.text = text;
            textComponent.faceColor = textColor ?? defaultColor;

            transform.SetAsLastSibling();
            transform.localScale = Vector3.one;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                ConsoleEvents.ShowContextMenu(gameObject, eventData, textComponent.text);
            }
        }
    }
}