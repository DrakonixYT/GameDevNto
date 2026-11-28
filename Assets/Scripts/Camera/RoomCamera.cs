using HangarGame.Core;
using HangarGame.Repair;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HangarGame.CameraControl
{
    public class RoomCamera : MonoBehaviour
    {
        public static RoomCamera Instance { get; private set; }

        [Header("Позиции")]
        [SerializeField] private Vector3 hangarPos = new Vector3(-14.94f, 1.53f, -0.02f);
        [SerializeField] private Vector3 dispatchPos = new Vector3(-14.89f, 1.38f, 1.02f);
        [SerializeField] private float hangarYaw = 180f;
        [SerializeField] private float dispatchYaw;

        [Header("Верстак и пульт")]
        [SerializeField] private Vector3 wbCameraOffset = new Vector3(0f, 1.62f, -1.26f);
        [SerializeField] private float wbCameraPitch = 25f;
        [SerializeField] private float dispatchDistance = 1.32f;
        [SerializeField] private Vector3 armoryFocusPos = new Vector3(-14.38f, 1.47f, 1.72f);
        [SerializeField] private float armoryFocusYaw = 30f;

        public bool IsArmoryFocused { get; private set; }

        private float targetYaw;
        private float currentYaw;
        private float wbZoom;
        private float shakeTime;
        private float shakeMag;

        private void Awake()
        {
            Instance = this;
            var camData = GetComponent<UniversalAdditionalCameraData>();
            if (camData != null) camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            foreach (var v in Object.FindObjectsByType<Volume>(FindObjectsInactive.Include))
            {
                if (Application.isPlaying && v.profile != null && v.profile.TryGet<ChromaticAberration>(out var ca))
                    ca.active = false;
            }
        }

        public void ConfigureAnchor(Vector3 hPos, Vector3 dPos, float hYaw, float dYaw)
        {
            hangarPos = hPos;
            dispatchPos = dPos;
            hangarYaw = hYaw;
            dispatchYaw = dYaw;
            targetYaw = hYaw;
            currentYaw = hYaw;
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
                        armoryFocusPos = armoryCanvas.transform.position - armoryCanvas.transform.forward * 0.58f;
                        armoryFocusYaw = armoryCanvas.transform.eulerAngles.y;
                    }
                }
            }

            targetYaw = currentYaw = hangarYaw;
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
                targetYaw += Mathf.DeltaAngle(targetYaw, hangarYaw);
            else if (view == RoomView.DispatchConsole)
                targetYaw += Mathf.DeltaAngle(targetYaw, dispatchYaw);
        }

        public void ToggleArmoryFocus()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentView != RoomView.DispatchConsole) return;
            IsArmoryFocused = !IsArmoryFocused;
            targetYaw += Mathf.DeltaAngle(targetYaw, IsArmoryFocused ? armoryFocusYaw : dispatchYaw);
            HangarAudio.Instance?.PlayClick();
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            RoomView view = gm != null ? gm.CurrentView : RoomView.Hangar;

            if (view != RoomView.WorkbenchFocus && (gm == null || (!gm.IsGameWon && !gm.IsGameLost)))
            {
                if (view == RoomView.DispatchConsole && (GameInput.KeyDown(KeyCode.Tab) || GameInput.KeyDown(KeyCode.I)))
                    ToggleArmoryFocus();
                if (GameInput.KeyDown(KeyCode.Q)) TurnLeft();
                if (GameInput.KeyDown(KeyCode.E)) TurnRight();
            }
            else if (view == RoomView.WorkbenchFocus)
            {
                float scroll = GameInput.ScrollY;
                if (Mathf.Abs(scroll) > 0.01f)
                    wbZoom = Mathf.Clamp(wbZoom - Mathf.Sign(scroll) * 0.14f, -0.35f, 0.55f);
            }

            Vector2 m = GameInput.MousePosition;
            float nx = Mathf.Clamp(m.x / Mathf.Max(1, Screen.width) * 2f - 1f, -1f, 1f);
            float ny = Mathf.Clamp(m.y / Mathf.Max(1, Screen.height) * 2f - 1f, -1f, 1f);

            currentYaw = Mathf.Lerp(currentYaw, targetYaw, Time.deltaTime * 8f);

            float pitch;
            float yaw;
            Vector3 targetPos = hangarPos;

            if (view == RoomView.WorkbenchFocus)
            {
                var wb = RepairWorkbench.Instance;
                float wbYaw = wb != null ? wb.transform.eulerAngles.y : 270f;
                pitch = wbCameraPitch - ny;
                yaw = wbYaw + nx * 1.2f;
                if (wb != null)
                    targetPos = wb.transform.TransformPoint(wbCameraOffset + new Vector3(0f, wbZoom * 0.2f, -wbZoom));
            }
            else
            {
                pitch = IsArmoryFocused ? -ny * 3.5f : -ny * 6.5f;
                yaw = currentYaw + (IsArmoryFocused ? nx * 5.0f : nx * 11f);
                if (view == RoomView.DispatchConsole)
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

        public void TurnLeft() => RotateRoom(-180f);
        public void TurnRight() => RotateRoom(180f);

        private void RotateRoom(float deltaYaw)
        {
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.CurrentView == RoomView.WorkbenchFocus)
            {
                RepairWorkbench.Instance?.CancelWork();
                return;
            }
            targetYaw += deltaYaw;
            float norm = Mathf.Repeat(targetYaw, 360f);
            bool toDispatch = Mathf.Abs(Mathf.DeltaAngle(norm, dispatchYaw)) < Mathf.Abs(Mathf.DeltaAngle(norm, hangarYaw));
            GameManager.Instance.SwitchView(toDispatch ? RoomView.DispatchConsole : RoomView.Hangar);
        }

        public void Shake(float duration, float magnitude)
        {
            shakeTime = duration;
            shakeMag = magnitude;
        }
    }
}
