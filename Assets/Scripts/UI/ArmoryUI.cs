using System.Collections.Generic;
using HangarGame.Armory;
using HangarGame.CameraControl;
using HangarGame.Core;
using HangarGame.Mechs;
using UnityEngine;
using UnityEngine.UI;

namespace HangarGame.UI
{
    public class ArmoryUI : MonoBehaviour
    {
        public static ArmoryUI Instance { get; private set; }
        public bool IsOpen => false;

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text txtResources;
        [SerializeField] private Text txtFocusBtn;
        [SerializeField] private Button btnFocusMonitor;

        [SerializeField] private GameObject pageMechSelect;
        [SerializeField] private Button[] btnMechChoices = new Button[3];
        [SerializeField] private Text[] txtMechChoices = new Text[3];
        [SerializeField] private Image[] imgMechCards = new Image[3];
        [SerializeField] private Image[] imgMechAccents = new Image[3];
        [SerializeField] private Image[] barMechHp = new Image[3];

        [SerializeField] private GameObject pageLoadout;
        [SerializeField] private Button btnBackToMechs;
        [SerializeField] private Button btnTabWeapons;
        [SerializeField] private Button btnTabShields;
        [SerializeField] private Text txtSelectedMechTitle;
        [SerializeField] private Text txtLoadoutSummary;
        [SerializeField] private Text txtFeedback;
        [SerializeField] private Transform itemsContainer;

        private ArmorySlot activeSlot = ArmorySlot.RightArm;
        private bool isSelectingMech = true;
        private Font cachedFont;
        private readonly List<GameObject> spawnedRows = new List<GameObject>();
        private bool eventsBound;

        public void BindFromHierarchy()
        {
            Transform bg = transform.Find("BG");
            if (bg == null) return;

            panelRoot = bg.gameObject;
            txtResources = bg.Find("Txt_ArmoryResources")?.GetComponent<Text>();
            btnFocusMonitor = bg.Find("Btn_FocusMonitor")?.GetComponent<Button>();
            txtFocusBtn = btnFocusMonitor != null ? btnFocusMonitor.GetComponentInChildren<Text>() : null;

            Transform pMech = bg.Find("Panel_MechSelect");
            pageMechSelect = pMech != null ? pMech.gameObject : null;
            if (pMech != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    Transform card = pMech.Find($"Btn_SelectMech_{i}");
                    if (card == null) continue;
                    btnMechChoices[i] = card.GetComponent<Button>();
                    imgMechCards[i] = card.GetComponent<Image>();
                    imgMechAccents[i] = card.Find("Accent")?.GetComponent<Image>();
                    txtMechChoices[i] = card.Find("Txt_Info")?.GetComponent<Text>();
                    barMechHp[i] = card.Find("Bar_Hp/Fill")?.GetComponent<Image>();
                }
            }

            Transform pLoad = bg.Find("Panel_LoadoutMenu");
            pageLoadout = pLoad != null ? pLoad.gameObject : null;
            if (pLoad != null)
            {
                btnBackToMechs = pLoad.Find("Btn_BackToMechs")?.GetComponent<Button>();
                btnTabWeapons = pLoad.Find("Btn_TabWeapons")?.GetComponent<Button>();
                btnTabShields = pLoad.Find("Btn_TabShields")?.GetComponent<Button>();
                txtSelectedMechTitle = pLoad.Find("Txt_SelectedMech")?.GetComponent<Text>();
                txtLoadoutSummary = pLoad.Find("SummaryBox/Txt_LoadoutSummary")?.GetComponent<Text>();
                txtFeedback = pLoad.Find("Txt_ArmoryFeedback")?.GetComponent<Text>();
                itemsContainer = pLoad.Find("ItemsContainer");
            }
        }

