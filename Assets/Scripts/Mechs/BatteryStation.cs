using HangarGame.Core;
using HangarGame.Interaction;
using UnityEngine;

namespace HangarGame.Mechs
{
    public class BatteryStation : MonoBehaviour, IInteractable
    {
        private Renderer rend;
        private Color baseColor = Color.white;

        private void Awake()
        {
            rend = GetComponent<Renderer>();
            if (rend != null) baseColor = ModelUtils.GetColor(rend);
        }

        public bool CanInteract() => gameObject.activeInHierarchy;

        public string GetPrompt()
        {
            var mech = HangarMechManager.Instance != null ? HangarMechManager.Instance.CurrentMech : null;
            if (mech == null) return "Зарядка недоступна";
            if (mech.isDeployed) return $"{mech.mechName} в бою";
            return $"Батарея {mech.currentEnergy:F0}% — зарядить";
        }

        public void OnHover(bool isHovered)
        {
            if (rend == null || !Application.isPlaying) return;
            ModelUtils.SetColor(rend, isHovered ? new Color(0.95f, 0.78f, 0.28f) : baseColor);
        }

        public void OnInteract()
        {
            var mech = HangarMechManager.Instance != null ? HangarMechManager.Instance.CurrentMech : null;
            if (mech != null && !mech.isDeployed)
            {
                mech.RechargeBattery();
                HangarAudio.Instance?.PlayBattery();
            }
        }

        public void OnInteractHold() { }
    }
}
