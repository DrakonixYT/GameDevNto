using HangarGame.Core;
using HangarGame.Interaction;
using UnityEngine;

namespace HangarGame.Mechs
{
    public class BatteryStation : MonoBehaviour, IInteractable
    {
        private Renderer rend;
        private Color origColor = Color.white;

        private void Awake()
        {
            rend = GetComponent<Renderer>();
            if (rend != null && rend.sharedMaterial != null)
            {
                origColor = rend.sharedMaterial.HasProperty("_BaseColor")
                    ? rend.sharedMaterial.GetColor("_BaseColor")
                    : rend.sharedMaterial.color;
            }
        }

        public bool CanInteract() => gameObject.activeInHierarchy;

        public string GetPrompt()
        {
            var mech = HangarMechManager.Instance != null ? HangarMechManager.Instance.CurrentMech : null;
            if (mech == null) return "[ЗАРЯДНАЯ СТАНЦИЯ] Нет активного робота на подиуме.";
            if (mech.isDeployed) return $"[ЗАРЯДНАЯ СТАНЦИЯ] {mech.mechName} на задании.";
            return $"[ЗАРЯДНАЯ СТАНЦИЯ] Батарея: {mech.currentEnergy:F0}%. Клик: зарядить до 100%";
        }

        public void OnHover(bool isHovered)
        {
            if (rend == null || !Application.isPlaying) return;
            Color target = isHovered ? Color.yellow : origColor;
            if (rend.material.HasProperty("_BaseColor"))
                rend.material.SetColor("_BaseColor", target);
            rend.material.color = target;
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
