using HangarGame.Core;
using HangarGame.Repair;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HangarGame.CameraControl
{
    public class RoomCamera : MonoBehaviour
    {
        public static RoomCamera Instance { get; private set; }

        [Header("Позиции обзора")]
        [SerializeField] private Vector3 hangarPos = new Vector3(-14.94f, 1.53f, -0.02f);
        [SerializeField] private Vector3 dispatchPos = new Vector3(-14.94f, 1.53f, -0.5f);
        [SerializeField] private float hangarYaw = 180f;
        [SerializeField] private float dispatchYaw = 0f;

        [Header("Камера верстака")]
        [SerializeField] private Vector3 wbCameraOffset = new Vector3(0f, 1.62f, -1.26f);
        [SerializeField] private float wbCameraPitch = 25f;
        [SerializeField] private float wbParallaxYaw = 1.2f;
        [SerializeField] private float wbParallaxPitch = 1.0f;

        [Header("Камера пульта")]
        [SerializeField] private float dispatchDistance = 1.32f;
        [SerializeField] private Vector3 armoryFocusPos = new Vector3(-14.44f, 1.47f, 1.56f);
        [SerializeField] private float armoryFocusYaw = 28f;

        public bool IsArmoryFocused { get; private set; }

        private float targetBaseYaw;
        private float curBaseYaw;
        private float wbZoom;
        private float shakeTime;
        private float shakeMag;

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

        public void ConfigureWorkbenchCamera(Vector3 offset, float pitch)
        {
            wbCameraOffset = offset;
            wbCameraPitch = pitch;
        }

        public void ConfigureDispatchMonitorCamera(float overviewDist, Vector3 armoryPos, float armoryYaw)
        {
            dispatchDistance = overviewDist;
            armoryFocusPos = armoryPos;
            armoryFocusYaw = armoryYaw;
        }

        public void ToggleArmoryFocus()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentView != RoomView.DispatchConsole) return;
            IsArmoryFocused = !IsArmoryFocused;
            float goalYaw = IsArmoryFocused ? armoryFocusYaw : dispatchYaw;
            targetBaseYaw += Mathf.DeltaAngle(targetBaseYaw, goalYaw);
            HangarAudio.Instance?.PlayClick();
        }

        public void SetArmoryFocus(bool focused)
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentView != RoomView.DispatchConsole)
            {
                IsArmoryFocused = false;
                return;
            }
            if (IsArmoryFocused == focused) return;
            IsArmoryFocused = focused;
            float goalYaw = IsArmoryFocused ? armoryFocusYaw : dispatchYaw;
            targetBaseYaw += Mathf.DeltaAngle(targetBaseYaw, goalYaw);
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
                    hangarYaw = Quaternion.LookRotation(c85.position - mid).eulerAngles.y;

                    var tCanvas = GameObject.Find("Terminal_Canvas");
                    Vector3 sPoint = tCanvas != null ? tCanvas.transform.position : c104.position + Vector3.up * 0.34f;
                    dispatchPos = new Vector3(sPoint.x, sPoint.y, sPoint.z - dispatchDistance);
                    dispatchYaw = 0f;
                    var armoryCanvas = GameObject.Find("Armory_Canvas");
                    if (armoryCanvas != null)
                    {
                        Vector3 screenOut = -armoryCanvas.transform.forward;
                        armoryFocusPos = armoryCanvas.transform.position + screenOut * 0.58f;
                        armoryFocusYaw = armoryCanvas.transform.eulerAngles.y;
                    }
                }
            }

            targetBaseYaw = hangarYaw;
            curBaseYaw = hangarYaw;
            transform.position = hangarPos;
            transform.rotation = Quaternion.Euler(0f, hangarYaw, 0f);

            if (GameManager.Instance != null)
                GameManager.Instance.OnViewChanged += HandleViewChanged;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnViewChanged -= HandleViewChanged;
        }

        private void HandleViewChanged(RoomView view)
        {
            IsArmoryFocused = false;
            if (view == RoomView.Hangar)
                targetBaseYaw += Mathf.DeltaAngle(targetBaseYaw, hangarYaw);
            else if (view == RoomView.DispatchConsole)
                targetBaseYaw += Mathf.DeltaAngle(targetBaseYaw, dispatchYaw);
        }

        private void Update()
        {
            RoomView view = GameManager.Instance != null ? GameManager.Instance.CurrentView : RoomView.Hangar;

            HandleTurnInput(view);
            HandleZoomInput(view);
            UpdateTransform(view);
        }

        private void HandleTurnInput(RoomView view)
        {
            if (view == RoomView.WorkbenchFocus) return;
            if (GameManager.Instance != null && (GameManager.Instance.IsGameWon || GameManager.Instance.IsGameLost)) return;

            bool turnL = false, turnR = false, toggleArmory = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                turnL = Keyboard.current.qKey.wasPressedThisFrame;
                turnR = Keyboard.current.eKey.wasPressedThisFrame;
                toggleArmory = Keyboard.current.tabKey.wasPressedThisFrame || Keyboard.current.iKey.wasPressedThisFrame;
            }
