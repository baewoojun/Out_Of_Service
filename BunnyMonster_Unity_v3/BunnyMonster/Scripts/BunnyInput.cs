// Keyboard helper that works with the old Input Manager, the new Input System, or both.
using UnityEngine;
#if !ENABLE_LEGACY_INPUT_MANAGER && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace BunnyMonsterKit
{
    public static class BunnyInput
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        public static Vector2 Move()
        {
            return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        }

        public static bool Run()
        {
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        }

        public static bool AttackDown()
        {
            return Input.GetKeyDown(KeyCode.Space);
        }

        public static bool ScreamDown()
        {
            return Input.GetKeyDown(KeyCode.R);
        }
#elif ENABLE_INPUT_SYSTEM
        public static Vector2 Move()
        {
            var k = Keyboard.current;
            if (k == null) return Vector2.zero;
            float x = ((k.dKey.isPressed || k.rightArrowKey.isPressed) ? 1f : 0f)
                    - ((k.aKey.isPressed || k.leftArrowKey.isPressed) ? 1f : 0f);
            float y = ((k.wKey.isPressed || k.upArrowKey.isPressed) ? 1f : 0f)
                    - ((k.sKey.isPressed || k.downArrowKey.isPressed) ? 1f : 0f);
            return new Vector2(x, y);
        }

        public static bool Run()
        {
            var k = Keyboard.current;
            return k != null && (k.leftShiftKey.isPressed || k.rightShiftKey.isPressed);
        }

        public static bool AttackDown()
        {
            var k = Keyboard.current;
            return k != null && k.spaceKey.wasPressedThisFrame;
        }

        public static bool ScreamDown()
        {
            var k = Keyboard.current;
            return k != null && k.rKey.wasPressedThisFrame;
        }
#else
        public static Vector2 Move() { return Vector2.zero; }
        public static bool Run() { return false; }
        public static bool AttackDown() { return false; }
        public static bool ScreamDown() { return false; }
#endif
    }
}
