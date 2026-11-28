using System;
using System.Collections;
using HangarGame.Core;
using HangarGame.Mechs;
using UnityEngine;

namespace HangarGame.Repair
{
    public class RepairWorkbench : MonoBehaviour
    {
        public static RepairWorkbench Instance { get; private set; }

        [Header("Размещение на верстаке")]
        [SerializeField] private Vector3 tableRestOffset = new Vector3(0f, 1.12f, 0f);
        [SerializeField] private float normalizedPartExtent = 0.68f;
        [SerializeField] private Vector3 initialEuler = new Vector3(0f, 0f, 0f);

        [Header("Вращение")]
        [SerializeField] private float rotationSpeed = 95f;
        [SerializeField] private float mouseSensitivity = 0.4f;
        [SerializeField] private float minPitch = -65f;
        [SerializeField] private float maxPitch = 65f;

        [Header("Мини-игра")]
        [SerializeField] private WorkbenchMinigame minigame;

        public MechPart CurrentPart { get; private set; }
        public GameObject CurrentJaegerModule { get; private set; }
        public bool HasItem => CurrentPart != null || CurrentJaegerModule != null;
        public bool IsJaegerModule => CurrentJaegerModule != null;
        public bool IsWorkComplete => minigame == null || minigame.IsComplete;
        public string RepairStepInfo => minigame != null ? minigame.StatusText : "";

        public string CurrentItemName
        {
            get
            {
                if (CurrentPart != null && CurrentPart.Data != null) return CurrentPart.Data.partName;
                return !string.IsNullOrEmpty(currentJaegerName) ? currentJaegerName : "Модуль";
            }
        }

        private Transform workPivot;
        private Coroutine mountCoroutine;
        private float targetYaw, targetPitch, curYaw, curPitch;
        private string currentJaegerName;
        private Action onJaegerDone;

        private void Awake()
        {
            Instance = this;
            EnsureMinigame();
        }

        private void EnsureMinigame()
        {
            if (minigame == null) minigame = GetComponent<WorkbenchMinigame>();
            if (minigame == null) minigame = gameObject.AddComponent<WorkbenchMinigame>();
        }

        public void SetPlatePrefab(GameObject platePrefab)
        {
            EnsureMinigame();
            minigame.SetPlatePrefab(platePrefab);
        }

        public void SetRestOffset(Vector3 offset) => tableRestOffset = offset;

        private void Update()
        {
            if (!HasItem) return;

            HandleKeyboardAndMouseInput();

            curYaw = Mathf.Lerp(curYaw, targetYaw, Time.deltaTime * 12f);
            curPitch = Mathf.Lerp(curPitch, targetPitch, Time.deltaTime * 12f);

            if (workPivot != null)
                workPivot.localRotation = Quaternion.Euler(curPitch, curYaw, 0f);
        }

        private void HandleKeyboardAndMouseInput()
        {
            bool esc = false;
            bool space = false;
            float h = 0f;
            float v = 0f;

#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                esc = kb.escapeKey.wasPressedThisFrame;
                space = kb.spaceKey.wasPressedThisFrame;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed || kb.qKey.isPressed) h -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed || kb.eKey.isPressed) h += 1f;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v -= 1f;
            }
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null && (mouse.rightButton.isPressed || mouse.middleButton.isPressed))
            {
                Vector2 d = mouse.delta.ReadValue();
                targetYaw += d.x * mouseSensitivity;
                targetPitch = Mathf.Clamp(targetPitch - d.y * mouseSensitivity, minPitch, maxPitch);
            }
#else
            esc = Input.GetKeyDown(KeyCode.Escape);
            space = Input.GetKeyDown(KeyCode.Space);
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.Q)) h -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.E)) h += 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v -= 1f;
            if (Input.GetMouseButton(1) || Input.GetMouseButton(2))
            {
                targetYaw += Input.GetAxis("Mouse X") * (mouseSensitivity * 10f);
                targetPitch = Mathf.Clamp(targetPitch - Input.GetAxis("Mouse Y") * (mouseSensitivity * 10f), minPitch, maxPitch);
            }
