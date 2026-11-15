using UnityEngine;
using HangarGame.Core;
using HangarGame.Repair;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HangarGame.CameraControl
{
    public class RoomCameraController : MonoBehaviour
    {
        [Header("View Transforms / Points")]
        [Tooltip("Базовая позиция игрока в комнате (по центру)")]
        [SerializeField] private Vector3 roomPosition = new Vector3(0f, 1.6f, 0f);
        [Tooltip("Позиция камеры в режиме верстака")]
        [SerializeField] private Vector3 workbenchPosition = new Vector3(0f, 1.4f, 1.3f);
        [Tooltip("Угол взгляда на верстаке")]
        [SerializeField] private Vector3 workbenchEulerAngles = new Vector3(30f, 0f, 0f);

        [Header("Room Angles (180 deg FNAF-style rotation)")]
        [Tooltip("0 градусов — вид на ангар с мехами")]
        [SerializeField] private float hangarYaw = 0f;
        [Tooltip("180 градусов — вид на диспетчерский пункт")]
        [SerializeField] private float dispatchYaw = 180f;

        [Header("Cursor Lean (Динамический доворот за курсором)")]
        [Tooltip("Максимальный угол доворота по горизонтали (в градусах)")]
        [SerializeField] private float maxLeanYaw = 12f;
        [Tooltip("Максимальный угол доворота по вертикали (в градусах)")]
        [SerializeField] private float maxLeanPitch = 8f;
        [Tooltip("Плавность следования за курсором")]
        [SerializeField] private float leanSmoothSpeed = 6f;

        [Header("Transition Smoothing")]
        [SerializeField] private float moveSmoothSpeed = 6f;
        [SerializeField] private float rotationSmoothSpeed = 7f;

        private RoomView currentView = RoomView.Hangar;
        private Vector2 currentLean;

        public RoomView CurrentView => currentView;

        private void OnEnable()
        {
            EventBus.OnRoomViewChanged += HandleRoomViewChanged;
        }

        private void OnDisable()
        {
            EventBus.OnRoomViewChanged -= HandleRoomViewChanged;
        }

        private void Start()
        {
            transform.position = roomPosition;
            transform.rotation = Quaternion.Euler(0f, hangarYaw, 0f);
        }

        private void Update()
        {
            HandleKeyboardNavigation();
            UpdateCursorLean();
            UpdateCameraTransform();
        }

        private void HandleKeyboardNavigation()
        {
            // Поддержка клавиатуры (A, D, Q, E, стрелки, Esc, Space)
            bool turnLeft = false;
            bool turnRight = false;
            bool escape = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                turnLeft = Keyboard.current.aKey.wasPressedThisFrame || 
                           Keyboard.current.qKey.wasPressedThisFrame || 
                           Keyboard.current.leftArrowKey.wasPressedThisFrame;

                turnRight = Keyboard.current.dKey.wasPressedThisFrame || 
                            Keyboard.current.eKey.wasPressedThisFrame || 
                            Keyboard.current.rightArrowKey.wasPressedThisFrame;

                escape = Keyboard.current.escapeKey.wasPressedThisFrame || 
                         Keyboard.current.backspaceKey.wasPressedThisFrame;
            }
#else
            turnLeft = Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.LeftArrow);
            turnRight = Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.RightArrow);
            escape = Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace);
#endif

            if (turnLeft || turnRight)
            {
                Toggle180View();
            }

            if (escape && currentView == RoomView.WorkbenchFocus)
            {
                RepairWorkbench.Instance?.ReturnPartToMech();
            }
        }

        private void HandleRoomViewChanged(RoomView view)
        {
            currentView = view;
        }

        /// <summary>
        /// Поворачивает камеру на 180 градусов (переключение между Ангаром и Диспетчерской)
        /// </summary>
        public void Toggle180View()
        {
            if (currentView == RoomView.Hangar)
            {
                GameManager.Instance?.SwitchRoomView(RoomView.DispatchConsole);
            }
            else if (currentView == RoomView.DispatchConsole)
            {
                GameManager.Instance?.SwitchRoomView(RoomView.Hangar);
            }
            else if (currentView == RoomView.WorkbenchFocus)
            {
                // Возврат из верстака обратно в ангар
                RepairWorkbench.Instance?.ReturnPartToMech();
            }
        }

        public void FocusHangar() => GameManager.Instance?.SwitchRoomView(RoomView.Hangar);
        public void FocusDispatch() => GameManager.Instance?.SwitchRoomView(RoomView.DispatchConsole);
        public void FocusWorkbench() => GameManager.Instance?.SwitchRoomView(RoomView.WorkbenchFocus);

        private Vector2 GetNormalizedMousePosition()
        {
            Vector2 mousePos = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                mousePos = Mouse.current.position.ReadValue();
            }
            else
            {
                mousePos = Input.mousePosition;
            }
#else
            mousePos = Input.mousePosition;
#endif

            float screenW = Mathf.Max(1, Screen.width);
            float screenH = Mathf.Max(1, Screen.height);

            float normX = (mousePos.x / screenW - 0.5f) * 2f;
            float normY = (mousePos.y / screenH - 0.5f) * 2f;

            normX = Mathf.Clamp(normX, -1f, 1f);
            normY = Mathf.Clamp(normY, -1f, 1f);

            return new Vector2(normX, normY);
        }

        private void UpdateCursorLean()
        {
            // В режиме верстака отключаем доворот, чтобы игрок мог свободно откручивать болты
            if (currentView == RoomView.WorkbenchFocus)
            {
                currentLean = Vector2.Lerp(currentLean, Vector2.zero, Time.deltaTime * leanSmoothSpeed);
                return;
            }

            Vector2 normMouse = GetNormalizedMousePosition();
            Vector2 targetLean = new Vector2(
                normMouse.x * maxLeanYaw,
                -normMouse.y * maxLeanPitch
            );

            currentLean = Vector2.Lerp(currentLean, targetLean, Time.deltaTime * leanSmoothSpeed);
        }

        private void UpdateCameraTransform()
        {
            Vector3 targetPosition = roomPosition;
            Quaternion targetRotation = Quaternion.identity;

            switch (currentView)
            {
                case RoomView.Hangar:
                    targetPosition = roomPosition;
                    targetRotation = Quaternion.Euler(currentLean.y, hangarYaw + currentLean.x, 0f);
                    break;

                case RoomView.DispatchConsole:
                    targetPosition = roomPosition;
                    targetRotation = Quaternion.Euler(currentLean.y, dispatchYaw - currentLean.x, 0f);
                    break;

                case RoomView.WorkbenchFocus:
                    targetPosition = workbenchPosition;
                    targetRotation = Quaternion.Euler(workbenchEulerAngles);
                    break;
            }

            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveSmoothSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSmoothSpeed);
        }
    }
}
