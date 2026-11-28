using System;
using HangarGame.Core;
using UnityEngine;

namespace HangarGame.Interaction
{
    public class ClickableObject : MonoBehaviour, IInteractable
    {
        public string promptText = "";
        public Action onClick;
        public bool disableHover;
        public bool isInteractable = true;

        [SerializeField] private Color hoverColor = new Color(0.35f, 0.85f, 1f);

        private Renderer rend;
        private Color baseColor = Color.white;

        private void Awake()
        {
            rend = GetComponentInChildren<Renderer>();
            if (rend != null) baseColor = ModelUtils.GetColor(rend);
        }

        public bool CanInteract() => isInteractable && gameObject.activeInHierarchy;
        public string GetPrompt() => promptText;
        public void OnInteract() => onClick?.Invoke();
        public void OnInteractHold() { }

        public void OnHover(bool isHovered)
        {
            if (disableHover || rend == null || !Application.isPlaying) return;
            ModelUtils.SetColor(rend, isHovered ? hoverColor : baseColor);
        }
    }
}
