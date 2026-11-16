using UnityEngine;
using HangarGame.Core;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HangarGame.CameraControl
{
    public class RoomCamera : MonoBehaviour
    {
        public static RoomCamera Instance { get; private set; }
        private readonly Vector3 hangarPos = new Vector3(0f, 1.6f, 0f), wbPos = new Vector3(0f, 1.4f, 1.3f);
        private float shakeTime = 0f, shakeMag = 0f;

        private void Awake() => Instance = this;

        private void Update()
        {
            bool turnL = false, turnR = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                turnL = Keyboard.current.qKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame;
                turnR = Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame;
                if (GameManager.Instance.CurrentView != RoomView.WorkbenchFocus)
                {
                    turnL |= Keyboard.current.aKey.wasPressedThisFrame;
                    turnR |= Keyboard.current.dKey.wasPressedThisFrame;
                }
            }
#else
            turnL = Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.LeftArrow) || (GameManager.Instance.CurrentView != RoomView.WorkbenchFocus && Input.GetKeyDown(KeyCode.A));
            turnR = Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.RightArrow) || (GameManager.Instance.CurrentView != RoomView.WorkbenchFocus && Input.GetKeyDown(KeyCode.D));
#endif
            if (turnL || turnR) Toggle180();

            RoomView v = GameManager.Instance != null ? GameManager.Instance.CurrentView : RoomView.Hangar;
            Vector2 m = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null) m = Mouse.current.position.ReadValue();
#else
            m = Input.mousePosition;
#endif
            float nx = Mathf.Clamp((m.x / Mathf.Max(1, Screen.width)) * 2f - 1f, -1f, 1f);
            float ny = Mathf.Clamp((m.y / Mathf.Max(1, Screen.height)) * 2f - 1f, -1f, 1f);

            float p = (v == RoomView.WorkbenchFocus) ? (30f - ny * 5f) : (-ny * 7f);
            float y = (v == RoomView.DispatchConsole) ? (180f + nx * 12f) : (nx * (v == RoomView.WorkbenchFocus ? 8f : 12f));

            transform.position = Vector3.Lerp(transform.position, (v == RoomView.WorkbenchFocus) ? wbPos : hangarPos, Time.deltaTime * 6f);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(p, y, 0f), Time.deltaTime * 7f);

            if (shakeTime > 0f) { transform.position += Random.insideUnitSphere * shakeMag; shakeTime -= Time.deltaTime; }
        }

        public void Toggle180()
        {
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.CurrentView == RoomView.WorkbenchFocus)
            {
                RepairWorkbench.Instance?.CancelWork();
                return;
            }
            GameManager.Instance.SwitchView(GameManager.Instance.CurrentView == RoomView.Hangar ? RoomView.DispatchConsole : RoomView.Hangar);
        }

        public void Shake(float d, float m) { shakeTime = d; shakeMag = m; }
    }
}
