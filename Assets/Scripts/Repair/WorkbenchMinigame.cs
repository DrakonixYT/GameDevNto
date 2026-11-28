using System.Collections;
using System.Collections.Generic;
using HangarGame.Core;
using HangarGame.Interaction;
using UnityEngine;

namespace HangarGame.Repair
{
    public class WorkbenchMinigame : MonoBehaviour
    {
        [SerializeField] private GameObject platePrefab;

        private static readonly Color[] ChannelColors =
        {
            new Color(0.75f, 0.24f, 0.24f),
            new Color(0.20f, 0.56f, 0.72f),
            new Color(0.82f, 0.58f, 0.20f),
            new Color(0.24f, 0.62f, 0.38f)
        };

        private readonly List<GameObject> spawned = new List<GameObject>();
        private readonly Dictionary<int, Vector3> leftPins = new Dictionary<int, Vector3>();
        private readonly Dictionary<int, Vector3> rightPins = new Dictionary<int, Vector3>();

        private Transform pivotRef;
        private GameObject hatchRoot;
        private GameObject coverPlate;
        private bool jaegerMode;
        private int boltsRemaining;
        private int stepsRemaining;

        private float panelW = 0.16f;
        private float panelH = 0.13f;
        private float boltScale = 0.022f;
        private float pinScale = 0.022f;

        private GameObject selectedObj;
        private int selectedId = -1;
        private int selectedSide = -1;
        private Color selectedOrigColor = Color.white;
        private Vector3 selectedBasePos;

        public bool IsComplete => boltsRemaining <= 0 && stepsRemaining <= 0;
        public string StatusText { get; private set; } = "";

        public void SetPlatePrefab(GameObject prefab) => platePrefab = prefab;

        public void Begin(Transform modelPivot, Bounds localBounds, Vector3 camLocalDir, bool isJaeger)
        {
            Clear();
            if (modelPivot == null) return;

            pivotRef = modelPivot;
            jaegerMode = isJaeger;

            Vector3 dir = camLocalDir.sqrMagnitude > 0.001f ? camLocalDir.normalized : Vector3.back;
            Bounds meshBounds = CalcMeshLocalBounds(modelPivot, localBounds);

            panelW = Mathf.Clamp(meshBounds.size.x * 0.62f, 0.11f, 0.25f);
            panelH = Mathf.Clamp(meshBounds.size.y * 0.50f, 0.10f, 0.19f);
            float minDim = Mathf.Min(panelW, panelH);
            boltScale = Mathf.Clamp(minDim * 0.16f, 0.016f, 0.028f);
            pinScale = Mathf.Clamp(minDim * 0.18f, 0.018f, 0.030f);

            float surfaceZ = SampleFrontSurfaceZ(modelPivot, meshBounds.center, dir, meshBounds);

            hatchRoot = new GameObject("Service_Bay");
            hatchRoot.transform.SetParent(modelPivot, false);
            hatchRoot.transform.localPosition = new Vector3(meshBounds.center.x, meshBounds.center.y, surfaceZ) + dir * 0.008f;
            hatchRoot.transform.localRotation = Quaternion.LookRotation(dir, Vector3.up);
            spawned.Add(hatchRoot);

            coverPlate = CreatePrimitive(PrimitiveType.Cube, "Service_Cover", hatchRoot.transform, new Vector3(0f, 0f, -0.003f), new Vector3(panelW, panelH, 0.008f), new Color(0.15f, 0.18f, 0.22f), false);
            SpawnBolts();
        }

