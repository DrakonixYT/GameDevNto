using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HangarGame.Core
{
    public static class GameInput
    {
        public static Vector2 PointerPos
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
                return Input.mousePosition;
#endif
            }
        }

        public static bool LeftDown
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
                return Input.GetMouseButtonDown(0);
#endif
            }
        }

        public static bool LeftHeld
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Mouse.current != null && Mouse.current.leftButton.isPressed;
#else
                return Input.GetMouseButton(0);
#endif
            }
        }

        public static bool RightHeld
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Mouse.current != null && (Mouse.current.rightButton.isPressed || Mouse.current.middleButton.isPressed);
#else
                return Input.GetMouseButton(1) || Input.GetMouseButton(2);
#endif
            }
        }

        public static Vector2 PointerDelta
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
#else
                return new Vector2(Input.GetAxis("Mouse X") * 10f, Input.GetAxis("Mouse Y") * 10f);
#endif
            }
        }

        public static float Scroll
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;
#else
                return Input.GetAxis("Mouse ScrollWheel");
#endif
            }
        }

        public static bool TurnLeft => WasPressed(KeyCode.Q);
        public static bool TurnRight => WasPressed(KeyCode.E);
        public static bool PrevMech => WasPressed(KeyCode.A) || WasPressed(KeyCode.LeftArrow);
        public static bool NextMech => WasPressed(KeyCode.D) || WasPressed(KeyCode.RightArrow);
        public static bool FocusArmory => WasPressed(KeyCode.Tab) || WasPressed(KeyCode.I);
        public static bool Confirm => WasPressed(KeyCode.Space);
        public static bool Cancel => WasPressed(KeyCode.Escape);

        public static Vector2 WorkbenchRotate()
        {
            float x = 0f;
            float y = 0f;
            if (IsHeld(KeyCode.A) || IsHeld(KeyCode.LeftArrow) || IsHeld(KeyCode.Q)) x -= 1f;
            if (IsHeld(KeyCode.D) || IsHeld(KeyCode.RightArrow) || IsHeld(KeyCode.E)) x += 1f;
            if (IsHeld(KeyCode.W) || IsHeld(KeyCode.UpArrow)) y += 1f;
            if (IsHeld(KeyCode.S) || IsHeld(KeyCode.DownArrow)) y -= 1f;
            return new Vector2(x, y);
        }

        public static void AddUIInputModule(GameObject es)
        {
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
        }

        private static bool WasPressed(KeyCode k)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null) return false;
            switch (k)
            {
                case KeyCode.Q: return Keyboard.current.qKey.wasPressedThisFrame;
                case KeyCode.E: return Keyboard.current.eKey.wasPressedThisFrame;
                case KeyCode.A: return Keyboard.current.aKey.wasPressedThisFrame;
                case KeyCode.D: return Keyboard.current.dKey.wasPressedThisFrame;
                case KeyCode.LeftArrow: return Keyboard.current.leftArrowKey.wasPressedThisFrame;
                case KeyCode.RightArrow: return Keyboard.current.rightArrowKey.wasPressedThisFrame;
                case KeyCode.Tab: return Keyboard.current.tabKey.wasPressedThisFrame;
                case KeyCode.I: return Keyboard.current.iKey.wasPressedThisFrame;
                case KeyCode.Escape: return Keyboard.current.escapeKey.wasPressedThisFrame;
                case KeyCode.Space: return Keyboard.current.spaceKey.wasPressedThisFrame;
                default: return false;
            }
#else
            return Input.GetKeyDown(k);
#endif
        }

        private static bool IsHeld(KeyCode k)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null) return false;
            switch (k)
            {
                case KeyCode.Q: return Keyboard.current.qKey.isPressed;
                case KeyCode.E: return Keyboard.current.eKey.isPressed;
                case KeyCode.A: return Keyboard.current.aKey.isPressed;
                case KeyCode.D: return Keyboard.current.dKey.isPressed;
                case KeyCode.W: return Keyboard.current.wKey.isPressed;
                case KeyCode.S: return Keyboard.current.sKey.isPressed;
                case KeyCode.LeftArrow: return Keyboard.current.leftArrowKey.isPressed;
                case KeyCode.RightArrow: return Keyboard.current.rightArrowKey.isPressed;
                case KeyCode.UpArrow: return Keyboard.current.upArrowKey.isPressed;
                case KeyCode.DownArrow: return Keyboard.current.downArrowKey.isPressed;
                default: return false;
            }
#else
            return Input.GetKey(k);
#endif
        }
    }
}
