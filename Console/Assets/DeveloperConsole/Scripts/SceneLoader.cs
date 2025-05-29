using UnityEngine.SceneManagement;
using UnityEngine;

namespace Anarkila.DeveloperConsole
{
    public class SceneLoader : MonoBehaviour
    {
        private AsyncOperation asyncOperation;
        private bool isLoading;

        private void Awake()
        {
            enabled = false; // disable this script on start

            ConsoleEvents.RegisterSceneUnLoadByIndex += UnLoadSceneByIndexAsync;
            ConsoleEvents.RegisterSceneUnLoadByName += UnLoadSceneByNameAsync;
            ConsoleEvents.RegisterSceneLoadByIndex += LoadSceneByIndexAsync;
            ConsoleEvents.RegisterSceneLoadByName += LoadSceneByNameAsync;
        }

        private void OnDestroy()
        {
            ConsoleEvents.RegisterSceneUnLoadByIndex -= UnLoadSceneByIndexAsync;
            ConsoleEvents.RegisterSceneUnLoadByName -= UnLoadSceneByNameAsync;
            ConsoleEvents.RegisterSceneLoadByIndex -= LoadSceneByIndexAsync;
            ConsoleEvents.RegisterSceneLoadByName -= LoadSceneByNameAsync;
        }

        private void Update()
        {
            if (!isLoading || asyncOperation == null)
            {
                return;
            }

            float progress = asyncOperation.progress;

            if (progress >= 0.9f)
            {
                asyncOperation.allowSceneActivation = true;
                enabled = false;
                isLoading = false;
            }
        }

        /// <summary>
        /// Try to load level by index
        /// scene must be included in Build settings!
        /// </summary>
        private void LoadSceneByIndexAsync(int index, LoadSceneMode mode)
        {
            if (isLoading)
            {
                return;
            }

            int sceneCount = SceneManager.sceneCountInBuildSettings;
            if (index > sceneCount || index < 0)
            {
#if UNITY_EDITOR
                Console.Log($"Scene index: [{index}] doesn't exist!");
#endif
                return;
            }

            if (mode == LoadSceneMode.Single)
            {
                ConsoleEvents.CloseConsole();
                asyncOperation = SceneManager.LoadSceneAsync(index, LoadSceneMode.Single);
                asyncOperation.allowSceneActivation = false;
                isLoading = true;
                enabled = true;
            }
            else
            {
                SceneManager.LoadSceneAsync(index, LoadSceneMode.Additive);
            }
        }

        /// <summary>
        /// Try to load level by name
        /// scene must be included in Build settings!
        /// </summary>
        private void LoadSceneByNameAsync(string sceneName)
        {
            if (isLoading)
            {
                return;
            }

            sceneName = ConsoleUtils.DeleteWhiteSpace(sceneName);
            if (Application.CanStreamedLevelBeLoaded(sceneName))
            {
                ConsoleEvents.CloseConsole();
                asyncOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            }
            else
            {
#if UNITY_EDITOR
                Console.Log($"Scene [{sceneName}] doesn't exist!");
#endif
                return;
            }

            asyncOperation.allowSceneActivation = false;
            isLoading = true;
            enabled = true;
        }


        /// <summary>
        /// Try to load level by index
        /// scene must be included in Build settings!
        /// </summary>
        private void UnLoadSceneByIndexAsync(int index)
        {
            if (SceneManager.sceneCount == 1)
            {
#if UNITY_EDITOR
                Console.Log($"Can't unload scene [{index}] because it's the only scene active!");
#endif
                return;
            }

            if (index > SceneManager.sceneCountInBuildSettings || index < 0)
            {
#if UNITY_EDITOR
                Console.Log($"Scene index: [{index}] doesn't exist!");
#endif
                return;
            }

            SceneManager.UnloadSceneAsync(index);
        }

        /// <summary>
        /// Try to load level by index
        /// scene must be included in Build settings!
        /// </summary>
        private void UnLoadSceneByNameAsync(string sceneName)
        {
            if (SceneManager.sceneCount == 1)
            {
#if UNITY_EDITOR
                Console.Log($"Can't unload scene [{sceneName}] because it's the only scene active!");
#endif
                return;
            }

            sceneName = ConsoleUtils.DeleteWhiteSpace(sceneName);
            if (Application.CanStreamedLevelBeLoaded(sceneName))
            {
                SceneManager.UnloadSceneAsync(sceneName);
            }
        }
    }
}