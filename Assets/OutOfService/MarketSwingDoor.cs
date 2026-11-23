using UnityEngine;

namespace OutOfService
{
    public class MarketSwingDoor : MonoBehaviour
    {
        public float openAngle = 95f;
        public float speed = 180f;
        public bool isOpen;
        Quaternion closedRotation;
        Quaternion targetRotation;
        bool previousOpen, moving;

        void Awake()
        {
            closedRotation = transform.localRotation;
            previousOpen = isOpen;
            targetRotation = closedRotation * Quaternion.Euler(0f, isOpen ? openAngle : 0f, 0f);
            moving = Quaternion.Angle(closedRotation, targetRotation) > .001f;
        }

        void Update()
        {
            // Keep direct state changes working as well as Toggle(), including
            // reversing a door in mid-swing. Resting doors do not dirty transforms
            // and collider poses every frame.
            if (previousOpen != isOpen)
            {
                previousOpen = isOpen;
                targetRotation = closedRotation * Quaternion.Euler(0f, isOpen ? openAngle : 0f, 0f);
                moving = true;
            }
            if (!moving) return;
            var rotation = Quaternion.RotateTowards(transform.localRotation, targetRotation, speed * Time.deltaTime);
            moving = Quaternion.Angle(rotation, targetRotation) > .001f;
            transform.localRotation = moving ? rotation : targetRotation;
        }

        public void Toggle()
        {
            isOpen = !isOpen;
        }
    }
}
