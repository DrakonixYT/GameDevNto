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

        public Vector3 tableRestOffset = new Vector3(0f, 1.00f, -0.04f);
        public float normalizedPartExtent = 0.64f;
        public Vector3 initialEuler;
        public GameObject platePrefab;

        [SerializeField] private float rotationSpeed = 95f;
        [SerializeField] private float mouseSensitivity = 0.4f;
        [SerializeField] private float minPitch = -65f;
        [SerializeField] private float maxPitch = 65f;

        public MechPart CurrentPart { get; private set; }
        public GameObject CurrentJaegerModule { get; private set; }
        public bool HasItem => CurrentPart != null || CurrentJaegerModule != null;
        public bool IsWorkComplete => minigame == null || minigame.IsComplete;
        public string RepairStepInfo => minigame != null ? minigame.StatusText : "";
        public string CurrentItemName => CurrentPart?.Data != null
            ? CurrentPart.Data.partName
            : (!string.IsNullOrEmpty(jaegerTitle) ? jaegerTitle : "Деталь");

        private WorkbenchMinigame minigame;
        private Transform workPivot;
        private Coroutine mountRoutine;
        private float targetYaw, targetPitch, curYaw, curPitch;
        private string jaegerTitle;
        private Action onJaegerDone;

        private void Awake()
        {
            Instance = this;
            minigame = ModelUtils.GetOrAdd<WorkbenchMinigame>(gameObject);
            if (platePrefab != null) minigame.SetPlatePrefab(platePrefab);
        }

        private void Update()
        {
            if (!HasItem) return;

            if (GameInput.Cancel) { CancelWork(); return; }
            if (GameInput.Confirm && IsWorkComplete) { FinishWork(); return; }

            Vector2 axis = GameInput.WorkbenchRotate();
            if (GameInput.RightHeld)
            {
                Vector2 d = GameInput.PointerDelta;
                targetYaw += d.x * mouseSensitivity;
                targetPitch = Mathf.Clamp(targetPitch - d.y * mouseSensitivity, minPitch, maxPitch);
            }

            targetYaw += axis.x * rotationSpeed * Time.deltaTime;
            targetPitch = Mathf.Clamp(targetPitch - axis.y * rotationSpeed * Time.deltaTime, minPitch, maxPitch);

            curYaw = Mathf.Lerp(curYaw, targetYaw, Time.deltaTime * 12f);
            curPitch = Mathf.Lerp(curPitch, targetPitch, Time.deltaTime * 12f);

            if (workPivot != null)
                workPivot.localRotation = Quaternion.Euler(curPitch, curYaw, 0f);
        }

        public void Rotate(float dYaw, float dPitch)
        {
            targetYaw += dYaw;
            targetPitch = Mathf.Clamp(targetPitch + dPitch, minPitch, maxPitch);
        }

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

            BeginMount(part.transform, false);
        }

        public void ReceiveJaegerModule(GameObject moduleObj, string moduleName, Action onDone)
        {
            if (HasItem || moduleObj == null) return;

            CurrentJaegerModule = moduleObj;
            jaegerTitle = moduleName;
            onJaegerDone = onDone;
            foreach (var col in moduleObj.GetComponentsInChildren<Collider>(true)) col.enabled = false;

            BeginMount(moduleObj.transform, true);
        }

        private void BeginMount(Transform target, bool isJaeger)
        {
            if (workPivot != null) ModelUtils.DestroySafe(workPivot.gameObject);

            var pivotObj = new GameObject("Work_Pivot");
            pivotObj.transform.SetParent(transform, false);
            pivotObj.transform.localPosition = tableRestOffset;
            pivotObj.transform.localRotation = Quaternion.Euler(initialEuler);
            workPivot = pivotObj.transform;
            curYaw = targetYaw = initialEuler.y;
            curPitch = targetPitch = initialEuler.x;

            FitTargetInPivot(target);
            GameManager.Instance?.SwitchView(RoomView.WorkbenchFocus);

            if (minigame == null) minigame = ModelUtils.GetOrAdd<WorkbenchMinigame>(gameObject);
            if (platePrefab != null) minigame.SetPlatePrefab(platePrefab);

            if (Application.isPlaying)
                mountRoutine = StartCoroutine(DropIntoPlaceRoutine(target, isJaeger));
            else
                StartMinigame(isJaeger);
        }

        private void FitTargetInPivot(Transform target)
        {
            target.SetParent(workPivot, true);

            if (CurrentPart != null)
            {
                Vector3 fwd = target.up;
                fwd.y = 0f;
                Vector3 desired = -transform.forward;
                desired.y = 0f;
                if (fwd.sqrMagnitude > 0.001f && desired.sqrMagnitude > 0.001f)
                    target.rotation = Quaternion.FromToRotation(fwd.normalized, desired.normalized) * target.rotation;
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

        private IEnumerator DropIntoPlaceRoutine(Transform target, bool isJaeger)
        {
            Vector3 endPos = target.localPosition;
            Vector3 startPos = endPos + workPivot.InverseTransformDirection(Vector3.up * 0.25f);
            target.localPosition = startPos;

            float t = 0f;
            while (t < 0.22f && target != null)
            {
                t += Time.deltaTime;
                target.localPosition = Vector3.Lerp(startPos, endPos, t / 0.22f);
                yield return null;
            }

            if (target != null) target.localPosition = endPos;
            mountRoutine = null;
            StartMinigame(isJaeger);
        }

        private void StartMinigame(bool isJaeger)
        {
            if (workPivot == null) return;
            Bounds world = ModelUtils.CalcBounds(workPivot.gameObject, false);
            Bounds local = new Bounds(workPivot.InverseTransformPoint(world.center), world.size);
            minigame.Begin(workPivot, local, Vector3.back, isJaeger);
        }

        public void FinishWork()
        {
            ClearCurrentWork(applyResult: true);
        }

        public void CancelWork()
        {
            ClearCurrentWork(applyResult: false);
        }

        private void ClearCurrentWork(bool applyResult)
        {
            if (mountRoutine != null)
            {
                StopCoroutine(mountRoutine);
                mountRoutine = null;
            }
            minigame?.Clear();

            if (CurrentJaegerModule != null)
            {
                GameObject mod = CurrentJaegerModule;
                CurrentJaegerModule = null;
                jaegerTitle = null;
                mod.transform.SetParent(null, true);
                DestroyPivot();
                ModelUtils.DestroySafe(mod);

                var cb = onJaegerDone;
                onJaegerDone = null;
                if (applyResult)
                {
                    cb?.Invoke();
                    HangarAudio.Play(Sfx.Success);
                }
            }
            else if (CurrentPart != null)
            {
                MechPart part = CurrentPart;
                CurrentPart = null;
                if (applyResult && part.Data != null)
                    GameManager.Instance?.TrySpend(part.Data.repairCredits, part.Data.repairAlloy);

                part.ReturnToSocket();
                DestroyPivot();

                if (applyResult)
                {
                    part.RepairFull();
                    part.GetComponentInParent<MechUnit>()?.SyncHpFromParts();
                    HangarAudio.Play(Sfx.Success);
                }
            }

            HangarMechManager.Instance?.UpdatePodiumDisplay();
            GameManager.Instance?.SwitchView(RoomView.Hangar);
        }

        private void DestroyPivot()
        {
            if (workPivot == null) return;
            ModelUtils.DestroySafe(workPivot.gameObject);
            workPivot = null;
        }
    }
}
