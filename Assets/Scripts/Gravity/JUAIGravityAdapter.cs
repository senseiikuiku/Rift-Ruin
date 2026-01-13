using UnityEngine;
using JU.CharacterSystem.AI;

[DisallowMultipleComponent]
public class JUAIGravityAdapter : MonoBehaviour
{
    [Header("References")]
    public JUCharacterAIBase AI;
    public Transform GravitySource; // thường = character root

    public Vector3 Up
    {
        get
        {
            if (GravitySource)
                return GravitySource.up;

            return transform.up;
        }
    }

    /// <summary>
    /// Project direction based on gravity-up
    /// </summary>
    public Vector3 ProjectMove(Vector3 dir)
    {
        if (dir.sqrMagnitude < 0.001f)
            return Vector3.zero;

        return Vector3.ProjectOnPlane(dir, Up).normalized;
    }

    /// <summary>
    /// Gravity-aware look direction
    /// </summary>
    public Vector3 ProjectLook(Vector3 dir)
    {
        if (dir.sqrMagnitude < 0.001f)
            return Vector3.zero;

        return Vector3.ProjectOnPlane(dir, Up).normalized;
    }
}
