using System.Collections.Generic;
using HangarGame.Core;
using HangarGame.Dispatch;
using HangarGame.Mechs;
using HangarGame.Repair;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HangarGame.UI
{
    public class TerminalUI : MonoBehaviour
    {
        [Header("Верхняя информация")]
        [SerializeField] private Text txtRadarTitle;
        [SerializeField] private Text txtBattleLog;
        [SerializeField] private Text txtJaegerInfo;
        [SerializeField] private Button btnBuildJaeger;
        [SerializeField] private Button btnScanThreat;

        [Header("Радар")]
        [SerializeField] private RectTransform radarArea;
        [SerializeField] private RadarClickHandler radarClick;
        [SerializeField] private Transform markerContainer;

        [Header("Слоты мехов")]
        [SerializeField] private Button[] btnMechSlots = new Button[3];
        [SerializeField] private Button[] btnShieldSlots = new Button[3];
        [SerializeField] private Button[] btnRecallSlots = new Button[3];
        [SerializeField] private Text[] txtMechSlots = new Text[3];
        [SerializeField] private Image[] imgMechSlots = new Image[3];

        private readonly List<GameObject> activeMarkers = new List<GameObject>();
        private GameObject baseMarkerObj;
        private GameObject orderTargetMarkerObj;
        private Canvas canvas;
        private float lastRadarClickTime = -1f;

        public void Configure(
            Text radarTitle, Text battleLog, Text jaegerInfo, Button buildJaeger, Button scanThreat,
            RectTransform radar, RadarClickHandler clickHandler, Transform markers,
            Button[] slots, Button[] shields, Button[] recalls, Text[] slotTexts, Image[] slotImages)
        {
            txtRadarTitle = radarTitle;
            txtBattleLog = battleLog;
            txtJaegerInfo = jaegerInfo;
            btnBuildJaeger = buildJaeger;
            btnScanThreat = scanThreat;
            radarArea = radar;
            radarClick = clickHandler;
            markerContainer = markers;
            btnMechSlots = slots;
            btnShieldSlots = shields;
            btnRecallSlots = recalls;
            txtMechSlots = slotTexts;
            imgMechSlots = slotImages;
        }

        private void Start()
        {
            canvas = GetComponent<Canvas>();
            if (canvas != null && canvas.worldCamera == null)
                canvas.worldCamera = UnityEngine.Camera.main;

            var gr = GetComponent<GraphicRaycaster>();
            if (gr != null)
            {
                gr.ignoreReversedGraphics = false;
                gr.blockingObjects = GraphicRaycaster.BlockingObjects.None;
            }

            if (btnBuildJaeger != null)
            {
                btnBuildJaeger.onClick.AddListener(() =>
                {
                    HangarAudio.Instance?.PlayClick();
                    JaegerProject.Instance?.StartNextStage();
                });
            }

            if (btnScanThreat != null)
            {
                btnScanThreat.onClick.AddListener(() =>
                {
                    HangarAudio.Instance?.PlayClick();
                    DispatchSystem.Instance?.SpawnEnemy();
                });
            }

            if (radarClick != null) radarClick.onRadarClick = OnRadarClicked;

            for (int i = 0; i < btnMechSlots.Length; i++)
            {
                int idx = i;
                if (btnMechSlots[i] != null) btnMechSlots[i].onClick.AddListener(() => OnMechSlotClicked(idx));
                if (btnShieldSlots[i] != null) btnShieldSlots[i].onClick.AddListener(() => OnShieldSlotClicked(idx));
                if (btnRecallSlots[i] != null) btnRecallSlots[i].onClick.AddListener(() => OnRecallSlotClicked(idx));
            }

            CreateSpecialMarkers();
        }

        private Font LoadTerminalFont()
        {
            Font f = Font.CreateDynamicFontFromOSFont("Arial", 12);
            if (f == null) f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return f;
        }

        private void CreateSpecialMarkers()
        {
            if (markerContainer == null) return;
            Font font = LoadTerminalFont();

            baseMarkerObj = CreateMarkerBox("Radar_BaseMarker", new Vector2(200f, 32f), new Vector2(0f, -240f),
                new Color(0.1f, 0.45f, 0.2f, 0.85f), font, Color.white, 12, "БАЗА АНГАР (ВЫХОД)");

            orderTargetMarkerObj = CreateMarkerBox("Radar_TargetMarker", new Vector2(130f, 26f), Vector2.zero,
                new Color(1f, 0.85f, 0.1f, 0.85f), font, Color.black, 11, "◎ ЦЕЛЬ ПРИКАЗА");
            orderTargetMarkerObj.SetActive(false);
        }

        private GameObject CreateMarkerBox(string objName, Vector2 size, Vector2 pos, Color bgCol, Font font, Color txtCol, int fontSize, string label)
        {
            GameObject go = new GameObject(objName);
            go.transform.SetParent(markerContainer, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = pos;
            var img = go.AddComponent<Image>();
            img.color = bgCol;
            img.raycastTarget = false;

            GameObject tObj = new GameObject("Txt");
            tObj.transform.SetParent(go.transform, false);
            var tRect = tObj.AddComponent<RectTransform>();
            tRect.sizeDelta = size;
            var t = tObj.AddComponent<Text>();
            t.font = font;
            t.fontSize = fontSize;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = txtCol;
            t.text = label;
            t.raycastTarget = false;
            return go;
        }

        private void Update()
        {
            var dispatch = DispatchSystem.Instance;
            var mechMgr = HangarMechManager.Instance;
            if (dispatch == null || mechMgr == null) return;

            UpdateHeader(dispatch);
            UpdateMechSlots(dispatch, mechMgr);
            UpdateRadarMarkers(dispatch, mechMgr);
            CheckDirectRadarRaycast();
        }

        private void UpdateHeader(DispatchSystem dispatch)
        {
            float baseHp = GameManager.Instance != null ? GameManager.Instance.BaseHp : 100f;
            if (txtRadarTitle != null)
            {
                int enemyCount = dispatch.Enemies.Count;
                if (enemyCount > 0)
                {
                    txtRadarTitle.text = $"БОЕВАЯ ТРЕВОГА: В СЕКТОРЕ {enemyCount} ВРАГОВ! | БАЗА: {baseHp:F0}%";
                    txtRadarTitle.color = Color.red;
                }
                else
                {
                    txtRadarTitle.text = $"СЕКТОР-7: РАДАР СКАНИРУЕТ ПЕРИМЕТР | БАЗА: {baseHp:F0}%";
                    txtRadarTitle.color = Color.cyan;
                }
            }

            if (baseMarkerObj != null)
            {
                var bTxt = baseMarkerObj.GetComponentInChildren<Text>();
                if (bTxt != null) bTxt.text = $"БАЗА АНГАР ({baseHp:F0}%)";
                var bImg = baseMarkerObj.GetComponent<Image>();
                if (bImg != null)
                {
                    bImg.color = baseHp <= 25f
                        ? Color.Lerp(new Color(0.65f, 0.1f, 0.1f, 0.9f), new Color(0.95f, 0.25f, 0.15f, 0.95f), Mathf.PingPong(Time.time * 4f, 1f))
                        : (baseHp <= 55f ? new Color(0.75f, 0.45f, 0.12f, 0.9f) : new Color(0.1f, 0.45f, 0.2f, 0.85f));
                }
            }

            if (txtBattleLog != null) txtBattleLog.text = dispatch.LastBattleLog;

            if (txtJaegerInfo != null && JaegerProject.Instance != null)
                txtJaegerInfo.text = JaegerProject.Instance.GetNextStageInfo();

            if (btnBuildJaeger != null && JaegerProject.Instance != null)
                btnBuildJaeger.interactable = JaegerProject.Instance.CanStartNext();
        }

        private void CheckDirectRadarRaycast()
        {
            if (radarArea == null) return;
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.IsGameWon || GameManager.Instance.IsGameLost) return;
            if (GameManager.Instance.CurrentView != RoomView.DispatchConsole) return;

            bool clicked = false;
            Vector2 mPos = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                clicked = Mouse.current.leftButton.wasPressedThisFrame;
                mPos = Mouse.current.position.ReadValue();
            }
#else
            clicked = Input.GetMouseButtonDown(0);
            mPos = Input.mousePosition;
#endif
            if (!clicked) return;

            var cam = (canvas != null && canvas.worldCamera != null) ? canvas.worldCamera : UnityEngine.Camera.main;
            if (cam == null) return;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(radarArea, mPos, cam, out var localPos)
                && radarArea.rect.Contains(localPos))
            {
                OnRadarClicked(localPos);
            }
        }

        private void UpdateMechSlots(DispatchSystem dispatch, HangarMechManager mechMgr)
        {
            for (int i = 0; i < btnMechSlots.Length; i++)
            {
                if (i >= mechMgr.Mechs.Count) continue;
                var mech = mechMgr.Mechs[i];
                if (mech == null) continue;

                var btn = btnMechSlots[i];
                var txt = txtMechSlots[i];
                var img = imgMechSlots[i];
                var bShield = i < btnShieldSlots.Length ? btnShieldSlots[i] : null;
                var bRecall = i < btnRecallSlots.Length ? btnRecallSlots[i] : null;

                bool isSelected = dispatch.SelectedMechId == mech.id;
                bool isDeployReady = dispatch.IsDeployMode && isSelected;

                if (mech.isDeployed)
                {
                    if (img != null)
                        img.color = isSelected ? new Color(0.95f, 0.45f, 0.1f, 0.95f) : new Color(0.7f, 0.22f, 0.15f, 0.9f);

                    if (txt != null)
                    {
                        string mode = mech.isReturning ? "ОТХОДИТ НА БАЗУ" : (isSelected ? "▶ ВЫБРАН (КЛИКНИТЕ РАДАР)" : "В БОЮ (КЛИК ДЛЯ ВЫБОРА)");
                        string shieldInfo = mech.isShieldActive ? " [ЩИТ]" : "";
                        txt.text = $"[ {mech.mechName} ]{shieldInfo}\n{mech.currentHp:F0}/{mech.maxHp:F0} HP   {mech.currentEnergy:F0}%\n{mode}";
                    }
                    if (btn != null) btn.interactable = true;

                    if (bShield != null)
                    {
                        bShield.gameObject.SetActive(true);
                        var sImg = bShield.GetComponent<Image>();
                        if (sImg != null) sImg.color = mech.isShieldActive ? new Color(0.15f, 0.75f, 0.95f) : new Color(0.25f, 0.35f, 0.45f);
                        var sTxt = bShield.GetComponentInChildren<Text>();
                        if (sTxt != null) sTxt.text = mech.isShieldActive ? "ЩИТ: ВКЛ" : "ЩИТ: ВЫКЛ";
                    }

                    if (bRecall != null)
                    {
                        bRecall.gameObject.SetActive(true);
                        var rTxt = bRecall.GetComponentInChildren<Text>();
                        if (rTxt != null) rTxt.text = mech.isReturning ? "ИДЕТ НА БАЗУ" : "НА БАЗУ";
                    }
                }
                else
                {
                    if (bShield != null) bShield.gameObject.SetActive(false);
                    if (bRecall != null) bRecall.gameObject.SetActive(false);

                    if (isDeployReady)
                    {
                        if (img != null) img.color = Color.Lerp(Color.yellow, Color.cyan, Mathf.PingPong(Time.time * 4f, 1f));
                        if (txt != null) txt.text = $"[ ВЫСАДКА: {mech.mechName} ]\nКЛИКНИТЕ В СЕКТОР РАДАРА\n{mech.currentHp:F0}/{mech.maxHp:F0} HP   {mech.currentEnergy:F0}%";
                        if (btn != null) btn.interactable = true;
                    }
                    else if (mech.CanDeploy())
                    {
                        if (img != null) img.color = new Color(0.12f, 0.45f, 0.35f, 0.9f);
                        if (txt != null) txt.text = $"[ {mech.mechName} ]\n{mech.currentHp:F0}/{mech.maxHp:F0} HP   {mech.currentEnergy:F0}%\nКЛИК: ВЫСАДИТЬ В БОЙ";
                        if (btn != null) btn.interactable = true;
                    }
                    else
                    {
                        if (img != null) img.color = new Color(0.3f, 0.15f, 0.15f, 0.75f);
                        string reason = mech.GetTotalHpPercent() <= 0.10f ? "ТРЕБУЕТСЯ РЕМОНТ (ХП < 10%)"
                                      : (mech.currentEnergy <= 10f ? "НУЖНА ЗАРЯДКА В АНГАРЕ" : "РАЗОБРАН НА ВЕРСТАКЕ");
                        if (txt != null) txt.text = $"[ {mech.mechName} ]\n{mech.currentHp:F0}/{mech.maxHp:F0} HP   {mech.currentEnergy:F0}%\n{reason}";
                        if (btn != null) btn.interactable = false;
                    }
                }
            }
        }

        private void OnMechSlotClicked(int index)
        {
            var dispatch = DispatchSystem.Instance;
            var mechMgr = HangarMechManager.Instance;
            if (dispatch == null || mechMgr == null || index < 0 || index >= mechMgr.Mechs.Count) return;

            var mech = mechMgr.Mechs[index];
            if (mech.isDeployed)
            {
                dispatch.SelectedMechId = mech.id;
                dispatch.IsDeployMode = false;
                HangarAudio.Instance?.PlayClick();
            }
            else if (mech.CanDeploy())
            {
                if (dispatch.IsDeployMode && dispatch.SelectedMechId == mech.id)
                {
                    dispatch.DeployMech(mech.id, new Vector2(0f, -80f));
                }
                else
                {
                    dispatch.SelectedMechId = mech.id;
                    dispatch.IsDeployMode = true;
                    HangarAudio.Instance?.PlayBolt();
                }
            }
        }

        private void OnShieldSlotClicked(int index)
        {
            var mechMgr = HangarMechManager.Instance;
            if (mechMgr == null || index < 0 || index >= mechMgr.Mechs.Count) return;
            var mech = mechMgr.Mechs[index];
            if (mech.isDeployed) DispatchSystem.Instance?.ToggleMechShield(mech.id);
        }

        private void OnRecallSlotClicked(int index)
        {
            var mechMgr = HangarMechManager.Instance;
            if (mechMgr == null || index < 0 || index >= mechMgr.Mechs.Count) return;
            var mech = mechMgr.Mechs[index];
            if (mech.isDeployed) DispatchSystem.Instance?.RecallMech(mech.id);
        }

        public void OnRadarClicked(Vector2 pos)
        {
            if (GameManager.Instance != null && (GameManager.Instance.IsGameWon || GameManager.Instance.IsGameLost)) return;
            if (Time.unscaledTime - lastRadarClickTime < 0.05f) return;
            lastRadarClickTime = Time.unscaledTime;

            var dispatch = DispatchSystem.Instance;
            var mechMgr = HangarMechManager.Instance;
            if (dispatch == null || mechMgr == null) return;

            MechUnit clickedMech = null;
            float bestDistSq = 120f * 120f;
            foreach (var m in mechMgr.Mechs)
            {
                if (m == null || !m.isDeployed) continue;
                Vector2 diff = pos - m.battlefieldPos;
                if (Mathf.Abs(diff.x) <= 90f && Mathf.Abs(diff.y) <= 32f && diff.sqrMagnitude < bestDistSq)
                {
                    bestDistSq = diff.sqrMagnitude;
                    clickedMech = m;
                }
            }

            if (clickedMech != null)
            {
                dispatch.SelectedMechId = clickedMech.id;
                dispatch.IsDeployMode = false;
                HangarAudio.Instance?.PlayClick();
                return;
            }

            if (Mathf.Abs(pos.x) <= 110f && Mathf.Abs(pos.y - (-240f)) <= 28f)
            {
                foreach (var m in mechMgr.Mechs)
                {
                    if (m != null && !m.isDeployed && m.CanDeploy())
                    {
                        dispatch.SelectedMechId = m.id;
                        dispatch.IsDeployMode = true;
                        HangarAudio.Instance?.PlayBolt();
                        return;
                    }
                }
            }

            if (dispatch.SelectedMechId < 0)
            {
                foreach (var m in mechMgr.Mechs)
                {
                    if (m != null && !m.isDeployed && m.CanDeploy())
                    {
                        dispatch.DeployMech(m.id, pos);
                        return;
                    }
                }
            }

            if (dispatch.SelectedMechId >= 0)
            {
                var mech = mechMgr.GetMechById(dispatch.SelectedMechId);
                if (mech == null) return;

                if (dispatch.IsDeployMode || (!mech.isDeployed && mech.CanDeploy()))
                    dispatch.DeployMech(mech.id, pos);
                else if (mech.isDeployed)
                    dispatch.OrderMechMove(mech.id, pos);
            }
        }

        private void UpdateRadarMarkers(DispatchSystem dispatch, HangarMechManager mechMgr)
        {
            if (radarArea == null || markerContainer == null) return;

            MechUnit selMech = dispatch.SelectedMechId >= 0 ? mechMgr.GetMechById(dispatch.SelectedMechId) : null;
            if (orderTargetMarkerObj != null)
            {
                bool showTarget = selMech != null && selMech.isDeployed && Vector2.Distance(selMech.battlefieldPos, selMech.targetPos) > 8f;
                orderTargetMarkerObj.SetActive(showTarget);
                if (showTarget) orderTargetMarkerObj.GetComponent<RectTransform>().anchoredPosition = selMech.targetPos;
            }

            int needed = dispatch.Enemies.Count;
            foreach (var m in mechMgr.Mechs)
                if (m != null && m.isDeployed) needed++;

            while (activeMarkers.Count < needed)
                activeMarkers.Add(CreateMarkerBox("Radar_Marker", new Vector2(170f, 48f), Vector2.zero, Color.red, LoadTerminalFont(), Color.white, 12, ""));

            int idx = 0;

            foreach (var enemy in dispatch.Enemies)
            {
                var m = activeMarkers[idx++];
                m.SetActive(true);
                m.GetComponent<RectTransform>().anchoredPosition = enemy.position;
                var img = m.GetComponent<Image>();
                if (img != null) img.color = Color.red;
                var txt = m.GetComponentInChildren<Text>();
                if (txt != null) txt.text = $"● {enemy.name}\n[{enemy.hp:F0} HP | УЯЗ: {enemy.weakAgainst}]";
            }

            foreach (var mech in mechMgr.Mechs)
            {
                if (mech == null || !mech.isDeployed) continue;
                var m = activeMarkers[idx++];
                m.SetActive(true);
                m.GetComponent<RectTransform>().anchoredPosition = mech.battlefieldPos;
                bool isSel = dispatch.SelectedMechId == mech.id;
                var img = m.GetComponent<Image>();
                if (img != null) img.color = isSel ? Color.yellow : mech.themeColor;
                var txt = m.GetComponentInChildren<Text>();
                string status = mech.isReturning ? "ОТХОД" : (mech.isShieldActive ? "ЩИТ" : "БОЙ");
                string selTag = isSel ? "▶ [ВЫБРАН] " : "";
                if (txt != null) txt.text = $"{selTag}▲ {mech.mechName} [{status}]\n{mech.currentHp:F0}/{mech.maxHp:F0} HP  {mech.currentEnergy:F0}%";
            }

            for (int i = idx; i < activeMarkers.Count; i++)
                activeMarkers[i].SetActive(false);
        }
    }
}
