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
        private Vector3 hangarPos = new Vector3(0f, 1.6f, -0.6f);
        private Vector3 dispatchPos = new Vector3(0f, 1.55f, -3.2f);
        private readonly Vector3 fallbackWbPos = new Vector3(-2.6f, 1.35f, 0.9f);

        private float hangarYaw = 0f;
        private float dispatchYaw = 180f;
        private float targetBaseYaw = 0f;
        private float curBaseYaw = 0f;
        private float wbZoom = 0f;
        private float shakeTime = 0f, shakeMag = 0f;

        private void Awake() => Instance = this;

        public void ConfigureAnchor(Vector3 hPos, Vector3 dPos, float hYaw, float dYaw)
        {
            hangarPos = hPos;
            dispatchPos = dPos;
            hangarYaw = hYaw;
            dispatchYaw = dYaw;
            targetBaseYaw = hYaw;
            curBaseYaw = hYaw;
            transform.position = hPos;
            transform.rotation = Quaternion.Euler(0f, hYaw, 0f);
        }

        private void Start()
        {
            var hf = GameObject.Find("HANGARFULL");
            if (hf != null)
            {
                var c85 = hf.transform.Find("Cube.085");
                var c104 = hf.transform.Find("Cube.104");
                if (c85 != null && c104 != null)
                {
                    Vector3 mid = Vector3.Lerp(c85.position, c104.position, 0.45f) + Vector3.up * 0.75f;
                    hangarPos = mid;
                    dispatchPos = mid;
                    hangarYaw = Quaternion.LookRotation(c85.position - mid).eulerAngles.y;
                    dispatchYaw = Quaternion.LookRotation(c104.position - mid).eulerAngles.y;
                    targetBaseYaw = hangarYaw;
                    curBaseYaw = hangarYaw;
                    transform.position = hangarPos;
                    transform.rotation = Quaternion.Euler(0f, hangarYaw, 0f);
                }
            }

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
                targetBaseYaw += Mathf.DeltaAngle(targetBaseYaw, hangarYaw);
            }
            else if (view == RoomView.DispatchConsole)
            {
                targetBaseYaw += Mathf.DeltaAngle(targetBaseYaw, dispatchYaw);
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

            float wbYaw = (RepairWorkbench.Instance != null) ? RepairWorkbench.Instance.transform.eulerAngles.y : 25f;
            float p = (v == RoomView.WorkbenchFocus) ? (30f - ny * 5f) : (-ny * 7f);
            float y = (v == RoomView.WorkbenchFocus) ? (wbYaw + nx * 5f) : (curBaseYaw + nx * 12f);

            Vector3 targetPos = hangarPos;
            if (v == RoomView.WorkbenchFocus)
            {
                Vector3 wbBase = (RepairWorkbench.Instance != null)
                    ? RepairWorkbench.Instance.transform.TransformPoint(new Vector3(0f, 1.45f, -1.15f))
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
            targetBaseYaw -= 180f;
            SyncViewWithYaw();
        }

        public void TurnRight()
        {
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.CurrentView == RoomView.WorkbenchFocus)
            {
                RepairWorkbench.Instance?.CancelWork();
                return;
            }
            targetBaseYaw += 180f;
            SyncViewWithYaw();
        }

        private void SyncViewWithYaw()
        {
            float norm = Mathf.Repeat(targetBaseYaw, 360f);
            float dH = Mathf.Abs(Mathf.DeltaAngle(norm, hangarYaw));
            float dD = Mathf.Abs(Mathf.DeltaAngle(norm, dispatchYaw));
            RoomView next = (dD < dH) ? RoomView.DispatchConsole : RoomView.Hangar;
            if (GameManager.Instance.CurrentView != next)
            {
                GameManager.Instance.SwitchView(next);
            }
        }

        public void Shake(float dur, float mag)
        {
            shakeTime = dur;
            shakeMag = mag;
        }
    }
}
