using System;
using System.Collections.Generic;
using HangarGame.Core;
using HangarGame.Dispatch;
using HangarGame.Mechs;
using HangarGame.Repair;
using UnityEngine;
using UnityEngine.UI;

namespace HangarGame.UI
{
    public class TerminalUI : MonoBehaviour
    {
        [Serializable]
        public class MechSlotView
        {
            public Button mainBtn;
            public Button shieldBtn;
            public Button recallBtn;
            public Image mainBg;
            public Image hpFill;
            public Image energyFill;
            public Text title;
            public Text stats;
        }

        [Header("Ссылки")]
        public GameManager game;
        public HangarMechManager hangar;
        public DispatchSystem dispatch;
        public JaegerProject jaeger;

        [Header("Виджеты")]
        public Text txtRadarTitle;
        public Text txtBattleLog;
        public Text txtJaegerInfo;
        public Button btnScanThreat;
        public RectTransform radarArea;
        public RadarClickHandler radarClick;
        public Transform markerContainer;
        public MechSlotView[] slots = new MechSlotView[3];

        private struct MarkerView
        {
            public GameObject root;
            public RectTransform rt;
            public Image bg;
            public Image fill;
            public Text label;
        }

        private readonly List<MarkerView> mechPool = new List<MarkerView>();
        private readonly List<MarkerView> enemyPool = new List<MarkerView>();
        private MarkerView baseMarker;
        private float refreshTimer;

        private void Awake()
        {
            game ??= GameManager.Instance ?? FindAnyObjectByType<GameManager>();
            hangar ??= game?.hangar ?? FindAnyObjectByType<HangarMechManager>();
            dispatch ??= game?.dispatch ?? FindAnyObjectByType<DispatchSystem>();
            jaeger ??= game?.jaeger ?? FindAnyObjectByType<JaegerProject>();

            if (radarClick != null) radarClick.onRadarClick = OnRadarClicked;

            if (btnScanThreat != null)
            {
                btnScanThreat.onClick.RemoveAllListeners();
                btnScanThreat.onClick.AddListener(() =>
                {
                    game?.PlaySfx(Sfx.Click);
                    dispatch?.SpawnEnemy();
                    Refresh();
                });
            }

            for (int i = 0; i < slots.Length; i++)
            {
                var s = slots[i];
                if (s == null) continue;
                int idx = i;

                if (s.mainBtn != null)
                {
                    s.mainBtn.onClick.RemoveAllListeners();
                    s.mainBtn.onClick.AddListener(() => OnSlotClicked(idx));
                }
                if (s.shieldBtn != null)
                {
                    s.shieldBtn.onClick.RemoveAllListeners();
                    s.shieldBtn.onClick.AddListener(() => OnShieldClicked(idx));
                }
                if (s.recallBtn != null)
                {
                    s.recallBtn.onClick.RemoveAllListeners();
                    s.recallBtn.onClick.AddListener(() => OnRecallClicked(idx));
                }
            }
        }

        private void Start() => HookEvents(true);
        private void OnEnable() => HookEvents(true);
        private void OnDisable() => HookEvents(false);

        private void HookEvents(bool subscribe)
        {
            if (dispatch != null)
            {
                dispatch.OnRadarChanged -= Refresh;
                if (subscribe) dispatch.OnRadarChanged += Refresh;
            }
            if (subscribe) Refresh();
        }

        private void Update()
        {
            if (game != null && game.CurrentView != RoomView.DispatchConsole)
                return;

            refreshTimer += Time.deltaTime;
            if (refreshTimer >= 0.1f)
            {
                refreshTimer = 0f;
                Refresh();
            }
        }

        private void OnSlotClicked(int slotIdx)
        {
            if (hangar == null || dispatch == null || slotIdx >= hangar.Mechs.Count) return;

            var mech = hangar.Mechs[slotIdx];
            game?.PlaySfx(Sfx.Click);

            bool toggleOff = !mech.CanDeploy() || (dispatch.SelectedMechId == mech.id && dispatch.IsDeployMode);
            dispatch.SelectedMechId = mech.id;
            dispatch.IsDeployMode = !toggleOff;
            Refresh();
        }

        private void OnShieldClicked(int slotIdx)
        {
            if (hangar == null || dispatch == null || slotIdx >= hangar.Mechs.Count) return;
            dispatch.ToggleMechShield(hangar.Mechs[slotIdx].id);
        }

        private void OnRecallClicked(int slotIdx)
        {
            if (hangar == null || dispatch == null || slotIdx >= hangar.Mechs.Count) return;
            dispatch.RecallMech(hangar.Mechs[slotIdx].id);
        }

