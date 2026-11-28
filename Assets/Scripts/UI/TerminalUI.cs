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
        [SerializeField] private Text txtRadarTitle;
        [SerializeField] private Text txtBattleLog;
        [SerializeField] private Text txtJaegerInfo;
        [SerializeField] private Button btnScanThreat;
        [SerializeField] private RectTransform radarArea;
        [SerializeField] private RadarClickHandler radarClick;
        [SerializeField] private Transform markerContainer;

        [SerializeField] private Button[] btnMechSlots = new Button[3];
        [SerializeField] private Button[] btnShieldSlots = new Button[3];
        [SerializeField] private Button[] btnRecallSlots = new Button[3];
        [SerializeField] private Text[] txtMechSlots = new Text[3];
        [SerializeField] private Image[] imgMechSlots = new Image[3];
        [SerializeField] private Image[] barMechHp = new Image[3];
        [SerializeField] private Image[] barMechEnergy = new Image[3];
        [SerializeField] private Text[] txtMechBars = new Text[3];

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
                if (slot == null) continue;
                Transform main = slot.Find($"Btn_MechSlot_{i}");
                btnMechSlots[i] = main != null ? main.GetComponent<Button>() : null;
                imgMechSlots[i] = main != null ? main.GetComponent<Image>() : null;
                txtMechSlots[i] = main != null ? main.GetComponentInChildren<Text>() : null;
                barMechHp[i] = slot.Find("Bar_Hp/Fill")?.GetComponent<Image>();
                barMechEnergy[i] = slot.Find("Bar_En/Fill")?.GetComponent<Image>();
                txtMechBars[i] = slot.Find("Txt_Stats")?.GetComponent<Text>();
                btnShieldSlots[i] = slot.Find($"Btn_Shield_{i}")?.GetComponent<Button>();
                btnRecallSlots[i] = slot.Find($"Btn_Recall_{i}")?.GetComponent<Button>();
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
                    HangarAudio.Instance?.PlayClick();
                    DispatchSystem.Instance?.SpawnEnemy();
                    Refresh();
                });
            }

            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                if (btnMechSlots[idx] != null)
                {
                    btnMechSlots[idx].onClick.RemoveAllListeners();
                    btnMechSlots[idx].onClick.AddListener(() => OnSlotClicked(idx));
                }
                if (btnShieldSlots[idx] != null)
                {
                    btnShieldSlots[idx].onClick.RemoveAllListeners();
                    btnShieldSlots[idx].onClick.AddListener(() => OnShieldClicked(idx));
                }
                if (btnRecallSlots[idx] != null)
                {
                    btnRecallSlots[idx].onClick.RemoveAllListeners();
                    btnRecallSlots[idx].onClick.AddListener(() => OnRecallClicked(idx));
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
            HangarAudio.Instance?.PlayClick();

            if (!mech.CanDeploy())
            {
                if (!mech.isDeployed)
                {
                    ds.SelectedMechId = mech.id;
                    ds.IsDeployMode = false;
                }
                else
                {
                    ds.SelectedMechId = mech.id;
                    ds.IsDeployMode = false;
                }
                Refresh();
                return;
            }

            if (ds.SelectedMechId == mech.id && ds.IsDeployMode)
            {
                ds.IsDeployMode = false;
            }
            else
            {
                ds.SelectedMechId = mech.id;
                ds.IsDeployMode = true;
            }
            Refresh();
        }

        private void OnShieldClicked(int slotIdx)
        {
            var mgr = HangarMechManager.Instance;
            var ds = DispatchSystem.Instance;
            if (mgr == null || ds == null || slotIdx >= mgr.Mechs.Count) return;

            ds.ToggleMechShield(mgr.Mechs[slotIdx].id);
            Refresh();
        }

        private void OnRecallClicked(int slotIdx)
        {
            var mgr = HangarMechManager.Instance;
            var ds = DispatchSystem.Instance;
            if (mgr == null || ds == null || slotIdx >= mgr.Mechs.Count) return;

            ds.RecallMech(mgr.Mechs[slotIdx].id);
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
                HangarAudio.Instance?.PlayClick();
                Refresh();
            }
        }

        public void Refresh()
        {
            var ds = DispatchSystem.Instance;
            var mgr = HangarMechManager.Instance;
            var jp = JaegerProject.Instance;
            var gm = GameManager.Instance;

            if (ds == null) return;

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

            for (int i = 0; i < 3; i++)
            {
                if (i >= mgr.Mechs.Count)
                {
                    if (btnMechSlots[i] != null) btnMechSlots[i].gameObject.SetActive(false);
                    continue;
                }

                var m = mgr.Mechs[i];
                bool selected = ds.SelectedMechId == m.id;
                bool missingParts = m.HasMissingOrDetachedParts();

                if (imgMechSlots[i] != null)
                {
                    if (selected && ds.IsDeployMode) imgMechSlots[i].color = UITheme.AccentAmber;
                    else if (selected) imgMechSlots[i].color = UITheme.AccentTeal;
                    else if (m.isDeployed) imgMechSlots[i].color = UITheme.AccentGreen;
                    else if (missingParts) imgMechSlots[i].color = UITheme.AccentRed;
                    else imgMechSlots[i].color = UITheme.BgHeader;
                }

                if (txtMechSlots[i] != null)
                {
                    string status = m.isReturning ? "Отходит"
                        : (m.isDeployed ? "В бою"
                        : (selected && ds.IsDeployMode ? "Укажите точку"
                        : (missingParts ? "Нужен ремонт"
                        : (m.CanDeploy() ? "Готов" : "Разряд / крит."))));
                    txtMechSlots[i].text = $"{m.mechName}  •  {status}";
                }

                if (barMechHp[i] != null)
                {
                    float hpR = m.GetTotalHpPercent();
                    barMechHp[i].color = hpR > 0.5f ? UITheme.AccentGreen : (hpR > 0.25f ? UITheme.AccentAmber : UITheme.AccentRed);
                    UIBuilder.SetBar(barMechHp[i], hpR);
                }

                if (barMechEnergy[i] != null)
                    UIBuilder.SetBar(barMechEnergy[i], m.maxEnergy > 0f ? m.currentEnergy / m.maxEnergy : 0f);

                if (txtMechBars[i] != null)
                    txtMechBars[i].text = $"ХП {m.HpPercent:F0}%     ЭН {m.currentEnergy:F0}%";

                if (btnShieldSlots[i] != null)
                {
                    btnShieldSlots[i].interactable = m.isDeployed;
                    var img = btnShieldSlots[i].GetComponent<Image>();
                    if (img != null) img.color = m.isShieldActive ? UITheme.AccentAmber : (m.isDeployed ? UITheme.AccentTeal : UITheme.BgRecessed);
                    var st = btnShieldSlots[i].GetComponentInChildren<Text>();
                    if (st != null) st.text = m.isShieldActive ? "Щит: ВКЛ" : "Щит";
                }

                if (btnRecallSlots[i] != null)
                {
                    btnRecallSlots[i].interactable = m.isDeployed && !m.isReturning;
                    var img = btnRecallSlots[i].GetComponent<Image>();
                    if (img != null) img.color = (m.isDeployed && !m.isReturning) ? UITheme.AccentRed : UITheme.BgRecessed;
                }
            }
        }

        private void UpdateRadarMarkers(HangarMechManager mgr, DispatchSystem ds)
        {
            if (markerContainer == null) return;

            EnsureBaseMarker();
            baseMarker.rt.anchoredPosition = RadarToUI(ds.BasePosition);

            int mechIdx = 0;
            if (mgr != null)
            {
                for (int i = 0; i < mgr.Mechs.Count; i++)
                {
                    var m = mgr.Mechs[i];
                    if (m == null || !m.isDeployed) continue;

                    var view = GetOrCreateMechMarker(mechIdx++);
                    view.root.SetActive(true);
                    view.rt.anchoredPosition = RadarToUI(m.battlefieldPos);
                    bool sel = ds.SelectedMechId == m.id;
                    view.bg.color = m.isShieldActive ? UITheme.AccentAmber : (sel ? UITheme.AccentTeal : UITheme.AccentGreen);
                    view.label.text = m.mechName.Split(' ')[0];
                    UIBuilder.SetBar(view.fill, m.GetTotalHpPercent());
                }
            }
            for (int i = mechIdx; i < mechPool.Count; i++)
                mechPool[i].root.SetActive(false);

            int enemyCount = ds.Enemies.Count;
            for (int i = 0; i < enemyCount; i++)
            {
                var e = ds.Enemies[i];
                var view = GetOrCreateEnemyMarker(i);
                view.root.SetActive(true);
                view.rt.anchoredPosition = RadarToUI(e.position);
                view.label.text = $"{e.name}\n{e.hp:F0}  •  {e.weakAgainst.ToRu()}";
                UIBuilder.SetBar(view.fill, e.maxHp > 0f ? e.hp / e.maxHp : 1f);
            }
            for (int i = enemyCount; i < enemyPool.Count; i++)
                enemyPool[i].root.SetActive(false);
        }

        private void EnsureBaseMarker()
        {
            if (baseMarker.root != null) return;
            GameObject go = UIBuilder.Panel(markerContainer, "BaseMarker", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110f, 22f), UITheme.BgHeader, UITheme.AccentTeal);
            go.GetComponent<Image>().raycastTarget = false;
            Text lbl = UIBuilder.Label(go.transform, "Txt", "БАЗА", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(104f, 18f), 11, UITheme.AccentTeal, null, FontStyle.Bold);
            baseMarker = new MarkerView { root = go, rt = go.GetComponent<RectTransform>(), label = lbl };
        }

        private MarkerView GetOrCreateMechMarker(int idx)
        {
            if (idx < mechPool.Count) return mechPool[idx];

            GameObject go = UIBuilder.Panel(markerContainer, $"MechMarker_{idx}", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(74f, 28f), UITheme.AccentTeal, UITheme.TextMain);
            go.GetComponent<Image>().raycastTarget = false;
            Text lbl = UIBuilder.Label(go.transform, "Txt", "", new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(70f, 16f), 11, UITheme.TextMain, null, FontStyle.Bold);
            Image fill = UIBuilder.BarFill(go.transform, "Hp", new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(64f, 4f), UITheme.BgRecessed, UITheme.AccentGreen);

            var mv = new MarkerView { root = go, rt = go.GetComponent<RectTransform>(), bg = go.GetComponent<Image>(), fill = fill, label = lbl };
            mechPool.Add(mv);
            return mv;
        }

        private MarkerView GetOrCreateEnemyMarker(int idx)
        {
            if (idx < enemyPool.Count) return enemyPool[idx];

            GameObject go = UIBuilder.Panel(markerContainer, $"EnemyMarker_{idx}", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(124f, 34f), new Color(0.28f, 0.08f, 0.09f, 0.96f), UITheme.AccentRed);
            go.GetComponent<Image>().raycastTarget = false;
            Text lbl = UIBuilder.Label(go.transform, "Txt", "", new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(118f, 24f), 10, UITheme.TextMain);
            Image fill = UIBuilder.BarFill(go.transform, "Hp", new Vector2(0.5f, 0f), new Vector2(0f, 3f), new Vector2(114f, 3f), UITheme.BgRecessed, UITheme.AccentRed);

            var mv = new MarkerView { root = go, rt = go.GetComponent<RectTransform>(), bg = go.GetComponent<Image>(), fill = fill, label = lbl };
            enemyPool.Add(mv);
            return mv;
        }
    }
}
