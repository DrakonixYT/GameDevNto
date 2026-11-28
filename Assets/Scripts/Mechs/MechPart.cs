using System.Collections;
using HangarGame.Armory;
using HangarGame.Core;
using HangarGame.Interaction;
using HangarGame.Repair;
using UnityEngine;

namespace HangarGame.Mechs
{
    public class MechPart : MonoBehaviour, IInteractable
    {
        [SerializeField] private MechPartData data;
        [SerializeField] private Renderer intactRenderer;
        [SerializeField] private GameObject brokenVisual;
        [SerializeField] private Transform weaponMountPoint;

        [SerializeField] private Vector3 socketPos;
        [SerializeField] private Quaternion socketRot;
        [SerializeField] private Vector3 socketScale = Vector3.one;
        [SerializeField] private Transform socketParent;

        public MechPartData Data { get => data; set => data = value; }
        public bool IsOnWorkbench { get; set; }
        public float DurabilityPercent => maxHp > 0f ? Mathf.Clamp01(currentHp / maxHp) : 1f;
        public bool IsDamaged => DurabilityPercent < 0.99f;
        public bool IsRepairableSlot => data != null && (data.slot == PartSlot.Head || data.slot == PartSlot.LeftArm || data.slot == PartSlot.RightArm);

        private float maxHp = 100f;
        private float currentHp = 100f;
        private Color baseColor = Color.white;
        private Material baseSharedMaterial;
        private bool isHoloMaterial;
        private bool hasInitializedColors;
        private bool isReturning;
        private GameObject equippedWeaponVisual;

        public Color BaseColor
        {
            get
            {
                EnsureColorsInitialized();
                return baseColor;
            }
            set => baseColor = value;
        }

        private void Awake()
        {
            if (intactRenderer == null)
            {
                intactRenderer = GetComponent<Renderer>();
                if (intactRenderer == null) intactRenderer = GetComponentInChildren<Renderer>();
            }

            if (data == null)
                data = new MechPartData { partName = gameObject.name };

            EnsureColorsInitialized();

            if (socketParent == null && transform.parent != null)
                InitSocket();

            UpdateVisuals();
        }

        private void EnsureColorsInitialized()
        {
            if (hasInitializedColors) return;

            if (intactRenderer == null)
            {
                intactRenderer = GetComponent<Renderer>();
                if (intactRenderer == null) intactRenderer = GetComponentInChildren<Renderer>();
            }

            if (intactRenderer != null && intactRenderer.sharedMaterial != null)
            {
                baseSharedMaterial = intactRenderer.sharedMaterial;
                isHoloMaterial = baseSharedMaterial.name.IndexOf("Holo", System.StringComparison.OrdinalIgnoreCase) >= 0;
                if (baseSharedMaterial.HasProperty("_BaseColor"))
                    baseColor = baseSharedMaterial.GetColor("_BaseColor");
                else
                    baseColor = baseSharedMaterial.color;
                hasInitializedColors = true;
                return;
            }

            var unit = GetComponentInParent<MechUnit>();
            baseColor = unit != null ? unit.themeColor : Color.white;
            hasInitializedColors = true;
        }

        public void InitSocket()
        {
            socketPos = transform.localPosition;
            socketRot = transform.localRotation;
            socketScale = transform.localScale;
            socketParent = transform.parent;
        }

        public void Setup(MechPartData partData)
        {
            data = partData;
            currentHp = maxHp;
            InitSocket();
            UpdateVisuals();
        }

        public void SetVisuals(Renderer intact, GameObject broken)
        {
            intactRenderer = intact;
            brokenVisual = broken;
            hasInitializedColors = false;
            EnsureColorsInitialized();
            UpdateVisuals();
        }

        public void AttachBrokenVisual(GameObject brokenPrefab)
        {
            if (brokenPrefab == null) return;

            var existing = transform.Find("Broken_Visual");
            if (existing != null) DestroyImmediate(existing.gameObject);

            EnsureColorsInitialized();

            GameObject brokenObj = Instantiate(brokenPrefab, transform, false);
            brokenObj.name = "Broken_Visual";
            brokenObj.transform.localPosition = Vector3.zero;
            brokenObj.transform.localRotation = Quaternion.identity;
            brokenObj.transform.localScale = Vector3.one;

            Material sourceMat = baseSharedMaterial != null ? baseSharedMaterial : intactRenderer?.sharedMaterial;
            if (sourceMat != null)
            {
                foreach (var r in brokenObj.GetComponentsInChildren<Renderer>(true))
                    r.sharedMaterial = sourceMat;
            }

            AlignBounds(brokenObj, intactRenderer);

            foreach (var col in brokenObj.GetComponentsInChildren<Collider>(true))
                DestroyImmediate(col);

            foreach (var r in brokenObj.GetComponentsInChildren<Renderer>(true))
            {
                if (r is MeshRenderer && r.GetComponent<MeshFilter>() != null && r.GetComponent<BoxCollider>() == null)
                {
                    var mf = r.GetComponent<MeshFilter>();
                    if (mf.sharedMesh != null)
                    {
                        var box = r.gameObject.AddComponent<BoxCollider>();
                        box.center = mf.sharedMesh.bounds.center;
                        box.size = mf.sharedMesh.bounds.size;
                    }
                }
            }

            brokenVisual = brokenObj;
            brokenVisual.SetActive(IsDamaged);
            UpdateVisuals();
        }

