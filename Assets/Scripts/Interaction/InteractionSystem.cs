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
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) { Clear(); return; }
            Vector2 m = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null) m = Mouse.current.position.ReadValue();
#else
            m = Input.mousePosition;
#endif
            if (cam == null) return;
            if (Physics.Raycast(cam.ScreenPointToRay(m), out var hit, 40f))
            {
                var obj = hit.collider.GetComponentInParent<IInteractable>();
                if (obj != null)
                {
                    if (current != obj) { Clear(); current = obj; current.OnHover(true); }
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
            }
            Clear();
        }

        private void Clear() { if (current != null) { current.OnHover(false); current = null; } CurrentPrompt = ""; }
    }
}
