using UnityEngine;

namespace CardWar.Utilities
{
    public static class ProjectExtensions
    {
        public static void ResetTransform(this Transform transform)
        {
            if (transform == null)
            {
                Debug.LogWarning("ProjectExtensions: Cannot reset null transform");
                return;
            }

            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }
    }
}