        private void AlignBounds(GameObject targetObj, Renderer referenceRend)
        {
            if (referenceRend == null) return;

            Bounds targetB = CalcBounds(targetObj);
            Bounds refB = referenceRend.bounds;
            if (targetB.size.sqrMagnitude < 0.0001f || refB.size.sqrMagnitude < 0.0001f) return;

            float scaleRatio = refB.size.magnitude / targetB.size.magnitude;
            targetObj.transform.localScale *= scaleRatio;

            targetB = CalcBounds(targetObj);
            Vector3 offset = refB.center - targetB.center;
            targetObj.transform.position += offset;
        }

        public void MountWeaponVisual(WeaponDefinition weapon, GameObject modelPrefab)
        {
            EnsureColorsInitialized();
            ClearEquippedWeaponOn(transform, ref equippedWeaponVisual);
            if (weapon == null || !weapon.hasCustomVisual) return;

            Transform parentMount = weaponMountPoint != null ? weaponMountPoint : transform;
            Bounds armBounds = ResolveArmBounds();
            equippedWeaponVisual = CreateWeaponVisualInstance(parentMount, armBounds, weapon, modelPrefab, isHoloMaterial ? baseSharedMaterial : null);
        }

        public static void MountWeaponVisualOnArm(Transform armRoot, WeaponDefinition weapon, GameObject modelPrefab)
        {
            if (armRoot == null) return;
            for (int i = armRoot.childCount - 1; i >= 0; i--)
            {
                var child = armRoot.GetChild(i);
                if (child.name.StartsWith("Equipped_"))
                {
                    if (Application.isPlaying) Destroy(child.gameObject);
                    else DestroyImmediate(child.gameObject);
                }
            }
            if (weapon == null || !weapon.hasCustomVisual) return;

            Bounds armBounds = CalcBounds(armRoot.gameObject);
            if (armBounds.size.sqrMagnitude < 0.0001f) armBounds = new Bounds(armRoot.position, Vector3.one);
            CreateWeaponVisualInstance(armRoot, armBounds, weapon, modelPrefab, null);
        }

        private static void ClearEquippedWeaponOn(Transform root, ref GameObject cached)
        {
            if (cached != null)
            {
                if (Application.isPlaying) Destroy(cached);
                else DestroyImmediate(cached);
                cached = null;
            }
        }

        private static GameObject CreateWeaponVisualInstance(Transform parentMount, Bounds armBounds, WeaponDefinition weapon, GameObject modelPrefab, Material holoMat)
        {
            GameObject visual;
            if (modelPrefab != null)
            {
                visual = Instantiate(modelPrefab, parentMount, false);
                visual.name = $"Equipped_{weapon.id}";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = weapon.slot == ArmorySlot.RightArm
                    ? Quaternion.Euler(-22f, 0f, -6f)
                    : Quaternion.Euler(0f, 90f, 0f);
                visual.transform.localScale = Vector3.one;

                foreach (var col in visual.GetComponentsInChildren<Collider>(true))
                {
                    if (Application.isPlaying) Destroy(col);
                    else DestroyImmediate(col);
                }

                Bounds wpBounds = CalcBounds(visual);
                if (wpBounds.size.sqrMagnitude > 0.0001f)
                {
                    float desiredLen = Mathf.Max(0.25f, armBounds.size.magnitude * (weapon.visualScaleFactor > 0f ? weapon.visualScaleFactor : 0.85f));
                    float ratio = desiredLen / wpBounds.size.magnitude;
                    visual.transform.localScale *= ratio;
                    wpBounds = CalcBounds(visual);
                    Vector3 sideOffset = weapon.slot == ArmorySlot.LeftArm
                        ? -parentMount.right * (armBounds.extents.x * 0.52f) - parentMount.forward * (armBounds.extents.z * 0.22f)
                        : parentMount.right * (armBounds.extents.x * 0.65f) - parentMount.forward * (armBounds.extents.z * 0.28f);
                    visual.transform.position += (armBounds.center + sideOffset) - wpBounds.center;
                }
            }
            else
            {
                visual = GameObject.CreatePrimitive(weapon.slot == ArmorySlot.LeftArm ? PrimitiveType.Cube : PrimitiveType.Cylinder);
                visual.name = $"Equipped_{weapon.id}";
                visual.transform.SetParent(parentMount, false);
                var col = visual.GetComponent<Collider>();
                if (col != null)
                {
                    if (Application.isPlaying) Destroy(col);
                    else DestroyImmediate(col);
                }

                float unitLen = Mathf.Max(0.15f, armBounds.size.magnitude * 0.42f);
                visual.transform.localScale = weapon.slot == ArmorySlot.LeftArm
                    ? new Vector3(unitLen * 0.3f, unitLen * 1.4f, unitLen * 1.1f)
                    : new Vector3(unitLen * 0.35f, unitLen * 1.8f, unitLen * 0.35f);
                visual.transform.position = armBounds.center + (weapon.slot == ArmorySlot.LeftArm ? -parentMount.right * (armBounds.extents.x * 0.5f) : parentMount.forward * (armBounds.extents.z * 0.5f));
                visual.transform.localRotation = weapon.slot == ArmorySlot.RightArm ? Quaternion.Euler(90f, 0f, 0f) : Quaternion.identity;
            }

            TintWeaponRenderers(visual, weapon.accentColor, holoMat);
            return visual;
        }

