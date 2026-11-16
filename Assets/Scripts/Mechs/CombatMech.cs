using System.Collections.Generic;
using UnityEngine;
using HangarGame.Core;
using HangarGame.Interaction;
using HangarGame.Repair;

namespace HangarGame.Mechs
{
    public class MechPart : MonoBehaviour, IInteractable
    {
        public MechPartData Data { get; set; }
        public float DurabilityPercent => maxHp > 0 ? (currentHp / maxHp) : 1f;
        public bool IsOnWorkbench { get; set; } = false;

        private float maxHp = 100f, currentHp = 100f;
        private Vector3 socketPos; private Quaternion socketRot; private Transform socketParent;
        private Renderer rend; private Color baseColor;

        private void Awake()
        {
            rend = GetComponent<Renderer>();
            if (rend != null) baseColor = rend.material.color;
            socketPos = transform.localPosition; socketRot = transform.localRotation; socketParent = transform.parent;
        }

        public void Setup(MechPartData d) { Data = d; currentHp = maxHp; UpdateVis(); }
        public void Damage(float a) { currentHp = Mathf.Max(0f, currentHp - a); UpdateVis(); }
        public void RepairFull() { currentHp = maxHp; UpdateVis(); }
        public void ReturnToSocket()
        {
            if (gameObject.activeInHierarchy) StartCoroutine(SlideBack());
            else InstantReturn();
        }
        private void InstantReturn() { transform.SetParent(socketParent); transform.localPosition = socketPos; transform.localRotation = socketRot; IsOnWorkbench = false; UpdateVis(); }
        private System.Collections.IEnumerator SlideBack()
        {
            Vector3 sP = transform.position; Quaternion sR = transform.rotation;
            Vector3 eP = socketParent != null ? socketParent.TransformPoint(socketPos) : socketPos;
            Quaternion eR = socketParent != null ? socketParent.rotation * socketRot : socketRot;
            float el = 0f, d = 0.3f;
            while (el < d) { el += Time.deltaTime; float t = Mathf.SmoothStep(0f, 1f, el / d); transform.position = Vector3.Lerp(sP, eP, t); transform.rotation = Quaternion.Slerp(sR, eR, t); yield return null; }
            InstantReturn();
        }
        private void UpdateVis() { if (rend != null) rend.material.color = Color.Lerp(Color.red, baseColor, DurabilityPercent); }

        public string GetPrompt() => $"[{Data?.partName}] Прочность: {Mathf.RoundToInt(DurabilityPercent * 100f)}%. Нажмите для ТО.";
        public void OnHover(bool h) { if (!IsOnWorkbench && rend != null) rend.material.color = h ? Color.cyan : Color.Lerp(Color.red, baseColor, DurabilityPercent); }
        public void OnInteract() { if (!IsOnWorkbench && !CombatMech.Instance.IsDeployed) RepairWorkbench.Instance?.ReceivePart(this); }
    }

    public class CombatMech : MonoBehaviour
    {
        public static CombatMech Instance { get; private set; }
        public List<MechPart> Parts = new List<MechPart>();
        public bool IsDeployed { get; private set; } = false;

        private void Awake() { Instance = this; Parts.AddRange(GetComponentsInChildren<MechPart>()); }

        public bool HasMissingOrDetachedParts()
        {
            if (Parts.Count < 5) return true;
            foreach (var p in Parts) if (p == null || p.IsOnWorkbench) return true;
            return false;
        }

        public void SetDeployed(bool dep) { IsDeployed = dep; gameObject.SetActive(!dep); }

        public float GetTotalDefense(DamageType incoming)
        {
            float def = 0f;
            foreach (var p in Parts) if (p != null && p.Data != null && p.Data.defenseType == incoming) def += p.Data.defensePower;
            return def;
        }

        public float GetTotalAttack(DamageType weakness)
        {
            float atk = 0f;
            foreach (var p in Parts) if (p != null && p.Data != null) atk += p.Data.attackPower * (p.Data.attackType == weakness ? 1.5f : 1f);
            return atk;
        }

        public void ApplyMissionDamage(float dmg)
        {
            float per = dmg / Mathf.Max(1, Parts.Count);
            foreach (var p in Parts) if (p != null) p.Damage(per * Random.Range(0.7f, 1.3f));
        }
    }
}
