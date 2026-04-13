using UnityEngine;

namespace Anarkila.DeveloperConsole
{
    public class ExampleSceneRotateCube : MonoBehaviour
    {
        private float degreesPerSecond = 29.0f;
        private float amplitude = 0.5f;
        private float frequency = 1f;

        private Vector3 startPosition;
        private Vector3 tempPos;

        private void OnEnable()
        {
            startPosition = transform.position;
        }

        private void Update()
        {
            // Spin object around Y-Axis
            Vector3 rotation = new(0f, Time.deltaTime * degreesPerSecond, 0f);
            transform.Rotate(rotation, Space.World);

            // Float up/down
            tempPos = startPosition;
            tempPos.y += Mathf.Sin(Time.time * Mathf.PI * frequency) * amplitude;
            transform.position = tempPos;
        }

        // Multiple instances of same command is allowed.
        // In the example scenes, all 3 cubes get disabled/enabled when this is called.
        [ConsoleCommand("cube_enabled", "false")]
        private void EnableCubeRotation(bool newEnabled)
        {
            enabled = newEnabled;
        }
    }
}