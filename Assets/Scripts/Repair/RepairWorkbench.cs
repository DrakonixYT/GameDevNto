using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HangarGame.Core;
using HangarGame.Mechs;
using HangarGame.Interaction;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HangarGame.Repair
{
    public class RepairWorkbench : MonoBehaviour
    {
        public static RepairWorkbench Instance { get; private set; }
        public MechPart CurrentPart { get; private set; }
        public GameObject CurrentJaegerModule { get; private set; }
        public string CurrentItemName => (CurrentPart != null && CurrentPart.Data != null) ? CurrentPart.Data.partName : (currentJaegerName ?? "Модуль");
        public bool HasItem => CurrentPart != null || CurrentJaegerModule != null;
        public bool IsJaegerModule => CurrentJaegerModule != null;
        public string RepairStepInfo { get; private set; } = "";

        [Header("Workbench Placement")]
        [SerializeField] private Vector3 tableRestOffset = new Vector3(0f, 0.85f, 0f);
        [SerializeField] private Vector3 initialPartEuler = new Vector3(0f, 270f, 0f);

        [Header("Service Hatch")]
        [SerializeField] private Vector3 hatchLocalOffset = new Vector3(0f, 0f, -0.48f);
        [SerializeField] private Vector3 hatchLocalRotation = new Vector3(0f, 180f, 0f);
        [SerializeField] private Vector3 hatchSize = new Vector3(0.42f, 0.32f, 0.04f);

        [Header("Bolts Config")]
        [SerializeField] private Vector3 boltScale = new Vector3(0.08f, 0.025f, 0.08f);
        [SerializeField] private float boltColliderRadius = 2.5f;
        [SerializeField] private Vector3[] boltOffsets = new Vector3[]
        {
            new Vector3(-0.16f, 0.12f, 0.035f),
            new Vector3(0.16f, 0.12f, 0.035f),
            new Vector3(-0.16f, -0.12f, 0.035f),
            new Vector3(0.16f, -0.12f, 0.035f)
        };

        [Header("Wires Config")]
        [SerializeField] private Vector3 pinScale = new Vector3(0.08f, 0.08f, 0.08f);
        [SerializeField] private float pinColliderRadius = 2.5f;
        [SerializeField] private Vector3[] leftPinOffsets = new Vector3[]
        {
            new Vector3(-0.13f, 0.09f, 0.035f),
            new Vector3(-0.13f, 0.0f, 0.035f),
            new Vector3(-0.13f, -0.09f, 0.035f)
        };
        [SerializeField] private Vector3[] rightPinOffsets = new Vector3[]
        {
            new Vector3(0.13f, -0.09f, 0.035f),
            new Vector3(0.13f, 0.09f, 0.035f),
            new Vector3(0.13f, 0.0f, 0.035f)
        };
        [SerializeField] private Color[] wireColors = new Color[]
        {
            new Color(1f, 0.25f, 0.25f),
            new Color(0.2f, 0.9f, 1f),
            new Color(1f, 0.88f, 0.15f)
        };

        [Header("Controls")]
        [SerializeField] private float rotationSpeed = 90f;
        [SerializeField] private float mouseSensitivity = 0.4f;
        [SerializeField] private float minPitch = -70f;
        [SerializeField] private float maxPitch = 70f;

        private float targetYaw = 180f, targetPitch = 0f, curYaw = 180f, curPitch = 0f;
        private string currentJaegerName;
        private Action onJaegerDone;
        private readonly List<GameObject> activeSubObjects = new List<GameObject>();
        private GameObject serviceHatchObj = null;

        private int boltsRemaining = 0;
        private int wiresRemaining = 0;

        private GameObject selectedPinObj = null;
        private int selectedColorId = -1;
        private int selectedSide = -1;
        private Color selectedOriginalColor = Color.white;

        private readonly Dictionary<int, Vector3> leftPinPositions = new Dictionary<int, Vector3>();
        private readonly Dictionary<int, Vector3> rightPinPositions = new Dictionary<int, Vector3>();

        private void Awake() => Instance = this;

        private void Update()
        {
            if (!HasItem) return;

            bool esc = false, space = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                esc = Keyboard.current.escapeKey.wasPressedThisFrame;
                space = Keyboard.current.spaceKey.wasPressedThisFrame;
            }
#else
            esc = Input.GetKeyDown(KeyCode.Escape); space = Input.GetKeyDown(KeyCode.Space);
#endif
            if (esc) CancelWork();
            else if (space && boltsRemaining <= 0 && wiresRemaining <= 0) FinishWork();

            float h = 0f, v = 0f;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed || Keyboard.current.qKey.isPressed) h -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed || Keyboard.current.eKey.isPressed) h += 1f;
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) v += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) v -= 1f;
            }
            if (Mouse.current != null && (Mouse.current.rightButton.isPressed || Mouse.current.middleButton.isPressed))
            {
                Vector2 d = Mouse.current.delta.ReadValue();
                targetYaw += d.x * mouseSensitivity; targetPitch = Mathf.Clamp(targetPitch - d.y * mouseSensitivity, minPitch, maxPitch);
            }
