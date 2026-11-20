using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using HangarGame.Core;
using HangarGame.Dispatch;
using HangarGame.Mechs;
using HangarGame.Repair;

namespace HangarGame.UI
{
    public class RadarClickHandler : MonoBehaviour, IPointerClickHandler, IPointerDownHandler
    {
        public Action<Vector2> onRadarClick;
        private RectTransform rect;
        private Canvas rootCanvas;

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            rootCanvas = GetComponentInParent<Canvas>();
        }

        public void OnPointerDown(PointerEventData eventData) { }

        public void OnPointerClick(PointerEventData eventData)
        {
            var cam = eventData.pressEventCamera ?? rootCanvas?.worldCamera ?? UnityEngine.Camera.main;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, cam, out var pos))
            {
                onRadarClick?.Invoke(pos);
            }
        }
    }

    public class TerminalUI : MonoBehaviour
    {
        [Header("Header Info")]
        [SerializeField] private Text txtRadarTitle;
        [SerializeField] private Text txtBattleLog;
        [SerializeField] private Text txtJaegerInfo;
        [SerializeField] private Button btnBuildJaeger;
        [SerializeField] private Button btnScanThreat;

        [Header("Radar Screen")]
        [SerializeField] private RectTransform radarArea;
        [SerializeField] private RadarClickHandler radarClick;
        [SerializeField] private GameObject markerPrefab;
        [SerializeField] private Transform markerContainer;

        [Header("Mech Dock Panel")]
        [SerializeField] private Button[] btnMechSlots = new Button[3];
        [SerializeField] private Text[] txtMechSlots = new Text[3];
        [SerializeField] private Image[] imgMechSlots = new Image[3];

        private readonly List<GameObject> activeMarkers = new List<GameObject>();
        private Canvas canvas;

        private void Start()
        {
            canvas = GetComponent<Canvas>();
            if (canvas != null && canvas.worldCamera == null)
            {
                canvas.worldCamera = UnityEngine.Camera.main;
            }

            if (btnBuildJaeger != null) btnBuildJaeger.onClick.AddListener(() => { HangarAudioLight.Instance?.PlayClick(); JaegerProject.Instance?.StartNextStage(); });
            if (btnScanThreat != null) btnScanThreat.onClick.AddListener(() => { HangarAudioLight.Instance?.PlayClick(); DispatchSystem.Instance?.SpawnEnemy(); });

            if (radarClick != null) radarClick.onRadarClick = OnRadarClicked;

            for (int i = 0; i < btnMechSlots.Length; i++)
            {
                int idx = i;
                if (btnMechSlots[i] != null) btnMechSlots[i].onClick.AddListener(() => OnMechSlotClicked(idx));
            }
        }

        private void Update()
        {
            var dispatch = DispatchSystem.Instance;
            var mechMgr = HangarMechManager.Instance;
            if (dispatch == null || mechMgr == null) return;

            if (txtRadarTitle != null)
            {
                int enemyCount = dispatch.Enemies.Count;
                if (enemyCount > 0)
                {
                    txtRadarTitle.text = $"🚨 БОЕВАЯ ТРЕВОГА: В СЕКТОРЕ ОБНАРУЖЕНО {enemyCount} ВРАГОВ!";
                    txtRadarTitle.color = Color.red;
                }
                else
                {
                    txtRadarTitle.text = "СЕКТОР-7: РАДАР СКАНИРУЕТ ПЕРИМЕТР [УГРОЗ НЕТ]";
                    txtRadarTitle.color = Color.cyan;
                }
            }

            if (txtBattleLog != null) txtBattleLog.text = dispatch.LastBattleLog;

            if (txtJaegerInfo != null && JaegerProject.Instance != null)
                txtJaegerInfo.text = JaegerProject.Instance.GetNextStageInfo();

            if (btnBuildJaeger != null && JaegerProject.Instance != null)
                btnBuildJaeger.interactable = JaegerProject.Instance.CanStartNext();

            UpdateMechSlots(dispatch, mechMgr);
            UpdateRadarMarkers(dispatch, mechMgr);
            CheckDirectRadarRaycast(dispatch);
        }

        private void CheckDirectRadarRaycast(DispatchSystem dispatch)
        {
            if (radarArea == null) return;

            bool clk = false;
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Mouse.current != null)
                clk = UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame;
#else
            clk = Input.GetMouseButtonDown(0);
#endif
            if (!clk) return;

            Vector2 mPos = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Mouse.current != null)
                mPos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
#else
            mPos = Input.mousePosition;
