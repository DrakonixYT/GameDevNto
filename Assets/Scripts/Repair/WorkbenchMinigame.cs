using System.Collections;
using System.Collections.Generic;
using HangarGame.Core;
using HangarGame.Interaction;
using UnityEngine;

namespace HangarGame.Repair
{
    public class WorkbenchMinigame : MonoBehaviour
    {
        [Header("Префаб платы для Егеря")]
        [SerializeField] private GameObject platePrefab;

        private readonly Color[] channelColors =
        {
            new Color(0.95f, 0.25f, 0.25f),
            new Color(0.15f, 0.85f, 1.0f),
            new Color(1.0f, 0.82f, 0.15f),
            new Color(0.3f, 0.95f, 0.45f)
        };

        private readonly List<GameObject> spawnedObjects = new List<GameObject>();
        private readonly Dictionary<int, Vector3> leftPins = new Dictionary<int, Vector3>();
        private readonly Dictionary<int, Vector3> rightPins = new Dictionary<int, Vector3>();

        private Transform pivotRef;
        private GameObject hatchRoot;
        private GameObject coverPlate;
        private bool isJaegerMode;
        private int boltsRemaining;
        private int stepsRemaining;

        private float panelWidth = 0.16f;
        private float panelHeight = 0.13f;
        private float boltScaleFactor = 0.022f;
        private float pinRadiusScale = 0.022f;

        private GameObject selectedObject;
        private int selectedId = -1;
        private int selectedSide = -1;
        private Color selectedOrigColor = Color.white;
        private Vector3 selectedBasePos;

        public bool IsComplete => boltsRemaining <= 0 && stepsRemaining <= 0;
        public string StatusText { get; private set; } = "";

        public void SetPlatePrefab(GameObject prefab) => platePrefab = prefab;

        public void Begin(Transform modelPivot, Bounds localModelBounds, Vector3 camLocalDir, bool jaegerPlatesMode)
        {
            Clear();
            if (modelPivot == null) return;

            pivotRef = modelPivot;
            isJaegerMode = jaegerPlatesMode;

            Vector3 dir = camLocalDir.sqrMagnitude > 0.001f ? camLocalDir.normalized : Vector3.back;
            Bounds meshBounds = CalcMeshLocalBounds(modelPivot, localModelBounds);

            panelWidth = Mathf.Clamp(meshBounds.size.x * 0.62f, 0.11f, 0.25f);
            panelHeight = Mathf.Clamp(meshBounds.size.y * 0.50f, 0.10f, 0.19f);
            float minDim = Mathf.Min(panelWidth, panelHeight);
            boltScaleFactor = Mathf.Clamp(minDim * 0.16f, 0.016f, 0.028f);
            pinRadiusScale = Mathf.Clamp(minDim * 0.18f, 0.018f, 0.030f);

            float surfaceDepth = SampleFrontSurfaceZ(modelPivot, meshBounds.center, dir, meshBounds);

            hatchRoot = new GameObject("Service_Bay");
            hatchRoot.transform.SetParent(modelPivot, false);
            hatchRoot.transform.localPosition = new Vector3(meshBounds.center.x, meshBounds.center.y, surfaceDepth) + dir * 0.008f;
            hatchRoot.transform.localRotation = Quaternion.LookRotation(dir, Vector3.up);
            hatchRoot.transform.localScale = Vector3.one;
            spawnedObjects.Add(hatchRoot);

            BuildCompactArmorHatch();
            StartBoltsPhase();
        }

        private Bounds CalcMeshLocalBounds(Transform modelPivot, Bounds fallback)
        {
            bool hasAny = false;
            Bounds result = new Bounds(fallback.center, Vector3.zero);
            var filters = modelPivot.GetComponentsInChildren<MeshFilter>(false);
            for (int i = 0; i < filters.Length; i++)
            {
                var r = filters[i].GetComponent<Renderer>();
                if (r == null || !r.enabled || filters[i].sharedMesh == null) continue;
                var verts = filters[i].sharedMesh.vertices;
                for (int v = 0; v < verts.Length; v++)
                {
                    Vector3 lp = modelPivot.InverseTransformPoint(filters[i].transform.TransformPoint(verts[v]));
                    if (!hasAny)
                    {
                        result = new Bounds(lp, Vector3.zero);
                        hasAny = true;
                    }
                    else
                    {
                        result.Encapsulate(lp);
                    }
                }
            }
            return hasAny && result.size.sqrMagnitude > 0.0001f ? result : fallback;
        }

