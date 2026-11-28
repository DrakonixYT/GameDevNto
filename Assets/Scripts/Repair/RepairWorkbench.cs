using System;
using System.Collections;
using HangarGame.Core;
using HangarGame.Mechs;
using UnityEngine;

namespace HangarGame.Repair
{
    public enum WorkbenchState
    {
        Idle,
        Mounting,
        Active
    }

    public class RepairWorkbench : MonoBehaviour
    {
        [Header("Ссылки")]
        public GameManager game;
        public HangarMechManager hangar;

        [Header("Размещение детали")]
        public Vector3 tableRestOffset = new Vector3(0f, 0.94f, -0.02f);
        public float normalizedPartExtent = 0.86f;
        public Vector3 initialEuler;
        public GameObject platePrefab;

        [SerializeField] private float rotationSpeed = 95f;
        [SerializeField] private float mouseSensitivity = 0.4f;
        [SerializeField] private float minPitch = -65f;
        [SerializeField] private float maxPitch = 65f;

        public WorkbenchState State { get; private set; } = WorkbenchState.Idle;
        public MechPart CurrentPart { get; private set; }
        public GameObject CurrentJaegerModule { get; private set; }
        public bool HasItem => State != WorkbenchState.Idle;
        public bool IsWorkComplete => minigame == null || minigame.IsComplete;
        public string RepairStepInfo => minigame != null ? minigame.StatusText : "";
        public string CurrentItemName => CurrentPart?.Data != null
            ? CurrentPart.Data.partName
            : (!string.IsNullOrEmpty(jaegerTitle) ? jaegerTitle : "Деталь");

        public event Action OnWorkChanged;

        private WorkbenchMinigame minigame;
        private Transform workPivot;
        private Coroutine mountRoutine;
        private float targetYaw, targetPitch, curYaw, curPitch;
        private string jaegerTitle;
        private Action onJaegerDone;

        private void Awake()
        {
            game ??= GameManager.Instance ?? FindAnyObjectByType<GameManager>();
            hangar ??= game?.hangar ?? FindAnyObjectByType<HangarMechManager>();
            minigame = ModelUtils.GetOrAdd<WorkbenchMinigame>(gameObject);
            minigame.game = game;
            minigame.OnStepChanged += NotifyWorkChanged;
            if (platePrefab != null) minigame.SetPlatePrefab(platePrefab);
        }

        private void OnDestroy()
        {
            if (minigame != null) minigame.OnStepChanged -= NotifyWorkChanged;
        }

        private void NotifyWorkChanged() => OnWorkChanged?.Invoke();

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

            State = WorkbenchState.Mounting;
            FitTargetInPivot(target);
            game?.SwitchView(RoomView.WorkbenchFocus);

            minigame ??= ModelUtils.GetOrAdd<WorkbenchMinigame>(gameObject);
            minigame.game = game;
            if (platePrefab != null) minigame.SetPlatePrefab(platePrefab);

            OnWorkChanged?.Invoke();

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
                if (CurrentPart == null && b.size.y > Mathf.Max(b.size.x, b.size.z) * 2.2f)
                    target.localRotation = Quaternion.Euler(0f, 180f, -68f);

                b = ModelUtils.CalcBounds(target.gameObject, false);
                target.position += workPivot.position - b.center;

                b = ModelUtils.CalcBounds(target.gameObject, false);
                float minCenterY = GetTableTopY() + 0.025f + b.extents.y;
                if (workPivot.position.y < minCenterY)
                    workPivot.position = new Vector3(workPivot.position.x, minCenterY, workPivot.position.z);
            }
            else
            {
                target.localPosition = Vector3.zero;
            }
        }

        private float GetTableTopY()
        {
            Transform top = transform.Find("Tabletop");
            Renderer r = top != null ? top.GetComponent<Renderer>() : null;
            return r != null ? r.bounds.max.y : transform.position.y + 0.81f;
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
            State = WorkbenchState.Active;
            Bounds world = ModelUtils.CalcBounds(workPivot.gameObject, false);
            Bounds local = new Bounds(workPivot.InverseTransformPoint(world.center), world.size);
            minigame.Begin(workPivot, local, Vector3.back, isJaeger);
            OnWorkChanged?.Invoke();
        }

        public void FinishWork() => ClearCurrentWork(applyResult: true);
        public void CancelWork() => ClearCurrentWork(applyResult: false);

        private void ClearCurrentWork(bool applyResult)
        {
            if (mountRoutine != null)
            {
                StopCoroutine(mountRoutine);
                mountRoutine = null;
            }
            minigame?.Clear();
            State = WorkbenchState.Idle;

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
                    game?.PlaySfx(Sfx.Success);
                }
            }
            else if (CurrentPart != null)
            {
                MechPart part = CurrentPart;
                CurrentPart = null;
                if (applyResult && part.Data != null)
                    game?.TrySpend(part.Data.repairCredits, part.Data.repairAlloy);

                part.ReturnToSocket();
                DestroyPivot();

                if (applyResult)
                {
                    part.RepairFull();
                    part.GetComponentInParent<MechUnit>()?.SyncHpFromParts();
                    game?.PlaySfx(Sfx.Success);
                }
            }

            hangar?.UpdatePodiumDisplay();
            game?.SwitchView(RoomView.Hangar);
            OnWorkChanged?.Invoke();
        }

        private void DestroyPivot()
        {
            if (workPivot == null) return;
            ModelUtils.DestroySafe(workPivot.gameObject);
            workPivot = null;
        }
    }
}
