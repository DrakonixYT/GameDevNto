using System.Collections.Generic;
using HangarGame.Armory;
using HangarGame.CameraControl;
using HangarGame.Core;
using HangarGame.Dispatch;
using HangarGame.Mechs;
using UnityEngine;
using UnityEngine.UI;

namespace HangarGame.UI
{
    public class ArmoryUI : MonoBehaviour
    {
        public static ArmoryUI Instance { get; private set; }
        public bool IsOpen => false;

        [Header("Экран терминала вооружения")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text txtResources;
        [SerializeField] private Text txtFocusBtn;
        [SerializeField] private Button btnFocusMonitor;

        [Header("Шаг 1: Выбор робота")]
        [SerializeField] private GameObject pageMechSelect;
        [SerializeField] private Button[] btnMechChoices = new Button[3];
        [SerializeField] private Text[] txtMechChoices = new Text[3];
        [SerializeField] private Image[] imgMechCards = new Image[3];
        [SerializeField] private Image[] imgMechAccents = new Image[3];

        [Header("Шаг 2: Меню снаряжения")]
        [SerializeField] private GameObject pageLoadout;
        [SerializeField] private Button btnBackToMechs;
        [SerializeField] private Button btnTabWeapons;
        [SerializeField] private Button btnTabShields;
        [SerializeField] private Text txtSelectedMechTitle;
        [SerializeField] private Text txtLoadoutSummary;
        [SerializeField] private Text txtFeedback;
        [SerializeField] private Transform itemsContainer;

        private readonly List<GameObject> rowObjects = new List<GameObject>();
        private Font cachedFont;
        private int selectedMechIndex;
        private ArmorySlot activeSlot = ArmorySlot.RightArm;
        private bool isLoadoutStep;
        private float lastStatusRefreshTime;

        private void Awake()
        {
            Instance = this;
        }

        public void ConfigureWorldMonitor(
            GameObject root, Text resText, Button focusBtn, Text focusTxt,
            GameObject mechPage, Button[] mechBtns, Text[] mechTxts, Image[] mechCardImgs, Image[] mechAccentImgs,
            GameObject loadoutPage, Button backBtn, Button tabWeaponsBtn, Button tabShieldsBtn,
            Text mechTitle, Text loadoutSummary, Text feedback, Transform container)
        {
            panelRoot = root;
            txtResources = resText;
            btnFocusMonitor = focusBtn;
            txtFocusBtn = focusTxt;
            pageMechSelect = mechPage;
            btnMechChoices = mechBtns;
            txtMechChoices = mechTxts;
            imgMechCards = mechCardImgs;
            imgMechAccents = mechAccentImgs;
            pageLoadout = loadoutPage;
            btnBackToMechs = backBtn;
            btnTabWeapons = tabWeaponsBtn;
            btnTabShields = tabShieldsBtn;
            txtSelectedMechTitle = mechTitle;
            txtLoadoutSummary = loadoutSummary;
            txtFeedback = feedback;
            itemsContainer = container;
        }

        private Font LoadUiFont()
        {
            if (cachedFont != null) return cachedFont;
            cachedFont = Font.CreateDynamicFontFromOSFont("Arial", 28);
            if (cachedFont == null) cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return cachedFont;
        }

        private void Start()
        {
            LoadUiFont();

            var canvas = GetComponent<Canvas>();
            if (canvas != null && canvas.worldCamera == null)
                canvas.worldCamera = UnityEngine.Camera.main;

            var gr = GetComponent<GraphicRaycaster>();
            if (gr != null)
            {
                gr.ignoreReversedGraphics = false;
                gr.blockingObjects = GraphicRaycaster.BlockingObjects.None;
            }

            if (btnFocusMonitor != null)
                btnFocusMonitor.onClick.AddListener(() => RoomCamera.Instance?.ToggleArmoryFocus());

            if (btnMechChoices != null)
            {
                for (int i = 0; i < btnMechChoices.Length; i++)
                {
                    int idx = i;
                    if (btnMechChoices[i] != null)
                        btnMechChoices[i].onClick.AddListener(() => SelectMechAndOpenMenu(idx));
                }
            }

            if (btnBackToMechs != null)
                btnBackToMechs.onClick.AddListener(ShowMechSelectStep);

            if (btnTabWeapons != null)
                btnTabWeapons.onClick.AddListener(() => SwitchSlotTab(ArmorySlot.RightArm));

            if (btnTabShields != null)
                btnTabShields.onClick.AddListener(() => SwitchSlotTab(ArmorySlot.LeftArm));

            if (ArmoryManager.Instance != null)
                ArmoryManager.Instance.OnArmoryChanged += Refresh;

            if (HangarMechManager.Instance != null)
                HangarMechManager.Instance.OnMechSwitched += HandleMechSwitched;

            if (GameManager.Instance != null)
                GameManager.Instance.OnResourcesChanged += HandleResourcesChanged;

            if (panelRoot != null) panelRoot.SetActive(true);
            ShowMechSelectStep();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (ArmoryManager.Instance != null)
                ArmoryManager.Instance.OnArmoryChanged -= Refresh;
            if (HangarMechManager.Instance != null)
                HangarMechManager.Instance.OnMechSwitched -= HandleMechSwitched;
            if (GameManager.Instance != null)
                GameManager.Instance.OnResourcesChanged -= HandleResourcesChanged;
        }

        private void Update()
        {
            if (txtFocusBtn != null && RoomCamera.Instance != null)
                txtFocusBtn.text = RoomCamera.Instance.IsArmoryFocused ? "◀ РАДАР" : "КРУПНЕЕ";

            if (Time.unscaledTime - lastStatusRefreshTime >= 0.4f)
            {
                lastStatusRefreshTime = Time.unscaledTime;
                RefreshResources();
                if (!isLoadoutStep) RefreshMechCards();
            }
        }

        private void HandleMechSwitched(int idx)
        {
            selectedMechIndex = idx;
            Refresh();
        }

        private void HandleResourcesChanged(int _, int __) => Refresh();

        public void ShowMechSelectStep()
        {
            isLoadoutStep = false;
            HangarAudio.Instance?.PlayClick();
            if (pageMechSelect != null) pageMechSelect.SetActive(true);
            if (pageLoadout != null) pageLoadout.SetActive(false);
            Refresh();
        }

        public void SelectMechAndOpenMenu(int idx)
        {
            var mgr = HangarMechManager.Instance;
            if (mgr == null || idx < 0 || idx >= mgr.Mechs.Count) return;

            selectedMechIndex = idx;
            mgr.SelectMech(idx);
            isLoadoutStep = true;
            HangarAudio.Instance?.PlayClick();

            if (pageMechSelect != null) pageMechSelect.SetActive(false);
            if (pageLoadout != null) pageLoadout.SetActive(true);
            if (txtFeedback != null) txtFeedback.text = "";
            Refresh();
        }

        public void SwitchSlotTab(ArmorySlot slot)
        {
            activeSlot = slot;
            HangarAudio.Instance?.PlayClick();
            RefreshLoadoutMenu();
        }

        public void Refresh()
        {
            RefreshResources();
            if (isLoadoutStep)
            {
                if (pageMechSelect != null) pageMechSelect.SetActive(false);
                if (pageLoadout != null) pageLoadout.SetActive(true);
                RefreshLoadoutMenu();
            }
            else
            {
                if (pageMechSelect != null) pageMechSelect.SetActive(true);
                if (pageLoadout != null) pageLoadout.SetActive(false);
                RefreshMechCards();
            }
        }

        private void RefreshResources()
        {
            if (txtResources == null || GameManager.Instance == null) return;
            var gm = GameManager.Instance;
            txtResources.text = $"{gm.Credits} ¢   |   {gm.Alloy} т";
        }

        private void RefreshMechCards()
        {
            var mgr = HangarMechManager.Instance;
            var armory = ArmoryManager.Instance;
            if (mgr == null || txtMechChoices == null) return;

            for (int i = 0; i < txtMechChoices.Length; i++)
            {
                if (i >= mgr.Mechs.Count || mgr.Mechs[i] == null) continue;
                var mech = mgr.Mechs[i];

                string shortTitle = StripParens(mech.mechName).ToUpperInvariant();
                string stateStr = mech.isDeployed ? "В БОЮ" : "В АНГАРЕ";
                var rightDef = armory != null ? armory.FindWeapon(mech.equippedRightWeaponId) : null;
                string rName = rightDef != null ? rightDef.displayName : "Автопушка";

                if (txtMechChoices[i] != null)
                {
                    txtMechChoices[i].text =
                        $"<size=30><b>{shortTitle}</b></size>  <color=#8FE8FF>[{stateStr}]</color>\n" +
                        $"<size=23>HP {mech.currentHp:F0}/{mech.maxHp:F0}  |  АТК {mech.attackPower:F0}  |  {rName}</size>";
                }

                if (imgMechAccents != null && i < imgMechAccents.Length && imgMechAccents[i] != null)
                    imgMechAccents[i].color = mech.isDeployed ? new Color(0.75f, 0.28f, 0.18f) : mech.themeColor;

                if (imgMechCards != null && i < imgMechCards.Length && imgMechCards[i] != null)
                {
                    bool curSelected = i == mgr.CurrentIndex;
                    imgMechCards[i].color = mech.isDeployed
                        ? new Color(0.18f, 0.10f, 0.10f, 0.96f)
                        : (curSelected ? new Color(0.10f, 0.22f, 0.30f, 0.98f) : new Color(0.08f, 0.13f, 0.19f, 0.95f));
                }
            }
        }

        private void RefreshLoadoutMenu()
        {
            var mgr = HangarMechManager.Instance;
            var armory = ArmoryManager.Instance;
            if (mgr == null || armory == null || itemsContainer == null) return;

            MechUnit mech = (selectedMechIndex >= 0 && selectedMechIndex < mgr.Mechs.Count)
                ? mgr.Mechs[selectedMechIndex]
                : mgr.CurrentMech;
            if (mech == null) return;

            string shortTitle = StripParens(mech.mechName).ToUpperInvariant();
            if (txtSelectedMechTitle != null)
                txtSelectedMechTitle.text = mech.isDeployed ? $"{shortTitle} [БОЙ]" : shortTitle;

            if (txtLoadoutSummary != null)
                txtLoadoutSummary.text = $"АТК {mech.attackPower:F0}   |   ЗАЩ {mech.TotalDefense:F0}   |   HP {mech.currentHp:F0}/{mech.maxHp:F0}";

            UpdateTabVisuals();
            ClearRows();

            var catalog = armory.Catalog;
            for (int i = 0; i < catalog.Count; i++)
            {
                if (catalog[i].slot == activeSlot)
                    CreateItemCard(catalog[i], armory, mech);
            }

            if (itemsContainer is RectTransform itemsRt)
                LayoutRebuilder.ForceRebuildLayoutImmediate(itemsRt);
            Canvas.ForceUpdateCanvases();
        }

        private void UpdateTabVisuals()
        {
            if (btnTabWeapons != null)
            {
                var img = btnTabWeapons.GetComponent<Image>();
                if (img != null)
                    img.color = activeSlot == ArmorySlot.RightArm ? new Color(0.14f, 0.52f, 0.72f) : new Color(0.11f, 0.16f, 0.22f);
            }
            if (btnTabShields != null)
            {
                var img = btnTabShields.GetComponent<Image>();
                if (img != null)
                    img.color = activeSlot == ArmorySlot.LeftArm ? new Color(0.14f, 0.52f, 0.72f) : new Color(0.11f, 0.16f, 0.22f);
            }
        }

        private void CreateItemCard(WeaponDefinition def, ArmoryManager armory, MechUnit mech)
        {
            LoadUiFont();

            GameObject card = new GameObject($"Card_{def.id}");
            card.transform.SetParent(itemsContainer, false);
            var cardRect = card.AddComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(640f, 162f);

            var bg = card.AddComponent<Image>();
            bool equipped = armory.IsEquipped(mech, def.id);
            bool owned = armory.IsOwned(def.id);
            bg.color = equipped ? new Color(0.09f, 0.28f, 0.22f, 0.98f) : new Color(0.08f, 0.12f, 0.18f, 0.96f);

            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(card.transform, false);
            var tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0f, 1f);
            tRect.anchorMax = new Vector2(1f, 1f);
            tRect.pivot = new Vector2(0.5f, 1f);
            tRect.anchoredPosition = new Vector2(0f, -12f);
            tRect.sizeDelta = new Vector2(-28f, 38f);
            var tTxt = titleObj.AddComponent<Text>();
            tTxt.font = cachedFont;
            tTxt.fontSize = 28;
            tTxt.fontStyle = FontStyle.Bold;
            tTxt.alignment = TextAnchor.MiddleLeft;
            tTxt.color = Color.white;
            tTxt.raycastTarget = false;
            tTxt.text = def.displayName;

            string stats = "";
            if (def.bonusAttack > 0) stats += $"+{def.bonusAttack:F0} АТК   ";
            if (def.bonusDefense > 0) stats += $"+{def.bonusDefense:F0} ЗАЩ   ";
            if (def.bonusMaxHp > 0) stats += $"+{def.bonusMaxHp:F0} HP   ";
            if (stats.Length == 0) stats = "Стандарт   ";

            GameObject statObj = new GameObject("Stats");
            statObj.transform.SetParent(card.transform, false);
            var sRect = statObj.AddComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0f, 1f);
            sRect.anchorMax = new Vector2(1f, 1f);
            sRect.pivot = new Vector2(0.5f, 1f);
            sRect.anchoredPosition = new Vector2(0f, -52f);
            sRect.sizeDelta = new Vector2(-28f, 32f);
            var sTxt = statObj.AddComponent<Text>();
            sTxt.font = cachedFont;
            sTxt.fontSize = 22;
            sTxt.alignment = TextAnchor.MiddleLeft;
            sTxt.color = new Color(0.65f, 0.88f, 1f);
            sTxt.raycastTarget = false;
            sTxt.text = $"{stats}|  {DispatchSystem.FormatDamageType(def.damageType)}";

