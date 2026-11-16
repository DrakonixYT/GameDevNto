using System;
using System.Collections.Generic;
using UnityEngine;
using HangarGame.Core;
using HangarGame.Mechs;
using HangarGame.Data;
using HangarGame.CameraControl;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HangarGame.Repair
{
    public class RepairWorkbench : MonoBehaviour
    {
        public static RepairWorkbench Instance { get; private set; }

        [Header("Workbench Mount")]
        [SerializeField] private Transform partMountPoint;
        [SerializeField] private float tableSurfaceY = 0.82f;

        [Header("2-Axis Inspection Rotation")]
        [SerializeField] private float keyboardRotateSpeed = 100f;
        [SerializeField] private float mouseRotateSensitivity = 0.5f;

        [Header("Mini-games")]
        [SerializeField] private BoltMiniGame boltGame;
        [SerializeField] private WeldingMiniGame weldingGame;
        [SerializeField] private WiringMiniGame wiringGame;

        private MechPartInstance currentPart;
        private Vector3 originalPartPosition;
        private Quaternion originalPartRotation;
        private Transform originalPartParent;
        private bool originalPartKinematic = true;

        // Вращение детали по 2 осям
        private float currentYaw = 0f;
        private float currentPitch = 0f;
        private float targetYaw = 0f;
        private float targetPitch = 0f;
        private Quaternion initialRotationOnTable;

        // Поддержка сборки модулей Егеря на верстаке
        private GameObject currentJaegerModule;
        private string currentJaegerModuleName;
        private Action onJaegerModuleCompleted;

        public MechPartInstance CurrentPart => currentPart;
        public GameObject CurrentJaegerModule => currentJaegerModule;
        public bool HasPart => currentPart != null || currentJaegerModule != null;
        public bool IsJaegerModule => currentJaegerModule != null;
        public string CurrentItemName => currentPart != null ? currentPart.PartName : (currentJaegerModule != null ? currentJaegerModuleName : string.Empty);
        public Transform MountPoint => partMountPoint;

        private void Awake()
        {
            Instance = this;

            if (boltGame == null) boltGame = GetComponentInChildren<BoltMiniGame>(true);
            if (weldingGame == null) weldingGame = GetComponentInChildren<WeldingMiniGame>(true);
            if (wiringGame == null) wiringGame = GetComponentInChildren<WiringMiniGame>(true);

            if (partMountPoint == null)
            {
                Transform existing = transform.Find("Part_Mount_Point");
                if (existing != null)
                {
                    partMountPoint = existing;
                }
                else
                {
                    GameObject mountObj = new GameObject("Part_Mount_Point");
                    mountObj.transform.position = new Vector3(transform.position.x, tableSurfaceY, transform.position.z);
                    mountObj.transform.SetParent(transform);
                    partMountPoint = mountObj.transform;
                }
            }
        }

        private void Start()
        {
            if (boltGame != null) boltGame.OnAllBoltsCompleted += HandleBoltsDone;
            if (weldingGame != null) weldingGame.OnWeldingCompleted += HandleWeldingDone;
            if (wiringGame != null) wiringGame.OnWiringCompleted += HandleWiringDone;
        }

        private void OnDestroy()
        {
            if (boltGame != null) boltGame.OnAllBoltsCompleted -= HandleBoltsDone;
            if (weldingGame != null) weldingGame.OnWeldingCompleted -= HandleWeldingDone;
            if (wiringGame != null) wiringGame.OnWiringCompleted -= HandleWiringDone;
        }

        private void Update()
        {
            if (!HasPart) return;

            HandleInput();
            Update2AxisRotation();
        }

        private void HandleInput()
        {
            bool esc = false;
            bool space = false;
            bool swapKey = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                esc = Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.backspaceKey.wasPressedThisFrame;
                space = Keyboard.current.spaceKey.wasPressedThisFrame;
                swapKey = Keyboard.current.tKey.wasPressedThisFrame;
            }
#else
            esc = Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace);
            space = Input.GetKeyDown(KeyCode.Space);
            swapKey = Input.GetKeyDown(KeyCode.T);
#endif

            if (esc)
            {
                CancelOrReturnCurrentPart();
            }
            else if (space)
            {
                FinishRepair();
            }
            else if (swapKey && currentPart != null)
            {
                CycleNextModuleForCurrentPart();
            }
        }

        private void Update2AxisRotation()
        {
            // Управление клавишами (A/D/Left/Right - Yaw, W/S/Up/Down - Pitch)
            float hInput = 0f;
            float vInput = 0f;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) hInput -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) hInput += 1f;
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) vInput += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) vInput -= 1f;
            }
#else
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) hInput -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) hInput += 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) vInput += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) vInput -= 1f;
#endif

            // Вращение перетаскиванием мыши (ПКМ или СКМ)
            bool isMouseDragging = false;
            Vector2 mouseDelta = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && (Mouse.current.rightButton.isPressed || Mouse.current.middleButton.isPressed))
            {
                isMouseDragging = true;
                mouseDelta = Mouse.current.delta.ReadValue();
            }
