using UnityEngine;
using UnityEngine.UI;

namespace Anarkila.DeveloperConsole
{
    public class CloseButton : MonoBehaviour
    {
        private Button button;

        private void Start()
        {
            if (TryGetComponent(out button))
            {
                button.onClick.AddListener(CloseButtonClicked);
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

        private void CloseButtonClicked()
        {
            ConsoleEvents.CloseConsole();
        }
    }
}