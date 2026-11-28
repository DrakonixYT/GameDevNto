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
            var mech = HangarMechManager.Instance?.CurrentMech;
            if (mech == null) return "Зарядка недоступна";
            return mech.isDeployed ? $"{mech.mechName} в бою" : $"Батарея {mech.currentEnergy:F0}% — зарядить";
        }

        public void OnHover(bool isHovered)
        {
            if (rend != null && Application.isPlaying)
                ModelUtils.SetColor(rend, isHovered ? new Color(0.95f, 0.78f, 0.28f) : baseColor);
        }

        public void OnInteract()
        {
            var mech = HangarMechManager.Instance?.CurrentMech;
            if (mech == null || mech.isDeployed) return;
            mech.RechargeBattery();
            HangarAudio.Play(Sfx.Battery);
        }

        public void OnInteractHold() { }
    }
}