#endif
            var cam = canvas != null ? canvas.worldCamera : UnityEngine.Camera.main;
            if (cam == null) cam = UnityEngine.Camera.main;
            if (cam == null) return;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(radarArea, mPos, cam, out var localPos))
            {
                Rect r = radarArea.rect;
                if (r.Contains(localPos))
                {
                    OnRadarClicked(localPos);
                }
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

                bool isSelectedForDeploy = dispatch.IsDeployMode && dispatch.SelectedMechId == mech.id;

                if (mech.isDeployed)
                {
                    if (img != null) img.color = new Color(0.85f, 0.25f, 0.15f, 0.95f);
                    if (txt != null) txt.text = $"[ {mech.mechName} ]\nВ БОЮ: {mech.currentHp:F0}% HP\nКЛИК: ОТОЗВАТЬ В АНГАР";
                    if (btn != null) btn.interactable = true;
                }
                else if (isSelectedForDeploy)
                {
                    float p = Mathf.PingPong(Time.time * 4f, 1f);
                    if (img != null) img.color = Color.Lerp(Color.yellow, Color.cyan, p);
                    if (txt != null) txt.text = $"[ ВЫСАДКА: {mech.mechName} ]\nКЛИКНИТЕ РАДАР (или кнопку)\nДЛЯ ВЫСАДКИ В БОЙ";
                    if (btn != null) btn.interactable = true;
                }
                else if (mech.CanDeploy())
                {
                    if (img != null) img.color = new Color(0.12f, 0.45f, 0.35f, 0.9f);
                    if (txt != null) txt.text = $"[ {mech.mechName} ]\nГОТОВ: {mech.currentHp:F0}% HP\nКЛИК: ВЫСАДИТЬ НА ПОЛЕ";
                    if (btn != null) btn.interactable = true;
                }
                else
                {
                    if (img != null) img.color = new Color(0.3f, 0.15f, 0.15f, 0.75f);
                    if (txt != null) txt.text = $"[ {mech.mechName} ]\nРАЗОБРАН НА ВЕРСТАКЕ!\nТребуется ремонт модулей";
                    if (btn != null) btn.interactable = false;
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
                dispatch.RecallMech(mech.id);
                dispatch.SelectedMechId = -1;
                dispatch.IsDeployMode = false;
            }
            else if (mech.CanDeploy())
            {
                if (dispatch.IsDeployMode && dispatch.SelectedMechId == mech.id)
                {
                    dispatch.DeployMech(mech.id, new Vector2(0f, -80f));
                    dispatch.IsDeployMode = false;
                    dispatch.SelectedMechId = -1;
                }
                else
                {
                    dispatch.SelectedMechId = mech.id;
                    dispatch.IsDeployMode = true;
                    HangarAudioLight.Instance?.PlayBolt();
                }
            }
        }

        private void OnRadarClicked(Vector2 pos)
        {
            var dispatch = DispatchSystem.Instance;
            if (dispatch == null) return;

            if (dispatch.IsDeployMode && dispatch.SelectedMechId >= 0)
            {
                dispatch.DeployMech(dispatch.SelectedMechId, pos);
                dispatch.IsDeployMode = false;
                dispatch.SelectedMechId = -1;
            }
            else if (dispatch.SelectedMechId >= 0)
            {
                dispatch.OrderMechMove(dispatch.SelectedMechId, pos);
            }
        }

        private void UpdateRadarMarkers(DispatchSystem dispatch, HangarMechManager mechMgr)
        {
            if (radarArea == null || markerContainer == null) return;

            int neededMarkers = dispatch.Enemies.Count;
            foreach (var m in mechMgr.Mechs) if (m.isDeployed) neededMarkers++;

            while (activeMarkers.Count < neededMarkers)
            {
                GameObject marker = InstantiateMarker();
                activeMarkers.Add(marker);
            }

            int markerIdx = 0;

            foreach (var enemy in dispatch.Enemies)
            {
                var m = activeMarkers[markerIdx++];
                m.SetActive(true);
                m.GetComponent<RectTransform>().anchoredPosition = enemy.position;
                var img = m.GetComponent<Image>();
                if (img != null) img.color = Color.red;
                var txt = m.GetComponentInChildren<Text>();
                if (txt != null) txt.text = $"● {enemy.name}\n[{enemy.hp:F0} HP]";
            }

            foreach (var mech in mechMgr.Mechs)
            {
                if (!mech.isDeployed) continue;
                var m = activeMarkers[markerIdx++];
                m.SetActive(true);
                m.GetComponent<RectTransform>().anchoredPosition = mech.battlefieldPos;
                var img = m.GetComponent<Image>();
                if (img != null) img.color = (dispatch.SelectedMechId == mech.id) ? Color.yellow : mech.themeColor;
                var txt = m.GetComponentInChildren<Text>();
                if (txt != null) txt.text = $"▲ {mech.mechName}\n[{mech.currentHp:F0} HP]";

                int mechId = mech.id;
                var btn = m.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() =>
                    {
                        dispatch.SelectedMechId = mechId;
                        dispatch.IsDeployMode = false;
                        HangarAudioLight.Instance?.PlayClick();
                    });
                }
            }

            for (int i = markerIdx; i < activeMarkers.Count; i++)
            {
                activeMarkers[i].SetActive(false);
            }
        }

        private GameObject InstantiateMarker()
        {
            GameObject m = new GameObject("Radar_Marker");
            m.transform.SetParent(markerContainer, false);
            var r = m.AddComponent<RectTransform>();
            r.sizeDelta = new Vector2(160f, 45f);
            var img = m.AddComponent<Image>();
            img.color = Color.red;
            m.AddComponent<Button>();

            Font font = Font.CreateDynamicFontFromOSFont("Arial", 12) ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameObject tObj = new GameObject("Txt");
            tObj.transform.SetParent(m.transform, false);
            var tr = tObj.AddComponent<RectTransform>();
            tr.sizeDelta = new Vector2(160f, 45f);
            var txt = tObj.AddComponent<Text>();
            txt.font = font;
            txt.fontSize = 12;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            return m;
        }
    }
}