        private float SampleFrontSurfaceZ(Transform modelPivot, Vector3 localCenter, Vector3 dir, Bounds meshBounds)
        {
            float bestZ = meshBounds.center.z - meshBounds.extents.z * 0.55f;
            float bestFront = float.PositiveInfinity;
            float maxOffsetSq = Mathf.Pow(Mathf.Max(panelWidth, panelHeight) * 0.45f, 2f);

            var filters = modelPivot.GetComponentsInChildren<MeshFilter>(false);
            for (int i = 0; i < filters.Length; i++)
            {
                var r = filters[i].GetComponent<Renderer>();
                if (r == null || !r.enabled || filters[i].sharedMesh == null) continue;
                var verts = filters[i].sharedMesh.vertices;
                int step = verts.Length > 800 ? verts.Length / 400 : 1;
                for (int v = 0; v < verts.Length; v += step)
                {
                    Vector3 lp = modelPivot.InverseTransformPoint(filters[i].transform.TransformPoint(verts[v]));
                    float dx = lp.x - localCenter.x;
                    float dy = lp.y - localCenter.y;
                    if (dx * dx + dy * dy > maxOffsetSq) continue;
                    if (lp.z < bestFront)
                    {
                        bestFront = lp.z;
                        bestZ = lp.z;
                    }
                }
            }

            if (bestFront < float.PositiveInfinity)
                return Mathf.Clamp(bestZ, meshBounds.min.z - 0.01f, meshBounds.center.z);

            return dir.z < 0f ? (meshBounds.center.z - meshBounds.extents.z * 0.5f) : (meshBounds.center.z + meshBounds.extents.z * 0.5f);
        }

