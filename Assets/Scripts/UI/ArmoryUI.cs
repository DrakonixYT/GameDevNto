using System;
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
        [Serializable]
        public class MechCardView
        {
            public Button btn;
            public Image bg;
            public Image accent;
            public Image hpFill;
            public Text info;
        }

        [Header("Ссылки")]
        public GameManager game;
        public HangarMechManager hangar;
        public ArmoryManager armory;
        public RoomCamera roomCamera;

        [Header("Виджеты")]
        public GameObject panelRoot;
        public Text txtResources;
        public Text txtFocusBtn;
        public Button btnFocusMonitor;
        public GameObject pageMechSelect;
        public MechCardView[] mechCards = new MechCardView[3];
        public GameObject pageLoadout;
        public Button btnBackToMechs;
        public Button btnTabWeapons;
        public Button btnTabShields;
        public Text txtSelectedMechTitle;
        public Text txtLoadoutSummary;
        public Text txtFeedback;
        public Transform itemsContainer;

        private ArmorySlot activeSlot = ArmorySlot.RightArm;
        private bool isSelectingMech = true;
        private Font cachedFont;
        private readonly List<GameObject> spawnedRows = new List<GameObject>();

        private void Awake()
        {
            game ??= GameManager.Instance ?? FindAnyObjectByType<GameManager>();
            hangar ??= game?.hangar ?? FindAnyObjectByType<HangarMechManager>();
            armory ??= game?.armory ?? FindAnyObjectByType<ArmoryManager>();
            roomCamera ??= game?.roomCamera ?? FindAnyObjectByType<RoomCamera>();

            if (panelRoot != null) panelRoot.SetActive(true);

            if (btnFocusMonitor != null)
            {
                btnFocusMonitor.onClick.RemoveAllListeners();
                btnFocusMonitor.onClick.AddListener(() => { roomCamera?.ToggleArmoryFocus(); Refresh(); });
            }
            if (btnBackToMechs != null)
            {
                btnBackToMechs.onClick.RemoveAllListeners();
                btnBackToMechs.onClick.AddListener(() => { game?.PlaySfx(Sfx.Click); ShowMechSelectStep(); });
            }
            if (btnTabWeapons != null)
            {
                btnTabWeapons.onClick.RemoveAllListeners();
                btnTabWeapons.onClick.AddListener(() => SwitchSlotTab(ArmorySlot.RightArm));
            }
            if (btnTabShields != null)
            {
                btnTabShields.onClick.RemoveAllListeners();
                btnTabShields.onClick.AddListener(() => SwitchSlotTab(ArmorySlot.LeftArm));
            }

            for (int i = 0; i < mechCards.Length; i++)
            {
                var card = mechCards[i];
                if (card?.btn == null) continue;
                int idx = i;
                card.btn.onClick.RemoveAllListeners();
                card.btn.onClick.AddListener(() => SelectMechAndOpenMenu(idx));
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
                game.OnViewChanged -= OnViewChanged;
                if (subscribe)
                {
                    game.OnResourcesChanged += OnResourcesChanged;
                    game.OnViewChanged += OnViewChanged;
                }
            }

            if (hangar != null)
            {
                hangar.OnMechSwitched -= OnMechSwitched;
                if (subscribe) hangar.OnMechSwitched += OnMechSwitched;
            }

            if (armory != null)
            {
                armory.OnArmoryChanged -= Refresh;
                if (subscribe) armory.OnArmoryChanged += Refresh;
            }

            if (roomCamera != null)
            {
                roomCamera.OnArmoryFocusChanged -= OnFocusChanged;
                if (subscribe) roomCamera.OnArmoryFocusChanged += OnFocusChanged;
            }

            if (subscribe) Refresh();
        }

        private void OnResourcesChanged(int c, int a) => Refresh();
        private void OnViewChanged(RoomView view) => Refresh();
        private void OnMechSwitched(int idx) => Refresh();
        private void OnFocusChanged(bool focused) => RefreshFocusButton();

        public void ShowMechSelectStep()
        {
            isSelectingMech = true;
            if (txtFeedback != null) txtFeedback.text = "";
            Refresh();
        }

        public void SelectMechAndOpenMenu(int idx)
        {
            game?.PlaySfx(Sfx.Click);
            hangar?.SelectMech(idx);
            isSelectingMech = false;
            if (txtFeedback != null) txtFeedback.text = "";
            Refresh();
        }

        public void SwitchSlotTab(ArmorySlot slot)
        {
            activeSlot = slot;
            game?.PlaySfx(Sfx.Click);
            if (txtFeedback != null) txtFeedback.text = "";
            Refresh();
        }

        public void Refresh()
        {
            if (txtResources != null && game != null)
                txtResources.text = $"Кредиты: {game.Credits}   •   Сплав: {game.Alloy}";

            RefreshFocusButton();

            if (pageMechSelect != null) pageMechSelect.SetActive(isSelectingMech);
            if (pageLoadout != null) pageLoadout.SetActive(!isSelectingMech);

            if (isSelectingMech) RefreshMechCards();
            else RefreshLoadoutMenu();
        }

        private void RefreshFocusButton()
        {
            if (txtFocusBtn != null && roomCamera != null)
                txtFocusBtn.text = roomCamera.IsArmoryFocused ? "ОБЗОР" : "КРУПНЕЕ";
        }

        private void RefreshMechCards()
        {
            for (int i = 0; i < mechCards.Length; i++)
            {
                var card = mechCards[i];
                if (card == null) continue;
                if (hangar == null || i >= hangar.Mechs.Count || hangar.Mechs[i] == null)
                {
                    if (card.btn != null) card.btn.gameObject.SetActive(false);
                    continue;
                }

                var m = hangar.Mechs[i];
                bool isCurrent = i == hangar.CurrentIndex;
                string wName = armory?.FindWeapon(m.equippedRightWeaponId)?.displayName ?? "Автопушка";
                string sName = armory?.FindWeapon(m.equippedLeftWeaponId)?.displayName ?? "Обычный щит";
                string state = m.isDeployed ? "В бою" : (m.HasMissingOrDetachedParts() ? "Повреждён" : "В ангаре");

                if (card.btn != null) card.btn.gameObject.SetActive(true);
                if (card.bg != null) card.bg.color = isCurrent ? UITheme.BgCardActive : UITheme.BgCard;
                if (card.accent != null) card.accent.color = Color.Lerp(m.themeColor, UITheme.Border, 0.35f);

                if (card.info != null)
                {
                    card.info.text =
                        $"<b>{m.mechName}</b>  •  {state}\n" +
                        $"ХП {m.currentHp:F0}/{m.maxHp:F0}  •  Урон {m.attackPower:F0}  •  Эн {m.currentEnergy:F0}%\n" +
                        $"<color=#94ABBE>{wName} / {sName}</color>";
                }

                if (card.hpFill != null)
                {
                    float r = m.GetTotalHpPercent();
                    card.hpFill.color = r > 0.5f ? UITheme.AccentGreen : (r > 0.25f ? UITheme.AccentAmber : UITheme.AccentRed);
                    UIBuilder.SetBar(card.hpFill, r);
                }
            }
        }

        private void RefreshLoadoutMenu()
        {
            var mech = hangar?.CurrentMech;

            if (txtSelectedMechTitle != null)
                txtSelectedMechTitle.text = mech != null ? mech.mechName.ToUpper() : "РОБОТ";

            if (txtLoadoutSummary != null && mech != null)
            {
                string rName = armory?.FindWeapon(mech.equippedRightWeaponId)?.displayName ?? "Автопушка";
                string lName = armory?.FindWeapon(mech.equippedLeftWeaponId)?.displayName ?? "Обычный щит";
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
            if (itemsContainer == null || armory == null) return;
            cachedFont ??= UIBuilder.GetFont(26);

            foreach (var def in armory.Catalog)
                if (def.slot == activeSlot) CreateItemRow(def, mech);

            if (mech != null && mech.HasMissingOrDetachedParts())
            {
                var hintBtn = UIBuilder.Button(itemsContainer, "Btn_GoHangarRepair", "Модуль повреждён — перейти в ангар", Vector2.one * 0.5f, Vector2.zero, new Vector2(596f, 64f), UITheme.AccentAmber, null, 22, cachedFont);
                hintBtn.onClick.AddListener(() =>
                {
                    game?.PlaySfx(Sfx.Click);
                    game?.SwitchView(RoomView.Hangar);
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

        private void CreateItemRow(WeaponDefinition def, MechUnit mech)
        {
            bool owned = armory.IsOwned(def.id);
            bool equipped = armory.IsEquipped(mech, def.id);
            bool canAfford = game != null && game.CanAfford(def.costCredits, def.costAlloy);
            bool mechBusy = mech != null && mech.isDeployed;

            Color bgCol = equipped ? UITheme.BgCardActive : UITheme.BgCard;
            GameObject row = UIBuilder.Panel(itemsContainer, $"Item_{def.id}", Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(596f, 148f), bgCol);
            row.GetComponent<Image>().raycastTarget = false;
            spawnedRows.Add(row);

            GameObject bar = UIBuilder.Panel(row.transform, "Accent", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(8f, 128f), def.accentColor);
            bar.GetComponent<Image>().raycastTarget = false;

            UIBuilder.Label(row.transform, "Name", def.displayName, new Vector2(0f, 0.5f), new Vector2(198f, 32f), new Vector2(352f, 44f), 27, UITheme.TextMain, cachedFont, FontStyle.Bold, TextAnchor.MiddleLeft);

            string stats = "";
            if (def.bonusAttack > 0f) stats += $"+{def.bonusAttack:F0} урон  •  ";
            if (def.bonusDefense > 0f) stats += $"+{def.bonusDefense:F0} броня  •  ";
            if (def.bonusMaxHp > 0f) stats += $"+{def.bonusMaxHp:F0} ХП  •  ";
            stats += def.damageType.ToRu();

            UIBuilder.Label(row.transform, "Stats", stats, new Vector2(0f, 0.5f), new Vector2(198f, -24f), new Vector2(352f, 46f), 22, UITheme.TextMuted, cachedFont, FontStyle.Normal, TextAnchor.MiddleLeft);

            string btnLabel = equipped ? "Надето" : (owned ? "Надеть" : $"{def.costCredits} кр / {def.costAlloy} м");
            Color btnCol = equipped ? UITheme.AccentGreen
                : (mechBusy ? UITheme.BgRecessed
                : (owned ? UITheme.AccentTeal
                : (canAfford ? UITheme.AccentAmber : UITheme.BgRecessed)));

            Button btn = UIBuilder.Button(row.transform, "Btn_Action", btnLabel, new Vector2(1f, 0.5f), new Vector2(-106f, 0f), new Vector2(190f, 66f), btnCol, null, 22, cachedFont);
            btn.interactable = !equipped && !mechBusy && (owned || canAfford);
            btn.onClick.AddListener(() =>
            {
                if (armory.TryBuyOrEquip(def.id))
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
