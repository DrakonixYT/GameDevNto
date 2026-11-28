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
        public string CurrentPrompt { get; private set; } = "";

        [SerializeField] private GameManager game;
        [SerializeField] private float maxRayDistance = 45f;
        [SerializeField] private float sphereCastRadius = 0.06f;

        private Camera cam;
        private IInteractable hovered;
        private IInteractable pressed;
        private readonly List<RaycastResult> uiResults = new List<RaycastResult>();

        private void Awake()
        {
            cam = GetComponent<Camera>() ?? Camera.main;
            game ??= GameManager.Instance ?? FindAnyObjectByType<GameManager>();
        }

        private void Update()
        {
            if (cam == null || (game != null && (game.IsGameWon || game.IsGameLost)))
            {
                ClearHover();
                pressed = null;
                return;
            }

            Vector2 mousePos = GameInput.PointerPos;
            if (IsPointerOverHUD(mousePos))
            {
                ClearHover();
                if (!GameInput.LeftHeld) pressed = null;
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

            if (GameInput.LeftDown && hovered != null)
            {
                pressed = hovered;
                hovered.OnInteract();
            }
            else if (GameInput.LeftHeld && pressed != null && pressed == hovered && pressed.CanInteract())
            {
                pressed.OnInteractHold();
            }
            else if (!GameInput.LeftHeld)
            {
                pressed = null;
            }
        }

        private IInteractable FindTarget(Ray ray)
        {
            return PickFirst(Physics.RaycastAll(ray, maxRayDistance))
                ?? PickFirst(Physics.SphereCastAll(ray, sphereCastRadius, maxRayDistance));
        }

        private static IInteractable PickFirst(RaycastHit[] hits)
        {
            if (hits == null || hits.Length == 0) return null;
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                var target = hit.collider.GetComponentInParent<IInteractable>();
                if (target != null && target.CanInteract()) return target;
            }
            return null;
        }

        private bool IsPointerOverHUD(Vector2 mousePos)
        {
            if (EventSystem.current == null) return false;
            var ped = new PointerEventData(EventSystem.current) { position = mousePos };
            uiResults.Clear();
            EventSystem.current.RaycastAll(ped, uiResults);

            foreach (var res in uiResults)
            {
                var go = res.gameObject;
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
            hovered?.OnHover(false);
            hovered = null;
            CurrentPrompt = "";
        }
    }
}