        private float SnapPointToMeshDepth(Vector2 xyLocal)
        {
            if (pivotRef == null || hatchRoot == null) return 0.004f;
            Vector3 probePivot = hatchRoot.transform.TransformPoint(new Vector3(xyLocal.x, xyLocal.y, 0f));
            Vector3 probeLocal = pivotRef.InverseTransformPoint(probePivot);

            float closestZ = 0f;
            float bestDistSq = float.PositiveInfinity;

            var filters = pivotRef.GetComponentsInChildren<MeshFilter>(false);
            for (int i = 0; i < filters.Length; i++)
            {
                if (filters[i].transform.IsChildOf(hatchRoot.transform)) continue;
                var r = filters[i].GetComponent<Renderer>();
                if (r == null || !r.enabled || filters[i].sharedMesh == null) continue;
                var verts = filters[i].sharedMesh.vertices;
                int step = verts.Length > 600 ? verts.Length / 300 : 1;
                for (int v = 0; v < verts.Length; v += step)
                {
                    Vector3 lp = pivotRef.InverseTransformPoint(filters[i].transform.TransformPoint(verts[v]));
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

            if (bestDistSq > 0.015f) return 0f;
            return Mathf.Clamp(closestZ, -0.04f, 0.025f);
        }

        private void BuildCompactArmorHatch()
        {
            coverPlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            coverPlate.name = "Service_Cover";
            coverPlate.transform.SetParent(hatchRoot.transform, false);
            coverPlate.transform.localPosition = new Vector3(0f, 0f, -0.003f);
            coverPlate.transform.localScale = new Vector3(panelWidth, panelHeight, 0.008f);
            SafeDestroy(coverPlate.GetComponent<Collider>());
            coverPlate.GetComponent<Renderer>().material = CreateMat(new Color(0.18f, 0.21f, 0.26f), false);
            spawnedObjects.Add(coverPlate);
        }

        private void StartBoltsPhase()
        {
            float bx = panelWidth * 0.37f;
            float by = panelHeight * 0.35f;
            Vector2[] corners2D =
            {
                new Vector2(-bx,  by),
                new Vector2( bx,  by),
                new Vector2(-bx, -by),
                new Vector2( bx, -by)
            };

            boltsRemaining = corners2D.Length;
            stepsRemaining = isJaegerMode ? 4 : 3;
            StatusText = $"Болты: {boltsRemaining}";

            for (int i = 0; i < corners2D.Length; i++)
            {
                Vector3 boltPos = new Vector3(corners2D[i].x, corners2D[i].y, 0.008f);

                GameObject bolt = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                bolt.name = $"Bolt_{i}";
                bolt.transform.SetParent(hatchRoot.transform, false);
                bolt.transform.localPosition = boltPos;
                bolt.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                bolt.transform.localScale = new Vector3(boltScaleFactor, 0.012f, boltScaleFactor);
                bolt.GetComponent<Renderer>().material = CreateMat(new Color(0.92f, 0.72f, 0.16f), true, 0.3f);

                GameObject washer = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                washer.name = "Washer";
                washer.transform.SetParent(bolt.transform, false);
                washer.transform.localPosition = new Vector3(0f, -0.4f, 0f);
                washer.transform.localScale = new Vector3(1.45f, 0.35f, 1.45f);
                washer.GetComponent<Renderer>().material = CreateMat(new Color(0.24f, 0.27f, 0.32f), false);
                SafeDestroy(washer.GetComponent<Collider>());

                SafeDestroy(bolt.GetComponent<Collider>());
                var col = bolt.AddComponent<SphereCollider>();
                col.radius = 0.85f;

                var clk = bolt.AddComponent<ClickableObject>();
                clk.promptText = "Болт — открутить";
                clk.onClick = () => UnscrewBolt(bolt, clk);
                spawnedObjects.Add(bolt);
            }
        }

        private void UnscrewBolt(GameObject bolt, ClickableObject clk)
        {
            if (bolt == null) return;
            clk.isInteractable = false;
            boltsRemaining--;
            HangarAudio.Instance?.PlayBolt();

            if (Application.isPlaying)
                StartCoroutine(AnimateBoltOut(bolt));
            else
            {
                spawnedObjects.Remove(bolt);
                SafeDestroy(bolt);
            }

            if (boltsRemaining <= 0)
            {
                HangarAudio.Instance?.PlaySuccess();
                if (Application.isPlaying)
                    StartCoroutine(OpenCoverAndBeginStep2());
                else
                    BeginStep2Immediate();
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
            const float dur = 0.18f;
            while (t < dur && bolt != null)
            {
                t += Time.deltaTime;
                float k = t / dur;
                bolt.transform.localPosition = Vector3.Lerp(startPos, endPos, k);
                bolt.transform.localRotation = startRot * Quaternion.Euler(0f, k * 480f, 0f);
                yield return null;
            }

            spawnedObjects.Remove(bolt);
            SafeDestroy(bolt);
        }

        private IEnumerator OpenCoverAndBeginStep2()
        {
            yield return new WaitForSeconds(0.12f);

            if (coverPlate != null)
            {
                spawnedObjects.Remove(coverPlate);
                SafeDestroy(coverPlate);
                coverPlate = null;
            }

            BeginStep2Immediate();
        }

        private void BeginStep2Immediate()
        {
            if (coverPlate != null)
            {
                spawnedObjects.Remove(coverPlate);
                SafeDestroy(coverPlate);
                coverPlate = null;
            }

            selectedObject = null;
            selectedId = -1;
            selectedSide = -1;

            if (isJaegerMode)
                StartPlatesPhase();
            else
                StartWiresPhase();
        }

        private void StartWiresPhase()
        {
            leftPins.Clear();
            rightPins.Clear();
            stepsRemaining = 3;
            StatusText = "Провода: 3";

            float xSpan = panelWidth * 0.34f;
            float yStep = panelHeight * 0.34f;

            for (int i = 0; i < 3; i++)
            {
                int colorId = i;
                float yL = yStep - i * yStep;
                int shuffledRightIdx = (i + 1) % 3;
                float yR = yStep - shuffledRightIdx * yStep;

                float zL = SnapPointToMeshDepth(new Vector2(-xSpan, yL)) + 0.01f;
                float zR = SnapPointToMeshDepth(new Vector2(xSpan, yR)) + 0.01f;

                Vector3 lPos = new Vector3(-xSpan, yL, zL);
                Vector3 rPos = new Vector3(xSpan, yR, zR);

                leftPins[colorId] = lPos;
                rightPins[colorId] = rPos;

                CreatePin(lPos, channelColors[i], colorId, 0);
                CreatePin(rPos, channelColors[i], colorId, 1);
            }
        }

        private void CreatePin(Vector3 localPos, Color c, int colorId, int side)
        {
            GameObject pin = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pin.name = $"Pin_{colorId}_{side}";
            pin.transform.SetParent(hatchRoot.transform, false);
            pin.transform.localPosition = localPos;
            pin.transform.localScale = Vector3.one * pinRadiusScale;
            pin.GetComponent<Renderer>().material = CreateMat(c, true, 1.0f);

            SafeDestroy(pin.GetComponent<Collider>());
            var col = pin.AddComponent<SphereCollider>();
            col.radius = 0.85f;

            var clk = pin.AddComponent<ClickableObject>();
            clk.promptText = $"Контакт #{colorId + 1}";
            clk.onClick = () => OnPinClicked(pin, colorId, side, c);
            spawnedObjects.Add(pin);
        }

        private void OnPinClicked(GameObject pin, int colorId, int side, Color origColor)
        {
            if (selectedObject == null)
            {
                SelectPin(pin, colorId, side, origColor);
                StatusText = $"Контакт #{colorId + 1} — выберите пару";
                return;
            }

            if (selectedObject == pin) return;

            if (selectedSide == side)
            {
                var prevClk = selectedObject.GetComponent<ClickableObject>();
                if (prevClk != null) prevClk.disableHover = false;
                selectedObject.GetComponent<Renderer>().material = CreateMat(selectedOrigColor, true, 1.0f);
                selectedObject.transform.localScale = Vector3.one * pinRadiusScale;

                SelectPin(pin, colorId, side, origColor);
                return;
            }

            if (selectedId != colorId)
            {
                HangarAudio.Instance?.PlayError();
                StatusText = "Неверный контакт";
                return;
            }

            CreateWireBridge(colorId, origColor);

            var clk1 = selectedObject.GetComponent<ClickableObject>();
            if (clk1 != null)
            {
                clk1.disableHover = true;
                clk1.isInteractable = false;
            }
            var clk2 = pin.GetComponent<ClickableObject>();
            if (clk2 != null)
            {
                clk2.disableHover = true;
                clk2.isInteractable = false;
            }

            selectedObject.transform.localScale = Vector3.one * pinRadiusScale;
            selectedObject.GetComponent<Renderer>().material = CreateMat(origColor, false);
            pin.GetComponent<Renderer>().material = CreateMat(origColor, false);

            selectedObject = null;
            selectedId = -1;
            selectedSide = -1;

            stepsRemaining--;
            HangarAudio.Instance?.PlayWire();

            StatusText = stepsRemaining <= 0
                ? "Готово — жмите ГОТОВО или Пробел"
                : $"Провода: {stepsRemaining}";
            if (stepsRemaining <= 0) HangarAudio.Instance?.PlaySuccess();
        }

        private void SelectPin(GameObject pin, int colorId, int side, Color origColor)
        {
            selectedObject = pin;
            selectedId = colorId;
            selectedSide = side;
            selectedOrigColor = origColor;

            var clk = pin.GetComponent<ClickableObject>();
            if (clk != null) clk.disableHover = true;

            pin.GetComponent<Renderer>().material = CreateMat(Color.white, true, 1.4f);
            pin.transform.localScale = Vector3.one * (pinRadiusScale * 1.3f);
            HangarAudio.Instance?.PlayClick();
        }

        private void CreateWireBridge(int colorId, Color c)
        {
            Vector3 pL = leftPins[colorId];
            Vector3 pR = rightPins[colorId];
            Vector3 mid = (pL + pR) * 0.5f + new Vector3(0f, 0f, 0.008f);

            GameObject wire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wire.name = $"Wire_Bridge_{colorId}";
            wire.transform.SetParent(hatchRoot.transform, false);
            wire.transform.localPosition = mid;
            wire.transform.localRotation = Quaternion.LookRotation(pR - pL) * Quaternion.Euler(90f, 0f, 0f);

            float dist = Vector3.Distance(pL, pR);
            wire.transform.localScale = new Vector3(pinRadiusScale * 0.48f, dist * 0.5f, pinRadiusScale * 0.48f);
            wire.GetComponent<Renderer>().material = CreateMat(c, true, 1.0f);
            SafeDestroy(wire.GetComponent<Collider>());
            spawnedObjects.Add(wire);
        }

        private void StartPlatesPhase()
        {
            stepsRemaining = 4;
            StatusText = "Платы: 4";

            float sx = panelWidth * 0.28f;
            float sy = panelHeight * 0.22f;
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
                int slotColorId = i;
                Color slotCol = channelColors[slotColorId];

                GameObject slotObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slotObj.name = $"Plate_Slot_{i}";
                slotObj.transform.SetParent(hatchRoot.transform, false);
                slotObj.transform.localPosition = slotCoords[i];
                slotObj.transform.localScale = new Vector3(panelWidth * 0.34f, panelHeight * 0.26f, 0.01f);
                slotObj.GetComponent<Renderer>().material = CreateMat(slotCol * 0.65f, true, 0.5f);

                var slotClk = slotObj.AddComponent<ClickableObject>();
                slotClk.promptText = $"Разъем #{i + 1}";
                Vector3 targetSlotPos = slotCoords[i] + new Vector3(0f, 0f, 0.012f);
                slotClk.onClick = () => OnPlateSlotClicked(slotObj, slotClk, slotColorId, targetSlotPos, slotCol);
                spawnedObjects.Add(slotObj);
            }

            float traySpacing = Mathf.Max(0.055f, panelWidth * 0.30f);
            float trayY = -panelHeight * 0.58f;
            for (int i = 0; i < 4; i++)
            {
                int plateColorId = plateOrder[i];
                Color pCol = channelColors[plateColorId];
                Vector3 trayPos = new Vector3((i - 1.5f) * traySpacing, trayY, 0.016f);

                GameObject plateObj = CreatePlateVisual(plateColorId, pCol);
                plateObj.transform.SetParent(hatchRoot.transform, false);
                plateObj.transform.localPosition = trayPos;

                var clk = plateObj.AddComponent<ClickableObject>();
                clk.promptText = $"Плата #{plateColorId + 1}";
                clk.onClick = () => OnPlateSelected(plateObj, plateColorId, pCol, trayPos);
                spawnedObjects.Add(plateObj);
            }
        }

        private GameObject CreatePlateVisual(int colorId, Color c)
        {
            GameObject holder = new GameObject($"Board_Plate_{colorId}");

            if (platePrefab != null)
            {
                GameObject orientPivot = new GameObject("Plate_Orient");
                orientPivot.transform.SetParent(holder.transform, false);

                GameObject mesh = Instantiate(platePrefab, orientPivot.transform, false);
                mesh.transform.localPosition = Vector3.zero;

                Renderer primary = PickPrimaryRenderer(mesh);
                if (primary != null)
                {
                    foreach (var r in mesh.GetComponentsInChildren<Renderer>(true))
                    {
                        if (r != primary && r.bounds.size.magnitude < primary.bounds.size.magnitude * 0.25f)
                            r.enabled = false;
                    }

                    Bounds b = primary.bounds;
                    if (b.size.sqrMagnitude > 0.0000001f)
                    {
                        float normScale = Mathf.Clamp(panelWidth * 0.30f, 0.048f, 0.08f) / b.size.magnitude;
                        mesh.transform.localScale *= normScale;
                        b = primary.bounds;
                        mesh.transform.position += orientPivot.transform.position - b.center;
                    }
                }

                orientPivot.transform.localRotation = Quaternion.Euler(72f, 0f, 0f);

                foreach (var r in mesh.GetComponentsInChildren<Renderer>(true))
                {
                    if (r.enabled) r.material = CreateMat(c, true, 0.75f);
                }
            }
            else
            {
                GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                box.transform.SetParent(holder.transform, false);
                box.transform.localScale = new Vector3(0.055f, 0.036f, 0.012f);
                SafeDestroy(box.GetComponent<Collider>());
                box.GetComponent<Renderer>().material = CreateMat(c, true, 0.7f);
            }

            var col = holder.AddComponent<BoxCollider>();
            col.size = new Vector3(0.065f, 0.048f, 0.028f);
            return holder;
        }

        private void OnPlateSelected(GameObject plateObj, int colorId, Color c, Vector3 restTrayPos)
        {
            if (selectedObject != null && selectedObject != plateObj)
            {
                var prevClk = selectedObject.GetComponent<ClickableObject>();
                if (prevClk != null) prevClk.disableHover = false;
                selectedObject.transform.localPosition = selectedBasePos;
            }

            selectedObject = plateObj;
            selectedId = colorId;
            selectedSide = 0;
            selectedOrigColor = c;
            selectedBasePos = restTrayPos;

            var clk = plateObj.GetComponent<ClickableObject>();
            if (clk != null) clk.disableHover = true;
            plateObj.transform.localPosition = restTrayPos + new Vector3(0f, 0f, 0.02f);
            HangarAudio.Instance?.PlayClick();
            StatusText = $"Плата #{colorId + 1} — выберите разъем";
        }

        private void OnPlateSlotClicked(GameObject slotObj, ClickableObject slotClk, int slotColorId, Vector3 targetPos, Color slotColor)
        {
            if (selectedObject == null)
            {
                HangarAudio.Instance?.PlayClick();
                StatusText = "Сначала выберите плату";
                return;
            }

            if (selectedId != slotColorId)
            {
                HangarAudio.Instance?.PlayError();
                StatusText = "Неверный разъем";
                return;
            }

            GameObject insertedPlate = selectedObject;
            var pClk = insertedPlate.GetComponent<ClickableObject>();
            if (pClk != null)
            {
                pClk.disableHover = true;
                pClk.isInteractable = false;
            }
            slotClk.disableHover = true;
            slotClk.isInteractable = false;

            selectedObject = null;
            selectedId = -1;

            slotObj.GetComponent<Renderer>().material = CreateMat(slotColor, true, 1.1f);

            if (Application.isPlaying)
                StartCoroutine(AnimatePlateInsert(insertedPlate, targetPos));
            else
                insertedPlate.transform.localPosition = targetPos;

            stepsRemaining--;
            HangarAudio.Instance?.PlayPlate();

            StatusText = stepsRemaining <= 0
                ? "Готово — жмите ГОТОВО или Пробел"
                : $"Платы: {stepsRemaining}";
            if (stepsRemaining <= 0) HangarAudio.Instance?.PlaySuccess();
        }

        private IEnumerator AnimatePlateInsert(GameObject plate, Vector3 endPos)
        {
            Vector3 startPos = plate.transform.localPosition;
            float t = 0f;
            const float dur = 0.2f;
            while (t < dur && plate != null)
            {
                t += Time.deltaTime;
                plate.transform.localPosition = Vector3.Lerp(startPos, endPos, Mathf.SmoothStep(0f, 1f, t / dur));
                yield return null;
            }
            if (plate != null) plate.transform.localPosition = endPos;
        }

        public void Clear()
        {
            StopAllCoroutines();
            for (int i = 0; i < spawnedObjects.Count; i++)
            {
                if (spawnedObjects[i] != null) SafeDestroy(spawnedObjects[i]);
            }
            spawnedObjects.Clear();
            leftPins.Clear();
            rightPins.Clear();
            pivotRef = null;
            hatchRoot = null;
            coverPlate = null;
            selectedObject = null;
            selectedId = -1;
            selectedSide = -1;
            boltsRemaining = 0;
            stepsRemaining = 0;
            StatusText = "";
        }

        private Renderer PickPrimaryRenderer(GameObject root)
        {
            var rends = root.GetComponentsInChildren<Renderer>(true);
            Renderer best = null;
            float bestMag = -1f;
            for (int i = 0; i < rends.Length; i++)
            {
                if (rends[i] == null) continue;
                float mag = rends[i].bounds.size.magnitude;
                if (mag > bestMag)
                {
                    bestMag = mag;
                    best = rends[i];
                }
            }
            return best;
        }

        private void SafeDestroy(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }

        private Material CreateMat(Color c, bool emissive, float emissionMult = 1.0f)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            var m = new Material(shader);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            m.color = c;
            if (emissive)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", c * emissionMult);
            }
            return m;
        }
    }
}