        private void Awake()
        {
            Instance = this;
            if (panelRoot == null) BindFromHierarchy();
            if (panelRoot != null) panelRoot.SetActive(true);

            WireBtn(btnFocusMonitor, () =>
            {
                RoomCamera.Instance?.ToggleArmoryFocus();
                Refresh();
            });
            WireBtn(btnBackToMechs, () =>
            {
                HangarAudio.Instance?.PlayClick();
                ShowMechSelectStep();
            });
            WireBtn(btnTabWeapons, () => SwitchSlotTab(ArmorySlot.RightArm));
            WireBtn(btnTabShields, () => SwitchSlotTab(ArmorySlot.LeftArm));

            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                WireBtn(btnMechChoices[idx], () => SelectMechAndOpenMenu(idx));
            }
        }

        private static void WireBtn(Button btn, UnityEngine.Events.UnityAction action)
        {
            if (btn == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(action);
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
            if (GameManager.Instance == null) return;
            GameManager.Instance.OnResourcesChanged += OnResourcesChanged;
            GameManager.Instance.OnViewChanged += OnViewChanged;
            if (HangarMechManager.Instance != null)
                HangarMechManager.Instance.OnMechSwitched += OnMechSwitched;
            if (ArmoryManager.Instance != null)
                ArmoryManager.Instance.OnArmoryChanged += Refresh;
            eventsBound = true;
        }

        private void UnbindEvents()
        {
            if (!eventsBound) return;
            eventsBound = false;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnResourcesChanged -= OnResourcesChanged;
                GameManager.Instance.OnViewChanged -= OnViewChanged;
            }
            if (HangarMechManager.Instance != null)
                HangarMechManager.Instance.OnMechSwitched -= OnMechSwitched;
            if (ArmoryManager.Instance != null)
                ArmoryManager.Instance.OnArmoryChanged -= Refresh;
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentView != RoomView.DispatchConsole)
                return;

            if (txtFocusBtn != null && RoomCamera.Instance != null)
                txtFocusBtn.text = RoomCamera.Instance.IsArmoryFocused ? "ОБЗОР" : "КРУПНЕЕ";
        }

        private void OnResourcesChanged(int c, int a) => Refresh();
        private void OnViewChanged(RoomView view) => Refresh();
        private void OnMechSwitched(int idx) => Refresh();

        public void ShowMechSelectStep()
        {
            isSelectingMech = true;
            if (txtFeedback != null) txtFeedback.text = "";
            Refresh();
        }

        public void SelectMechAndOpenMenu(int idx)
        {
            HangarAudio.Instance?.PlayClick();
            HangarMechManager.Instance?.SelectMech(idx);
            isSelectingMech = false;
            if (txtFeedback != null) txtFeedback.text = "";
            Refresh();
        }

        public void SwitchSlotTab(ArmorySlot slot)
        {
            activeSlot = slot;
            HangarAudio.Instance?.PlayClick();
            if (txtFeedback != null) txtFeedback.text = "";
            Refresh();
        }

        public void Refresh()
        {
            var gm = GameManager.Instance;
            if (txtResources != null && gm != null)
                txtResources.text = $"Кредиты: {gm.Credits}   •   Сплав: {gm.Alloy}";

            if (pageMechSelect != null) pageMechSelect.SetActive(isSelectingMech);
            if (pageLoadout != null) pageLoadout.SetActive(!isSelectingMech);

            if (isSelectingMech) RefreshMechCards();
            else RefreshLoadoutMenu();
        }

        private void RefreshMechCards()
        {
            var mgr = HangarMechManager.Instance;
            var arm = ArmoryManager.Instance;

            for (int i = 0; i < 3; i++)
            {
                if (mgr == null || i >= mgr.Mechs.Count || mgr.Mechs[i] == null)
                {
                    if (btnMechChoices[i] != null) btnMechChoices[i].gameObject.SetActive(false);
                    continue;
                }

                var m = mgr.Mechs[i];
                bool isCurrent = i == mgr.CurrentIndex;
                string wName = arm?.FindWeapon(m.equippedRightWeaponId)?.displayName ?? "Автопушка";
                string sName = arm?.FindWeapon(m.equippedLeftWeaponId)?.displayName ?? "Обычный щит";
                string state = m.isDeployed ? "В бою" : (m.HasMissingOrDetachedParts() ? "Повреждён" : "В ангаре");

                if (btnMechChoices[i] != null) btnMechChoices[i].gameObject.SetActive(true);
                if (imgMechCards[i] != null) imgMechCards[i].color = isCurrent ? UITheme.BgCardActive : UITheme.BgCard;
                if (imgMechAccents[i] != null) imgMechAccents[i].color = Color.Lerp(m.themeColor, UITheme.Border, 0.35f);

                if (txtMechChoices[i] != null)
                {
                    txtMechChoices[i].text =
                        $"<b>{m.mechName}</b>   ({state})\n" +
                        $"ХП: {m.currentHp:F0}/{m.maxHp:F0}   •   Урон: {m.attackPower:F0}   •   Эн.: {m.currentEnergy:F0}%\n" +
                        $"<color=#94ABBE>{wName}  /  {sName}</color>";
                }

                if (barMechHp[i] != null)
                {
                    float r = m.GetTotalHpPercent();
                    barMechHp[i].color = r > 0.5f ? UITheme.AccentGreen : (r > 0.25f ? UITheme.AccentAmber : UITheme.AccentRed);
                    UIBuilder.SetBar(barMechHp[i], r);
                }
            }
        }

        private void RefreshLoadoutMenu()
        {
            var mgr = HangarMechManager.Instance;
            var arm = ArmoryManager.Instance;
            var mech = mgr != null ? mgr.CurrentMech : null;

            if (txtSelectedMechTitle != null)
                txtSelectedMechTitle.text = mech != null ? mech.mechName.ToUpper() : "РОБОТ";

            if (txtLoadoutSummary != null && mech != null)
            {
                string rName = arm?.FindWeapon(mech.equippedRightWeaponId)?.displayName ?? "Автопушка";
                string lName = arm?.FindWeapon(mech.equippedLeftWeaponId)?.displayName ?? "Обычный щит";
                txtLoadoutSummary.text = $"Урон: {mech.attackPower:F0}  •  Броня: {mech.TotalDefense:F0}  •  {rName} / {lName}";
            }

            if (btnTabWeapons != null)
            {
                var img = btnTabWeapons.GetComponent<Image>();
                if (img != null) img.color = activeSlot == ArmorySlot.RightArm ? UITheme.AccentTeal : UITheme.BgCard;
            }
            if (btnTabShields != null)
            {
                var img = btnTabShields.GetComponent<Image>();
                if (img != null) img.color = activeSlot == ArmorySlot.LeftArm ? UITheme.AccentTeal : UITheme.BgCard;
            }

            ClearRows();
            if (itemsContainer == null || arm == null) return;

            if (cachedFont == null) cachedFont = UIBuilder.GetFont(26);

            foreach (var def in arm.Catalog)
            {
                if (def.slot != activeSlot) continue;
                CreateItemRow(def, mech, arm);
            }

            if (mech != null && mech.HasMissingOrDetachedParts())
            {
                var hintBtn = UIBuilder.Button(itemsContainer, "Btn_GoHangarRepair", "Модуль повреждён — перейти в ангар (ремонт)", Vector2.one * 0.5f, Vector2.zero, new Vector2(648f, 68f), UITheme.AccentAmber, UITheme.Border, 22, cachedFont);
                hintBtn.onClick.AddListener(() =>
                {
                    HangarAudio.Instance?.PlayClick();
                    GameManager.Instance?.SwitchView(RoomView.Hangar);
                });
                spawnedRows.Add(hintBtn.gameObject);
            }

            if (itemsContainer is RectTransform rt)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }

        private void ClearRows()
        {
            foreach (var go in spawnedRows)
            {
                if (go == null) continue;
                go.transform.SetParent(null, false);
                ModelUtils.DestroySafe(go);
            }
            spawnedRows.Clear();
        }

        private void CreateItemRow(WeaponDefinition def, MechUnit mech, ArmoryManager arm)
        {
            bool owned = arm.IsOwned(def.id);
            bool equipped = arm.IsEquipped(mech, def.id);
            bool canAfford = GameManager.Instance != null && GameManager.Instance.CanAfford(def.costCredits, def.costAlloy);
            bool mechBusy = mech != null && mech.isDeployed;

            Color bgCol = equipped ? UITheme.BgCardActive : UITheme.BgCard;
            GameObject row = UIBuilder.Panel(itemsContainer, $"Item_{def.id}", Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(648f, 154f), bgCol, equipped ? UITheme.AccentTeal : UITheme.Border);
            row.GetComponent<Image>().raycastTarget = false;
            spawnedRows.Add(row);

            GameObject bar = UIBuilder.Panel(row.transform, "Accent", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(10f, 134f), def.accentColor);
            bar.GetComponent<Image>().raycastTarget = false;

            Text nTxt = UIBuilder.Label(row.transform, "Name", def.displayName, new Vector2(0f, 0.5f), new Vector2(210f, 34f), new Vector2(375f, 44f), 28, UITheme.TextMain, cachedFont, FontStyle.Bold, TextAnchor.MiddleLeft);
            nTxt.raycastTarget = false;

            string stats = "";
            if (def.bonusAttack > 0f) stats += $"+{def.bonusAttack:F0} урон  •  ";
            if (def.bonusDefense > 0f) stats += $"+{def.bonusDefense:F0} броня  •  ";
            if (def.bonusMaxHp > 0f) stats += $"+{def.bonusMaxHp:F0} ХП  •  ";
            stats += def.damageType.ToRu();

            UIBuilder.Label(row.transform, "Stats", stats, new Vector2(0f, 0.5f), new Vector2(210f, -24f), new Vector2(375f, 46f), 22, UITheme.TextMuted, cachedFont, FontStyle.Normal, TextAnchor.MiddleLeft);

            string btnLabel = equipped ? "Надето" : (owned ? "Надеть" : $"{def.costCredits} кр / {def.costAlloy} м");
            Color btnCol = equipped ? UITheme.AccentGreen
                : (mechBusy ? UITheme.BgRecessed
                : (owned ? UITheme.AccentTeal
                : (canAfford ? UITheme.AccentAmber : UITheme.BgRecessed)));

            Button btn = UIBuilder.Button(row.transform, "Btn_Action", btnLabel, new Vector2(1f, 0.5f), new Vector2(-114f, 0f), new Vector2(204f, 68f), btnCol, UITheme.Border, 22, cachedFont);
            btn.interactable = !equipped && !mechBusy && (owned || canAfford);
            btn.onClick.AddListener(() =>
            {
                if (arm.TryBuyOrEquip(def.id))
                {
                    if (txtFeedback != null)
                    {
                        txtFeedback.color = UITheme.AccentGreen;
                        txtFeedback.text = $"Установлено: {def.displayName}";
                    }
                    Refresh();
                }
                else if (txtFeedback != null)
                {
                    txtFeedback.color = UITheme.AccentRed;
                    txtFeedback.text = mechBusy ? "Робот в бою" : "Недостаточно ресурсов";
                }
            });
        }
    }
}
