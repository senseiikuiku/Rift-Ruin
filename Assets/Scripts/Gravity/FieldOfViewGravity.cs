using UnityEngine;

namespace JU.CharacterSystem.AI.Gravity
{
    public class FieldOfViewGravity : MonoBehaviour
    {
        public FieldOfView Source;
        public Transform CharacterRoot;
        public Transform Head;

        Vector3 Up => CharacterRoot ? CharacterRoot.up : Vector3.up;

        Vector3 Forward
        {
            get
            {
                Transform t = Head ? Head : transform;
                return Vector3.ProjectOnPlane(t.forward, Up).normalized;
            }
        }

        public bool IsInView(Vector3 target)
        {
            if (Source == null || !Source.Enabled)
                return false;

            Vector3 center = Source.Center;
            Vector3 dir = target - center;
            dir = Vector3.ProjectOnPlane(dir, Up);

            if (Vector3.Angle(Forward, dir) > Source.Angle)
                return false;

            if (Physics.Linecast(
                center,
                target,
                Source.ObstaclesLayer,
                QueryTriggerInteraction.Ignore))
                return false;

            return true;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (Source == null) return;

            UnityEditor.Handles.color = Color.cyan;
            UnityEditor.Handles.DrawWireDisc(Source.Center, Up, Source.Distance);
            UnityEditor.Handles.DrawWireArc(
                Source.Center,
                Up,
                Forward,
                Source.Angle,
                Source.Distance);
        }
#endif
    }
}
