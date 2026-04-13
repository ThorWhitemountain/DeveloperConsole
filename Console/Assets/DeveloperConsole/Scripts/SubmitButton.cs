using UnityEngine.UI;
using UnityEngine;

namespace Anarkila.DeveloperConsole
{
    [RequireComponent(typeof(Button))]
    public class SubmitButton : MonoBehaviour
    {
        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(SubmitButtonClick);
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(SubmitButtonClick);
            }
        }

        private void SubmitButtonClick()
        {
            ConsoleEvents.InputFieldSubmit();
        }
    }
}