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
        [SerializeField] private Vector3 tableRestOffset = new Vector3(0f, 1.00f, -0.04f);
        [SerializeField] private float normalizedPartExtent = 0.64f;
        [SerializeField] private Vector3 initialEuler;

        [Header("Вращение")]
        [SerializeField] private float rotationSpeed = 95f;
        [SerializeField] private float mouseSensitivity = 0.4f;
        [SerializeField] private float minPitch = -65f;
        [SerializeField] private float maxPitch = 65f;
        [SerializeField] private WorkbenchMinigame minigame;

        public MechPart CurrentPart { get; private set; }
        public GameObject CurrentJaegerModule { get; private set; }
        public bool HasItem => CurrentPart != null || CurrentJaegerModule != null;
        public bool IsWorkComplete => minigame == null || minigame.IsComplete;
        public string RepairStepInfo => minigame != null ? minigame.StatusText : "";
        public string CurrentItemName => CurrentPart != null && CurrentPart.Data != null
            ? CurrentPart.Data.partName
            : (!string.IsNullOrEmpty(jaegerTitle) ? jaegerTitle : "Деталь");

        private Transform workPivot;
        private Coroutine mountRoutine;
        private float targetYaw, targetPitch, curYaw, curPitch;
        private string jaegerTitle;
        private Action onJaegerDone;

        private void Awake()
        {
            Instance = this;
            minigame = ModelUtils.GetOrAdd<WorkbenchMinigame>(gameObject);
        }

        public void SetPlatePrefab(GameObject platePrefab)
        {
            minigame = ModelUtils.GetOrAdd<WorkbenchMinigame>(gameObject);
            minigame.SetPlatePrefab(platePrefab);
        }

        public void SetRestOffset(Vector3 offset) => tableRestOffset = offset;

        private void Update()
        {
            if (!HasItem) return;

            if (GameInput.KeyDown(KeyCode.Escape))
            {
                CancelWork();
                return;
            }
            if (GameInput.KeyDown(KeyCode.Space) && IsWorkComplete)
            {
                FinishWork();
                return;
            }

            float h = 0f;
            float v = 0f;
            if (GameInput.KeyHeld(KeyCode.A) || GameInput.KeyHeld(KeyCode.LeftArrow) || GameInput.KeyHeld(KeyCode.Q)) h -= 1f;
            if (GameInput.KeyHeld(KeyCode.D) || GameInput.KeyHeld(KeyCode.RightArrow) || GameInput.KeyHeld(KeyCode.E)) h += 1f;
            if (GameInput.KeyHeld(KeyCode.W) || GameInput.KeyHeld(KeyCode.UpArrow)) v += 1f;
            if (GameInput.KeyHeld(KeyCode.S) || GameInput.KeyHeld(KeyCode.DownArrow)) v -= 1f;

            if (GameInput.MouseRightHeld)
            {
                Vector2 d = GameInput.MouseDelta;
                targetYaw += d.x * mouseSensitivity;
                targetPitch = Mathf.Clamp(targetPitch - d.y * mouseSensitivity, minPitch, maxPitch);
            }

            targetYaw += h * rotationSpeed * Time.deltaTime;
            targetPitch = Mathf.Clamp(targetPitch - v * rotationSpeed * Time.deltaTime, minPitch, maxPitch);

            curYaw = Mathf.Lerp(curYaw, targetYaw, Time.deltaTime * 12f);
            curPitch = Mathf.Lerp(curPitch, targetPitch, Time.deltaTime * 12f);

            if (workPivot != null)
                workPivot.localRotation = Quaternion.Euler(curPitch, curYaw, 0f);
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

            CreatePivot();
            GameManager.Instance?.SwitchView(RoomView.WorkbenchFocus);

            if (Application.isPlaying) mountRoutine = StartCoroutine(MountRoutine(part.transform, false));
            else MountImmediate(part.transform, false);
        }

        public void ReceiveJaegerModule(GameObject moduleObj, string moduleName, Action onDone)
        {
            if (HasItem || moduleObj == null) return;

            CurrentJaegerModule = moduleObj;
            jaegerTitle = moduleName;
            onJaegerDone = onDone;
            foreach (var col in moduleObj.GetComponentsInChildren<Collider>(true)) col.enabled = false;

            CreatePivot();
            moduleObj.transform.position = workPivot.position + Vector3.up * 0.5f;
            GameManager.Instance?.SwitchView(RoomView.WorkbenchFocus);

            if (Application.isPlaying) mountRoutine = StartCoroutine(MountRoutine(moduleObj.transform, true));
            else MountImmediate(moduleObj.transform, true);
        }

        private void CreatePivot()
        {
            if (workPivot != null) ModelUtils.DestroySafe(workPivot.gameObject);

            var pivotObj = new GameObject("Work_Pivot");
            pivotObj.transform.SetParent(transform, false);
            pivotObj.transform.localPosition = tableRestOffset;
            pivotObj.transform.localRotation = Quaternion.Euler(initialEuler);
            workPivot = pivotObj.transform;

            curYaw = targetYaw = initialEuler.y;
            curPitch = targetPitch = initialEuler.x;
        }

        private IEnumerator MountRoutine(Transform target, bool jaegerMode)
        {
            MountIntoPivot(target);

            Vector3 finalLocal = target.localPosition;
            Vector3 startLocal = finalLocal + workPivot.InverseTransformDirection(Vector3.up * 0.3f);
            target.localPosition = startLocal;

            float t = 0f;
            while (t < 0.22f && target != null)
            {
                t += Time.deltaTime;
                target.localPosition = Vector3.Lerp(startLocal, finalLocal, t / 0.22f);
                yield return null;
            }

            if (target != null) target.localPosition = finalLocal;
            mountRoutine = null;
            if (workPivot != null) minigame.Begin(workPivot, GetPivotLocalBounds(), Vector3.back, jaegerMode);
        }

        private void MountImmediate(Transform target, bool jaegerMode)
        {
            MountIntoPivot(target);
            minigame.Begin(workPivot, GetPivotLocalBounds(), Vector3.back, jaegerMode);
        }

        private void MountIntoPivot(Transform target)
        {
            target.SetParent(workPivot, true);

            if (CurrentPart != null)
            {
                Vector3 partFwd = target.up;
                partFwd.y = 0f;
                Vector3 desired = -transform.forward;
                desired.y = 0f;
                if (partFwd.sqrMagnitude > 0.001f && desired.sqrMagnitude > 0.001f)
                    target.rotation = Quaternion.FromToRotation(partFwd.normalized, desired.normalized) * target.rotation;
            }
            else
            {
                target.localRotation = Quaternion.Euler(0f, 180f, 0f);
            }

            Bounds b = ModelUtils.CalcBounds(target.gameObject, false);
            if (b.size.sqrMagnitude > 0.0001f)
            {
                target.localScale *= normalizedPartExtent / b.size.magnitude;
                b = ModelUtils.CalcBounds(target.gameObject, false);
                target.position += workPivot.position - b.center;
            }
            else
            {
                target.localPosition = Vector3.zero;
            }
        }

        public void FinishWork()
        {
            StopMount();
            minigame?.Clear();

            if (CurrentJaegerModule != null)
            {
                GameObject doneMod = CurrentJaegerModule;
                CurrentJaegerModule = null;
                jaegerTitle = null;
                doneMod.transform.SetParent(null, true);
                CleanupPivot();
                ModelUtils.DestroySafe(doneMod);

                var cb = onJaegerDone;
                onJaegerDone = null;
                cb?.Invoke();
                HangarAudio.Instance?.PlaySuccess();
                GameManager.Instance?.SwitchView(RoomView.Hangar);
                return;
            }

            if (CurrentPart != null)
            {
                MechPart part = CurrentPart;
                CurrentPart = null;
                if (part.Data != null)
                    GameManager.Instance?.TrySpend(part.Data.repairCredits, part.Data.repairAlloy);

                part.ReturnToSocket();
                CleanupPivot();
                part.RepairFull();
                part.GetComponentInParent<MechUnit>()?.SyncHpFromParts();

                HangarAudio.Instance?.PlaySuccess();
                HangarMechManager.Instance?.UpdatePodiumDisplay();
                GameManager.Instance?.SwitchView(RoomView.Hangar);
            }
        }

        public void CancelWork()
        {
            StopMount();
            minigame?.Clear();

            if (CurrentJaegerModule != null)
            {
                GameObject mod = CurrentJaegerModule;
                CurrentJaegerModule = null;
                jaegerTitle = null;
                onJaegerDone = null;
                mod.transform.SetParent(null, true);
                CleanupPivot();
                ModelUtils.DestroySafe(mod);
            }

            if (CurrentPart != null)
            {
                MechPart part = CurrentPart;
                CurrentPart = null;
                part.ReturnToSocket();
                CleanupPivot();
            }

            HangarMechManager.Instance?.UpdatePodiumDisplay();
            GameManager.Instance?.SwitchView(RoomView.Hangar);
        }

        private void StopMount()
        {
            if (mountRoutine != null)
            {
                StopCoroutine(mountRoutine);
                mountRoutine = null;
            }
        }

        private void CleanupPivot()
        {
            if (workPivot != null)
            {
                ModelUtils.DestroySafe(workPivot.gameObject);
                workPivot = null;
            }
        }

        private Bounds GetPivotLocalBounds()
        {
            Bounds world = ModelUtils.CalcBounds(workPivot.gameObject, false);
            return new Bounds(workPivot.InverseTransformPoint(world.center), world.size);
        }
    }
}
