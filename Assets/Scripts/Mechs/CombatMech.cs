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
        [SerializeField] private GameObject pristineVisual;
        [SerializeField] private GameObject brokenVisual;

        public MechPartData Data { get => data; set => data = value; }
        public float DurabilityPercent => maxHp > 0 ? Mathf.Clamp01(currentHp / maxHp) : 1f;
        public bool IsOnWorkbench { get; set; } = false;

        private float maxHp = 100f, currentHp = 100f;
        private Vector3 socketPos;
        private Quaternion socketRot;
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
            socketParent = transform.parent;
            if (data == null) data = new MechPartData { partName = gameObject.name };
        }

        public void Setup(MechPartData d)
        {
            data = d;
            currentHp = maxHp;
            UpdateVis();
        }

        public void SetVisuals(GameObject pristine, GameObject broken)
        {
            pristineVisual = pristine;
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
            IsOnWorkbench = false;
            foreach (var col in GetComponentsInChildren<Collider>()) col.enabled = true;
            UpdateVis();
        }

        private IEnumerator SlideBack()
        {
            Vector3 sP = transform.position;
            Quaternion sR = transform.rotation;
            Vector3 eP = socketParent != null ? socketParent.TransformPoint(socketPos) : socketPos;
            Quaternion eR = socketParent != null ? socketParent.rotation * socketRot : socketRot;
            float el = 0f, d = 0.3f;
            while (el < d)
            {
                el += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, el / d);
                transform.position = Vector3.Lerp(sP, eP, t);
                transform.rotation = Quaternion.Slerp(sR, eR, t);
                yield return null;
            }
            InstantReturn();
        }

        public void UpdateVis()
        {
            bool damaged = DurabilityPercent < 0.99f;
            if (brokenVisual != null && pristineVisual != null)
            {
                pristineVisual.SetActive(!damaged);
                brokenVisual.SetActive(damaged);
                if (rend != null && rend.gameObject == gameObject)
                {
                    rend.enabled = !damaged;
                }
            }
            if (rend != null && rend.enabled)
            {
                rend.material.color = Color.Lerp(Color.red, baseColor, DurabilityPercent);
            }
        }

        public string GetPrompt() => $"[{(Data != null ? Data.partName : gameObject.name)}] {Mathf.RoundToInt(DurabilityPercent * 100f)}% HP. Клик для ТО.";

        public void OnHover(bool h)
        {
            if (!IsOnWorkbench && rend != null)
            {
                rend.material.color = h ? Color.cyan : Color.Lerp(Color.red, baseColor, DurabilityPercent);
            }
        }

        public void OnInteract()
        {
            if (!IsOnWorkbench && HangarMechManager.Instance != null && !HangarMechManager.Instance.CurrentMech.isDeployed)
            {
                RepairWorkbench.Instance?.ReceivePart(this);
            }
        }
    }

    public class MechUnit : MonoBehaviour
    {
        public int id;
        public string mechName;
        public Color themeColor;
        public List<MechPart> parts = new List<MechPart>();
        public bool isDeployed = false;
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

        public bool CanDeploy() => !isDeployed && !HasMissingOrDetachedParts() && currentHp > 5f;

        public void SetDeployed(bool dep, Vector2 startPos = default)
        {
            isDeployed = dep;
            battlefieldPos = startPos;
            targetPos = startPos;
            hasTarget = false;
            gameObject.SetActive(!dep && HangarMechManager.Instance != null && HangarMechManager.Instance.CurrentMech == this);
        }

        public void ApplyDamage(float dmg)
        {
            currentHp = Mathf.Max(0f, currentHp - dmg);
            float perPart = (dmg * 0.5f) / Mathf.Max(1, parts.Count);
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

    public class HangarMechManager : MonoBehaviour
    {
        public static HangarMechManager Instance { get; private set; }
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