        private static void TintWeaponRenderers(GameObject go, Color accent, Material holoMat)
        {
            Shader shader = Shader.Find("SimpleURPToonLitExample(With Outline)");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (holoMat != null)
                {
                    var mat = new Material(holoMat);
                    if (mat.HasProperty("_EmissionColor"))
                    {
                        Color baseC = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : accent;
                        mat.SetColor("_EmissionColor", baseC * 0.4f);
                    }
                    r.sharedMaterial = mat;
                }
                else
                {
                    Color solidCol = new Color(accent.r * 0.72f, accent.g * 0.72f, accent.b * 0.72f, 1f);
                    var mat = new Material(shader);
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", solidCol);
                    mat.color = solidCol;
                    if (mat.HasProperty("_UseEmission")) mat.SetFloat("_UseEmission", 0f);
                    if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", Color.black);
                    r.sharedMaterial = mat;
                }
            }
        }

        public void Damage(float amount)
        {
            if (!IsRepairableSlot) return;
            currentHp = Mathf.Max(0f, currentHp - amount);
            UpdateVisuals();
            SyncParentHpIfInHangar();
        }

        public void RepairFull()
        {
            currentHp = maxHp;
            UpdateVisuals();
            SyncParentHpIfInHangar();
        }

        public void SetDurabilityPercent(float pct)
        {
            currentHp = maxHp * Mathf.Clamp01(pct);
            UpdateVisuals();
            SyncParentHpIfInHangar();
        }

        private void SyncParentHpIfInHangar()
        {
            var unit = GetComponentInParent<MechUnit>();
            if (unit != null && !unit.isDeployed)
                unit.SyncHpFromParts();
        }

        public void ReturnToSocket()
        {
            if (socketParent != null)
                transform.SetParent(socketParent, true);

            if (Application.isPlaying && gameObject.activeInHierarchy)
            {
                isReturning = true;
                StartCoroutine(SlideBackCoroutine());
            }
            else
            {
                InstantReturn();
            }
        }

        private void OnDisable()
        {
            if (isReturning)
                InstantReturn();
        }

        private void InstantReturn()
        {
            isReturning = false;
            if (socketParent != null) transform.SetParent(socketParent, false);
            transform.localPosition = socketPos;
            transform.localRotation = socketRot;
            transform.localScale = socketScale;
            IsOnWorkbench = false;
            foreach (var col in GetComponentsInChildren<Collider>(true)) col.enabled = true;
            UpdateVisuals();
        }

        private IEnumerator SlideBackCoroutine()
        {
            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;
            Vector3 startScale = transform.localScale;

            Vector3 endPos = socketParent != null ? socketParent.TransformPoint(socketPos) : socketPos;
            Quaternion endRot = socketParent != null ? socketParent.rotation * socketRot : socketRot;

            float elapsed = 0f;
            const float duration = 0.28f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                transform.position = Vector3.Lerp(startPos, endPos, t);
                transform.rotation = Quaternion.Slerp(startRot, endRot, t);
                transform.localScale = Vector3.Lerp(startScale, socketScale, t);
                yield return null;
            }