        private Vector2 RadarToUI(Vector2 worldPos)
        {
            if (radarArea == null) return worldPos;
            float sx = Mathf.Max(0.1f, (radarArea.rect.width - 130f) / 900f);
            float sy = Mathf.Max(0.1f, (radarArea.rect.height - 64f) / 480f);
            return new Vector2(worldPos.x * sx, worldPos.y * sy);
        }

        private Vector2 UIToRadar(Vector2 localPos)
        {
            if (radarArea == null) return localPos;
            float sx = Mathf.Max(0.1f, (radarArea.rect.width - 130f) / 900f);
            float sy = Mathf.Max(0.1f, (radarArea.rect.height - 64f) / 480f);
            return new Vector2(Mathf.Clamp(localPos.x / sx, -440f, 440f), Mathf.Clamp(localPos.y / sy, -235f, 235f));
        }

        public void OnRadarClicked(Vector2 pos)
        {
            if (dispatch == null || hangar == null) return;

            Vector2 mapPos = UIToRadar(pos);
            if (dispatch.SelectedMechId >= 0)
            {
                var mech = hangar.GetMechById(dispatch.SelectedMechId);
                if (mech != null && dispatch.IsDeployMode && !mech.isDeployed)
                {
                    dispatch.DeployMech(mech.id, mapPos);
                    return;
                }
                if (mech != null && mech.isDeployed)
                {
                    dispatch.OrderMechMove(mech.id, mapPos);
                    return;
                }
            }

            var nearby = dispatch.GetClosestMech(mapPos, 65f);
            if (nearby != null)
            {
                dispatch.SelectedMechId = nearby.id;
                dispatch.IsDeployMode = false;
                game?.PlaySfx(Sfx.Click);
                Refresh();
            }
        }

        public void Refresh()
        {
            if (dispatch == null) return;

            if (txtRadarTitle != null)
            {
                int enemies = dispatch.Enemies.Count;
                float baseHp = game != null ? game.BaseHp : 100f;
                txtRadarTitle.text = enemies > 0
                    ? $"РАДАР ОБОРОНЫ   •   УГРОЗ: {enemies}   •   БАЗА: {baseHp:F0}%"
                    : $"РАДАР ОБОРОНЫ   •   ПЕРИМЕТР ЧИСТ   •   БАЗА: {baseHp:F0}%";
                txtRadarTitle.color = enemies > 0 ? UITheme.AccentAmber : UITheme.TextMain;
            }

            if (txtJaegerInfo != null && jaeger != null)
                txtJaegerInfo.text = jaeger.IsComplete ? "Егерь 5/5" : $"Егерь {jaeger.CurrentStage}/{jaeger.TotalStages}";

            if (txtBattleLog != null)
                txtBattleLog.text = dispatch.LastBattleLog;

            UpdateSlots();
            UpdateRadarMarkers();
        }

        private void UpdateSlots()
        {
            if (hangar == null) return;

            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot == null) continue;
                if (i >= hangar.Mechs.Count)
                {
                    if (slot.mainBtn != null) slot.mainBtn.gameObject.SetActive(false);
                    continue;
                }

                var m = hangar.Mechs[i];
                bool selected = dispatch.SelectedMechId == m.id;
                bool damagedParts = m.HasMissingOrDetachedParts();

                if (slot.mainBg != null)
                {
                    if (selected && dispatch.IsDeployMode) slot.mainBg.color = UITheme.AccentAmber;
                    else if (selected) slot.mainBg.color = UITheme.AccentTeal;
                    else if (m.isDeployed) slot.mainBg.color = UITheme.AccentGreen;
                    else if (damagedParts) slot.mainBg.color = UITheme.AccentRed;
                    else slot.mainBg.color = UITheme.BgHeader;
                }

                if (slot.title != null)
                    slot.title.text = $"{m.mechName}  •  {GetMechStatus(m, selected, damagedParts)}";

                if (slot.hpFill != null)
                {
                    float hpR = m.GetTotalHpPercent();
                    slot.hpFill.color = hpR > 0.5f ? UITheme.AccentGreen : (hpR > 0.25f ? UITheme.AccentAmber : UITheme.AccentRed);
                    UIBuilder.SetBar(slot.hpFill, hpR);
                }

                if (slot.energyFill != null)
                    UIBuilder.SetBar(slot.energyFill, m.maxEnergy > 0f ? m.currentEnergy / m.maxEnergy : 0f);

                if (slot.stats != null)
                    slot.stats.text = $"ХП {m.HpPercent:F0}%     ЭН {m.currentEnergy:F0}%";

                if (slot.shieldBtn != null)
                {
                    slot.shieldBtn.interactable = m.isDeployed;
                    var img = slot.shieldBtn.GetComponent<Image>();
                    if (img != null) img.color = m.isShieldActive ? UITheme.AccentAmber : (m.isDeployed ? UITheme.AccentTeal : UITheme.BgRecessed);
                    var st = slot.shieldBtn.GetComponentInChildren<Text>();
                    if (st != null) st.text = m.isShieldActive ? "Щит: ВКЛ" : "Щит";
                }