#else
            turnL = Input.GetKeyDown(KeyCode.Q);
            turnR = Input.GetKeyDown(KeyCode.E);
            toggleArmory = Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.I);
#endif
            if (toggleArmory && view == RoomView.DispatchConsole) ToggleArmoryFocus();
            if (turnL) TurnLeft();
            if (turnR) TurnRight();
        }

        private void HandleZoomInput(RoomView view)
        {
            if (view != RoomView.WorkbenchFocus) return;

            float scroll = 0f;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null) scroll = Mouse.current.scroll.ReadValue().y;
#else
            scroll = Input.GetAxis("Mouse ScrollWheel");
#endif
            if (Mathf.Abs(scroll) > 0.01f)
                wbZoom = Mathf.Clamp(wbZoom - Mathf.Sign(scroll) * 0.14f, -0.35f, 0.55f);
        }

        private void UpdateTransform(RoomView view)
        {
            Vector2 m = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null) m = Mouse.current.position.ReadValue();
#else
            m = Input.mousePosition;
#endif
            float nx = Mathf.Clamp((m.x / Mathf.Max(1, Screen.width)) * 2f - 1f, -1f, 1f);
            float ny = Mathf.Clamp((m.y / Mathf.Max(1, Screen.height)) * 2f - 1f, -1f, 1f);

            curBaseYaw = Mathf.Lerp(curBaseYaw, targetBaseYaw, Time.deltaTime * 8f);

            float wbYaw = RepairWorkbench.Instance != null ? RepairWorkbench.Instance.transform.eulerAngles.y : 270f;
            float pitch = view == RoomView.WorkbenchFocus
                ? (wbCameraPitch - ny * wbParallaxPitch)
                : (IsArmoryFocused ? -ny * 3.5f : -ny * 6.5f);
            float yaw = view == RoomView.WorkbenchFocus
                ? (wbYaw + nx * wbParallaxYaw)
                : (curBaseYaw + (IsArmoryFocused ? nx * 5.0f : nx * 11f));

            Vector3 targetPos = hangarPos;
            if (view == RoomView.WorkbenchFocus)
            {
                Vector3 wbLocal = wbCameraOffset + new Vector3(0f, wbZoom * 0.2f, -wbZoom);
                targetPos = RepairWorkbench.Instance != null
                    ? RepairWorkbench.Instance.transform.TransformPoint(wbLocal)
                    : hangarPos;
            }
            else if (view == RoomView.DispatchConsole)
            {
                targetPos = IsArmoryFocused ? armoryFocusPos : dispatchPos;
            }

            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 6f);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

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
            RoomView next = dD < dH ? RoomView.DispatchConsole : RoomView.Hangar;
            GameManager.Instance.SwitchView(next);
        }

        public void Shake(float duration, float magnitude)
        {
            shakeTime = duration;
            shakeMag = magnitude;
        }
    }
}