        private GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Color color, bool keepCollider, bool emissive = false, float emMult = 0.4f)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().material = ModelUtils.CreateLitMaterial(color, emissive, emMult);
            if (!keepCollider) ModelUtils.DestroySafe(go.GetComponent<Collider>());
            spawned.Add(go);
            return go;
        }

        private static IEnumerable<Vector3> EnumerateLocalPoints(Transform pivot, MeshFilter mf, int maxSamples)
        {
            Mesh m = mf.sharedMesh;
            if (m == null) yield break;

            if (m.isReadable)
            {
                Vector3[] verts = m.vertices;
                int step = maxSamples > 0 && verts.Length > maxSamples * 2 ? verts.Length / maxSamples : 1;
                for (int v = 0; v < verts.Length; v += step)
                    yield return pivot.InverseTransformPoint(mf.transform.TransformPoint(verts[v]));
                yield break;
            }

            Bounds mb = m.bounds;
            Vector3 c = mb.center, e = mb.extents;
            for (int ix = -1; ix <= 1; ix += 2)
                for (int iy = -1; iy <= 1; iy += 2)
                    for (int iz = -1; iz <= 1; iz += 2)
                        yield return pivot.InverseTransformPoint(mf.transform.TransformPoint(c + new Vector3(ix * e.x, iy * e.y, iz * e.z)));
        }

        private Bounds CalcMeshLocalBounds(Transform pivot, Bounds fallback)
        {
            bool found = false;
            Bounds bounds = new Bounds(fallback.center, Vector3.zero);
            foreach (var mf in pivot.GetComponentsInChildren<MeshFilter>(false))
            {
                var r = mf.GetComponent<Renderer>();
                if (r == null || !r.enabled || mf.sharedMesh == null) continue;
                foreach (var lp in EnumerateLocalPoints(pivot, mf, 0))
                {
                    if (!found) { bounds = new Bounds(lp, Vector3.zero); found = true; }
                    else bounds.Encapsulate(lp);
                }
            }
            return found && bounds.size.sqrMagnitude > 0.0001f ? bounds : fallback;
        }

        private float SampleFrontSurfaceZ(Transform pivot, Vector3 center, Vector3 dir, Bounds bounds)
        {
            float bestZ = bounds.center.z - bounds.extents.z * 0.55f;
            float minZ = float.PositiveInfinity;
            float maxOffsetSq = Mathf.Pow(Mathf.Max(panelW, panelH) * 0.45f, 2f);

            foreach (var mf in pivot.GetComponentsInChildren<MeshFilter>(false))
            {
                var r = mf.GetComponent<Renderer>();
                if (r == null || !r.enabled || mf.sharedMesh == null) continue;
                foreach (var lp in EnumerateLocalPoints(pivot, mf, 400))
                {
                    float dx = lp.x - center.x;
                    float dy = lp.y - center.y;
                    if (dx * dx + dy * dy <= maxOffsetSq && lp.z < minZ)
                    {
                        minZ = lp.z;
                        bestZ = lp.z;
                    }
                }
            }

            if (minZ < float.PositiveInfinity)
                return Mathf.Clamp(bestZ, bounds.min.z - 0.01f, bounds.center.z - bounds.extents.z * 0.45f);

            return dir.z < 0f ? bounds.center.z - bounds.extents.z * 0.55f : bounds.center.z + bounds.extents.z * 0.55f;
        }

        private float SnapPointToMeshDepth(Vector2 xyLocal)
        {
            if (pivotRef == null || hatchRoot == null) return 0.004f;
            Vector3 probeLocal = pivotRef.InverseTransformPoint(hatchRoot.transform.TransformPoint(new Vector3(xyLocal.x, xyLocal.y, 0f)));
            float closestZ = 0f;
            float bestDistSq = float.PositiveInfinity;

            foreach (var mf in pivotRef.GetComponentsInChildren<MeshFilter>(false))
            {
                if (mf.transform.IsChildOf(hatchRoot.transform)) continue;
                var r = mf.GetComponent<Renderer>();
                if (r == null || !r.enabled || mf.sharedMesh == null) continue;

                foreach (var lp in EnumerateLocalPoints(pivotRef, mf, 300))
                {
                    float dx = lp.x - probeLocal.x;
                    float dy = lp.y - probeLocal.y;
                    float dSq = dx * dx + dy * dy;
                    if (dSq < bestDistSq)
                    {
                        bestDistSq = dSq;
                        closestZ = lp.z - probeLocal.z;
                    }
                }
            }

            return bestDistSq > 0.015f ? 0f : Mathf.Clamp(closestZ, -0.04f, 0.025f);
        }

        private void SpawnBolts()
        {
            float bx = panelW * 0.37f, by = panelH * 0.35f;
            Vector2[] corners = { new Vector2(-bx, by), new Vector2(bx, by), new Vector2(-bx, -by), new Vector2(bx, -by) };

            boltsRemaining = corners.Length;
            stepsRemaining = jaegerMode ? 4 : 3;
            StatusText = $"Болты: {boltsRemaining}";

            for (int i = 0; i < corners.Length; i++)
            {
                GameObject bolt = CreatePrimitive(PrimitiveType.Cylinder, $"Bolt_{i}", hatchRoot.transform,
                    new Vector3(corners[i].x, corners[i].y, 0.008f), new Vector3(boltScale, 0.012f, boltScale),
                    new Color(0.68f, 0.52f, 0.22f), false);
                bolt.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                bolt.AddComponent<SphereCollider>().radius = 0.85f;

                CreatePrimitive(PrimitiveType.Cylinder, "Washer", bolt.transform,
                    new Vector3(0f, -0.4f, 0f), new Vector3(1.4f, 0.35f, 1.4f),
                    new Color(0.22f, 0.25f, 0.30f), false);

                var clk = bolt.AddComponent<ClickableObject>();
                clk.promptText = "Болт — открутить";
                clk.onClick = () => UnscrewBolt(bolt, clk);
            }
        }

        private void UnscrewBolt(GameObject bolt, ClickableObject clk)
        {
            if (bolt == null) return;
            clk.isInteractable = false;
            boltsRemaining--;
            HangarAudio.Play(Sfx.Bolt);

            if (Application.isPlaying) StartCoroutine(AnimateBoltOut(bolt));
            else { spawned.Remove(bolt); ModelUtils.DestroySafe(bolt); }

            if (boltsRemaining <= 0)
            {
                HangarAudio.Play(Sfx.Success);
                if (Application.isPlaying) StartCoroutine(OpenCoverNextFrame());
                else BeginStep2();
            }
            else
            {
                StatusText = $"Болты: {boltsRemaining}";
            }
        }

        private IEnumerator AnimateBoltOut(GameObject bolt)
        {
            Vector3 startPos = bolt.transform.localPosition;
            Quaternion startRot = bolt.transform.localRotation;
            Vector3 endPos = startPos + new Vector3(0f, 0f, 0.06f);

            float t = 0f;
            while (t < 0.18f && bolt != null)
            {
                t += Time.deltaTime;
                float k = t / 0.18f;
                bolt.transform.localPosition = Vector3.Lerp(startPos, endPos, k);
                bolt.transform.localRotation = startRot * Quaternion.Euler(0f, k * 480f, 0f);
                yield return null;
            }

            spawned.Remove(bolt);
            ModelUtils.DestroySafe(bolt);
        }

        private IEnumerator OpenCoverNextFrame()
        {
            yield return new WaitForSeconds(0.12f);
            BeginStep2();
        }

        private void BeginStep2()
        {
            if (coverPlate != null)
            {
                spawned.Remove(coverPlate);
                ModelUtils.DestroySafe(coverPlate);
                coverPlate = null;
            }

            selectedObj = null;
            selectedId = -1;
            selectedSide = -1;

            if (jaegerMode) StartPlatesPhase();
            else StartWiresPhase();
        }

        private void StartWiresPhase()
        {
            leftPins.Clear();
            rightPins.Clear();
            stepsRemaining = 3;
            StatusText = "Провода: 3";

            float xSpan = panelW * 0.34f, yStep = panelH * 0.34f;
            for (int i = 0; i < 3; i++)
            {
                int id = i;
                float yL = yStep - i * yStep;
                float yR = yStep - ((i + 1) % 3) * yStep;

                Vector3 lPos = new Vector3(-xSpan, yL, SnapPointToMeshDepth(new Vector2(-xSpan, yL)) + 0.01f);
                Vector3 rPos = new Vector3(xSpan, yR, SnapPointToMeshDepth(new Vector2(xSpan, yR)) + 0.01f);

                leftPins[id] = lPos;
                rightPins[id] = rPos;
                CreatePin(lPos, ChannelColors[i], id, 0);
                CreatePin(rPos, ChannelColors[i], id, 1);
            }
        }

        private void CreatePin(Vector3 localPos, Color c, int colorId, int side)
        {
            GameObject pin = CreatePrimitive(PrimitiveType.Sphere, $"Pin_{colorId}_{side}", hatchRoot.transform, localPos, Vector3.one * pinScale, c, false, true, 0.45f);
            pin.AddComponent<SphereCollider>().radius = 0.85f;
            var clk = pin.AddComponent<ClickableObject>();
            clk.promptText = $"Контакт #{colorId + 1}";
            clk.onClick = () => OnPinClicked(pin, colorId, side, c);
        }

        private void OnPinClicked(GameObject pin, int colorId, int side, Color origColor)
        {
            if (selectedObj == null)
            {
                SelectPin(pin, colorId, side, origColor);
                StatusText = $"Контакт #{colorId + 1} — выберите пару";
                return;
            }
            if (selectedObj == pin) return;

            if (selectedSide == side)
            {
                var prevClk = selectedObj.GetComponent<ClickableObject>();
                if (prevClk != null) prevClk.disableHover = false;
                selectedObj.GetComponent<Renderer>().material = ModelUtils.CreateLitMaterial(selectedOrigColor, true, 0.45f);
                selectedObj.transform.localScale = Vector3.one * pinScale;
                SelectPin(pin, colorId, side, origColor);
                return;
            }

            if (selectedId != colorId)
            {
                HangarAudio.Play(Sfx.Error);
                StatusText = "Неверный контакт";
                return;
            }

            Vector3 pL = leftPins[colorId], pR = rightPins[colorId];
            GameObject wire = CreatePrimitive(PrimitiveType.Cylinder, $"Wire_{colorId}", hatchRoot.transform,
                (pL + pR) * 0.5f + new Vector3(0f, 0f, 0.008f),
                new Vector3(pinScale * 0.45f, Vector3.Distance(pL, pR) * 0.5f, pinScale * 0.45f),
                origColor, false, true, 0.5f);
            wire.transform.localRotation = Quaternion.LookRotation(pR - pL) * Quaternion.Euler(90f, 0f, 0f);

            DisablePin(selectedObj, origColor);
            DisablePin(pin, origColor);

            selectedObj = null;
            selectedId = -1;
            selectedSide = -1;

            stepsRemaining--;
            HangarAudio.Play(Sfx.Wire);
            StatusText = stepsRemaining <= 0 ? "Готово — жмите ГОТОВО или Пробел" : $"Провода: {stepsRemaining}";
            if (stepsRemaining <= 0) HangarAudio.Play(Sfx.Success);
        }

        private static void DisablePin(GameObject pin, Color color)
        {
            var clk = pin.GetComponent<ClickableObject>();
            if (clk != null) { clk.disableHover = true; clk.isInteractable = false; }
            pin.GetComponent<Renderer>().material = ModelUtils.CreateLitMaterial(color);
        }

        private void SelectPin(GameObject pin, int colorId, int side, Color origColor)
        {
            selectedObj = pin;
            selectedId = colorId;
            selectedSide = side;
            selectedOrigColor = origColor;

            var clk = pin.GetComponent<ClickableObject>();
            if (clk != null) clk.disableHover = true;
            pin.GetComponent<Renderer>().material = ModelUtils.CreateLitMaterial(Color.white, true, 0.7f);
            pin.transform.localScale = Vector3.one * (pinScale * 1.28f);
            HangarAudio.Play(Sfx.Click);
        }

        private void StartPlatesPhase()
        {
            stepsRemaining = 4;
            StatusText = "Платы: 4";

            float sx = panelW * 0.28f, sy = panelH * 0.22f;
            Vector3[] slotCoords =
            {
                new Vector3(-sx,  sy, SnapPointToMeshDepth(new Vector2(-sx,  sy)) + 0.008f),
                new Vector3( sx,  sy, SnapPointToMeshDepth(new Vector2( sx,  sy)) + 0.008f),
                new Vector3(-sx, -sy * 0.4f, SnapPointToMeshDepth(new Vector2(-sx, -sy * 0.4f)) + 0.008f),
                new Vector3( sx, -sy * 0.4f, SnapPointToMeshDepth(new Vector2( sx, -sy * 0.4f)) + 0.008f)
            };
            int[] plateOrder = { 2, 0, 3, 1 };

            for (int i = 0; i < 4; i++)
            {
                int slotId = i;
                Color slotCol = ChannelColors[slotId];
                GameObject slot = CreatePrimitive(PrimitiveType.Cube, $"Plate_Slot_{i}", hatchRoot.transform,
                    slotCoords[i], new Vector3(panelW * 0.34f, panelH * 0.26f, 0.01f), slotCol * 0.55f, true, true, 0.25f);
                var slotClk = slot.AddComponent<ClickableObject>();
                slotClk.promptText = $"Разъем #{i + 1}";
                Vector3 targetPos = slotCoords[i] + new Vector3(0f, 0f, 0.012f);
                slotClk.onClick = () => OnPlateSlotClicked(slot, slotClk, slotId, targetPos, slotCol);
            }

            float traySpacing = Mathf.Max(0.055f, panelW * 0.30f);
            float trayY = -panelH * 0.58f;
            for (int i = 0; i < 4; i++)
            {
                int plateId = plateOrder[i];
                Color pCol = ChannelColors[plateId];
                Vector3 trayPos = new Vector3((i - 1.5f) * traySpacing, trayY, 0.016f);

                GameObject plateObj = CreatePlateVisual(plateId, pCol);
                plateObj.transform.SetParent(hatchRoot.transform, false);
                plateObj.transform.localPosition = trayPos;
                spawned.Add(plateObj);

                var clk = plateObj.AddComponent<ClickableObject>();
                clk.promptText = $"Плата #{plateId + 1}";
                clk.onClick = () => OnPlateSelected(plateObj, plateId, pCol, trayPos);
            }
        }

        private GameObject CreatePlateVisual(int colorId, Color c)
        {
            var holder = new GameObject($"Board_Plate_{colorId}");
            if (platePrefab != null)
            {
                var orient = new GameObject("Orient");
                orient.transform.SetParent(holder.transform, false);
                GameObject mesh = Instantiate(platePrefab, orient.transform, false);

                Renderer primary = PickPrimaryRenderer(mesh);
                if (primary != null)
                {
                    foreach (var r in mesh.GetComponentsInChildren<Renderer>(true))
                        if (r != primary && r.bounds.size.magnitude < primary.bounds.size.magnitude * 0.25f)
                            r.enabled = false;

                    Bounds b = primary.bounds;
                    if (b.size.sqrMagnitude > 0.0000001f)
                    {
                        mesh.transform.localScale *= Mathf.Clamp(panelW * 0.30f, 0.048f, 0.08f) / b.size.magnitude;
                        mesh.transform.position += orient.transform.position - primary.bounds.center;
                    }
                }
                orient.transform.localRotation = Quaternion.Euler(72f, 0f, 0f);
                foreach (var r in mesh.GetComponentsInChildren<Renderer>(true))
                    if (r.enabled) r.material = ModelUtils.CreateLitMaterial(c, true, 0.35f);
            }
            else
            {
                CreatePrimitive(PrimitiveType.Cube, "Box", holder.transform, Vector3.zero, new Vector3(0.055f, 0.036f, 0.012f), c, false, true, 0.35f);
            }

            holder.AddComponent<BoxCollider>().size = new Vector3(0.065f, 0.048f, 0.028f);
            return holder;
        }

        private void OnPlateSelected(GameObject plateObj, int colorId, Color c, Vector3 restPos)
        {
            if (selectedObj != null && selectedObj != plateObj)
            {
                var prev = selectedObj.GetComponent<ClickableObject>();
                if (prev != null) prev.disableHover = false;
                selectedObj.transform.localPosition = selectedBasePos;
            }

            selectedObj = plateObj;
            selectedId = colorId;
            selectedOrigColor = c;
            selectedBasePos = restPos;

            var clk = plateObj.GetComponent<ClickableObject>();
            if (clk != null) clk.disableHover = true;
            plateObj.transform.localPosition = restPos + new Vector3(0f, 0f, 0.02f);
            HangarAudio.Play(Sfx.Click);
            StatusText = $"Плата #{colorId + 1} — выберите разъем";
        }

        private void OnPlateSlotClicked(GameObject slotObj, ClickableObject slotClk, int slotId, Vector3 targetPos, Color slotColor)
        {
            if (selectedObj == null)
            {
                HangarAudio.Play(Sfx.Click);
                StatusText = "Сначала выберите плату";
                return;
            }
            if (selectedId != slotId)
            {
                HangarAudio.Play(Sfx.Error);
                StatusText = "Неверный разъем";
                return;
            }

            GameObject inserted = selectedObj;
            var pClk = inserted.GetComponent<ClickableObject>();
            if (pClk != null) { pClk.disableHover = true; pClk.isInteractable = false; }
            slotClk.disableHover = true;
            slotClk.isInteractable = false;

            selectedObj = null;
            selectedId = -1;
            slotObj.GetComponent<Renderer>().material = ModelUtils.CreateLitMaterial(slotColor, true, 0.6f);

            if (Application.isPlaying) StartCoroutine(AnimatePlateInsert(inserted, targetPos));
            else inserted.transform.localPosition = targetPos;

            stepsRemaining--;
            HangarAudio.Play(Sfx.Plate);
            StatusText = stepsRemaining <= 0 ? "Готово — жмите ГОТОВО или Пробел" : $"Платы: {stepsRemaining}";
            if (stepsRemaining <= 0) HangarAudio.Play(Sfx.Success);
        }

        private static IEnumerator AnimatePlateInsert(GameObject plate, Vector3 endPos)
        {
            Vector3 startPos = plate.transform.localPosition;
            float t = 0f;
            while (t < 0.2f && plate != null)
            {
                t += Time.deltaTime;
                plate.transform.localPosition = Vector3.Lerp(startPos, endPos, t / 0.2f);
                yield return null;
            }
            if (plate != null) plate.transform.localPosition = endPos;
        }

        public void Clear()
        {
            StopAllCoroutines();
            foreach (var go in spawned) ModelUtils.DestroySafe(go);
            spawned.Clear();
            leftPins.Clear();
            rightPins.Clear();
            pivotRef = null;
            hatchRoot = null;
            coverPlate = null;
            selectedObj = null;
            selectedId = -1;
            selectedSide = -1;
            boltsRemaining = 0;
            stepsRemaining = 0;
            StatusText = "";
        }

        private static Renderer PickPrimaryRenderer(GameObject root)
        {
            Renderer best = null;
            float bestMag = -1f;
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null) continue;
                float mag = r.bounds.size.magnitude;
                if (mag > bestMag) { bestMag = mag; best = r; }
            }
            return best;
        }
    }
}
