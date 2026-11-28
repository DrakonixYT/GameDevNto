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

        [SerializeField] private Text txtRadarTitle;
        [SerializeField] private Text txtBattleLog;
        [SerializeField] private Text txtJaegerInfo;
        [SerializeField] private Button btnScanThreat;
        [SerializeField] private RectTransform radarArea;
        [SerializeField] private RadarClickHandler radarClick;
        [SerializeField] private Transform markerContainer;
        [SerializeField] private MechSlotView[] slots = new MechSlotView[3];

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

        public void BindFromHierarchy()
        {
            Transform bg = transform.Find("BG");
            if (bg == null) return;

            txtRadarTitle = bg.Find("Header/Txt_RadarTitle")?.GetComponent<Text>();
            txtJaegerInfo = bg.Find("Header/Txt_JaegerInfo")?.GetComponent<Text>();
            btnScanThreat = bg.Find("Header/Btn_ScanThreat")?.GetComponent<Button>();

            Transform rBox = bg.Find("Radar_Screen");
            radarArea = rBox != null ? rBox.GetComponent<RectTransform>() : null;
            radarClick = rBox != null ? ModelUtils.GetOrAdd<RadarClickHandler>(rBox.gameObject) : null;
            markerContainer = rBox != null ? rBox.Find("Marker_Container") : null;
            txtBattleLog = bg.Find("Txt_BattleLog")?.GetComponent<Text>();

            for (int i = 0; i < 3; i++)
            {
                Transform slot = bg.Find($"SlotRoot_{i}");
                if (slot == null) { slots[i] = null; continue; }

                Transform main = slot.Find($"Btn_MechSlot_{i}");
                slots[i] = new MechSlotView
                {
                    mainBtn = main?.GetComponent<Button>(),
                    mainBg = main?.GetComponent<Image>(),
                    title = main?.GetComponentInChildren<Text>(),
                    hpFill = slot.Find("Bar_Hp/Fill")?.GetComponent<Image>(),
                    energyFill = slot.Find("Bar_En/Fill")?.GetComponent<Image>(),
                    stats = slot.Find("Txt_Stats")?.GetComponent<Text>(),
                    shieldBtn = slot.Find($"Btn_Shield_{i}")?.GetComponent<Button>(),
                    recallBtn = slot.Find($"Btn_Recall_{i}")?.GetComponent<Button>()
                };
            }
        }

        private void Awake()
        {
            if (radarArea == null) BindFromHierarchy();
            if (radarClick != null) radarClick.onRadarClick = OnRadarClicked;

            if (btnScanThreat != null)
            {
                btnScanThreat.onClick.RemoveAllListeners();
                btnScanThreat.onClick.AddListener(() =>
                {
                    HangarAudio.Play(Sfx.Click);
                    DispatchSystem.Instance?.SpawnEnemy();
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

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentView != RoomView.DispatchConsole)
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
            var mgr = HangarMechManager.Instance;
            var ds = DispatchSystem.Instance;
            if (mgr == null || ds == null || slotIdx >= mgr.Mechs.Count) return;

            var mech = mgr.Mechs[slotIdx];
            HangarAudio.Play(Sfx.Click);

            bool toggleOff = !mech.CanDeploy() || (ds.SelectedMechId == mech.id && ds.IsDeployMode);
            ds.SelectedMechId = mech.id;
            ds.IsDeployMode = !toggleOff;
            Refresh();
        }

        private void OnShieldClicked(int slotIdx)
        {
            var mgr = HangarMechManager.Instance;
            if (mgr == null || DispatchSystem.Instance == null || slotIdx >= mgr.Mechs.Count) return;
            DispatchSystem.Instance.ToggleMechShield(mgr.Mechs[slotIdx].id);
            Refresh();
        }

        private void OnRecallClicked(int slotIdx)
        {
            var mgr = HangarMechManager.Instance;
            if (mgr == null || DispatchSystem.Instance == null || slotIdx >= mgr.Mechs.Count) return;
            DispatchSystem.Instance.RecallMech(mgr.Mechs[slotIdx].id);
            Refresh();
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
            var ds = DispatchSystem.Instance;
            var mgr = HangarMechManager.Instance;
            if (ds == null || mgr == null) return;

            Vector2 mapPos = UIToRadar(pos);
            if (ds.SelectedMechId >= 0)
            {
                var mech = mgr.GetMechById(ds.SelectedMechId);
                if (mech != null && ds.IsDeployMode && !mech.isDeployed)
                {
                    ds.DeployMech(mech.id, mapPos);
                    Refresh();
                    return;
                }
                if (mech != null && mech.isDeployed)
                {
                    ds.OrderMechMove(mech.id, mapPos);
                    Refresh();
                    return;
                }
            }

            var nearby = ds.GetClosestMech(mapPos, 65f);
            if (nearby != null)
            {
                ds.SelectedMechId = nearby.id;
                ds.IsDeployMode = false;
                HangarAudio.Play(Sfx.Click);
                Refresh();
            }
        }

        public void Refresh()
        {
            var ds = DispatchSystem.Instance;
            if (ds == null) return;

            var mgr = HangarMechManager.Instance;
            var jp = JaegerProject.Instance;
            var gm = GameManager.Instance;

            if (txtRadarTitle != null)
            {
                int enemies = ds.Enemies.Count;
                float baseHp = gm != null ? gm.BaseHp : 100f;
                txtRadarTitle.text = enemies > 0
                    ? $"РАДАР ОБОРОНЫ   •   УГРОЗ: {enemies}   •   БАЗА: {baseHp:F0}%"
                    : $"РАДАР ОБОРОНЫ   •   ПЕРИМЕТР ЧИСТ   •   БАЗА: {baseHp:F0}%";
                txtRadarTitle.color = enemies > 0 ? UITheme.AccentAmber : UITheme.TextMain;
            }

            if (txtJaegerInfo != null && jp != null)
                txtJaegerInfo.text = jp.IsComplete ? "Егерь 5/5" : $"Егерь {jp.CurrentStage}/{jp.TotalStages}";

            if (txtBattleLog != null)
                txtBattleLog.text = ds.LastBattleLog;

            UpdateSlots(mgr, ds);
            UpdateRadarMarkers(mgr, ds);
        }

        private void UpdateSlots(HangarMechManager mgr, DispatchSystem ds)
        {
            if (mgr == null) return;

            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot == null) continue;
                if (i >= mgr.Mechs.Count)
                {
                    if (slot.mainBtn != null) slot.mainBtn.gameObject.SetActive(false);
                    continue;
                }

                var m = mgr.Mechs[i];
                bool selected = ds.SelectedMechId == m.id;
                bool damagedParts = m.HasMissingOrDetachedParts();

                if (slot.mainBg != null)
                {
                    if (selected && ds.IsDeployMode) slot.mainBg.color = UITheme.AccentAmber;
                    else if (selected) slot.mainBg.color = UITheme.AccentTeal;
                    else if (m.isDeployed) slot.mainBg.color = UITheme.AccentGreen;
                    else if (damagedParts) slot.mainBg.color = UITheme.AccentRed;
                    else slot.mainBg.color = UITheme.BgHeader;
                }

                if (slot.title != null)
                {
                    string status = m.isReturning ? "Отходит"
                        : (m.isDeployed ? "В бою"
                        : (selected && ds.IsDeployMode ? "Укажите точку"
                        : (damagedParts ? "Нужен ремонт"
                        : (m.CanDeploy() ? "Готов" : "Разряд / крит."))));
                    slot.title.text = $"{m.mechName}  •  {status}";
                }

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

        private void UpdateRadarMarkers(HangarMechManager mgr, DispatchSystem ds)
        {
            if (markerContainer == null) return;

            if (baseMarker.root == null)
            {
                GameObject go = UIBuilder.Panel(markerContainer, "BaseMarker", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110f, 22f), UITheme.BgHeader, UITheme.AccentTeal);
                go.GetComponent<Image>().raycastTarget = false;
                Text lbl = UIBuilder.Label(go.transform, "Txt", "БАЗА", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(104f, 18f), 11, UITheme.AccentTeal, null, FontStyle.Bold);
                baseMarker = new MarkerView { root = go, rt = go.GetComponent<RectTransform>(), label = lbl };
            }
            baseMarker.rt.anchoredPosition = RadarToUI(ds.BasePosition);

            int mechIdx = 0;
            if (mgr != null)
            {
                foreach (var m in mgr.Mechs)
                {
                    if (m == null || !m.isDeployed) continue;
                    var view = GetOrCreateMarker(mechPool, mechIdx++, true);
                    view.root.SetActive(true);
                    view.rt.anchoredPosition = RadarToUI(m.battlefieldPos);
                    view.bg.color = m.isShieldActive ? UITheme.AccentAmber : (ds.SelectedMechId == m.id ? UITheme.AccentTeal : UITheme.AccentGreen);
                    view.label.text = m.mechName.Split(' ')[0];
                    UIBuilder.SetBar(view.fill, m.GetTotalHpPercent());
                }
            }
            for (int i = mechIdx; i < mechPool.Count; i++) mechPool[i].root.SetActive(false);

            int enemyCount = ds.Enemies.Count;
            for (int i = 0; i < enemyCount; i++)
            {
                var e = ds.Enemies[i];
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
