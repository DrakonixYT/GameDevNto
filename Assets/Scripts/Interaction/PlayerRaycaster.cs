using System;
using System.Collections.Generic;
using HangarGame.Core;
using HangarGame.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HangarGame.Interaction
{
    public class PlayerRaycaster : MonoBehaviour
    {
        public static PlayerRaycaster Instance { get; private set; }
        public string CurrentPrompt { get; private set; } = "";

        [SerializeField] private float maxRayDistance = 45f;
        [SerializeField] private float sphereCastFallbackRadius = 0.06f;

        private UnityEngine.Camera cam;
        private IInteractable hovered;
        private IInteractable pressedTarget;
        private readonly List<RaycastResult> uiResults = new List<RaycastResult>();

        private void Awake()
        {
            Instance = this;
            cam = GetComponent<UnityEngine.Camera>();
            if (cam == null) cam = UnityEngine.Camera.main;
        }

        private void Update()
        {
            if (cam == null) return;

            if (GameManager.Instance != null && (GameManager.Instance.IsGameWon || GameManager.Instance.IsGameLost))
            {
                ClearHover();
                pressedTarget = null;
                return;
            }

            Vector2 mousePos = ReadMousePosition();

            if (IsPointerOverUI(mousePos))
            {
                ClearHover();
                if (!ReadMouseHeld()) pressedTarget = null;
                return;
            }

            IInteractable best = FindTarget(cam.ScreenPointToRay(mousePos));

            if (best != hovered)
            {
                ClearHover();
                hovered = best;
                hovered?.OnHover(true);
            }

            CurrentPrompt = hovered != null ? hovered.GetPrompt() : "";

            if (ReadMouseDown() && hovered != null)
            {
                pressedTarget = hovered;
                hovered.OnInteract();
            }
            else if (ReadMouseHeld() && pressedTarget != null && pressedTarget == hovered && pressedTarget.CanInteract())
            {
                pressedTarget.OnInteractHold();
            }
            else if (!ReadMouseHeld())
            {
                pressedTarget = null;
            }
        }

        private IInteractable FindTarget(Ray ray)
        {
            var hits = Physics.RaycastAll(ray, maxRayDistance);
            IInteractable match = PickFirstInteractable(hits);
            if (match != null) return match;

            var sphereHits = Physics.SphereCastAll(ray, sphereCastFallbackRadius, maxRayDistance);
            return PickFirstInteractable(sphereHits);
        }

        private IInteractable PickFirstInteractable(RaycastHit[] hits)
        {
            if (hits == null || hits.Length == 0) return null;
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                var interactable = hits[i].collider.GetComponentInParent<IInteractable>();
                if (interactable != null && interactable.CanInteract())
                    return interactable;
            }
            return null;
        }

        private bool IsPointerOverUI(Vector2 mousePos)
        {
            if (EventSystem.current == null) return false;
            var ped = new PointerEventData(EventSystem.current) { position = mousePos };
            uiResults.Clear();
            EventSystem.current.RaycastAll(ped, uiResults);

            for (int i = 0; i < uiResults.Count; i++)
            {
                var hitGo = uiResults[i].gameObject;
                if (hitGo.GetComponentInParent<Selectable>() != null)
                    return true;

                var canvas = hitGo.GetComponentInParent<Canvas>();
                var graphic = hitGo.GetComponent<Graphic>();
                if (canvas != null && canvas.renderMode != RenderMode.WorldSpace && graphic != null && graphic.raycastTarget)
                    return true;
            }
            return false;
        }

        private void ClearHover()
        {
            if (hovered != null)
            {
                hovered.OnHover(false);
                hovered = null;
            }
            CurrentPrompt = "";
        }

        private Vector2 ReadMousePosition()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
            return Input.mousePosition;
#endif
        }

        private bool ReadMouseDown()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(0);
#endif
        }

        private bool ReadMouseHeld()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.isPressed;
#else
            return Input.GetMouseButton(0);
#endif
        }
    }
}
