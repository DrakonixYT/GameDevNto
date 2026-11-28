using System;
using HangarGame.Core;
using HangarGame.Dispatch;
using HangarGame.Repair;
using UnityEngine;
using UnityEngine.UI;

namespace HangarGame.UI
{
    public class StatusMonitorUI : MonoBehaviour
    {
        [Serializable]
        public class StageRowView
        {
            public Image bg;
            public Image accent;
            public Text label;
        }

        [Header("Ссылки")]
        public GameManager game;
        public JaegerProject jaeger;
        public DispatchSystem dispatch;

        [Header("Виджеты")]
        public Text txtHeaderStatus;
        public Text txtBaseHp;
        public Image barBaseHp;
        public Text txtResources;
        public Text txtJaegerHeader;
        public StageRowView[] stages = new StageRowView[5];
        public Button btnBuildJaeger;
        public Text txtBuildBtn;

        private void Awake()
        {
            game ??= GameManager.Instance ?? FindAnyObjectByType<GameManager>();
            jaeger ??= game?.jaeger ?? FindAnyObjectByType<JaegerProject>();
            dispatch ??= game?.dispatch ?? FindAnyObjectByType<DispatchSystem>();

            if (btnBuildJaeger != null)
            {
                btnBuildJaeger.onClick.RemoveAllListeners();
                btnBuildJaeger.onClick.AddListener(OnBuildClicked);
            }
        }

        private void Start() => HookEvents(true);
        private void OnEnable() => HookEvents(true);
        private void OnDisable() => HookEvents(false);

        private void HookEvents(bool subscribe)
        {
            if (game != null)
            {
                game.OnResourcesChanged -= OnResourcesChanged;
                game.OnBaseHpChanged -= OnBaseChanged;
                game.OnJaegerProgressChanged -= OnJaegerChanged;

                if (subscribe)
                {
                    game.OnResourcesChanged += OnResourcesChanged;
                    game.OnBaseHpChanged += OnBaseChanged;
                    game.OnJaegerProgressChanged += OnJaegerChanged;
                }
            }

            if (dispatch != null)
            {
                dispatch.OnRadarChanged -= Refresh;
                if (subscribe) dispatch.OnRadarChanged += Refresh;
            }

            if (subscribe) Refresh();
        }

        private void OnResourcesChanged(int c, int a) => Refresh();
        private void OnBaseChanged(float hp, float max) => Refresh();
        private void OnJaegerChanged(float p) => Refresh();

        private void OnBuildClicked()
        {
            game?.PlaySfx(Sfx.Click);
            jaeger?.StartNextStage();
            Refresh();
        }

        public void Refresh()
        {
            if (txtHeaderStatus != null)
            {
                int active = dispatch != null ? dispatch.Enemies.Count : 0;
                txtHeaderStatus.text = active > 0 ? $"Угроз: {active}" : "Спокойно";
                txtHeaderStatus.color = active > 0 ? UITheme.AccentAmber : UITheme.TextMuted;
            }

            if (game != null)
            {
                float ratio = game.maxBaseHp > 0f ? Mathf.Clamp01(game.BaseHp / game.maxBaseHp) : 1f;
                Color hpColor = ratio > 0.5f ? UITheme.AccentGreen : (ratio > 0.25f ? UITheme.AccentAmber : UITheme.AccentRed);

                if (txtBaseHp != null)
                {
                    txtBaseHp.text = $"{game.BaseHp:F0}%";
                    txtBaseHp.color = hpColor;
                }
                if (barBaseHp != null)
                {
                    barBaseHp.color = hpColor;
                    UIBuilder.SetBar(barBaseHp, ratio);
                }
                if (txtResources != null)
                    txtResources.text = $"Кредиты: {game.Credits}   •   Сплав: {game.Alloy}";
            }

            if (jaeger == null) return;

            if (txtJaegerHeader != null)
                txtJaegerHeader.text = $"ПРОЕКТ «ЕГЕРЬ»   {jaeger.CurrentStage}/{jaeger.TotalStages}";

            for (int i = 0; i < stages.Length; i++)
            {
                var row = stages[i];
                if (row == null) continue;

                bool done = i < jaeger.CurrentStage;
                bool next = i == jaeger.CurrentStage;
                if (row.bg != null) row.bg.color = done ? UITheme.BgCardActive : (next ? UITheme.BgHeader : UITheme.BgCard);
                if (row.accent != null) row.accent.color = done ? UITheme.AccentGreen : (next ? UITheme.AccentAmber : UITheme.Border);
                if (row.label != null)
                {
                    string state = done ? "ГОТОВО" : $"{jaeger.GetStageCredits(i)} кр / {jaeger.GetStageAlloy(i)} м";
                    row.label.text = $"{i + 1}. {jaeger.GetStageName(i)}    —    {state}";
                    row.label.color = (next && !done) ? UITheme.TextMain : UITheme.TextMuted;
                }
            }

            if (btnBuildJaeger != null)
            {
                bool canBuild = jaeger.CanStartNext();
                btnBuildJaeger.interactable = canBuild;
                var img = btnBuildJaeger.GetComponent<Image>();
                if (img != null)
                    img.color = jaeger.IsComplete ? UITheme.AccentGreen : (canBuild ? UITheme.AccentTeal : UITheme.BgRecessed);

                if (txtBuildBtn != null)
                {
                    if (jaeger.IsComplete)
                    {
                        txtBuildBtn.text = "ЕГЕРЬ СОБРАН";
                    }
                    else
                    {
                        int s = jaeger.CurrentStage;
                        txtBuildBtn.text = $"СОБРАТЬ: {jaeger.GetStageName(s).ToUpper()} ({jaeger.GetStageCredits(s)}/{jaeger.GetStageAlloy(s)})";
                        txtBuildBtn.color = canBuild ? UITheme.TextMain : UITheme.TextMuted;
                    }
                }
            }
        }
    }
}
