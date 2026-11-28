using System;
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

        [SerializeField] private Color hoverColor = Color.cyan;

        private Renderer rend;
        private Color baseColor = Color.white;
        private bool hasColorInit;

        private void Awake()
        {
            EnsureInit();
        }

        private void EnsureInit()
        {
            if (hasColorInit) return;
            rend = GetComponent<Renderer>();
            if (rend == null) rend = GetComponentInChildren<Renderer>();
            if (rend != null && rend.sharedMaterial != null)
            {
                if (rend.sharedMaterial.HasProperty("_BaseColor"))
                    baseColor = rend.sharedMaterial.GetColor("_BaseColor");
                else
                    baseColor = rend.sharedMaterial.color;
            }
            hasColorInit = true;
        }

        public void SetBaseColor(Color c)
        {
            EnsureInit();
            baseColor = c;
            ApplyColor(c);
        }

        public bool CanInteract() => isInteractable && gameObject.activeInHierarchy;

        public string GetPrompt() => promptText;

        public void OnHover(bool isHovered)
        {
            if (disableHover) return;
            EnsureInit();
            ApplyColor(isHovered ? hoverColor : baseColor);
        }

        public void OnInteract() => onClick?.Invoke();

        public void OnInteractHold() => onHold?.Invoke();

        private void ApplyColor(Color col)
        {
            if (rend == null) return;
            if (Application.isPlaying)
            {
                if (rend.material.HasProperty("_BaseColor"))
                    rend.material.SetColor("_BaseColor", col);
                rend.material.color = col;
            }
        }
    }
}
