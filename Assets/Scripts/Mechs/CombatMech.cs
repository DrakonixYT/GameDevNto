using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HangarGame.Core;
using HangarGame.Interaction;
using HangarGame.Repair;

namespace HangarGame.Mechs
{
    public class MechPart : MonoBehaviour, IInteractable
    {
        [SerializeField] private MechPartData data;
        [SerializeField] private Renderer pristineRenderer;
        [SerializeField] private GameObject brokenVisual;

        public MechPartData Data { get => data; set => data = value; }
        public float DurabilityPercent => maxHp > 0 ? Mathf.Clamp01(currentHp / maxHp) : 1f;
        public bool IsOnWorkbench { get; set; } = false;

        private float maxHp = 100f, currentHp = 100f;
        private Vector3 socketPos;
        private Quaternion socketRot;
        private Vector3 socketScale;
        private Transform socketParent;
        private Renderer rend;
        private Color baseColor;

        private void Awake()
        {
            rend = GetComponent<Renderer>() ?? GetComponentInChildren<Renderer>();
            if (rend != null && rend.sharedMaterial != null)
            {
                baseColor = rend.material.color;
            }
            socketPos = transform.localPosition;
            socketRot = transform.localRotation;
            socketScale = transform.localScale;
            socketParent = transform.parent;
            if (data == null) data = new MechPartData { partName = gameObject.name };
        }

        public void Setup(MechPartData d)
        {
            data = d;
            currentHp = maxHp;
            UpdateVis();
        }

        public void SetVisuals(Renderer pristine, GameObject broken)
        {
            pristineRenderer = pristine;
            brokenVisual = broken;
            UpdateVis();
        }

        public void Damage(float a)
        {
            currentHp = Mathf.Max(0f, currentHp - a);
            UpdateVis();
        }

        public void RepairFull()
        {
            currentHp = maxHp;
            UpdateVis();
        }

        public void SetDurabilityPercent(float pct)
        {
            currentHp = maxHp * Mathf.Clamp01(pct);
            UpdateVis();
        }

        public void ReturnToSocket()
        {
            if (gameObject.activeInHierarchy) StartCoroutine(SlideBack());
            else InstantReturn();
        }

        private void InstantReturn()
        {
            transform.SetParent(socketParent);
            transform.localPosition = socketPos;
            transform.localRotation = socketRot;
            transform.localScale = socketScale;
            IsOnWorkbench = false;
            foreach (var col in GetComponentsInChildren<Collider>(true)) col.enabled = true;
            UpdateVis();
        }

        private IEnumerator SlideBack()
        {
            Vector3 sP = transform.position;
            Quaternion sR = transform.rotation;
            Vector3 sS = transform.localScale;
            Vector3 eP = socketParent != null ? socketParent.TransformPoint(socketPos) : socketPos;
            Quaternion eR = socketParent != null ? socketParent.rotation * socketRot : socketRot;
            float el = 0f, d = 0.3f;
            while (el < d)
            {
                el += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, el / d);
                transform.position = Vector3.Lerp(sP, eP, t);
                transform.rotation = Quaternion.Slerp(sR, eR, t);
                transform.localScale = Vector3.Lerp(sS, socketScale, t);
                yield return null;
            }
            InstantReturn();
        }

        public void UpdateVis()
        {
            bool damaged = DurabilityPercent < 0.99f;
            if (brokenVisual != null)
            {
                if (pristineRenderer != null)
                {
                    pristineRenderer.enabled = !damaged;
                }
                brokenVisual.SetActive(damaged);
            }
            else
            {
                if (pristineRenderer != null)
                {
                    pristineRenderer.enabled = true;
                }
            }
            Color targetCol = Color.Lerp(Color.red, baseColor, DurabilityPercent);
            var rends = GetComponentsInChildren<Renderer>(false);
            foreach (var r in rends)
            {
                if (r != null && r.sharedMaterial != null)
                {
                    var owner = r.GetComponentInParent<MechPart>();
                    if (owner != this) continue;

                    if (Application.isPlaying) r.material.color = targetCol;
                }
            }
        }

        public string GetPrompt() => $"[{(Data != null ? Data.partName : gameObject.name)}] {Mathf.RoundToInt(DurabilityPercent * 100f)}% HP. Клик для ТО.";

