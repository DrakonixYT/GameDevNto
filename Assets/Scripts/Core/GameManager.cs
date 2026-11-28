using System;
using HangarGame.Armory;
using HangarGame.CameraControl;
using HangarGame.Dispatch;
using HangarGame.Interaction;
using HangarGame.Mechs;
using HangarGame.Repair;
using UnityEngine;

namespace HangarGame.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Системы")]
        public HangarMechManager hangar;
        public ArmoryManager armory;
        public DispatchSystem dispatch;
        public JaegerProject jaeger;
        public RepairWorkbench workbench;
        public RoomCamera roomCamera;
        public HangarAudio sfx;
        public HangarAlarmLight alarmLight;
        public PlayerRaycaster raycaster;

        [Header("Экономика и база")]
        public int startingCredits = 220;
        public int startingAlloy = 95;
        public float maxBaseHp = 100f;

        public int Credits { get; private set; }
        public int Alloy { get; private set; }
        public float BaseHp { get; private set; }
        public float JaegerProgress { get; private set; }
        public RoomView CurrentView { get; private set; } = RoomView.Hangar;
        public bool IsGameWon { get; private set; }
        public bool IsGameLost { get; private set; }

        public event Action<int, int> OnResourcesChanged;
        public event Action<float, float> OnBaseHpChanged;
        public event Action<RoomView> OnViewChanged;
        public event Action<float> OnJaegerProgressChanged;
        public event Action OnGameWon;
        public event Action OnGameLost;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            ResolveMissingRefs();

            Credits = startingCredits;
            Alloy = startingAlloy;
            BaseHp = maxBaseHp;
        }

        private void Start()
        {
            OnResourcesChanged?.Invoke(Credits, Alloy);
            OnBaseHpChanged?.Invoke(BaseHp, maxBaseHp);
        }

        public void ResolveMissingRefs()
        {
            hangar ??= FindAnyObjectByType<HangarMechManager>();
            armory ??= GetComponent<ArmoryManager>() ?? FindAnyObjectByType<ArmoryManager>();
            dispatch ??= GetComponent<DispatchSystem>() ?? FindAnyObjectByType<DispatchSystem>();
            jaeger ??= GetComponent<JaegerProject>() ?? FindAnyObjectByType<JaegerProject>();
            workbench ??= FindAnyObjectByType<RepairWorkbench>();
            roomCamera ??= FindAnyObjectByType<RoomCamera>();
            sfx ??= GetComponent<HangarAudio>() ?? FindAnyObjectByType<HangarAudio>();
            alarmLight ??= GetComponent<HangarAlarmLight>() ?? FindAnyObjectByType<HangarAlarmLight>();
            raycaster ??= FindAnyObjectByType<PlayerRaycaster>();
        }

        public void PlaySfx(Sfx sound) => sfx?.Play(sound);

        public bool CanAfford(int credits, int alloy) => Credits >= credits && Alloy >= alloy;

        public bool TrySpend(int credits, int alloy)
        {
            if (!CanAfford(credits, alloy)) return false;
            Credits -= credits;
            Alloy -= alloy;
            OnResourcesChanged?.Invoke(Credits, Alloy);
            return true;
        }

        public void AddResources(int credits, int alloy)
        {
            Credits += credits;
            Alloy += alloy;
            OnResourcesChanged?.Invoke(Credits, Alloy);
        }

        public void SwitchView(RoomView view)
        {
            if (CurrentView == view) return;
            CurrentView = view;
            OnViewChanged?.Invoke(view);
        }

        public void SetJaegerProgress(float progress)
        {
            JaegerProgress = Mathf.Clamp01(progress);
            OnJaegerProgressChanged?.Invoke(JaegerProgress);
        }

        public (int lostCredits, int lostAlloy) ApplyBreachPenalty(int creditsCost, int alloyCost, float baseDamage)
        {
            int lostCredits = Mathf.Min(Credits, Mathf.Max(0, creditsCost));
            int lostAlloy = Mathf.Min(Alloy, Mathf.Max(0, alloyCost));
            Credits -= lostCredits;
            Alloy -= lostAlloy;
            OnResourcesChanged?.Invoke(Credits, Alloy);

            if (!IsGameWon && !IsGameLost)
            {
                BaseHp = Mathf.Max(0f, BaseHp - Mathf.Max(0f, baseDamage));
                OnBaseHpChanged?.Invoke(BaseHp, maxBaseHp);
                if (BaseHp <= 0f)
                {
                    IsGameLost = true;
                    OnGameLost?.Invoke();
                }
            }

            return (lostCredits, lostAlloy);
        }

        public void TriggerVictory()
        {
            if (IsGameWon || IsGameLost) return;
            IsGameWon = true;
            OnGameWon?.Invoke();
        }
    }
}
