using UnityEngine;
using HangarGame.Core;
using HangarGame.Repair;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HangarGame.CameraControl
{
    public class RoomCamera : MonoBehaviour
    {
        public static RoomCamera Instance { get; private set; }
        private readonly Vector3 hangarPos = new Vector3(0f, 1.6f, -0.6f);
        private readonly Vector3 dispatchPos = new Vector3(0f, 1.55f, -3.2f);
        private readonly Vector3 fallbackWbPos = new Vector3(-2.6f, 1.35f, 0.9f);

        private float targetBaseYaw = 0f;
        private float curBaseYaw = 0f;
        private float wbZoom = 0f;
        private float shakeTime = 0f, shakeMag = 0f;

        private void Awake() => Instance = this;

        private void Start()
        {
            transform.position = hangarPos;
            transform.rotation = Quaternion.identity;
            targetBaseYaw = 0f;
            curBaseYaw = 0f;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnViewChanged += HandleViewChanged;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnViewChanged -= HandleViewChanged;
            }
        }

        private void HandleViewChanged(RoomView view)
        {
            if (view == RoomView.Hangar)
            {
                targetBaseYaw = 0f;
            }
            else if (view == RoomView.DispatchConsole)
            {
                targetBaseYaw = 180f;
            }
        }

        private void Update()
        {
            RoomView v = GameManager.Instance != null ? GameManager.Instance.CurrentView : RoomView.Hangar;

            bool turnL = false, turnR = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                turnL = Keyboard.current.qKey.wasPressedThisFrame;
                turnR = Keyboard.current.eKey.wasPressedThisFrame;
            }
#else
            turnL = Input.GetKeyDown(KeyCode.Q);
            turnR = Input.GetKeyDown(KeyCode.E);
#endif
            if (v != RoomView.WorkbenchFocus)
            {
                if (turnL) TurnLeft();
                if (turnR) TurnRight();
            }

            if (v == RoomView.WorkbenchFocus)
            {
                float scroll = 0f;
#if ENABLE_INPUT_SYSTEM
                if (Mouse.current != null) scroll = Mouse.current.scroll.ReadValue().y;
#else
                scroll = Input.GetAxis("Mouse ScrollWheel");
#endif
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    wbZoom = Mathf.Clamp(wbZoom - Mathf.Sign(scroll) * 0.15f, -0.4f, 0.6f);
                }
            }

            Vector2 m = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null) m = Mouse.current.position.ReadValue();
#else
            m = Input.mousePosition;
#endif
            float nx = Mathf.Clamp((m.x / Mathf.Max(1, Screen.width)) * 2f - 1f, -1f, 1f);
            float ny = Mathf.Clamp((m.y / Mathf.Max(1, Screen.height)) * 2f - 1f, -1f, 1f);

            curBaseYaw = Mathf.Lerp(curBaseYaw, targetBaseYaw, Time.deltaTime * 8f);

            float p = (v == RoomView.WorkbenchFocus) ? (32f - ny * 5f) : (-ny * 7f);
            float y = (v == RoomView.WorkbenchFocus) ? (25f + nx * 5f) : (curBaseYaw + nx * 12f);

            Vector3 targetPos = hangarPos;
            if (v == RoomView.WorkbenchFocus)
            {
                Vector3 wbBase = (RepairWorkbench.Instance != null)
                    ? RepairWorkbench.Instance.transform.position + new Vector3(0f, 0.65f, -1.05f)
                    : fallbackWbPos;
                targetPos = wbBase + new Vector3(0f, wbZoom * 0.2f, -wbZoom);
            }
            else if (v == RoomView.DispatchConsole)
            {
                targetPos = dispatchPos;
            }

            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 6f);
            transform.rotation = Quaternion.Euler(p, y, 0f);

            if (shakeTime > 0f)
            {
                transform.position += Random.insideUnitSphere * shakeMag;
                shakeTime -= Time.deltaTime;
            }
        }

        public void TurnLeft()
        {
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.CurrentView == RoomView.WorkbenchFocus)
            {
                RepairWorkbench.Instance?.CancelWork();
                return;
            }
            var nextView = (GameManager.Instance.CurrentView == RoomView.Hangar) ? RoomView.DispatchConsole : RoomView.Hangar;
            GameManager.Instance.SwitchView(nextView);
        }

        public void TurnRight()
        {
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.CurrentView == RoomView.WorkbenchFocus)
            {
                RepairWorkbench.Instance?.CancelWork();
                return;
            }
            var nextView = (GameManager.Instance.CurrentView == RoomView.Hangar) ? RoomView.DispatchConsole : RoomView.Hangar;
            GameManager.Instance.SwitchView(nextView);
        }

        public void Shake(float dur, float mag)
        {
            shakeTime = dur;
            shakeMag = mag;
        }
    }
}
