using UnityEngine;
using UnityEngine.InputSystem;

namespace OutOfService
{
    public class MarketPlayer : MonoBehaviour
    {
        public static MarketPlayer Instance { get; private set; }
        public Camera viewCamera;
        public Light flashlight;
        public GameObject bareHandsModel;
        public GameObject flashlightModel;
        public GameObject calculatorModel;
        public float Stamina { get; private set; } = 1f;
        public float Battery { get; private set; } = 1f;
        public int DamageStacks { get; private set; }
        public int SelectedSlot { get; private set; }
        CharacterController controller;
        MarketProduct heldProduct;
        float verticalLook;
        float gravityVelocity;
        float lastSprintTime;
        const float JumpHeight = 0.85f;
        const float Gravity = -18f;

        void Awake()
        {
            Instance = this;
            controller = GetComponent<CharacterController>();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            SelectSlot(0);
        }

        void Update()
        {
            if (Keyboard.current == null || MarketGame.Instance == null || MarketGame.Instance.finished || MarketGame.Instance.failed) return;
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = Cursor.lockState == CursorLockMode.Locked ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = Cursor.lockState != CursorLockMode.Locked;
            }
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
                return;
            }
            if (Keyboard.current.digit1Key.wasPressedThisFrame) SelectSlot(0);
            if (Keyboard.current.digit2Key.wasPressedThisFrame) SelectSlot(1);
            Vector2 mouse = Mouse.current == null ? Vector2.zero : Mouse.current.delta.ReadValue();
            bool inspecting=heldProduct && Mouse.current!=null && Mouse.current.rightButton.isPressed;
            if(heldProduct)heldProduct.SetInspecting(viewCamera,inspecting);
            if(inspecting){
                heldProduct.RotateHeld(mouse.x*.45f,-mouse.y*.45f);
                mouse=Vector2.zero;
            }
            transform.Rotate(Vector3.up, mouse.x * 0.12f);
            verticalLook = Mathf.Clamp(verticalLook - mouse.y * 0.12f, -78f, 78f);
            viewCamera.transform.localRotation = Quaternion.Euler(verticalLook, 0, 0);

            float x = (Keyboard.current.dKey.isPressed ? 1 : 0) - (Keyboard.current.aKey.isPressed ? 1 : 0);
            float z = (Keyboard.current.wKey.isPressed ? 1 : 0) - (Keyboard.current.sKey.isPressed ? 1 : 0);
            Vector3 move = (transform.right * x + transform.forward * z).normalized;
            bool running = Keyboard.current.leftShiftKey.isPressed && move.sqrMagnitude > 0 && Stamina > 0.05f;
            if (running)
            {
                Stamina = Mathf.Max(0f, Stamina - 0.28f * Time.deltaTime);
                lastSprintTime = Time.time;
            }
            else if (Time.time - lastSprintTime > 1.2f)
                Stamina = Mathf.Min(1f, Stamina + 0.18f * Time.deltaTime);
            bool wasGrounded = controller.isGrounded;
            float impactSpeed = -gravityVelocity;
            if (controller.isGrounded && gravityVelocity < 0) gravityVelocity = -1f;
            if (controller.isGrounded && Keyboard.current.spaceKey.wasPressedThisFrame)
                gravityVelocity = Mathf.Sqrt(2f * -Gravity * JumpHeight);
            gravityVelocity += Gravity * Time.deltaTime;
            controller.Move((move * (running ? 7f : 4.2f) + Vector3.up * gravityVelocity) * Time.deltaTime);
            if (!wasGrounded && controller.isGrounded && impactSpeed > 13.5f)
                TakeDamage((impactSpeed - 13.5f) * 7f);

            if (flashlight != null && flashlight.enabled)
            {
                Battery = Mathf.Max(0f, Battery - Time.deltaTime / 180f);
                if (Battery <= 0f)
                {
                    flashlight.enabled = false;
                    MarketGame.Instance.Show("손전등 배터리가 다 됐다. 건전지를 찾아라.", 4f);
                }
            }
            if (Keyboard.current.fKey.wasPressedThisFrame && flashlight != null && Battery > 0f && SelectedSlot == 1)
                flashlight.enabled = !flashlight.enabled;
#if UNITY_EDITOR
            if (Keyboard.current.kKey.wasPressedThisFrame) TakeDamage(25f);
#endif
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                if (heldProduct != null)
                {
                    heldProduct.Drop(viewCamera);
                    heldProduct = null;
                    return;
                }
                if (Physics.Raycast(viewCamera.transform.position, viewCamera.transform.forward, out RaycastHit hit, 3f))
                {
                    MarketSwingDoor door = hit.collider.GetComponentInParent<MarketSwingDoor>();
                    if (door != null) door.Toggle();
                    else
                    {
                        MarketProduct product = hit.collider.GetComponentInParent<MarketProduct>();
                        if (product != null)
                        {
                            heldProduct = product;
                            product.PickUp(viewCamera);
                            return;
                        }
                        MarketInteractable target = hit.collider.GetComponentInParent<MarketInteractable>();
                        if (target != null) target.Use();
                    }
                }
            }
        }

        public void Recharge(float amount)
        {
            Battery = Mathf.Clamp01(Battery + amount);
            MarketGame.Instance.Show("건전지를 교체했다.");
        }

        public void TakeDamage(float amount)
        {
            if (amount <= 0 || DamageStacks >= 3) return;
            DamageStacks++;
            MarketGame.Instance?.ShowDamage(DamageStacks);
            if (DamageStacks >= 3) MarketGame.Instance?.Lose();
        }

        void SelectSlot(int slot)
        {
            SelectedSlot = Mathf.Clamp(slot, 0, 1);
            if (bareHandsModel != null) bareHandsModel.SetActive(SelectedSlot == 0);
            if (flashlightModel != null) flashlightModel.SetActive(SelectedSlot == 1);
            if (calculatorModel != null) calculatorModel.SetActive(false);
            if (SelectedSlot != 1 && flashlight != null) flashlight.enabled = false;
        }
    }
}