#else
            if (Input.GetMouseButton(1) || Input.GetMouseButton(2))
            {
                isMouseDragging = true;
                mouseDelta = new Vector2(Input.GetAxis("Mouse X") * 8f, Input.GetAxis("Mouse Y") * 8f);
            }
#endif

            if (isMouseDragging)
            {
                targetYaw += mouseDelta.x * mouseRotateSensitivity;
                targetPitch -= mouseDelta.y * mouseRotateSensitivity;
            }

            if (Mathf.Abs(hInput) > 0.01f || Mathf.Abs(vInput) > 0.01f)
            {
                targetYaw += hInput * keyboardRotateSpeed * Time.deltaTime;
                targetPitch -= vInput * keyboardRotateSpeed * Time.deltaTime;
            }

            targetPitch = Mathf.Clamp(targetPitch, -75f, 75f);
            currentYaw = Mathf.Lerp(currentYaw, targetYaw, Time.deltaTime * 14f);
            currentPitch = Mathf.Lerp(currentPitch, targetPitch, Time.deltaTime * 14f);

            Transform targetTransform = currentPart != null ? currentPart.transform : (currentJaegerModule != null ? currentJaegerModule.transform : null);
            if (targetTransform != null)
            {
                targetTransform.rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
            }
        }

        #region Public 2-Axis Rotation Controls (Для кнопок UI)

        public void RotatePartYaw(float deltaYaw)
        {
            targetYaw += deltaYaw;
        }

        public void RotatePartPitch(float deltaPitch)
        {
            targetPitch = Mathf.Clamp(targetPitch + deltaPitch, -75f, 75f);
        }

        public void ResetPartRotation()
        {
            targetYaw = initialRotationOnTable.eulerAngles.y;
            targetPitch = initialRotationOnTable.eulerAngles.x;
        }

        #endregion

        public void ReceivePartForRepair(MechPartInstance part)
        {
            if (HasPart)
            {
                Debug.LogWarning("[RepairWorkbench] На верстаке уже лежит объект!");
                return;
            }

            currentPart = part;
            originalPartPosition = part.transform.position;
            originalPartRotation = part.transform.rotation;
            originalPartParent = part.transform.parent;

            Rigidbody rb = part.GetComponent<Rigidbody>();
            if (rb != null)
            {
                originalPartKinematic = rb.isKinematic;
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
            }

            Collider col = part.GetComponent<Collider>();
            float halfHeight = 0.25f;
            if (col != null)
            {
                halfHeight = col.bounds.extents.y;
            }

            Vector3 tableCenter = partMountPoint != null ? partMountPoint.position : new Vector3(0f, tableSurfaceY, 2.2f);
            
            if (part.Slot == PartSlot.Legs)
            {
                part.transform.rotation = Quaternion.Euler(0f, 90f, 90f);
                part.transform.position = new Vector3(tableCenter.x, tableSurfaceY + 0.25f, tableCenter.z);
            }
            else if (part.Slot == PartSlot.RightArm)
            {
                part.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                part.transform.position = new Vector3(tableCenter.x, tableSurfaceY + 0.25f, tableCenter.z);
            }
            else
            {
                part.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                part.transform.position = new Vector3(tableCenter.x, tableSurfaceY + halfHeight + 0.05f, tableCenter.z);
            }

            initialRotationOnTable = part.transform.rotation;
            targetYaw = initialRotationOnTable.eulerAngles.y;
            targetPitch = initialRotationOnTable.eulerAngles.x;
            currentYaw = targetYaw;
            currentPitch = targetPitch;

            GameManager.Instance?.SwitchRoomView(RoomView.WorkbenchFocus);

            Vector3 miniGameCenter = new Vector3(tableCenter.x, tableSurfaceY + 0.35f, tableCenter.z);

            // Разнообразие мини-игр в зависимости от слота и урона
            if (part.Slot == PartSlot.Head && wiringGame != null)
            {
                wiringGame.StartMiniGameAt(miniGameCenter);
            }
            else if (part.DurabilityPercent < 0.5f && weldingGame != null)
            {
                weldingGame.StartMiniGameAt(miniGameCenter);
            }
            else if (boltGame != null)
            {
                boltGame.StartMiniGameAt(miniGameCenter);
            }

            EventBus.TriggerPartSentToWorkbench(part);
            Debug.Log($"<color=cyan>[RepairWorkbench]</color> Деталь {part.name} на верстаке. (A/D/W/S или ПКМ - вращение, Пробел - ремонт, T - смена модуля, Esc - выход).");
        }

        public void ReceiveJaegerModule(GameObject moduleObj, string moduleName, int miniGameType, Action onComplete)
        {
            if (HasPart)
            {
                Debug.LogWarning("[RepairWorkbench] На верстаке уже лежит объект!");
                return;
            }

            currentJaegerModule = moduleObj;
            currentJaegerModuleName = moduleName;
            onJaegerModuleCompleted = onComplete;

            Vector3 tableCenter = partMountPoint != null ? partMountPoint.position : new Vector3(0f, tableSurfaceY, 2.2f);
            currentJaegerModule.transform.position = new Vector3(tableCenter.x, tableSurfaceY + 0.3f, tableCenter.z);
            currentJaegerModule.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            initialRotationOnTable = currentJaegerModule.transform.rotation;
            targetYaw = 180f;
            targetPitch = 0f;
            currentYaw = 180f;
            currentPitch = 0f;

            GameManager.Instance?.SwitchRoomView(RoomView.WorkbenchFocus);

            Vector3 miniGameCenter = new Vector3(tableCenter.x, tableSurfaceY + 0.35f, tableCenter.z);
            if (miniGameType == 0 && wiringGame != null)
            {
                wiringGame.StartMiniGameAt(miniGameCenter);
            }
            else if (miniGameType == 1 && weldingGame != null)
            {
                weldingGame.StartMiniGameAt(miniGameCenter);
            }
            else if (boltGame != null)
            {
                boltGame.StartMiniGameAt(miniGameCenter);
            }

            Debug.Log($"<color=cyan>[RepairWorkbench]</color> Сборка модуля Егеря: <b>{moduleName}</b>. Вращайте A/D/W/S или ПКМ, выполните мини-игру!");
        }

        public void CycleNextModuleForCurrentPart()
        {
            if (currentPart == null) return;

            List<MechPartData> availableModules = ModuleCatalog.GetModulesForSlot(currentPart.Slot);
            if (availableModules.Count <= 1)
            {
                Debug.LogWarning("[RepairWorkbench] Для этого слота нет других доступных модулей!");
                return;
            }

            int currentIndex = -1;
            for (int i = 0; i < availableModules.Count; i++)
            {
                if (currentPart.Data != null && currentPart.Data.partId == availableModules[i].partId)
                {
                    currentIndex = i;
                    break;
                }
            }

            int nextIndex = (currentIndex + 1) % availableModules.Count;
            MechPartData nextModule = availableModules[nextIndex];

            // Устанавливаем новый модуль
            currentPart.Initialize(nextModule, currentPart.Slot, 100f);

            HangarAudio.Instance?.PlayBolt();
            CameraShaker.Instance?.Shake(0.2f, 0.08f);

            Debug.Log($"<color=green>[КАСТОМИЗАЦИЯ]</color> Установлен новый модуль: <b>{nextModule.partName}</b>! Урон: {nextModule.attackType} ({nextModule.attackPower}), Защита: {nextModule.resistanceType} ({nextModule.defensePower})");
        }

        private void HandleWeldingDone()
        {
            Vector3 center = partMountPoint != null ? partMountPoint.position : new Vector3(0f, tableSurfaceY, 2.2f);
            if (boltGame != null && currentPart != null && currentPart.DurabilityPercent < 0.3f)
            {
                boltGame.StartMiniGameAt(new Vector3(center.x, tableSurfaceY + 0.35f, center.z));
            }
            else
            {
                FinishRepair();
            }
        }

        private void HandleWiringDone()
        {
            FinishRepair();
        }

        private void HandleBoltsDone()
        {
            FinishRepair();
        }

        public void FinishRepair()
        {
            if (currentJaegerModule != null)
            {
                CleanupMiniGames();
                Action callback = onJaegerModuleCompleted;
                GameObject modObj = currentJaegerModule;
                currentJaegerModule = null;
                currentJaegerModuleName = null;
                onJaegerModuleCompleted = null;

                if (modObj != null) Destroy(modObj);

                HangarAudio.Instance?.PlaySuccess();
                callback?.Invoke();
                GameManager.Instance?.SwitchRoomView(RoomView.Hangar);
                return;
            }

            if (currentPart == null) return;

            int creditCost = currentPart.Data != null ? currentPart.Data.repairCreditCost : 30;
            int alloyCost = currentPart.Data != null ? currentPart.Data.repairAlloyCost : 15;

            GameManager.Instance?.TrySpendResources(creditCost, alloyCost);
            currentPart.RepairFull();

            HangarAudio.Instance?.PlaySuccess();
            Debug.Log($"<color=green>[RepairWorkbench] Деталь {currentPart.name} отремонтирована на 100%!</color>");

            EventBus.TriggerPartRepairFinished(currentPart);
            ReturnPartToMech();
        }

        public void CancelOrReturnCurrentPart()
        {
            if (currentJaegerModule != null)
            {
                CleanupMiniGames();
                Destroy(currentJaegerModule);
                currentJaegerModule = null;
                currentJaegerModuleName = null;
                onJaegerModuleCompleted = null;
                GameManager.Instance?.SwitchRoomView(RoomView.Hangar);
                return;
            }

            ReturnPartToMech();
        }

        public void ReturnPartToMech()
        {
            if (currentPart == null) return;

            CleanupMiniGames();

            Rigidbody rb = currentPart.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = originalPartKinematic;
            }

            currentPart.ReturnToSocket();
            currentPart = null;

            GameManager.Instance?.SwitchRoomView(RoomView.Hangar);
        }

        private void CleanupMiniGames()
        {
            if (boltGame != null) boltGame.Cleanup();
            if (weldingGame != null) weldingGame.Cleanup();
            if (wiringGame != null) wiringGame.Cleanup();
        }
    }
}
