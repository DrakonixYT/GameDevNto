using System;
using System.Collections.Generic;
using HangarGame.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HangarGame.Interaction
{
    public class PlayerRaycaster : MonoBehaviour
    {
        public static PlayerRaycaster Instance { get; private set; }
        public string CurrentPrompt { get; private set; } = "";

        [SerializeField] private float maxRayDistance = 45f;
        [SerializeField] private float sphereCastRadius = 0.06f;

        private Camera cam;
        private IInteractable hovered;
        private IInteractable pressed;
        private readonly List<RaycastResult> uiResults = new List<RaycastResult>();

        private void Awake()
        {
            Instance = this;
            cam = GetComponent<Camera>() ?? Camera.main;
        }

        private void Update()
        {
            if (cam == null) return;
            var gm = GameManager.Instance;
            if (gm != null && (gm.IsGameWon || gm.IsGameLost))
            {
                ClearHover();
                pressed = null;
                return;
            }

            Vector2 mousePos = GameInput.MousePosition;
            if (IsPointerOverUI(mousePos))
            {
                ClearHover();
                if (!GameInput.MouseLeftHeld) pressed = null;
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

            if (GameInput.MouseLeftDown && hovered != null)
            {
                pressed = hovered;
                hovered.OnInteract();
            }
            else if (GameInput.MouseLeftHeld && pressed != null && pressed == hovered && pressed.CanInteract())
            {
                pressed.OnInteractHold();
            }
            else if (!GameInput.MouseLeftHeld)
            {
                pressed = null;
            }
        }

        private IInteractable FindTarget(Ray ray)
        {
            var hit = PickFirst(Physics.RaycastAll(ray, maxRayDistance));
            return hit ?? PickFirst(Physics.SphereCastAll(ray, sphereCastRadius, maxRayDistance));
        }

        private IInteractable PickFirst(RaycastHit[] hits)
        {
            if (hits == null || hits.Length == 0) return null;
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                var target = hits[i].collider.GetComponentInParent<IInteractable>();
                if (target != null && target.CanInteract()) return target;
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
                var go = uiResults[i].gameObject;
                if (go.GetComponentInParent<Selectable>() != null) return true;
                var canvas = go.GetComponentInParent<Canvas>();
                var graphic = go.GetComponent<Graphic>();
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
    }
}
