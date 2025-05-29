using UnityEngine.UI;
using UnityEngine;

namespace Anarkila.DeveloperConsole
{
    public class SubmitButton : MonoBehaviour
    {
        private Button button;

        private void Start()
        {
            if (TryGetComponent(out Button btn))
            {
                button = btn;
                button.onClick.AddListener(SubmitButtonClick);
            }
#if UNITY_EDITOR
            else
            {
                Debug.Log($"Gameobject: {gameObject.name} doesn't have Button component!");
            }
#endif
        }

        private void OnDestroy()
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
        }

        private void SubmitButtonClick()
        {
            ConsoleEvents.InputFieldSubmit();
        }
    }
}