            GameObject btnObj = new GameObject("ActionBtn");
            btnObj.transform.SetParent(card.transform, false);
            var bRect = btnObj.AddComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0.5f, 0f);
            bRect.anchorMax = new Vector2(0.5f, 0f);
            bRect.pivot = new Vector2(0.5f, 0f);
            bRect.anchoredPosition = new Vector2(0f, 14f);
            bRect.sizeDelta = new Vector2(608f, 54f);
            var btnImg = btnObj.AddComponent<Image>();
            var btn = btnObj.AddComponent<Button>();

            bool canBuy = GameManager.Instance != null && GameManager.Instance.CanAfford(def.costCredits, def.costAlloy);
            if (equipped)
            {
                btnImg.color = new Color(0.18f, 0.55f, 0.34f);
                btn.interactable = false;
            }
            else if (owned)
            {
                btnImg.color = mech.isDeployed ? new Color(0.28f, 0.25f, 0.25f) : new Color(0.18f, 0.52f, 0.82f);
                btn.interactable = !mech.isDeployed;
            }
            else
            {
                btnImg.color = canBuy && !mech.isDeployed ? new Color(0.82f, 0.52f, 0.14f) : new Color(0.36f, 0.18f, 0.18f);
                btn.interactable = canBuy && !mech.isDeployed;
            }