        public void OnHover(bool h)
        {
            if (IsOnWorkbench) return;
            Color targetCol = h ? Color.cyan : Color.Lerp(Color.red, baseColor, DurabilityPercent);
            var rends = GetComponentsInChildren<Renderer>(false);
            foreach (var r in rends)
            {
                if (r != null && r.sharedMaterial != null)
                {
                    var owner = r.GetComponentInParent<MechPart>();
                    if (owner != this) continue;

                    if (Application.isPlaying) r.material.color = targetCol;
                }
            }
        }

        public void OnInteract()
        {
            if (IsOnWorkbench) return;
            var unit = GetComponentInParent<MechUnit>();
            if (unit != null && unit.isDeployed) return;
            RepairWorkbench.Instance?.ReceivePart(this);
        }
    }

    public class MechUnit : MonoBehaviour
    {
        public int id;
        public string mechName;
        public Color themeColor;
        public List<MechPart> parts = new List<MechPart>();
        public bool isDeployed = false;
        public bool isReturning = false;
        public bool isShieldActive = false;
        public float currentEnergy = 100f, maxEnergy = 100f;
        public float currentHp = 100f, maxHp = 100f;
        public Vector2 battlefieldPos, targetPos;
        public bool hasTarget = false;
        public float attackPower = 25f;
        public float moveSpeed = 65f;

        private void Awake()
        {
            EnsurePartsLoaded();
        }

        public void EnsurePartsLoaded()
        {
            if (parts == null) parts = new List<MechPart>();
            if (parts.Count == 0)
            {
                parts.AddRange(GetComponentsInChildren<MechPart>(true));
            }
        }

        public bool HasMissingOrDetachedParts()
        {
            EnsurePartsLoaded();
            if (parts.Count == 0) return false;
            foreach (var p in parts)
            {
                if (p != null && p.IsOnWorkbench) return true;
            }
            return false;
        }

        public bool CanDeploy() => !isDeployed && !isReturning && !HasMissingOrDetachedParts() && currentHp > 10f && currentEnergy > 10f;

        public void RechargeBattery()
        {
            currentEnergy = maxEnergy;
        }

        public void ToggleShield()
        {
            if (currentEnergy > 5f)
            {
                isShieldActive = !isShieldActive;
            }
            else
            {
                isShieldActive = false;
            }
        }

        public void SyncHpFromParts()
        {
            EnsurePartsLoaded();
            if (parts.Count == 0) return;
            float total = 0f;
            int valid = 0;
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i] != null)
                {
                    total += parts[i].DurabilityPercent;
                    valid++;
                }
            }
            float ratio = valid > 0 ? (total / valid) : 1f;
            currentHp = maxHp * ratio;
        }

        public void SyncPartsFromHp(float ratio)
        {
            EnsurePartsLoaded();
            ratio = Mathf.Clamp01(ratio);
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i] == null) continue;
                float partRatio = ratio;
                if (ratio < 0.98f)
                {
                    partRatio = Mathf.Clamp01(ratio + UnityEngine.Random.Range(-0.15f, 0.08f));
                }
                parts[i].SetDurabilityPercent(partRatio);
            }
            SyncHpFromParts();
        }

        public void SetDeployed(bool dep, Vector2 startPos = default)
        {
            isDeployed = dep;
            battlefieldPos = startPos;
            targetPos = startPos;
            hasTarget = false;
            if (!dep)
            {
                isReturning = false;
                isShieldActive = false;
            }
            gameObject.SetActive(!dep && HangarMechManager.Instance != null && HangarMechManager.Instance.CurrentMech == this);
        }

        public void ApplyDamage(float dmg)
        {
            if (isShieldActive)
            {
                dmg *= 0.35f;
                currentEnergy = Mathf.Max(0f, currentEnergy - dmg * 0.25f);
                if (currentEnergy <= 0f) isShieldActive = false;
            }
            currentHp = Mathf.Max(0f, currentHp - dmg);
            float perPart = (dmg * 0.6f) / Mathf.Max(1, parts.Count);
            foreach (var p in parts)
            {
                if (p != null) p.Damage(perPart);
            }
        }

        public void RepairAll()
        {
            currentHp = maxHp;
            foreach (var p in parts)
            {
                if (p != null) p.RepairFull();
            }
        }

        public float GetTotalHpPercent() => maxHp > 0 ? (currentHp / maxHp) : 1f;
    }

    public class BatteryStation : MonoBehaviour, IInteractable
    {
        private Renderer rend;
        private Color origColor;

        private void Awake()
        {
            rend = GetComponent<Renderer>();
            if (rend != null) origColor = rend.material.color;
        }

        public string GetPrompt()
        {
            var mech = HangarMechManager.Instance != null ? HangarMechManager.Instance.CurrentMech : null;
            if (mech == null) return "[ЗАРЯДНАЯ СТАНЦИЯ] Нет активного робота на подиуме.";
            if (mech.isDeployed) return $"[ЗАРЯДНАЯ СТАНЦИЯ] {mech.mechName} в бою на выезде.";
            return $"[ЗАРЯДНАЯ СТАНЦИЯ] Батарея: {mech.currentEnergy:F0}%. Клик: Зарядить до 100% ⚡";
        }

        public void OnHover(bool isHovered)
        {
            if (rend != null) rend.material.color = isHovered ? Color.yellow : origColor;
        }

        public void OnInteract()
        {
            var mech = HangarMechManager.Instance != null ? HangarMechManager.Instance.CurrentMech : null;
            if (mech != null && !mech.isDeployed)
            {
                mech.RechargeBattery();
                HangarAudioLight.Instance?.PlaySuccess();
                HangarAudioLight.Instance?.PlayBolt();
            }
        }
    }

    public class HangarMechManager : MonoBehaviour
    {
        private static HangarMechManager instance;
        public static HangarMechManager Instance
        {
            get
            {
                if (instance == null) instance = FindAnyObjectByType<HangarMechManager>();
                return instance;
            }
            private set => instance = value;
        }
        public List<MechUnit> Mechs = new List<MechUnit>();
        public int CurrentIndex { get; private set; } = 0;
        public MechUnit CurrentMech => (Mechs.Count > 0 && CurrentIndex >= 0 && CurrentIndex < Mechs.Count) ? Mechs[CurrentIndex] : null;

        public event Action<int> OnMechSwitched;

        private void Awake()
        {
            Instance = this;
            Mechs.Clear();
            Mechs.AddRange(GetComponentsInChildren<MechUnit>(true));
            for (int i = 0; i < Mechs.Count; i++)
            {
                if (Mechs[i] != null) Mechs[i].EnsurePartsLoaded();
            }
            UpdatePodiumDisplay();
        }

        public void NextMech()
        {
            if (Mechs.Count <= 1) return;
            CurrentIndex = (CurrentIndex + 1) % Mechs.Count;
            UpdatePodiumDisplay();
            HangarAudioLight.Instance?.PlayBolt();
            OnMechSwitched?.Invoke(CurrentIndex);
        }

        public void PrevMech()
        {
            if (Mechs.Count <= 1) return;
            CurrentIndex = (CurrentIndex - 1 + Mechs.Count) % Mechs.Count;
            UpdatePodiumDisplay();
            HangarAudioLight.Instance?.PlayBolt();
            OnMechSwitched?.Invoke(CurrentIndex);
        }

        public void SelectMech(int idx)
        {
            if (idx < 0 || idx >= Mechs.Count) return;
            CurrentIndex = idx;
            UpdatePodiumDisplay();
            OnMechSwitched?.Invoke(CurrentIndex);
        }

        public void RechargeCurrentMech()
        {
            if (CurrentMech != null && !CurrentMech.isDeployed)
            {
                CurrentMech.RechargeBattery();
            }
        }

        public void UpdatePodiumDisplay()
        {
            for (int i = 0; i < Mechs.Count; i++)
            {
                if (Mechs[i] == null) continue;
                bool shouldShow = (i == CurrentIndex) && !Mechs[i].isDeployed;
                Mechs[i].gameObject.SetActive(shouldShow);
            }
        }

        public MechUnit GetMechById(int id)
        {
            return Mechs.Find(m => m.id == id);
        }
    }
}