#endif

            if (esc)
            {
                CancelWork();
                return;
            }

            if (space && IsWorkComplete)
            {
                FinishWork();
                return;
            }

            targetYaw += h * rotationSpeed * Time.deltaTime;
            targetPitch = Mathf.Clamp(targetPitch - v * rotationSpeed * Time.deltaTime, minPitch, maxPitch);
        }

        public void RotateYaw(float delta) => targetYaw += delta;
        public void RotatePitch(float delta) => targetPitch = Mathf.Clamp(targetPitch + delta, minPitch, maxPitch);

        public void ResetRotation()
        {
            targetYaw = initialEuler.y;
            targetPitch = initialEuler.x;
        }

        public void ReceivePart(MechPart part)
        {
            if (HasItem || part == null || !part.IsRepairableSlot || !part.IsDamaged) return;

            part.OnHover(false);
            CurrentPart = part;
            part.IsOnWorkbench = true;

            foreach (var col in part.GetComponentsInChildren<Collider>(true)) col.enabled = false;

            PrepareWorkPivot();
            GameManager.Instance?.SwitchView(RoomView.WorkbenchFocus);

            if (Application.isPlaying)
                mountCoroutine = StartCoroutine(SlideAndMountRoutine(part.transform, false));
            else
                MountTargetImmediate(part.transform, false);
        }

        public void ReceiveJaegerModule(GameObject moduleObj, string moduleName, Action onDone)
        {
            if (HasItem || moduleObj == null) return;

            CurrentJaegerModule = moduleObj;
            currentJaegerName = moduleName;
            onJaegerDone = onDone;

            foreach (var col in moduleObj.GetComponentsInChildren<Collider>(true)) col.enabled = false;

            PrepareWorkPivot();
            moduleObj.transform.position = workPivot.position + Vector3.up * 0.5f;
            GameManager.Instance?.SwitchView(RoomView.WorkbenchFocus);

            if (Application.isPlaying)
                mountCoroutine = StartCoroutine(SlideAndMountRoutine(moduleObj.transform, true));
            else
                MountTargetImmediate(moduleObj.transform, true);
        }

        private void PrepareWorkPivot()
        {
            CleanupWorkPivot();

            GameObject pivotObj = new GameObject("Work_Pivot");
            pivotObj.transform.SetParent(transform, false);
            pivotObj.transform.localPosition = tableRestOffset;
            pivotObj.transform.localRotation = Quaternion.Euler(initialEuler);
            pivotObj.transform.localScale = Vector3.one;
            workPivot = pivotObj.transform;

            curYaw = targetYaw = initialEuler.y;
            curPitch = targetPitch = initialEuler.x;
        }

        private IEnumerator SlideAndMountRoutine(Transform target, bool jaegerMode)
        {
            MountIntoPivot(target);

            Vector3 finalLocalPos = target.localPosition;
            Vector3 startLocalPos = finalLocalPos + workPivot.InverseTransformDirection(Vector3.up * 0.35f);
            target.localPosition = startLocalPos;

            float t = 0f;
            const float dur = 0.25f;
            while (t < dur && target != null)
            {
                t += Time.deltaTime;
                target.localPosition = Vector3.Lerp(startLocalPos, finalLocalPos, Mathf.SmoothStep(0f, 1f, t / dur));
                yield return null;
            }

            if (target != null) target.localPosition = finalLocalPos;
            mountCoroutine = null;
            if (workPivot == null) yield break;

            Bounds localBounds = CalcPivotLocalBounds(workPivot);
            minigame.Begin(workPivot, localBounds, Vector3.back, jaegerMode);
        }

        private void MountTargetImmediate(Transform target, bool jaegerMode)
        {
            MountIntoPivot(target);
            Bounds localBounds = CalcPivotLocalBounds(workPivot);
            minigame.Begin(workPivot, localBounds, Vector3.back, jaegerMode);
        }

        private void MountIntoPivot(Transform target)
        {
            target.SetParent(workPivot, true);

            if (CurrentPart != null)
            {
                Vector3 camFwd = transform.forward;
                Vector3 partFwd = target.up;
                partFwd.y = 0f;
                Vector3 desired = -camFwd;
                desired.y = 0f;
                if (partFwd.sqrMagnitude > 0.001f && desired.sqrMagnitude > 0.001f)
                {
                    Quaternion yawDelta = Quaternion.FromToRotation(partFwd.normalized, desired.normalized);
                    target.rotation = yawDelta * target.rotation;
                }
            }
            else
            {
                target.localRotation = Quaternion.Euler(0f, 180f, 0f);
            }

            Bounds b = CalcWorldBounds(target.gameObject);
            if (b.size.sqrMagnitude > 0.0001f)
            {
                float ratio = normalizedPartExtent / b.size.magnitude;
                target.localScale *= ratio;
                b = CalcWorldBounds(target.gameObject);
                target.position += (workPivot.position - b.center);
            }
            else
            {
                target.localPosition = Vector3.zero;
            }
        }

        public void FinishWork()
        {
            if (mountCoroutine != null)
            {
                StopCoroutine(mountCoroutine);
                mountCoroutine = null;
            }
            minigame?.Clear();

            if (CurrentJaegerModule != null)
            {
                GameObject doneMod = CurrentJaegerModule;
                CurrentJaegerModule = null;
                currentJaegerName = null;
                doneMod.transform.SetParent(null, true);
                CleanupWorkPivot();
                DestroySafe(doneMod);

                var callback = onJaegerDone;
                onJaegerDone = null;
                callback?.Invoke();
                HangarAudio.Instance?.PlaySuccess();
                GameManager.Instance?.SwitchView(RoomView.Hangar);
                return;
            }

            if (CurrentPart != null)
            {
                MechPart finishedPart = CurrentPart;
                CurrentPart = null;

                if (finishedPart.Data != null)
                    GameManager.Instance?.TrySpend(finishedPart.Data.repairCredits, finishedPart.Data.repairAlloy);

                finishedPart.ReturnToSocket();
                CleanupWorkPivot();
                finishedPart.RepairFull();
                finishedPart.GetComponentInParent<MechUnit>()?.SyncHpFromParts();

                HangarAudio.Instance?.PlaySuccess();
                HangarMechManager.Instance?.UpdatePodiumDisplay();
                GameManager.Instance?.SwitchView(RoomView.Hangar);
            }
        }

        public void CancelWork()
        {
            if (mountCoroutine != null)
            {
                StopCoroutine(mountCoroutine);
                mountCoroutine = null;
            }
            minigame?.Clear();

            if (CurrentJaegerModule != null)
            {
                GameObject mod = CurrentJaegerModule;
                CurrentJaegerModule = null;
                currentJaegerName = null;
                onJaegerDone = null;
                mod.transform.SetParent(null, true);
                CleanupWorkPivot();
                DestroySafe(mod);
            }

            if (CurrentPart != null)
            {
                MechPart canceledPart = CurrentPart;
                CurrentPart = null;
                canceledPart.ReturnToSocket();
                CleanupWorkPivot();
            }

            HangarMechManager.Instance?.UpdatePodiumDisplay();
            GameManager.Instance?.SwitchView(RoomView.Hangar);
        }

        private void CleanupWorkPivot()
        {
            if (workPivot != null)
            {
                DestroySafe(workPivot.gameObject);
                workPivot = null;
            }
        }

        private void DestroySafe(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }

        private Bounds CalcWorldBounds(GameObject go)
        {
            var rends = go.GetComponentsInChildren<Renderer>(false);
            if (rends.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            return b;
        }

        private Bounds CalcPivotLocalBounds(Transform pivot)
        {
            Bounds world = CalcWorldBounds(pivot.gameObject);
            Vector3 localCenter = pivot.InverseTransformPoint(world.center);
            return new Bounds(localCenter, world.size);
        }
    }
}
