using UnityEngine;
using HangarGame.Core;
using HangarGame.Data;
using HangarGame.Interaction;
using HangarGame.Repair;

namespace HangarGame.Mechs
{
    [RequireComponent(typeof(Collider))]
    public class MechPartInstance : MonoBehaviour, IInteractable
    {
        [Header("Part Definition")]
        [SerializeField] private MechPartData partData;
        [SerializeField] private PartSlot slot = PartSlot.LeftArm;

        [Header("Status")]
        [SerializeField] private float currentDurability = 100f;
        [SerializeField] private bool isOnWorkbench = false;

        [Header("Visuals")]
        [SerializeField] private Renderer partRenderer;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color damagedColor = new Color(0.9f, 0.2f, 0.2f);
        [SerializeField] private Color hoverColor = new Color(0.3f, 0.8f, 1f);

        private Vector3 socketLocalPosition;
        private Quaternion socketLocalRotation;
        private Transform originalParent;
        private MechInstance parentMech;

        public MechPartData Data => partData;
        public PartSlot Slot => slot;
        public float DurabilityPercent => partData != null && partData.maxDurability > 0 ? (currentDurability / partData.maxDurability) : 1f;
        public bool IsOnWorkbench => isOnWorkbench;

        public string PartName => partData != null ? partData.partName : gameObject.name;
        public DamageType AttackType => partData != null ? partData.attackType : DamageType.Physical;
        public float AttackPower => partData != null ? partData.attackPower : 0f;
        public DamageType ResistanceType => partData != null ? partData.resistanceType : DamageType.Physical;
        public float DefensePower => partData != null ? partData.defensePower : 0f;

        private void Awake()
        {
            if (partRenderer == null)
            {
                partRenderer = GetComponent<Renderer>();
            }

            socketLocalPosition = transform.localPosition;
            socketLocalRotation = transform.localRotation;
            originalParent = transform.parent;
            parentMech = GetComponentInParent<MechInstance>();

            if (partData != null)
            {
                currentDurability = partData.maxDurability;
            }

            UpdateVisualState();
        }

        public void Initialize(MechPartData data, PartSlot partSlot, float initialDurability = 100f)
        {
            partData = data;
            slot = partSlot;
            currentDurability = initialDurability;
            UpdateVisualState();
        }

        public void ApplyDamage(float damageAmount)
        {
            currentDurability = Mathf.Max(0f, currentDurability - damageAmount);
            UpdateVisualState();
        }

        public void RepairFull()
        {
            if (partData != null)
            {
                currentDurability = partData.maxDurability;
            }
            else
            {
                currentDurability = 100f;
            }
            UpdateVisualState();
        }

        public void UpdateVisualState()
        {
            if (partRenderer == null) return;

            float health01 = DurabilityPercent;
            Color stateColor = Color.Lerp(damagedColor, normalColor, health01);
            partRenderer.material.color = stateColor;
        }

        #region IInteractable

        public void OnHoverEnter()
        {
            if (isOnWorkbench) return;
            if (partRenderer != null)
            {
                partRenderer.material.color = hoverColor;
            }
        }

        public void OnHoverExit()
        {
            if (isOnWorkbench) return;
            UpdateVisualState();
        }

        public void OnInteract()
        {
            if (isOnWorkbench) return;

            // Если мех сейчас на боевой операции — трогать нельзя
            if (parentMech != null && parentMech.IsDeployed)
            {
                Debug.LogWarning("[MechPartInstance] Мех сейчас находится на боевом дежурстве!");
                return;
            }

            SendToWorkbench();
        }

        public string GetInteractionPrompt()
        {
            string partName = partData != null ? partData.partName : name;
            int hp = Mathf.RoundToInt(DurabilityPercent * 100f);
            return $"[{partName}] Целостность: {hp}%. Нажмите, чтобы отправить на верстак.";
        }

        #endregion

        public void SendToWorkbench()
        {
            if (RepairWorkbench.Instance == null)
            {
                Debug.LogWarning("[MechPartInstance] Верстак не найден на сцене!");
                return;
            }

            isOnWorkbench = true;
            RepairWorkbench.Instance.ReceivePartForRepair(this);
        }

        public void ReturnToSocket()
        {
            transform.SetParent(originalParent);
            transform.localPosition = socketLocalPosition;
            transform.localRotation = socketLocalRotation;
            isOnWorkbench = false;
            UpdateVisualState();
        }
    }
}
