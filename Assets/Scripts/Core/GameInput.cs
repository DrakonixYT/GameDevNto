using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HangarGame.Core
{
    public static class GameInput
    {
#if ENABLE_INPUT_SYSTEM
        private static readonly Dictionary<KeyCode, Key> KeyMap = new Dictionary<KeyCode, Key>
        {
            { KeyCode.Q, Key.Q }, { KeyCode.E, Key.E },
            { KeyCode.W, Key.W }, { KeyCode.S, Key.S },
            { KeyCode.A, Key.A }, { KeyCode.D, Key.D },
            { KeyCode.LeftArrow, Key.LeftArrow }, { KeyCode.RightArrow, Key.RightArrow },
            { KeyCode.UpArrow, Key.UpArrow }, { KeyCode.DownArrow, Key.DownArrow },
            { KeyCode.Tab, Key.Tab }, { KeyCode.I, Key.I },
            { KeyCode.Escape, Key.Escape }, { KeyCode.Space, Key.Space }
        };
#endif

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

        public static bool TurnLeft => Pressed(KeyCode.Q);
        public static bool TurnRight => Pressed(KeyCode.E);
        public static bool PrevMech => Pressed(KeyCode.A) || Pressed(KeyCode.LeftArrow);
        public static bool NextMech => Pressed(KeyCode.D) || Pressed(KeyCode.RightArrow);
        public static bool FocusArmory => Pressed(KeyCode.Tab) || Pressed(KeyCode.I);
        public static bool Confirm => Pressed(KeyCode.Space);
        public static bool Cancel => Pressed(KeyCode.Escape);

        public static Vector2 WorkbenchRotate()
        {
            float x = 0f, y = 0f;
            if (Held(KeyCode.A) || Held(KeyCode.LeftArrow) || Held(KeyCode.Q)) x -= 1f;
            if (Held(KeyCode.D) || Held(KeyCode.RightArrow) || Held(KeyCode.E)) x += 1f;
            if (Held(KeyCode.W) || Held(KeyCode.UpArrow)) y += 1f;
            if (Held(KeyCode.S) || Held(KeyCode.DownArrow)) y -= 1f;
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

        private static bool Pressed(KeyCode k)
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && KeyMap.TryGetValue(k, out var key) && Keyboard.current[key].wasPressedThisFrame;
#else
            return Input.GetKeyDown(k);
#endif
        }

        private static bool Held(KeyCode k)
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && KeyMap.TryGetValue(k, out var key) && Keyboard.current[key].isPressed;
#else
            return Input.GetKey(k);
#endif
        }
    }
}
