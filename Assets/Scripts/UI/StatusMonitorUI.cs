using HangarGame.Core;
using HangarGame.Dispatch;
using HangarGame.Repair;
using UnityEngine;
using UnityEngine.UI;

namespace HangarGame.UI
{
    public class StatusMonitorUI : MonoBehaviour
    {
        [SerializeField] private Text txtHeaderStatus;
        [SerializeField] private Text txtBaseHp;
        [SerializeField] private Image barBaseHp;
        [SerializeField] private Text txtResources;
        [SerializeField] private Text txtJaegerHeader;
        [SerializeField] private Image[] stageRows = new Image[5];
        [SerializeField] private Image[] stageBars = new Image[5];
        [SerializeField] private Text[] stageTexts = new Text[5];
        [SerializeField] private Button btnBuildJaeger;
        [SerializeField] private Text txtBuildBtn;

        private float refreshTimer;
        private bool eventsBound;

        public void BindFromHierarchy()
        {
            Transform bg = transform.Find("BG");
            if (bg == null) return;

            txtHeaderStatus = bg.Find("Header/Txt_Wave")?.GetComponent<Text>();
            Transform baseCard = bg.Find("BaseCard");
            if (baseCard != null)
            {
                txtBaseHp = baseCard.Find("Txt_BaseHp")?.GetComponent<Text>();
                barBaseHp = baseCard.Find("Bar_BaseHp/Fill")?.GetComponent<Image>();
                txtResources = baseCard.Find("Txt_Resources")?.GetComponent<Text>();
            }

            txtJaegerHeader = bg.Find("Txt_JaegerHeader")?.GetComponent<Text>();
            for (int i = 0; i < 5; i++)
            {
                Transform row = bg.Find($"Stage_{i}");
                if (row == null) continue;
                stageRows[i] = row.GetComponent<Image>();
                stageBars[i] = row.Find("Accent")?.GetComponent<Image>();
                stageTexts[i] = row.Find("Txt_Stage")?.GetComponent<Text>();
            }

            btnBuildJaeger = bg.Find("Btn_BuildJaeger")?.GetComponent<Button>();
            txtBuildBtn = btnBuildJaeger != null ? btnBuildJaeger.GetComponentInChildren<Text>() : null;
        }

        private void Awake()
        {
            if (txtBaseHp == null) BindFromHierarchy();
            if (btnBuildJaeger != null)
            {
                btnBuildJaeger.onClick.RemoveAllListeners();
                btnBuildJaeger.onClick.AddListener(OnBuildClicked);
            }
        }

        private void Start()
        {
            BindEvents();
            Refresh();
        }

        private void OnEnable()
        {
            BindEvents();
            Refresh();
        }

        private void OnDisable() => UnbindEvents();

        private void BindEvents()
        {
            if (eventsBound) return;
            var gm = GameManager.Instance;
            if (gm == null) return;
            gm.OnResourcesChanged += OnResourcesChanged;
            gm.OnBaseHpChanged += OnBaseChanged;
            gm.OnJaegerProgressChanged += OnJaegerChanged;
            eventsBound = true;
        }

        private void UnbindEvents()
        {
            if (!eventsBound) return;
            eventsBound = false;
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnResourcesChanged -= OnResourcesChanged;
                gm.OnBaseHpChanged -= OnBaseChanged;
                gm.OnJaegerProgressChanged -= OnJaegerChanged;
            }
        }

        private void Update()
        {
            refreshTimer += Time.deltaTime;
            if (refreshTimer >= 0.25f)
            {
                refreshTimer = 0f;
                Refresh();
            }
        }

        private void OnResourcesChanged(int c, int a) => Refresh();
        private void OnBaseChanged(float hp, float max) => Refresh();
        private void OnJaegerChanged(float p) => Refresh();

        private void OnBuildClicked()
        {
            HangarAudio.Instance?.PlayClick();
            JaegerProject.Instance?.StartNextStage();
            Refresh();
        }

        public void Refresh()
        {
            var gm = GameManager.Instance;
            var jp = JaegerProject.Instance;
            var ds = DispatchSystem.Instance;

            if (txtHeaderStatus != null)
            {
                int active = ds != null ? ds.Enemies.Count : 0;
                txtHeaderStatus.text = active > 0 ? $"Угроз: {active}" : "Спокойно";
                txtHeaderStatus.color = active > 0 ? UITheme.AccentAmber : UITheme.TextMuted;
            }

            if (gm != null)
            {
                float ratio = gm.MaxBaseHp > 0f ? Mathf.Clamp01(gm.BaseHp / gm.MaxBaseHp) : 1f;
                if (txtBaseHp != null)
                {
                    txtBaseHp.text = $"{gm.BaseHp:F0}%";
                    txtBaseHp.color = ratio > 0.5f ? UITheme.AccentGreen : (ratio > 0.25f ? UITheme.AccentAmber : UITheme.AccentRed);
                }
                if (barBaseHp != null)
                {
                    barBaseHp.color = ratio > 0.5f ? UITheme.AccentGreen : (ratio > 0.25f ? UITheme.AccentAmber : UITheme.AccentRed);
                    UIBuilder.SetBar(barBaseHp, ratio);
                }
                if (txtResources != null)
                    txtResources.text = $"Кредиты: {gm.Credits}   •   Сплав: {gm.Alloy}";
            }

            if (jp == null) return;

            if (txtJaegerHeader != null)
                txtJaegerHeader.text = $"ПРОЕКТ «ЕГЕРЬ»   {jp.CurrentStage}/{jp.TotalStages}";

            for (int i = 0; i < 5; i++)
            {
                bool done = i < jp.CurrentStage;
                bool next = i == jp.CurrentStage;
                string name = jp.GetStageName(i);
                int cr = jp.GetStageCredits(i);
                int al = jp.GetStageAlloy(i);

                if (stageRows[i] != null)
                    stageRows[i].color = done ? UITheme.BgCardActive : (next ? UITheme.BgHeader : UITheme.BgCard);
                if (stageBars[i] != null)
                    stageBars[i].color = done ? UITheme.AccentGreen : (next ? UITheme.AccentAmber : UITheme.Border);
                if (stageTexts[i] != null)
                {
                    string right = done ? "ГОТОВО" : $"{cr} кр / {al} м";
                    stageTexts[i].text = $"{i + 1}. {name}    —    {right}";
                    stageTexts[i].color = done ? UITheme.TextMuted : (next ? UITheme.TextMain : UITheme.TextMuted);
                }
            }

            if (btnBuildJaeger != null)
            {
                bool canBuild = jp.CanStartNext();
                btnBuildJaeger.interactable = canBuild;
                var img = btnBuildJaeger.GetComponent<Image>();
                if (img != null)
                {
                    img.color = jp.IsComplete
                        ? UITheme.AccentGreen
                        : (canBuild ? UITheme.AccentTeal : UITheme.BgRecessed);
                }
                if (txtBuildBtn != null)
                {
                    if (jp.IsComplete)
                        txtBuildBtn.text = "ЕГЕРЬ СОБРАН";
                    else
                    {
                        int s = jp.CurrentStage;
                        txtBuildBtn.text = $"СОБРАТЬ: {jp.GetStageName(s).ToUpper()} ({jp.GetStageCredits(s)}/{jp.GetStageAlloy(s)})";
                        txtBuildBtn.color = canBuild ? UITheme.TextMain : UITheme.TextMuted;
                    }
                }
            }
        }
    }
}