            InstantReturn();
        }

        public void UpdateVisuals()
        {
            EnsureColorsInitialized();
            bool showBroken = IsDamaged && brokenVisual != null;
            if (intactRenderer != null) intactRenderer.enabled = !showBroken;
            if (brokenVisual != null) brokenVisual.SetActive(showBroken);

            ApplyColor(GetRestColor());
        }

        private Color GetRestColor()
        {
            EnsureColorsInitialized();
            if (isHoloMaterial)
            {
                return IsDamaged
                    ? Color.Lerp(new Color(0.55f, 0.10f, 0.08f), baseColor, DurabilityPercent)
                    : baseColor;
            }
            return Color.Lerp(new Color(0.75f, 0.15f, 0.1f), baseColor, DurabilityPercent);
        }

        private void ApplyColor(Color col)
        {
            var renderers = GetComponentsInChildren<Renderer>(false);
            foreach (var r in renderers)
            {
                if (r == null || r.sharedMaterial == null) continue;
                if (equippedWeaponVisual != null && r.transform.IsChildOf(equippedWeaponVisual.transform)) continue;
                if (r.GetComponentInParent<MechPart>() != this) continue;

                if (Application.isPlaying)
                {
                    if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", col);
                    if (r.material.HasProperty("_Color")) r.material.SetColor("_Color", col);
                    r.material.color = col;
                    if (isHoloMaterial && r.material.HasProperty("_EmissionColor"))
                        r.material.SetColor("_EmissionColor", col * 0.45f);
                }
                else
                {
                    var block = new MaterialPropertyBlock();
                    r.GetPropertyBlock(block);
                    block.SetColor("_BaseColor", col);
                    block.SetColor("_Color", col);
                    r.SetPropertyBlock(block);
                }
            }
        }

        public bool CanInteract()
        {
            if (!IsRepairableSlot || IsOnWorkbench || !gameObject.activeInHierarchy) return false;
            var unit = GetComponentInParent<MechUnit>();
            return unit == null || !unit.isDeployed;
        }

        public string GetPrompt()
        {
            string title = data != null ? data.partName : gameObject.name;
            int hpPct = Mathf.RoundToInt(DurabilityPercent * 100f);

            if (!IsDamaged)
                return $"[{title}] {hpPct}% — Исправен (ремонт не нужен)";

            if (data != null && data.repairCredits > 0)
                return $"[{title}] {hpPct}% — Клик: на верстак ({data.repairCredits}¢, {data.repairAlloy}т)";

            return $"[{title}] {hpPct}% — Клик: снять на верстак";
        }

        public void OnHover(bool isHovered)
        {
            if (IsOnWorkbench || !IsRepairableSlot) return;
            EnsureColorsInitialized();
            Color hoverTarget = isHoloMaterial
                ? (IsDamaged ? new Color(0.75f, 0.30f, 0.26f) : new Color(0.18f, 0.65f, 0.72f))
                : (IsDamaged ? Color.cyan : new Color(0.45f, 1f, 0.65f));
            ApplyColor(isHovered ? hoverTarget : GetRestColor());
        }

        public void OnInteract()
        {
            if (!CanInteract()) return;

            if (!IsDamaged)
            {
                HangarAudio.Instance?.PlayClick();
                return;
            }

            RepairWorkbench.Instance?.ReceivePart(this);
        }

        public void OnInteractHold() { }

        private Bounds ResolveArmBounds()
        {
            if (intactRenderer != null && intactRenderer.enabled && intactRenderer.bounds.size.sqrMagnitude > 0.0001f)
                return intactRenderer.bounds;

            if (brokenVisual != null && brokenVisual.activeInHierarchy)
            {
                Bounds brokenBounds = CalcBounds(brokenVisual);
                if (brokenBounds.size.sqrMagnitude > 0.0001f)
                    return brokenBounds;
            }

            if (intactRenderer != null)
            {
                var mf = intactRenderer.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    Bounds lb = mf.sharedMesh.bounds;
                    Vector3 worldCenter = intactRenderer.transform.TransformPoint(lb.center);
                    Vector3 worldSize = Vector3.Scale(lb.size, intactRenderer.transform.lossyScale);
                    worldSize = new Vector3(Mathf.Abs(worldSize.x), Mathf.Abs(worldSize.y), Mathf.Abs(worldSize.z));
                    if (worldSize.sqrMagnitude > 0.0001f)
                        return new Bounds(worldCenter, worldSize);
                }
            }

            return new Bounds(transform.position, Vector3.one * 0.3f);
        }

        private static Bounds CalcBounds(GameObject go)
        {
            var rends = go.GetComponentsInChildren<Renderer>(true);
            if (rends.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            return b;
        }
    }
}
