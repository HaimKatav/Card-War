using UnityEngine;

public static class ProjectExtensions
{
    /// <summary>
    /// Resets a transform to identity values (zero position, identity rotation, unit scale)
    /// </summary>
    /// <param name="transform">The transform to reset</param>
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
