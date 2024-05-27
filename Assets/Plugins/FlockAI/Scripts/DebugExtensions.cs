using UnityEngine;

namespace FlockAI
{
    public static class DebugExtensions
    {
        public static bool showDebugItems = false;

        public static void DrawLine(Vector3 start, Vector3 end, Color color, float duration = 0.0f, bool depthTest = true)
        {
            if (showDebugItems)
                Debug.DrawLine(start, end, color, duration, depthTest);
        }

        public static void DrawRay(Vector3 start, Vector3 dir, Color color, float duration = 0.0f, bool depthTest = true)
        {
            if (showDebugItems)
                Debug.DrawRay(start, dir, color, duration, depthTest);
        }

    }
}
