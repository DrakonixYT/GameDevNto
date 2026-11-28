using System;
using HangarGame.Core;
using HangarGame.Repair;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HangarGame.CameraControl
{
    [Serializable]
    public struct ViewAnchor
    {
        public Vector3 position;
        public float yaw;
    }

    public class RoomCamera : MonoBehaviour
    {
        [Header("Ссылки")]
        public GameManager game;
        public RepairWorkbench workbench;

        [Header("Точки обзора")]
        public ViewAnchor hangarView = new ViewAnchor { position = new Vector3(-14.94f, 1.53f, -0.02f), yaw = 180f };
        public ViewAnchor dispatchView = new ViewAnchor { position = new Vector3(-14.89f, 1.38f, 1.02f), yaw = 0f };
        public ViewAnchor armoryView = new ViewAnchor { position = new Vector3(-14.38f, 1.47f, 1.72f), yaw = 30f };

        [Header("Верстак")]
        public Vector3 wbCameraOffset = new Vector3(0f, 1.54f, -1.24f);
        public float wbCameraPitch = 22f;
        public float dispatchDistance = 1.32f;

        public bool IsArmoryFocused { get; private set; }
        public event Action<bool> OnArmoryFocusChanged;

        private float targetYaw;
        private float currentYaw;
        private float wbZoom;
        private float shakeTime;
        private float shakeMag;

        private void Awake()
        {
            game ??= GameManager.Instance ?? FindAnyObjectByType<GameManager>();
            workbench ??= game?.workbench ?? FindAnyObjectByType<RepairWorkbench>();

            var camData = GetComponent<UniversalAdditionalCameraData>();
            if (camData != null) camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;

            // Убираем CA только у рантайм-профиля, чтобы текст на боковых мониторах не двоился
            if (Application.isPlaying)
            {
                foreach (var v in FindObjectsByType<Volume>(FindObjectsInactive.Include))
                    if (v.profile != null && v.profile.TryGet<ChromaticAberration>(out var ca))
                        ca.active = false;
            }
        }

        private void Start()
        {
            targetYaw = currentYaw = hangarView.yaw;
            transform.position = hangarView.position;
            transform.rotation = Quaternion.Euler(0f, hangarView.yaw, 0f);

            if (game != null)
                game.OnViewChanged += HandleViewChanged;
        }

        private void OnDestroy()
        {
            if (game != null)
                game.OnViewChanged -= HandleViewChanged;
        }

        private void HandleViewChanged(RoomView view)
        {
            if (IsArmoryFocused)
            {
                IsArmoryFocused = false;
                OnArmoryFocusChanged?.Invoke(false);
            }

            if (view == RoomView.Hangar)
                targetYaw += Mathf.DeltaAngle(targetYaw, hangarView.yaw);
            else if (view == RoomView.DispatchConsole)
                targetYaw += Mathf.DeltaAngle(targetYaw, dispatchView.yaw);
        }

        public void ToggleArmoryFocus()
        {
            if (game == null || game.CurrentView != RoomView.DispatchConsole) return;
            IsArmoryFocused = !IsArmoryFocused;
            targetYaw += Mathf.DeltaAngle(targetYaw, IsArmoryFocused ? armoryView.yaw : dispatchView.yaw);
            game.PlaySfx(Sfx.Click);
            OnArmoryFocusChanged?.Invoke(IsArmoryFocused);
        }

        private void Update()
        {
            RoomView view = game != null ? game.CurrentView : RoomView.Hangar;
            HandleInput(view);
            UpdateCameraTransform(view);
        }

        private void HandleInput(RoomView view)
        {
            if (view == RoomView.WorkbenchFocus)
            {
                float scroll = GameInput.Scroll;
                if (Mathf.Abs(scroll) > 0.01f)
                    wbZoom = Mathf.Clamp(wbZoom - Mathf.Sign(scroll) * 0.14f, -0.35f, 0.55f);
                return;
            }

            if (game != null && (game.IsGameWon || game.IsGameLost)) return;

            if (view == RoomView.DispatchConsole && GameInput.FocusArmory) ToggleArmoryFocus();
            if (GameInput.TurnLeft) TurnLeft();
            if (GameInput.TurnRight) TurnRight();
        }

        private void UpdateCameraTransform(RoomView view)
        {
            Vector2 m = GameInput.PointerPos;
            float nx = Mathf.Clamp(m.x / Mathf.Max(1, Screen.width) * 2f - 1f, -1f, 1f);
            float ny = Mathf.Clamp(m.y / Mathf.Max(1, Screen.height) * 2f - 1f, -1f, 1f);

            currentYaw = Mathf.Lerp(currentYaw, targetYaw, Time.deltaTime * 8f);

            Vector3 targetPos;
            float pitch;
            float yaw;

            if (view == RoomView.WorkbenchFocus)
            {
                float wbYaw = workbench != null ? workbench.transform.eulerAngles.y : 270f;
                pitch = wbCameraPitch - ny;
                yaw = wbYaw + nx * 1.2f;
                targetPos = workbench != null
                    ? workbench.transform.TransformPoint(wbCameraOffset + new Vector3(0f, wbZoom * 0.2f, -wbZoom))
                    : hangarView.position;
            }
            else
            {
                pitch = IsArmoryFocused ? -ny * 3.5f : -ny * 6.5f;
                yaw = currentYaw + (IsArmoryFocused ? nx * 5.0f : nx * 11f);
                targetPos = view == RoomView.DispatchConsole
                    ? (IsArmoryFocused ? armoryView.position : dispatchView.position)
                    : hangarView.position;
            }

            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 6f);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

            if (shakeTime > 0f)
            {
                transform.position += UnityEngine.Random.insideUnitSphere * shakeMag;
                shakeTime -= Time.deltaTime;
            }
        }

        public void TurnLeft() => RotateRoom(-180f);
        public void TurnRight() => RotateRoom(180f);

        private void RotateRoom(float deltaYaw)
        {
            if (game == null) return;
            if (game.CurrentView == RoomView.WorkbenchFocus)
            {
                workbench?.CancelWork();
                return;
            }

            targetYaw += deltaYaw;
            float norm = Mathf.Repeat(targetYaw, 360f);
            bool toDispatch = Mathf.Abs(Mathf.DeltaAngle(norm, dispatchView.yaw)) < Mathf.Abs(Mathf.DeltaAngle(norm, hangarView.yaw));
            game.SwitchView(toDispatch ? RoomView.DispatchConsole : RoomView.Hangar);
        }

        public void Shake(float duration, float magnitude)
        {
            shakeTime = duration;
            shakeMag = magnitude;
        }
    }
}
