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

        [Header("Точки комнат")]
        public Vector3 hangarPos = new Vector3(-14.94f, 1.53f, -0.02f);
        public Vector3 dispatchPos = new Vector3(-14.89f, 1.38f, 1.02f);
        public float hangarYaw = 180f;
        public float dispatchYaw;

        [Header("Верстак и пульт")]
        public Vector3 wbCameraOffset = new Vector3(0f, 1.54f, -1.24f);
        public float wbCameraPitch = 22f;
        public float dispatchDistance = 1.32f;
        public Vector3 armoryFocusPos = new Vector3(-14.38f, 1.47f, 1.72f);
        public float armoryFocusYaw = 30f;

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

            if (Application.isPlaying)
            {
                foreach (var v in FindObjectsByType<Volume>(FindObjectsInactive.Include))
                    if (v.profile != null && v.profile.TryGet<ChromaticAberration>(out var ca))
                        ca.active = false;
            }
        }

        private void Start()
        {
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
            HangarAudio.Play(Sfx.Click);
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            RoomView view = gm != null ? gm.CurrentView : RoomView.Hangar;

            if (view != RoomView.WorkbenchFocus && (gm == null || (!gm.IsGameWon && !gm.IsGameLost)))
            {
                if (view == RoomView.DispatchConsole && GameInput.FocusArmory) ToggleArmoryFocus();
                if (GameInput.TurnLeft) TurnLeft();
                if (GameInput.TurnRight) TurnRight();
            }
            else if (view == RoomView.WorkbenchFocus)
            {
                float scroll = GameInput.Scroll;
                if (Mathf.Abs(scroll) > 0.01f)
                    wbZoom = Mathf.Clamp(wbZoom - Mathf.Sign(scroll) * 0.14f, -0.35f, 0.55f);
            }

            Vector2 m = GameInput.PointerPos;
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
