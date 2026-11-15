using UnityEngine;
using UnityEngine.EventSystems;
using HangarGame.Interaction;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HangarGame.Interaction
{
    public class PlayerRaycaster : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private LayerMask interactableLayers = ~0;
        [SerializeField] private float maxRayDistance = 50f;

        public static PlayerRaycaster Instance { get; private set; }

        public IInteractable CurrentHovered { get; private set; }
        public string CurrentPrompt => CurrentHovered != null ? CurrentHovered.GetInteractionPrompt() : string.Empty;

        private UnityEngine.Camera playerCamera;

        private void Awake()
        {
            Instance = this;
            playerCamera = GetComponent<UnityEngine.Camera>();
            if (playerCamera == null)
            {
                playerCamera = UnityEngine.Camera.main;
            }
        }

        private void Update()
        {
            HandleRaycasting();
            HandleClick();
        }

        private void HandleRaycasting()
        {
            // Не рейкастим сквозь 2D UI
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                ClearHover();
                return;
            }

            Vector2 mouseScreenPos = GetMousePosition();
            if (playerCamera == null) return;

            Ray ray = playerCamera.ScreenPointToRay(mouseScreenPos);
            if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, interactableLayers))
            {
                IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
                if (interactable != null)
                {
                    if (CurrentHovered != interactable)
                    {
                        ClearHover();
                        CurrentHovered = interactable;
                        CurrentHovered.OnHoverEnter();
                    }
                    return;
                }
            }

            ClearHover();
        }

        private void HandleClick()
        {
            if (IsClickTriggered() && CurrentHovered != null)
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                    return;

                CurrentHovered.OnInteract();
            }
        }

        private void ClearHover()
        {
            if (CurrentHovered != null)
            {
                CurrentHovered.OnHoverExit();
                CurrentHovered = null;
            }
        }

        private bool IsClickTriggered()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                return Mouse.current.leftButton.wasPressedThisFrame;
            }
            return Input.GetMouseButtonDown(0);
#else
            return Input.GetMouseButtonDown(0);
#endif
        }

        private Vector2 GetMousePosition()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                return Mouse.current.position.ReadValue();
            }
            return Input.mousePosition;
#else
            return Input.mousePosition;
#endif
        }
    }
}
