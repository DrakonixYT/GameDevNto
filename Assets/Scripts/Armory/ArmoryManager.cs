using System;
using System.Collections.Generic;
using HangarGame.Core;
using HangarGame.Mechs;
using UnityEngine;

namespace HangarGame.Armory
{
    public class ArmoryManager : MonoBehaviour
    {
        public static ArmoryManager Instance { get; private set; }

        [Header("Каталог вооружения")]
        [SerializeField] private List<WeaponDefinition> catalog = new List<WeaponDefinition>();

        private readonly HashSet<string> ownedIds = new HashSet<string>();

        public IReadOnlyList<WeaponDefinition> Catalog
        {
            get
            {
                EnsureCatalog();
                return catalog;
            }
        }
        public event Action OnArmoryChanged;

        public void EnsureCatalog()
        {
            if (catalog == null || catalog.Count == 0)
                catalog = ArmoryCatalog.CreateDefaultCatalog();
        }

        public void ResetCatalogFromDefaults()
        {
            var prevPrefabs = new Dictionary<string, GameObject>();
            if (catalog != null)
            {
                foreach (var item in catalog)
                {
                    if (item != null && !string.IsNullOrEmpty(item.id) && item.visualPrefab != null)
                        prevPrefabs[item.id] = item.visualPrefab;
                }
            }

            catalog = ArmoryCatalog.CreateDefaultCatalog();
            foreach (var item in catalog)
            {
                if (item != null && prevPrefabs.TryGetValue(item.id, out var prefab))
                    item.visualPrefab = prefab;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureCatalog();

            ownedIds.Add(ArmoryCatalog.DefaultWeaponId);
            ownedIds.Add(ArmoryCatalog.DefaultShieldId);
        }

        private void Start()
        {
            if (HangarMechManager.Instance == null) return;
            foreach (var mech in HangarMechManager.Instance.Mechs)
            {
                if (mech != null) SyncMechLoadout(mech);
            }
        }

        public void SetWeaponPrefab(string weaponId, GameObject prefab)
        {
            EnsureCatalog();
            var def = FindWeapon(weaponId);
            if (def != null) def.visualPrefab = prefab;
        }

        public WeaponDefinition FindWeapon(string id)
        {
            EnsureCatalog();
            return catalog.Find(w => w.id == id);
        }

        public bool IsOwned(string id) => ownedIds.Contains(id);

        public bool IsEquipped(MechUnit mech, string weaponId)
        {
            if (mech == null) return false;
            return mech.equippedLeftWeaponId == weaponId || mech.equippedRightWeaponId == weaponId;
        }

        public bool TryBuyOrEquip(string weaponId)
        {
            var mech = HangarMechManager.Instance != null ? HangarMechManager.Instance.CurrentMech : null;
            if (mech == null || mech.isDeployed) return false;

            var def = FindWeapon(weaponId);
            if (def == null) return false;

            if (!IsOwned(weaponId))
            {
                if (GameManager.Instance == null || !GameManager.Instance.TrySpend(def.costCredits, def.costAlloy))
                {
                    HangarAudio.Play(Sfx.Error);
                    return false;
                }
                ownedIds.Add(weaponId);
                HangarAudio.Play(Sfx.Buy);
            }
            else
            {
                HangarAudio.Play(Sfx.Equip);
            }

            if (def.slot == ArmorySlot.LeftArm) mech.equippedLeftWeaponId = def.id;
            else mech.equippedRightWeaponId = def.id;

            SyncMechLoadout(mech);
            OnArmoryChanged?.Invoke();
            return true;
        }

        public void SyncMechLoadout(MechUnit mech)
        {
            if (mech == null) return;
            var left = FindWeapon(mech.equippedLeftWeaponId) ?? FindWeapon(ArmoryCatalog.DefaultShieldId);
            var right = FindWeapon(mech.equippedRightWeaponId) ?? FindWeapon(ArmoryCatalog.DefaultWeaponId);

            mech.ApplyLoadout(left, right, left?.visualPrefab, right?.visualPrefab);
        }
    }
}
