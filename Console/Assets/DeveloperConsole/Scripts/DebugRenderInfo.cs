#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace Anarkila.DeveloperConsole
{
    /// <summary>
    /// This script collects rendering information in Unity Editor
    /// if 'collectRenderInfoEditor' option is set to true.
    /// To print rendering information to console call 'debug.renderinfo'
    /// </summary>
    public class DebugRenderInfo : MonoBehaviour
    {
        private int HighestTrianglessCount;
        private int HighestDrawCallsCount;
        private int HighestVerticesCount;
        private int HighestBatchesCount;

        private int highestFPS;
        private float avgFPS;

        private void Awake()
        {
            ConsoleSettings settings = ConsoleManager.GetSettings();
            if (!settings.collectRenderInfoEditor)
            {
                Console.RemoveCommand("debug.renderinfo");
                enabled = false;
            }
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            // calculate low and high FPS
            float fps = 1.0f / deltaTime;
            if (fps > highestFPS)
            {
                highestFPS = (int)fps;
            }

            // calculate average FPS
            avgFPS += (deltaTime / Time.timeScale - avgFPS) * 0.03f;

            if (HighestDrawCallsCount < UnityStats.drawCalls)
            {
                HighestDrawCallsCount = UnityStats.drawCalls;
            }

            if (HighestBatchesCount < UnityStats.batches)
            {
                HighestBatchesCount = UnityStats.batches;
            }

            if (HighestTrianglessCount < UnityStats.triangles)
            {
                HighestTrianglessCount = UnityStats.triangles;
            }

            if (HighestVerticesCount < UnityStats.vertices)
            {
                HighestVerticesCount = UnityStats.vertices;
            }
        }

        [ConsoleCommand("debug_renderinfo", info: "Print rendering information (Editor only)")]
        private void PrintRenderInfo()
        {
            int currentTargetFPS = Application.targetFrameRate;
            string target;
            if (currentTargetFPS <= 0)
            {
                target = ConsoleConstants.UNLIMITED;
            }
            else
            {
                target = currentTargetFPS.ToString();
            }

            Console.LogEmpty();
            Debug.Log($"Current resolution is: {Screen.width} x {Screen.height}");
            Debug.Log($"Application target frame rate is set to: {target}");
            Debug.Log($"Highest FPS: {highestFPS} --- Avg FPS: {(int)(1f / avgFPS)}");
            Debug.Log($"Highest batches count: {HighestBatchesCount}");
            Debug.Log($"Highest draw call count: {HighestDrawCallsCount}");
            Debug.Log($"Highest vertices count: {HighestVerticesCount}");
            Debug.Log($"Highest triangles count: {HighestTrianglessCount}");
            Console.LogEmpty();
        }
    }
}

#endif