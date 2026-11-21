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
                targetYaw += d.x * 0.4f; targetPitch = Mathf.Clamp(targetPitch - d.y * 0.4f, -70f, 70f);
            }
#else
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.Q)) h -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.E)) h += 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v -= 1f;
            if (Input.GetMouseButton(1) || Input.GetMouseButton(2))
            {
                targetYaw += Input.GetAxis("Mouse X") * 4f; targetPitch = Mathf.Clamp(targetPitch - Input.GetAxis("Mouse Y") * 4f, -70f, 70f);
            }
#endif
            targetYaw += h * 90f * Time.deltaTime;
            targetPitch = Mathf.Clamp(targetPitch - v * 90f * Time.deltaTime, -70f, 70f);
            curYaw = Mathf.Lerp(curYaw, targetYaw, Time.deltaTime * 12f);
            curPitch = Mathf.Lerp(curPitch, targetPitch, Time.deltaTime * 12f);

            Transform t = CurrentPart != null ? CurrentPart.transform : (CurrentJaegerModule != null ? CurrentJaegerModule.transform : null);
            if (t != null) t.rotation = Quaternion.Euler(curPitch, curYaw, 0f);
        }

        public void RotateYaw(float d) => targetYaw += d;
        public void RotatePitch(float d) => targetPitch = Mathf.Clamp(targetPitch + d, -70f, 70f);
        public void ResetRotation() { targetYaw = 180f; targetPitch = 0f; }

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
            CurrentPart = p; p.IsOnWorkbench = true;

            foreach (var col in p.GetComponentsInChildren<Collider>()) col.enabled = false;

            ResetRotation();
            GameManager.Instance?.SwitchView(RoomView.WorkbenchFocus);
            StartCoroutine(SlideToWorkbench(p.transform, new Vector3(0f, 1.25f, 2.3f), Quaternion.Euler(0f, 180f, 0f), StartBoltsPhase));
        }

        public void ReceiveJaegerModule(GameObject mod, string name, Action onDone)
        {
            if (HasItem) return;
            CurrentJaegerModule = mod; currentJaegerName = name; onJaegerDone = onDone;

            foreach (var col in mod.GetComponentsInChildren<Collider>()) col.enabled = false;

            ResetRotation();
            GameManager.Instance?.SwitchView(RoomView.WorkbenchFocus);
            mod.transform.position = new Vector3(0f, 2.2f, 2.3f);
            StartCoroutine(SlideToWorkbench(mod.transform, new Vector3(0f, 1.25f, 2.3f), Quaternion.Euler(0f, 180f, 0f), StartBoltsPhase));
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

            boltsRemaining = 4;
            RepairStepInfo = "Шаг 1/2: Открутите 4 золотых болта на передней панели детали";

            CreateServiceHatchBase(root);

            Vector3[] boltLocalOffsets = {
                new Vector3(-0.48f, 0.36f, 0.08f),
                new Vector3(0.48f, 0.36f, 0.08f),
                new Vector3(-0.48f, -0.36f, 0.08f),
                new Vector3(0.48f, -0.36f, 0.08f)
            };

            for (int i = 0; i < 4; i++)
            {
                GameObject bolt = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                bolt.name = $"Bolt_{i}";
                bolt.transform.SetParent(serviceHatchObj.transform, false);
                bolt.transform.localPosition = boltLocalOffsets[i];
                bolt.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                bolt.transform.localScale = new Vector3(0.24f, 0.07f, 0.24f);

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
                if (capCol != null) Destroy(capCol);
                var sphCol = bolt.AddComponent<SphereCollider>();
                sphCol.radius = 0.55f;

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

            float forwardZ = 0.65f * Mathf.Max(root.lossyScale.z, 1.0f);
            serviceHatchObj.transform.localPosition = new Vector3(0f, 0f, -forwardZ * inv.z);

            GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plate.name = "Hatch_Backplate";
            plate.transform.SetParent(serviceHatchObj.transform, false);
            plate.transform.localPosition = Vector3.zero;
            plate.transform.localScale = new Vector3(1.25f, 0.95f, 0.06f);
            plate.GetComponent<Renderer>().material = CreateMaterial(new Color(0.12f, 0.15f, 0.2f), false);

            var pCol = plate.GetComponent<Collider>();
            if (pCol != null) Destroy(pCol);

            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "Hatch_Border";
            frame.transform.SetParent(serviceHatchObj.transform, false);
            frame.transform.localPosition = new Vector3(0f, 0f, 0.02f);
            frame.transform.localScale = new Vector3(1.3f, 1.0f, 0.03f);
            frame.GetComponent<Renderer>().material = CreateMaterial(new Color(0.15f, 0.65f, 0.85f), true);
            var fCol = frame.GetComponent<Collider>();
            if (fCol != null) Destroy(fCol);

            activeSubObjects.Add(serviceHatchObj);
        }

        private void OnBoltUnscrewed(GameObject b)
        {
            HangarAudioLight.Instance?.PlayBolt();
            activeSubObjects.Remove(b);
            Destroy(b);
            boltsRemaining--;
            if (boltsRemaining <= 0)
            {
                HangarAudioLight.Instance?.PlaySuccess();
                StartWiresPhase();
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

            wiresRemaining = 3;
            selectedPinObj = null;
            selectedColorId = -1;
            selectedSide = -1;
            leftPinPositions.Clear();
            rightPinPositions.Clear();

            RepairStepInfo = "Шаг 2/2: Соедините 3 пары клемм одинакового цвета";

            Color[] colors = {
                new Color(1f, 0.25f, 0.25f),
                new Color(0.2f, 0.9f, 1f),
                new Color(1f, 0.88f, 0.15f)
            };

            Vector3[] leftOffsets = {
                new Vector3(-0.38f, 0.26f, 0.08f),
                new Vector3(-0.38f, 0.0f, 0.08f),
                new Vector3(-0.38f, -0.26f, 0.08f)
            };

            Vector3[] rightOffsets = {
                new Vector3(0.38f, -0.26f, 0.08f),
                new Vector3(0.38f, 0.26f, 0.08f),
                new Vector3(0.38f, 0.0f, 0.08f)
            };

            for (int i = 0; i < 3; i++)
            {
                int colorId = i;
                leftPinPositions[colorId] = leftOffsets[i];
                rightPinPositions[colorId] = rightOffsets[i];

                CreateWirePin(serviceHatchObj.transform, leftOffsets[i], colors[i], colorId, 0);
                CreateWirePin(serviceHatchObj.transform, rightOffsets[i], colors[i], colorId, 1);
            }
        }

        private void CreateWirePin(Transform parent, Vector3 localPos, Color c, int colorId, int side)
        {
            GameObject pin = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pin.name = $"Pin_{colorId}_{side}";
            pin.transform.SetParent(parent, false);
            pin.transform.localPosition = localPos;
            pin.transform.localScale = new Vector3(0.24f, 0.24f, 0.24f);

            var r = pin.GetComponent<Renderer>();
            r.material = CreateMaterial(c, true);

            GameObject socket = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            socket.name = "Socket";
            socket.transform.SetParent(pin.transform, false);
            socket.transform.localPosition = new Vector3(0f, 0f, -0.3f);
            socket.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            socket.transform.localScale = new Vector3(1.3f, 0.3f, 1.3f);
            socket.GetComponent<Renderer>().material = CreateMaterial(new Color(0.2f, 0.22f, 0.25f), false);
            var sCol = socket.GetComponent<Collider>();
            if (sCol != null) Destroy(sCol);

            var capCol = pin.GetComponent<Collider>();
            if (capCol != null) Destroy(capCol);
            var sphCol = pin.AddComponent<SphereCollider>();
            sphCol.radius = 0.6f;

            ClickableObject clk = pin.AddComponent<ClickableObject>();
            clk.promptText = $"⚡ Контакт #{colorId + 1} (Клик: соединить)";
            clk.onClick = () => OnWirePinClicked(pin, colorId, side, c);
            activeSubObjects.Add(pin);
        }

        private void OnWirePinClicked(GameObject pin, int colorId, int side, Color origColor)
        {
            if (selectedPinObj == null)
            {
                selectedPinObj = pin;
                selectedColorId = colorId;
                selectedSide = side;
                selectedOriginalColor = origColor;

                pin.GetComponent<Renderer>().material = CreateMaterial(Color.white, true);
                pin.transform.localScale = new Vector3(0.32f, 0.32f, 0.32f);
                HangarAudioLight.Instance?.PlayClick();
                RepairStepInfo = "Выбран контакт цепи! Теперь кликните парный контакт того же цвета";
            }
            else
            {
                if (selectedPinObj == pin)
                {
                    selectedPinObj.GetComponent<Renderer>().material = CreateMaterial(selectedOriginalColor, true);
                    selectedPinObj.transform.localScale = new Vector3(0.24f, 0.24f, 0.24f);
                    selectedPinObj = null;
                    selectedColorId = -1;
                    selectedSide = -1;
                    HangarAudioLight.Instance?.PlayClick();
                    RepairStepInfo = "Выбор отменен. Кликните контакт для соединения.";
                    return;
                }

                if (selectedColorId == colorId && selectedSide != side)
                {
                    selectedPinObj.GetComponent<Renderer>().material = CreateMaterial(selectedOriginalColor, true);
                    selectedPinObj.transform.localScale = new Vector3(0.24f, 0.24f, 0.24f);

                    var clk1 = selectedPinObj.GetComponent<ClickableObject>();
                    if (clk1 != null) Destroy(clk1);
                    var clk2 = pin.GetComponent<ClickableObject>();
                    if (clk2 != null) Destroy(clk2);

                    CreateWireBridge(colorId, origColor);

                    selectedPinObj = null;
                    selectedColorId = -1;
                    selectedSide = -1;
                    wiresRemaining--;

                    HangarAudioLight.Instance?.PlayWeld();

                    if (wiresRemaining <= 0)
                    {
                        RepairStepInfo = "✔ Все цепи замкнуты! Деталь восстановлена на 100%! Нажмите [ПРОБЕЛ] для установки.";
                        HangarAudioLight.Instance?.PlaySuccess();
                    }
                    else
                    {
                        RepairStepInfo = $"✔ Цепь замкнута! Осталось соединить: {wiresRemaining}";
                    }
                }
                else
                {
                    selectedPinObj.GetComponent<Renderer>().material = CreateMaterial(selectedOriginalColor, true);
                    selectedPinObj.transform.localScale = new Vector3(0.24f, 0.24f, 0.24f);

                    selectedPinObj = pin;
                    selectedColorId = colorId;
                    selectedSide = side;
                    selectedOriginalColor = origColor;

                    pin.GetComponent<Renderer>().material = CreateMaterial(Color.white, true);
                    pin.transform.localScale = new Vector3(0.32f, 0.32f, 0.32f);
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
            Vector3 mid = (pL + pR) * 0.5f + new Vector3(0f, 0f, 0.04f);

            GameObject wire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wire.name = $"Wire_Bridge_{colorId}";
            wire.transform.SetParent(serviceHatchObj.transform, false);
            wire.transform.localPosition = mid;
            wire.transform.localRotation = Quaternion.LookRotation(pR - pL) * Quaternion.Euler(90f, 0f, 0f);

            float dist = Vector3.Distance(pL, pR);
            wire.transform.localScale = new Vector3(0.08f, dist * 0.5f, 0.08f);
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
