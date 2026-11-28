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

        [Header("Размеры сервисной панели")]
        [SerializeField] private Vector3 panelSize = new Vector3(0.36f, 0.26f, 0.018f);
        [SerializeField] private Vector3 boltScale = new Vector3(0.036f, 0.022f, 0.036f);
        [SerializeField] private float boltColliderRadius = 0.7f;
        [SerializeField] private Vector3 pinScale = new Vector3(0.036f, 0.036f, 0.036f);
        [SerializeField] private float pinColliderRadius = 0.7f;

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

        private GameObject hatchRoot;
        private GameObject coverPlate;
        private bool isJaegerMode;
        private int boltsRemaining;
        private int stepsRemaining;

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

            isJaegerMode = jaegerPlatesMode;

            hatchRoot = new GameObject("Service_Bay");
            hatchRoot.transform.SetParent(modelPivot, false);

            Vector3 dir = camLocalDir.sqrMagnitude > 0.001f ? camLocalDir.normalized : Vector3.back;
            float surfaceDist = Mathf.Clamp(localModelBounds.extents.magnitude * 0.56f, 0.16f, 0.28f);
            hatchRoot.transform.localPosition = dir * surfaceDist;
            hatchRoot.transform.localRotation = Quaternion.LookRotation(dir, Vector3.up);
            hatchRoot.transform.localScale = Vector3.one;
            spawnedObjects.Add(hatchRoot);

            BuildBackingFrame();
            StartBoltsPhase();
        }

        private void BuildBackingFrame()
        {
            coverPlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            coverPlate.name = "Service_Cover";
            coverPlate.transform.SetParent(hatchRoot.transform, false);
            coverPlate.transform.localPosition = Vector3.zero;
            coverPlate.transform.localScale = panelSize;
            SafeDestroy(coverPlate.GetComponent<Collider>());
            coverPlate.GetComponent<Renderer>().material = CreateMat(new Color(0.14f, 0.16f, 0.2f), false);
            spawnedObjects.Add(coverPlate);
        }

        private void StartBoltsPhase()
        {
            Vector3[] corners =
            {
                new Vector3(-0.13f,  0.085f, 0.016f),
                new Vector3( 0.13f,  0.085f, 0.016f),
                new Vector3(-0.13f, -0.085f, 0.016f),
                new Vector3( 0.13f, -0.085f, 0.016f)
            };

            boltsRemaining = corners.Length;
            stepsRemaining = isJaegerMode ? 4 : 3;
            StatusText = $"Шаг 1/2: Открутите {boltsRemaining} крепежных болта";

            for (int i = 0; i < corners.Length; i++)
            {
                GameObject bolt = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                bolt.name = $"Bolt_{i}";
                bolt.transform.SetParent(hatchRoot.transform, false);
                bolt.transform.localPosition = corners[i];
                bolt.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                bolt.transform.localScale = boltScale;
                bolt.GetComponent<Renderer>().material = CreateMat(new Color(0.95f, 0.78f, 0.15f), true, 0.35f);

                GameObject washer = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                washer.name = "Washer";
                washer.transform.SetParent(bolt.transform, false);
                washer.transform.localPosition = new Vector3(0f, -0.45f, 0f);
                washer.transform.localScale = new Vector3(1.4f, 0.35f, 1.4f);
                washer.GetComponent<Renderer>().material = CreateMat(new Color(0.25f, 0.28f, 0.32f), false);
                SafeDestroy(washer.GetComponent<Collider>());

                SafeDestroy(bolt.GetComponent<Collider>());
                var col = bolt.AddComponent<SphereCollider>();
                col.radius = boltColliderRadius;

                var clk = bolt.AddComponent<ClickableObject>();
                clk.promptText = "Крепежный болт (Клик: открутить)";
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
                StatusText = $"Шаг 1/2: Осталось открутить {boltsRemaining} болта";
            }
        }

        private IEnumerator AnimateBoltOut(GameObject bolt)
        {
            Vector3 startPos = bolt.transform.localPosition;
            Quaternion startRot = bolt.transform.localRotation;
            Vector3 endPos = startPos + new Vector3(0f, 0f, 0.08f);

            float t = 0f;
            const float dur = 0.2f;
            while (t < dur && bolt != null)
            {
                t += Time.deltaTime;
                float k = t / dur;
                bolt.transform.localPosition = Vector3.Lerp(startPos, endPos, k);
                bolt.transform.localRotation = startRot * Quaternion.Euler(0f, k * 540f, 0f);
                yield return null;
            }

            spawnedObjects.Remove(bolt);
            SafeDestroy(bolt);
        }

        private IEnumerator OpenCoverAndBeginStep2()
        {
            yield return new WaitForSeconds(0.14f);

            if (coverPlate != null)
            {
                Vector3 startPos = coverPlate.transform.localPosition;
                Vector3 endPos = startPos + new Vector3(0f, -0.04f, -0.01f);
                Color darkInterior = new Color(0.06f, 0.08f, 0.11f);
                coverPlate.GetComponent<Renderer>().material = CreateMat(darkInterior, false);
                coverPlate.transform.localPosition = endPos;
                coverPlate.transform.localScale = new Vector3(panelSize.x, panelSize.y, 0.008f);
            }

            BeginStep2Immediate();
        }

        private void BeginStep2Immediate()
        {
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
            StatusText = "Шаг 2/2: Соедините 3 пары контактов одного цвета";

            for (int i = 0; i < 3; i++)
            {
                int colorId = i;
                float yL = 0.075f - i * 0.075f;
                int shuffledRightIdx = (i + 1) % 3;
                float yR = 0.075f - shuffledRightIdx * 0.075f;

                Vector3 lPos = new Vector3(-0.115f, yL, 0.02f);
                Vector3 rPos = new Vector3( 0.115f, yR, 0.02f);

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
            pin.transform.localScale = pinScale;
            pin.GetComponent<Renderer>().material = CreateMat(c, true, 1.2f);

            SafeDestroy(pin.GetComponent<Collider>());
            var col = pin.AddComponent<SphereCollider>();
            col.radius = pinColliderRadius;

            string sideName = side == 0 ? "Входной" : "Выходной";
            var clk = pin.AddComponent<ClickableObject>();
            clk.promptText = $"{sideName} контакт #{colorId + 1} (Клик: соединить)";
            clk.onClick = () => OnPinClicked(pin, colorId, side, c);
            spawnedObjects.Add(pin);
        }

        private void OnPinClicked(GameObject pin, int colorId, int side, Color origColor)
        {
            if (selectedObject == null)
            {
                SelectPin(pin, colorId, side, origColor);
                StatusText = $"Выбран контакт #{colorId + 1}. Найдите парный контакт такого же цвета.";
                return;
            }

            if (selectedObject == pin) return;

            if (selectedSide == side)
            {
                var prevClk = selectedObject.GetComponent<ClickableObject>();
                if (prevClk != null) prevClk.disableHover = false;
                selectedObject.GetComponent<Renderer>().material = CreateMat(selectedOrigColor, true, 1.2f);
                selectedObject.transform.localScale = pinScale;

                SelectPin(pin, colorId, side, origColor);
                StatusText = $"Переключено на контакт #{colorId + 1}.";
                return;
            }

            if (selectedId != colorId)
            {
                HangarAudio.Instance?.PlayError();
                StatusText = "Короткое замыкание! Цвета контактов не совпадают.";
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

            selectedObject.transform.localScale = pinScale;
            selectedObject.GetComponent<Renderer>().material = CreateMat(origColor, false);
            pin.GetComponent<Renderer>().material = CreateMat(origColor, false);

            selectedObject = null;
            selectedId = -1;
            selectedSide = -1;

            stepsRemaining--;
            HangarAudio.Instance?.PlayWire();

            if (stepsRemaining <= 0)
            {
                HangarAudio.Instance?.PlaySuccess();
                StatusText = "Цепи восстановлены! Нажмите ГОТОВО (3D кнопка) или [SPACE].";
            }
            else
            {
                StatusText = $"Цепь соединена! Осталось подключить: {stepsRemaining}";
            }
        }

        private void SelectPin(GameObject pin, int colorId, int side, Color origColor)
        {
            selectedObject = pin;
            selectedId = colorId;
            selectedSide = side;
            selectedOrigColor = origColor;

            var clk = pin.GetComponent<ClickableObject>();
            if (clk != null) clk.disableHover = true;

            pin.GetComponent<Renderer>().material = CreateMat(Color.white, true, 1.6f);
            pin.transform.localScale = pinScale * 1.32f;
            HangarAudio.Instance?.PlayClick();
        }

        private void CreateWireBridge(int colorId, Color c)
        {
            Vector3 pL = leftPins[colorId];
            Vector3 pR = rightPins[colorId];
            Vector3 mid = (pL + pR) * 0.5f + new Vector3(0f, 0f, 0.01f);

            GameObject wire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wire.name = $"Wire_Bridge_{colorId}";
            wire.transform.SetParent(hatchRoot.transform, false);
            wire.transform.localPosition = mid;
            wire.transform.localRotation = Quaternion.LookRotation(pR - pL) * Quaternion.Euler(90f, 0f, 0f);

            float dist = Vector3.Distance(pL, pR);
            wire.transform.localScale = new Vector3(0.016f, dist * 0.5f, 0.016f);
            wire.GetComponent<Renderer>().material = CreateMat(c, true, 1.3f);
            SafeDestroy(wire.GetComponent<Collider>());
            spawnedObjects.Add(wire);
        }

        private void StartPlatesPhase()
        {
            stepsRemaining = 4;
            StatusText = "Шаг 2/2: Выберите и вставьте 4 платы в разъемы своего цвета";

            Vector3[] slotCoords =
            {
                new Vector3(-0.095f,  0.048f, 0.012f),
                new Vector3( 0.095f,  0.048f, 0.012f),
                new Vector3(-0.095f, -0.025f, 0.012f),
                new Vector3( 0.095f, -0.025f, 0.012f)
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
                slotObj.transform.localScale = new Vector3(0.095f, 0.055f, 0.012f);
                slotObj.GetComponent<Renderer>().material = CreateMat(slotCol * 0.65f, true, 0.6f);

                var slotClk = slotObj.AddComponent<ClickableObject>();
                slotClk.promptText = $"Слот #{i + 1} — Клик: вставить выбранную плату";
                Vector3 targetSlotPos = slotCoords[i] + new Vector3(0f, 0f, 0.016f);
                slotClk.onClick = () => OnPlateSlotClicked(slotObj, slotClk, slotColorId, targetSlotPos, slotCol);
                spawnedObjects.Add(slotObj);
            }

            for (int i = 0; i < 4; i++)
            {
                int plateColorId = plateOrder[i];
                Color pCol = channelColors[plateColorId];
                Vector3 trayPos = new Vector3(-0.12f + i * 0.08f, -0.115f, 0.028f);

                GameObject plateObj = CreatePlateVisual(plateColorId, pCol);
                plateObj.transform.SetParent(hatchRoot.transform, false);
                plateObj.transform.localPosition = trayPos;

                var clk = plateObj.AddComponent<ClickableObject>();
                clk.promptText = $"Плата #{plateColorId + 1} (Клик: выбрать для установки)";
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
                        float normScale = 0.09f / b.size.magnitude;
                        mesh.transform.localScale *= normScale;
                        b = primary.bounds;
                        mesh.transform.position += orientPivot.transform.position - b.center;
                    }
                }

                orientPivot.transform.localRotation = Quaternion.Euler(72f, 0f, 0f);

                foreach (var r in mesh.GetComponentsInChildren<Renderer>(true))
                {
                    if (r.enabled) r.material = CreateMat(c, true, 0.85f);
                }
            }
            else
            {
                GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                box.transform.SetParent(holder.transform, false);
                box.transform.localScale = new Vector3(0.068f, 0.045f, 0.014f);
                SafeDestroy(box.GetComponent<Collider>());
                box.GetComponent<Renderer>().material = CreateMat(c, true, 0.8f);
            }

            var col = holder.AddComponent<BoxCollider>();
            col.size = new Vector3(0.08f, 0.055f, 0.03f);
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
            plateObj.transform.localPosition = restTrayPos + new Vector3(0f, 0f, 0.025f);
            HangarAudio.Instance?.PlayClick();
            StatusText = $"Выбрана плата #{colorId + 1}. Кликните по разъему того же цвета.";
        }

        private void OnPlateSlotClicked(GameObject slotObj, ClickableObject slotClk, int slotColorId, Vector3 targetPos, Color slotColor)
        {
            if (selectedObject == null)
            {
                HangarAudio.Instance?.PlayClick();
                StatusText = "Сначала выберите нужную плату снизу.";
                return;
            }

            if (selectedId != slotColorId)
            {
                HangarAudio.Instance?.PlayError();
                StatusText = "Цвет платы не совпадает с разъемом! Подберите правильную плату.";
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

            slotObj.GetComponent<Renderer>().material = CreateMat(slotColor, true, 1.4f);

            if (Application.isPlaying)
                StartCoroutine(AnimatePlateInsert(insertedPlate, targetPos));
            else
                insertedPlate.transform.localPosition = targetPos;

            stepsRemaining--;
            HangarAudio.Instance?.PlayPlate();

            if (stepsRemaining <= 0)
            {
                HangarAudio.Instance?.PlaySuccess();
                StatusText = "Все 4 платы установлены! Нажмите ГОТОВО (3D кнопка) или [SPACE].";
            }
            else
            {
                StatusText = $"Плата установлена! Осталось вставить плат: {stepsRemaining}";
            }
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

        private Material CreateMat(Color c, bool emissive, float emissionMult = 1.2f)
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
