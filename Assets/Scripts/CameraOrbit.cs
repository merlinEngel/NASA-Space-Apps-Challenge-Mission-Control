using UnityEngine;
using UnityEngine.InputSystem;

namespace MissionGame
{
    // The camera orbits its parent (the planet holder that PlanetManager attaches it to).
    // Drag with the right mouse button = rotate, mouse wheel = zoom.
    public class CameraOrbit : MonoBehaviour
    {
        public float rotateSpeed = 0.2f;    // degrees per pixel of mouse movement
        public float zoomFactor = 1.1f;     // per mouse wheel notch
        public float minDistance = 0.02f;
        public float maxDistance = 50f;

        float yaw;
        float pitch = 89f;                  // start: from above, as before
        float distance;

        void LateUpdate()
        {
            // take the distance from the start position in the first frame
            if (distance <= 0f)
                distance = Mathf.Max(transform.localPosition.magnitude, minDistance);

            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.rightButton.isPressed)
                {
                    Vector2 delta = mouse.delta.ReadValue();
                    yaw += delta.x * rotateSpeed;
                    pitch = Mathf.Clamp(pitch - delta.y * rotateSpeed, -89f, 89f);
                }

                float scroll = mouse.scroll.ReadValue().y;
                if (scroll > 0f) distance /= zoomFactor;
                else if (scroll < 0f) distance *= zoomFactor;
                distance = Mathf.Clamp(distance, minDistance, maxDistance);
            }

            // position on a sphere around the parent, always looking at the center
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.SetLocalPositionAndRotation(rotation * new Vector3(0f, 0f, -distance), rotation);
        }
    }
}
