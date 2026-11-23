// Tiny player for the demo scene: WASD/arrows to move, Shift to sprint, camera follows.
using UnityEngine;

namespace BunnyMonsterKit
{
    [RequireComponent(typeof(CharacterController))]
    public class BunnyDemoPlayer : MonoBehaviour
    {
        public float walkSpeed = 2.2f;
        public float sprintSpeed = 4.2f;
        public Vector3 cameraOffset = new Vector3(0f, 4.5f, -6.5f);

        CharacterController controller;
        Transform cam;
        float verticalSpeed;
        int timesCaught;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (Camera.main != null) cam = Camera.main.transform;
        }

        void Update()
        {
            Vector2 m = BunnyInput.Move();
            Vector3 dir = new Vector3(m.x, 0f, m.y);
            if (dir.sqrMagnitude > 1f) dir.Normalize();
            float speed = BunnyInput.Run() ? sprintSpeed : walkSpeed;
            verticalSpeed = controller.isGrounded ? -1f : verticalSpeed - 20f * Time.deltaTime;
            controller.Move((dir * speed + Vector3.up * verticalSpeed) * Time.deltaTime);
            if (dir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(dir);
        }

        void LateUpdate()
        {
            if (cam == null) return;
            cam.position = transform.position + cameraOffset;
            cam.LookAt(transform.position + Vector3.up * 1.0f);
        }

        // hooked to BunnyMonster.onAttackHit in the demo scene
        public void OnCaught()
        {
            timesCaught++;
            Debug.Log("[BunnyMonster demo] Caught by the bunny! (" + timesCaught + ")");
        }
    }
}