#else
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.Q)) h -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.E)) h += 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v -= 1f;
            if (Input.GetMouseButton(1) || Input.GetMouseButton(2))
            {
                targetYaw += Input.GetAxis("Mouse X") * (mouseSensitivity * 10f); targetPitch = Mathf.Clamp(targetPitch - Input.GetAxis("Mouse Y") * (mouseSensitivity * 10f), minPitch, maxPitch);
            }
#endif
            targetYaw += h * rotationSpeed * Time.deltaTime;
            targetPitch = Mathf.Clamp(targetPitch - v * rotationSpeed * Time.deltaTime, minPitch, maxPitch);
            curYaw = Mathf.Lerp(curYaw, targetYaw, Time.deltaTime * 12f);
            curPitch = Mathf.Lerp(curPitch, targetPitch, Time.deltaTime * 12f);

            Transform t = CurrentPart != null ? CurrentPart.transform : (CurrentJaegerModule != null ? CurrentJaegerModule.transform : null);
            if (t != null) t.rotation = Quaternion.Euler(curPitch, curYaw, 0f);
        }

        public void RotateYaw(float d) => targetYaw += d;
        public void RotatePitch(float d) => targetPitch = Mathf.Clamp(targetPitch + d, minPitch, maxPitch);
        public void ResetRotation() { targetYaw = initialPartEuler.y; targetPitch = initialPartEuler.x; }

        public void CycleModule()
        {
            if (CurrentPart == null || CurrentPart.Data == null) return;
            CurrentPart.Data = ModuleCatalog.GetNextModule(CurrentPart.Data);
            CurrentPart.name = CurrentPart.Data.partName;
            HangarAudioLight.Instance?.PlayBolt();
        }

        public void ReceivePart(MechPart p)
        {
            if (HasItem) return;
            p.OnHover(false);
            CurrentPart = p;
            p.IsOnWorkbench = true;

            foreach (var col in p.GetComponentsInChildren<Collider>(true)) col.enabled = false;

            ResetRotation();
            GameManager.Instance?.SwitchView(RoomView.WorkbenchFocus);
            Vector3 tablePos = transform.TransformPoint(tableRestOffset);
            StartCoroutine(SlideToWorkbench(p.transform, tablePos, Quaternion.Euler(initialPartEuler), StartBoltsPhase));
        }

        public void ReceiveJaegerModule(GameObject mod, string name, Action onDone)
        {
            if (HasItem) return;
            CurrentJaegerModule = mod; currentJaegerName = name; onJaegerDone = onDone;

            foreach (var col in mod.GetComponentsInChildren<Collider>(true)) col.enabled = false;

            ResetRotation();
            GameManager.Instance?.SwitchView(RoomView.WorkbenchFocus);
            Vector3 tablePos = transform.TransformPoint(tableRestOffset);
            mod.transform.position = tablePos + new Vector3(0f, 0.5f, 0f);
            StartCoroutine(SlideToWorkbench(mod.transform, tablePos, Quaternion.Euler(initialPartEuler), StartBoltsPhase));
        }

        private IEnumerator SlideToWorkbench(Transform t, Vector3 targetPos, Quaternion targetRot, Action onDone)
        {
            Vector3 sP = t.position; Quaternion sR = t.rotation;
            float el = 0f, d = 0.28f;
            while (el < d && t != null)
            {
                el += Time.deltaTime; float p = Mathf.SmoothStep(0f, 1f, el / d);
                t.position = Vector3.Lerp(sP, targetPos, p); t.rotation = Quaternion.Slerp(sR, targetRot, p);
                yield return null;
            }
            if (t != null) { t.position = targetPos; t.rotation = targetRot; }
            onDone?.Invoke();
        }

        private void StartBoltsPhase()
        {
            ClearSubObjects();
            Transform root = CurrentPart != null ? CurrentPart.transform : (CurrentJaegerModule != null ? CurrentJaegerModule.transform : null);
            if (root == null) return;

            Vector3[] offsets = (boltOffsets != null && boltOffsets.Length > 0) ? boltOffsets : new Vector3[] {
                new Vector3(-0.48f, 0.36f, 0.08f),
                new Vector3(0.48f, 0.36f, 0.08f),
                new Vector3(-0.48f, -0.36f, 0.08f),
                new Vector3(0.48f, -0.36f, 0.08f)
            };

            boltsRemaining = offsets.Length;
            RepairStepInfo = $"Шаг 1/2: Открутите {boltsRemaining} золотых болта на сервисной панели";

            CreateServiceHatchBase(root);

            for (int i = 0; i < offsets.Length; i++)
            {
                GameObject bolt = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                bolt.name = $"Bolt_{i}";
                bolt.transform.SetParent(serviceHatchObj.transform, false);
                bolt.transform.localPosition = offsets[i];
                bolt.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                bolt.transform.localScale = boltScale;

                var r = bolt.GetComponent<Renderer>();
                r.material = CreateMaterial(new Color(1f, 0.85f, 0.1f), true);

                GameObject washer = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                washer.name = "Washer";
                washer.transform.SetParent(bolt.transform, false);
                washer.transform.localPosition = new Vector3(0f, -0.4f, 0f);
                washer.transform.localScale = new Vector3(1.35f, 0.4f, 1.35f);
                washer.GetComponent<Renderer>().material = CreateMaterial(new Color(0.25f, 0.28f, 0.32f), false);
                var wCol = washer.GetComponent<Collider>();
                if (wCol != null) Destroy(wCol);

                var capCol = bolt.GetComponent<Collider>();
                if (capCol != null) DestroyImmediate(capCol);
                var sphCol = bolt.AddComponent<SphereCollider>();
                sphCol.radius = boltColliderRadius;
                sphCol.center = new Vector3(0f, 0.5f, 0f);

                ClickableObject clk = bolt.AddComponent<ClickableObject>();
                clk.promptText = "🔧 Крепёжный болт (Клик: открутить)";
                clk.onClick = () => OnBoltUnscrewed(bolt);
                activeSubObjects.Add(bolt);
            }
        }

        private void CreateServiceHatchBase(Transform root)
        {
            serviceHatchObj = new GameObject("Service_Hatch");
            serviceHatchObj.transform.SetParent(root, false);

            Vector3 inv = new Vector3(1f / Mathf.Max(0.1f, root.lossyScale.x), 1f / Mathf.Max(0.1f, root.lossyScale.y), 1f / Mathf.Max(0.1f, root.lossyScale.z));
            serviceHatchObj.transform.localScale = inv;
            serviceHatchObj.transform.localPosition = hatchLocalOffset;
            serviceHatchObj.transform.localRotation = Quaternion.Euler(hatchLocalRotation);

            GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plate.name = "Hatch_Backing";
            plate.transform.SetParent(serviceHatchObj.transform, false);
            plate.transform.localPosition = Vector3.zero;
            plate.transform.localScale = hatchSize;
            var col = plate.GetComponent<Collider>();
            if (col != null) Destroy(col);

            plate.GetComponent<Renderer>().material = CreateMaterial(new Color(0.15f, 0.17f, 0.20f), false);
            activeSubObjects.Add(plate);
            activeSubObjects.Add(serviceHatchObj);
        }

        private void OnBoltUnscrewed(GameObject bolt)
        {
            boltsRemaining--;
            HangarAudioLight.Instance?.PlayBolt();
            activeSubObjects.Remove(bolt);
            Destroy(bolt);

            if (boltsRemaining <= 0)
            {
                HangarAudioLight.Instance?.PlaySuccess();
                StartWiresPhase();
            }
            else
            {
                RepairStepInfo = $"Шаг 1/2: Осталось открутить {boltsRemaining} болта";
            }
        }

        private void StartWiresPhase()
        {
            for (int i = activeSubObjects.Count - 1; i >= 0; i--)
            {
                if (activeSubObjects[i] != null && activeSubObjects[i] != serviceHatchObj)
                {
                    Destroy(activeSubObjects[i]);
                    activeSubObjects.RemoveAt(i);
                }
            }

            Transform root = CurrentPart != null ? CurrentPart.transform : (CurrentJaegerModule != null ? CurrentJaegerModule.transform : null);
            if (root == null || serviceHatchObj == null) return;

            Color[] colors = (wireColors != null && wireColors.Length > 0) ? wireColors : new Color[] {
                new Color(1f, 0.25f, 0.25f),
                new Color(0.2f, 0.9f, 1f),
                new Color(1f, 0.88f, 0.15f)
            };

            int count = colors.Length;
            wiresRemaining = count;
            selectedPinObj = null;
            selectedColorId = -1;
            selectedSide = -1;
            leftPinPositions.Clear();
            rightPinPositions.Clear();

            RepairStepInfo = $"Шаг 2/2: Соедините {count} пары клемм одинакового цвета";

            for (int i = 0; i < count; i++)
            {
                int colorId = i;
                Vector3 lPos = (leftPinOffsets != null && i < leftPinOffsets.Length) ? leftPinOffsets[i] : new Vector3(-0.38f, 0.26f - i * 0.26f, 0.08f);
                Vector3 rPos = (rightPinOffsets != null && i < rightPinOffsets.Length) ? rightPinOffsets[i] : new Vector3(0.38f, -0.26f + i * 0.26f, 0.08f);

                leftPinPositions[colorId] = lPos;
                rightPinPositions[colorId] = rPos;

                CreateWirePin(serviceHatchObj.transform, lPos, colors[i], colorId, 0);
                CreateWirePin(serviceHatchObj.transform, rPos, colors[i], colorId, 1);
            }
        }

        private void CreateWirePin(Transform parent, Vector3 localPos, Color c, int colorId, int side)
        {
            GameObject pin = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pin.name = $"Pin_{colorId}_{side}";
            pin.transform.SetParent(parent, false);
            pin.transform.localPosition = localPos;
            pin.transform.localScale = pinScale;

            var r = pin.GetComponent<Renderer>();
            r.material = CreateMaterial(c, true);

            var col = pin.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);
            var sphCol = pin.AddComponent<SphereCollider>();
            sphCol.radius = pinColliderRadius;

            string sideName = side == 0 ? "Входной" : "Выходной";
            ClickableObject clk = pin.AddComponent<ClickableObject>();
            clk.promptText = $"⚡ {sideName} контакт #{colorId + 1} (Клик: подключить)";
            clk.onClick = () => OnPinClicked(pin, colorId, side, c);

            activeSubObjects.Add(pin);
        }

        private void OnPinClicked(GameObject pin, int colorId, int side, Color origColor)
        {
            if (selectedPinObj == null)
            {
                selectedPinObj = pin;
                selectedColorId = colorId;
                selectedSide = side;
                selectedOriginalColor = origColor;

                pin.GetComponent<Renderer>().material = CreateMaterial(Color.white, true);
                pin.transform.localScale = pinScale * 1.35f;
                HangarAudioLight.Instance?.PlayClick();
                RepairStepInfo = $"Выбран контакт #{colorId + 1}. Теперь кликните парный контакт того же цвета!";
            }
            else
            {
                if (selectedPinObj == pin) return;

                if (selectedSide != side)
                {
                    if (selectedColorId == colorId)
                    {
                        CreateWireBridge(colorId, origColor);

                        var clk1 = selectedPinObj.GetComponent<ClickableObject>();
                        if (clk1 != null) Destroy(clk1);
                        var clk2 = pin.GetComponent<ClickableObject>();
                        if (clk2 != null) Destroy(clk2);

                        selectedPinObj.transform.localScale = pinScale;
                        selectedPinObj.GetComponent<Renderer>().material = CreateMaterial(origColor, false);
                        pin.GetComponent<Renderer>().material = CreateMaterial(origColor, false);

                        selectedPinObj = null;
                        selectedColorId = -1;
                        selectedSide = -1;

                        wiresRemaining--;
                        HangarAudioLight.Instance?.PlayBolt();

                        if (wiresRemaining <= 0)
                        {
                            HangarAudioLight.Instance?.PlaySuccess();
                            RepairStepInfo = "✔ Все цепи восстановлены! Нажмите [SPACE] для завершения.";
                        }
                        else
                        {
                            RepairStepInfo = $"✔ Цепь замкнута! Осталось соединить: {wiresRemaining}";
                        }
                    }
                    else
                    {
                        HangarAudioLight.Instance?.PlayClick();
                        RepairStepInfo = "❌ Короткое замыкание! Цвета не совпадают. Попробуйте снова.";
                    }
                }
                else
                {
                    selectedPinObj.GetComponent<Renderer>().material = CreateMaterial(selectedOriginalColor, true);
                    selectedPinObj.transform.localScale = pinScale;

                    selectedPinObj = pin;
                    selectedColorId = colorId;
                    selectedSide = side;
                    selectedOriginalColor = origColor;

                    pin.GetComponent<Renderer>().material = CreateMaterial(Color.white, true);
                    pin.transform.localScale = pinScale * 1.35f;
                    HangarAudioLight.Instance?.PlayClick();
                    RepairStepInfo = $"Переключено на контакт #{colorId + 1}. Найдите парный контакт того же цвета!";
                }
            }
        }

        private void CreateWireBridge(int colorId, Color c)
        {
            if (serviceHatchObj == null || !leftPinPositions.ContainsKey(colorId) || !rightPinPositions.ContainsKey(colorId)) return;

            Vector3 pL = leftPinPositions[colorId];
            Vector3 pR = rightPinPositions[colorId];
            Vector3 mid = (pL + pR) * 0.5f + new Vector3(0f, 0f, 0.02f);

            GameObject wire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wire.name = $"Wire_Bridge_{colorId}";
            wire.transform.SetParent(serviceHatchObj.transform, false);
            wire.transform.localPosition = mid;
            wire.transform.localRotation = Quaternion.LookRotation(pR - pL) * Quaternion.Euler(90f, 0f, 0f);

            float dist = Vector3.Distance(pL, pR);
            wire.transform.localScale = new Vector3(0.03f, dist * 0.5f, 0.03f);
            wire.GetComponent<Renderer>().material = CreateMaterial(c, true);

            var col = wire.GetComponent<Collider>();
            if (col != null) Destroy(col);

            activeSubObjects.Add(wire);
        }

        public void FinishWork()
        {
            ClearSubObjects();
            if (CurrentJaegerModule != null)
            {
                foreach (var col in CurrentJaegerModule.GetComponentsInChildren<Collider>()) col.enabled = true;
                Destroy(CurrentJaegerModule); CurrentJaegerModule = null;
                onJaegerDone?.Invoke(); HangarAudioLight.Instance?.PlaySuccess();
                GameManager.Instance?.SwitchView(RoomView.Hangar);
            }
            else if (CurrentPart != null)
            {
                foreach (var col in CurrentPart.GetComponentsInChildren<Collider>()) col.enabled = true;

                if (CurrentPart.Data != null)
                    GameManager.Instance?.TrySpend(CurrentPart.Data.repairCredits, CurrentPart.Data.repairAlloy);
                CurrentPart.IsOnWorkbench = false;
                CurrentPart.RepairFull();
                CurrentPart.ReturnToSocket();
                CurrentPart = null;
                HangarAudioLight.Instance?.PlaySuccess();
                HangarMechManager.Instance?.UpdatePodiumDisplay();
                GameManager.Instance?.SwitchView(RoomView.Hangar);
            }
        }

        public void CancelWork()
        {
            ClearSubObjects();
            if (CurrentJaegerModule != null)
            {
                foreach (var col in CurrentJaegerModule.GetComponentsInChildren<Collider>()) col.enabled = true;
                Destroy(CurrentJaegerModule); CurrentJaegerModule = null;
            }
            if (CurrentPart != null)
            {
                foreach (var col in CurrentPart.GetComponentsInChildren<Collider>()) col.enabled = true;
                CurrentPart.IsOnWorkbench = false;
                CurrentPart.ReturnToSocket(); CurrentPart = null;
            }
            HangarMechManager.Instance?.UpdatePodiumDisplay();
            GameManager.Instance?.SwitchView(RoomView.Hangar);
        }

        private void ClearSubObjects()
        {
            foreach (var o in activeSubObjects) if (o != null) Destroy(o);
            activeSubObjects.Clear();
            serviceHatchObj = null;
            selectedPinObj = null;
            selectedColorId = -1;
            selectedSide = -1;
        }

        private Material CreateMaterial(Color color, bool emissive = false)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                      ?? Shader.Find("Standard")
                      ?? Shader.Find("Unlit/Color");
            var mat = new Material(shader);
            mat.color = color;
            if (emissive)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * 1.5f);
            }
            return mat;
        }
    }
}
