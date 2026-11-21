using System;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HangarGame.Interaction
{
    public interface IInteractable
    {
        string GetPrompt();
        void OnHover(bool isHovered);
        void OnInteract();
    }

    public class ClickableObject : MonoBehaviour, IInteractable
    {
        public string promptText = "";
        public Action onClick;
        private Renderer rend;
        private Color origColor;

        private void Awake() { rend = GetComponent<Renderer>(); if (rend != null) origColor = rend.material.color; }
        public string GetPrompt() => promptText;
        public void OnHover(bool h) { if (rend != null) rend.material.color = h ? Color.cyan : origColor; }
        public void OnInteract() => onClick?.Invoke();
    }

    public class PlayerRaycaster : MonoBehaviour
    {
        public static PlayerRaycaster Instance { get; private set; }
        public string CurrentPrompt { get; private set; } = "";
        private UnityEngine.Camera cam;
        private IInteractable current;

        private void Awake() { Instance = this; cam = GetComponent<UnityEngine.Camera>() ?? UnityEngine.Camera.main; }

        private void Update()
        {
            Vector2 m = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null) m = Mouse.current.position.ReadValue();
#else
            m = Input.mousePosition;
#endif
            if (cam == null) return;

            bool isOverInteractiveUI = false;
            if (EventSystem.current != null)
            {
                var ped = new PointerEventData(EventSystem.current) { position = m };
                var uiHits = new System.Collections.Generic.List<RaycastResult>();
                EventSystem.current.RaycastAll(ped, uiHits);
                foreach (var h in uiHits)
                {
                    if (h.gameObject.GetComponentInParent<UnityEngine.UI.Selectable>() != null)
                    {
                        isOverInteractiveUI = true;
                        break;
                    }
                }
            }

            if (isOverInteractiveUI)
            {
                Clear();
                return;
            }

            Ray ray = cam.ScreenPointToRay(m);
            IInteractable best = null;

            var hits = Physics.RaycastAll(ray, 40f);
            if (hits != null && hits.Length > 0)
            {
                Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                foreach (var h in hits)
                {
                    var interactable = h.collider.GetComponentInParent<IInteractable>();
                    if (interactable != null)
                    {
                        best = interactable;
                        break;
                    }
                }
            }

            if (best == null)
            {
                var sphereHits = Physics.SphereCastAll(ray, 0.08f, 40f);
                if (sphereHits != null && sphereHits.Length > 0)
                {
                    Array.Sort(sphereHits, (a, b) => a.distance.CompareTo(b.distance));
                    foreach (var sh in sphereHits)
                    {
                        var interactable = sh.collider.GetComponentInParent<IInteractable>();
                        if (interactable != null)
                        {
                            best = interactable;
                            break;
                        }
                    }
                }
            }

            if (best != null)
            {
                if (current != best)
                {
                    Clear();
                    current = best;
                    current.OnHover(true);
                }
                CurrentPrompt = current.GetPrompt();

                bool clk = false;
#if ENABLE_INPUT_SYSTEM
                if (Mouse.current != null) clk = Mouse.current.leftButton.wasPressedThisFrame;
#else
                clk = Input.GetMouseButtonDown(0);
#endif
                if (clk) current.OnInteract();
                return;
            }

            Clear();
        }

        private void Clear() { if (current != null) { current.OnHover(false); current = null; } CurrentPrompt = ""; }
    }
}