            GameObject btnTxtObj = new GameObject("Text");
            btnTxtObj.transform.SetParent(btnObj.transform, false);
            var btRect = btnTxtObj.AddComponent<RectTransform>();
            btRect.anchorMin = Vector2.zero;
            btRect.anchorMax = Vector2.one;
            btRect.sizeDelta = Vector2.zero;
            var bt = btnTxtObj.AddComponent<Text>();
            bt.font = cachedFont;
            bt.fontSize = 24;
            bt.fontStyle = FontStyle.Bold;
            bt.alignment = TextAnchor.MiddleCenter;
            bt.color = Color.white;
            bt.raycastTarget = false;
            bt.text = equipped
                ? "НАДЕТО"
                : (mech.isDeployed
                    ? "В БОЮ"
                    : (owned ? "НАДЕТЬ" : $"{def.costCredits} ¢  /  {def.costAlloy} т"));

            string weaponId = def.id;
            string weaponName = def.displayName;
            btn.onClick.AddListener(() =>
            {
                bool ok = ArmoryManager.Instance != null && ArmoryManager.Instance.TryBuyOrEquip(weaponId);
                if (txtFeedback != null)
                {
                    txtFeedback.text = ok ? $"Надето: {weaponName}" : "Не хватает ресурсов";
                    txtFeedback.color = ok ? new Color(0.45f, 1f, 0.65f) : new Color(1f, 0.45f, 0.4f);
                }
                if (!ok) RefreshLoadoutMenu();
            });

            rowObjects.Add(card);
        }

        private string StripParens(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            int idx = text.IndexOf('(');
            return idx > 0 ? text.Substring(0, idx).Trim() : text;
        }

        private void ClearRows()
        {
            for (int i = 0; i < rowObjects.Count; i++)
            {
                if (rowObjects[i] != null)
                {
                    rowObjects[i].SetActive(false);
                    rowObjects[i].transform.SetParent(null, false);
                    Destroy(rowObjects[i]);
                }
            }
            rowObjects.Clear();
        }
    }
}
