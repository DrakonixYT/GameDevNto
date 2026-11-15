using UnityEngine;
using UnityEngine.Events;
using HangarGame.Core;
using HangarGame.Dispatch;
using HangarGame.Mechs;
using HangarGame.Repair;

namespace HangarGame.Interaction
{
    public enum ClickableActionType
    {
        CustomEvent,
        DeployMech,
        ScanRadar,
        ToggleRoomView,
        BuildJaegerModule,
        ExitWorkbench,
        InstantRepair
    }

    [RequireComponent(typeof(Collider))]
    public class Clickable3DObject : MonoBehaviour, IInteractable
    {
        [Header("General")]
        [SerializeField] private string objectName = "3D Кнопка";
        [SerializeField] private string actionDescription = "Нажмите для активации";

        [Header("Built-in Action Type")]
        [SerializeField] private ClickableActionType actionType = ClickableActionType.CustomEvent;

        [Header("Event Callback")]
        [Tooltip("Сюда можно подключить ЛЮБУЮ функцию любого объекта из 100500 вариантов")]
        public UnityEvent onInteract = new UnityEvent();

        [Header("Visual Feedback")]
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color hoverColor = Color.yellow;
        [SerializeField] private Color pressColor = Color.green;

        private Vector3 originalLocalPos;
        private bool isPressed = false;

        public ClickableActionType ActionType
        {
            get => actionType;
            set => actionType = value;
        }

        private void Awake()
        {
            if (targetRenderer == null)
            {
                targetRenderer = GetComponent<Renderer>();
            }

            if (targetRenderer != null)
            {
                targetRenderer.material.color = normalColor;
            }

            originalLocalPos = transform.localPosition;
        }

        public void OnHoverEnter()
        {
            if (targetRenderer != null && !isPressed)
            {
                targetRenderer.material.color = hoverColor;
            }
        }

        public void OnHoverExit()
        {
            if (targetRenderer != null && !isPressed)
            {
                targetRenderer.material.color = normalColor;
            }
        }

        public void OnInteract()
        {
            HangarAudio.Instance?.PlayClick();

            // Визуальный отклик — кратковременное вдавливание кнопки
            StopAllCoroutines();
            StartCoroutine(PressFeedbackRoutine());

            // 1. Выполняем встроенное действие по типу
            ExecuteBuiltinAction();

            // 2. Вызов подключенного события UnityEvent (если есть)
            onInteract?.Invoke();

            Debug.Log($"<color=cyan>[Clickable3DObject]</color> Нажата 3D кнопка '{objectName}' (Тип действия: {actionType})");
        }

        private void ExecuteBuiltinAction()
        {
            switch (actionType)
            {
                case ClickableActionType.DeployMech:
                    if (DispatchManager.Instance != null)
                    {
                        DispatchManager.Instance.DeployAvailableMech();
                    }
                    break;

                case ClickableActionType.ScanRadar:
                    if (DispatchManager.Instance != null)
                    {
                        DispatchManager.Instance.SpawnNextThreat();
                    }
                    break;

                case ClickableActionType.ToggleRoomView:
                    if (GameManager.Instance != null)
                    {
                        var target = GameManager.Instance.CurrentView == RoomView.Hangar 
                            ? RoomView.DispatchConsole 
                            : RoomView.Hangar;
                        GameManager.Instance.SwitchRoomView(target);
                    }
                    break;

                case ClickableActionType.BuildJaegerModule:
                    JaegerProjectManager.Instance?.InvestInJaeger();
                    break;

                case ClickableActionType.ExitWorkbench:
                    RepairWorkbench.Instance?.ReturnPartToMech();
                    break;

                case ClickableActionType.InstantRepair:
                    RepairWorkbench.Instance?.FinishRepair();
                    break;
            }
        }

        private System.Collections.IEnumerator PressFeedbackRoutine()
        {
            isPressed = true;
            if (targetRenderer != null)
            {
                targetRenderer.material.color = pressColor;
            }

            transform.localPosition = originalLocalPos - transform.up * 0.05f;
            yield return new WaitForSeconds(0.12f);
            transform.localPosition = originalLocalPos;
            isPressed = false;

            if (targetRenderer != null)
            {
                targetRenderer.material.color = normalColor;
            }
        }

        public string GetInteractionPrompt()
        {
            return $"{objectName} — {actionDescription}";
        }
    }
}