                if (slot.recallBtn != null)
                {
                    slot.recallBtn.interactable = m.isDeployed && !m.isReturning;
                    var img = slot.recallBtn.GetComponent<Image>();
                    if (img != null) img.color = (m.isDeployed && !m.isReturning) ? UITheme.AccentRed : UITheme.BgRecessed;
                }
            }
        }

        private string GetMechStatus(MechUnit m, bool selected, bool damagedParts)
        {
            if (m.isReturning) return "Отходит";
            if (m.isDeployed) return "В бою";
            if (selected && dispatch.IsDeployMode) return "Укажите точку";
            if (damagedParts) return "Нужен ремонт";
            return m.CanDeploy() ? "Готов" : "Разряд / крит.";
        }

        private void UpdateRadarMarkers()
        {
            if (markerContainer == null) return;

            if (baseMarker.root == null)
            {
                GameObject go = UIBuilder.Panel(markerContainer, "BaseMarker", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110f, 22f), UITheme.BgHeader, UITheme.AccentTeal);
                go.GetComponent<Image>().raycastTarget = false;
                Text lbl = UIBuilder.Label(go.transform, "Txt", "БАЗА", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(104f, 18f), 11, UITheme.AccentTeal, null, FontStyle.Bold);
                baseMarker = new MarkerView { root = go, rt = go.GetComponent<RectTransform>(), label = lbl };
            }
            baseMarker.rt.anchoredPosition = RadarToUI(dispatch.BasePosition);

            int mechIdx = 0;
            if (hangar != null)
            {
                foreach (var m in hangar.Mechs)
                {
                    if (m == null || !m.isDeployed) continue;
                    var view = GetOrCreateMarker(mechPool, mechIdx++, true);
                    view.root.SetActive(true);
                    view.rt.anchoredPosition = RadarToUI(m.battlefieldPos);
                    view.bg.color = m.isShieldActive ? UITheme.AccentAmber : (dispatch.SelectedMechId == m.id ? UITheme.AccentTeal : UITheme.AccentGreen);
                    view.label.text = m.mechName.Split(' ')[0];
                    UIBuilder.SetBar(view.fill, m.GetTotalHpPercent());
                }
            }
            for (int i = mechIdx; i < mechPool.Count; i++) mechPool[i].root.SetActive(false);

            int enemyCount = dispatch.Enemies.Count;
            for (int i = 0; i < enemyCount; i++)
            {
                var e = dispatch.Enemies[i];
                var view = GetOrCreateMarker(enemyPool, i, false);
                view.root.SetActive(true);
                view.rt.anchoredPosition = RadarToUI(e.position);
                view.label.text = $"{e.name}\n{e.hp:F0}  •  {e.weakAgainst.ToRu()}";
                UIBuilder.SetBar(view.fill, e.maxHp > 0f ? e.hp / e.maxHp : 1f);
            }
            for (int i = enemyCount; i < enemyPool.Count; i++) enemyPool[i].root.SetActive(false);
        }

        private MarkerView GetOrCreateMarker(List<MarkerView> pool, int idx, bool isMech)
        {
            if (idx < pool.Count) return pool[idx];

            Vector2 size = isMech ? new Vector2(74f, 28f) : new Vector2(124f, 34f);
            Color bgCol = isMech ? UITheme.AccentTeal : new Color(0.28f, 0.08f, 0.09f, 0.96f);
            Color border = isMech ? UITheme.TextMain : UITheme.AccentRed;

            GameObject go = UIBuilder.Panel(markerContainer, isMech ? $"MechMarker_{idx}" : $"EnemyMarker_{idx}", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size, bgCol, border);
            go.GetComponent<Image>().raycastTarget = false;

            Text lbl = UIBuilder.Label(go.transform, "Txt", "", new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(size.x - 6f, isMech ? 16f : 24f), isMech ? 11 : 10, UITheme.TextMain, null, isMech ? FontStyle.Bold : FontStyle.Normal);
            Image fill = UIBuilder.BarFill(go.transform, "Hp", new Vector2(0.5f, 0f), new Vector2(0f, 3f), new Vector2(size.x - 10f, isMech ? 4f : 3f), UITheme.BgRecessed, isMech ? UITheme.AccentGreen : UITheme.AccentRed);

            var mv = new MarkerView { root = go, rt = go.GetComponent<RectTransform>(), bg = go.GetComponent<Image>(), fill = fill, label = lbl };
            pool.Add(mv);
            return mv;
        }
    }
}
