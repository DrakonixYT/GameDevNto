using System;
using HangarGame.Core;
using UnityEngine;

namespace HangarGame.Interaction
{
    public class ClickableObject : MonoBehaviour, IInteractable
    {
        public string promptText = "";
        public Action onClick;
        public Action onHold;
        public bool disableHover;
        public bool isInteractable = true;

        [SerializeField] private Color hoverColor = new Color(0.35f, 0.85f, 1f);

        private Renderer rend;
        private Color baseColor = Color.white;
        private bool colorReady;

        private void Awake() => InitColor();

        private void InitColor()
        {
            if (colorReady) return;
            rend = GetComponent<Renderer>() ?? GetComponentInChildren<Renderer>();
            if (rend != null) baseColor = ModelUtils.GetColor(rend);
            colorReady = true;
        }

        public void SetBaseColor(Color c)
        {
            InitColor();
            baseColor = c;
            if (Application.isPlaying) ModelUtils.SetColor(rend, c);
        }

        public bool CanInteract() => isInteractable && gameObject.activeInHierarchy;
        public string GetPrompt() => promptText;
        public void OnInteract() => onClick?.Invoke();
        public void OnInteractHold() => onHold?.Invoke();

        public void OnHover(bool isHovered)
        {
            if (disableHover) return;
            InitColor();
            if (Application.isPlaying) ModelUtils.SetColor(rend, isHovered ? hoverColor : baseColor);
        }
    }
}
