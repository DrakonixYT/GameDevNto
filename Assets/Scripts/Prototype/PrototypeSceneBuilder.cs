using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using HangarGame.Core;
using HangarGame.CameraControl;
using HangarGame.Interaction;
using HangarGame.Data;
using HangarGame.Mechs;
using HangarGame.Repair;
using HangarGame.Dispatch;
using HangarGame.UI;

namespace HangarGame.Prototype
{
    public class PrototypeSceneBuilder : MonoBehaviour
    {
        [Header("Auto Build on Play if empty")]
        [SerializeField] private bool autoBuildOnStart = true;

        private void Start()
        {
            if (autoBuildOnStart && FindObjectOfType<GameManager>() == null)
            {
                BuildCompleteHangarPrototype();
            }
        }

        [ContextMenu("Build Prototype Hangar Scene")]
        public void BuildCompleteHangarPrototype()
        {
            Debug.Log("<color=cyan>[PrototypeSceneBuilder] Генерация 3D сцены ангара, мехов и UI...</color>");

            // 1. Создаем GameManager и EventBus
            GameObject coreObj = new GameObject("--- CORE MANAGERS ---");
            GameManager gm = coreObj.AddComponent<GameManager>();
            DispatchManager dispatch = coreObj.AddComponent<DispatchManager>();
            EnemyAdaptationSystem adaptation = coreObj.AddComponent<EnemyAdaptationSystem>();
            JaegerProjectManager jaeger = coreObj.AddComponent<JaegerProjectManager>();
            HangarAudio audio = coreObj.AddComponent<HangarAudio>();

            // 2. Освещение
            CreateLighting();

            // 3. Комната / Ангар (Стены, пол, потолок)
            GameObject room = CreateRoomEnvironment();

            // 4. Камера
            UnityEngine.Camera cam = SetupPlayerCamera();

            // 5. Верстак для починки
            RepairWorkbench workbench = CreateWorkbench(room.transform);

            // 6. Мех в ангаре (0 градусов)
            MechInstance mech = CreateMechInHangar(room.transform);

            // 7. Диспетчерский пульт с 3D кнопками (180 градусов)
            CreateDispatchConsole(room.transform, mech);

            // 8. EventSystem
            if (FindObjectOfType<EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                es.AddComponent<StandaloneInputModule>();
#endif
            }

            // 9. 2D UI Canvas
            CreateUI(cam);

            Debug.Log("<color=green>[PrototypeSceneBuilder] Прототип игры успешно создан и готов к игре!</color>");
        }

        private void CreateLighting()
        {
            if (FindObjectOfType<Light>() == null)
            {
                GameObject dirLightObj = new GameObject("Directional Light");
                Light dirLight = dirLightObj.AddComponent<Light>();
                dirLight.type = LightType.Directional;
                dirLight.intensity = 1.2f;
                dirLight.color = new Color(0.95f, 0.98f, 1f);
                dirLightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }
        }

        private GameObject CreateRoomEnvironment()
        {
            GameObject room = new GameObject("Hangar_Room");

            // Пол
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(14f, 1f, 14f);
            floor.transform.SetParent(room.transform);
            SetColor(floor, new Color(0.18f, 0.2f, 0.22f));

            // Потолок
            GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Ceiling";
            ceiling.transform.position = new Vector3(0f, 5.5f, 0f);
            ceiling.transform.localScale = new Vector3(14f, 1f, 14f);
            ceiling.transform.SetParent(room.transform);
            SetColor(ceiling, new Color(0.12f, 0.13f, 0.15f));

            // Стена Ангара (Спереди, Z = 7)
            GameObject wallHangar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallHangar.name = "Wall_Hangar_Gate";
            wallHangar.transform.position = new Vector3(0f, 2.5f, 7f);
            wallHangar.transform.localScale = new Vector3(14f, 5f, 0.5f);
            wallHangar.transform.SetParent(room.transform);
            SetColor(wallHangar, new Color(0.15f, 0.18f, 0.22f));

            // Стена Диспетчерской (Сзади, Z = -7)
            GameObject wallConsole = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallConsole.name = "Wall_Console_Back";
            wallConsole.transform.position = new Vector3(0f, 2.5f, -7f);
            wallConsole.transform.localScale = new Vector3(14f, 5f, 0.5f);
            wallConsole.transform.SetParent(room.transform);
            SetColor(wallConsole, new Color(0.14f, 0.15f, 0.18f));

            // Левая стена (X = -7)
            GameObject wallLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallLeft.name = "Wall_Left";
            wallLeft.transform.position = new Vector3(-7f, 2.5f, 0f);
            wallLeft.transform.localScale = new Vector3(0.5f, 5f, 14f);
            wallLeft.transform.SetParent(room.transform);
            SetColor(wallLeft, new Color(0.16f, 0.17f, 0.19f));

            // Правая стена (X = 7)
            GameObject wallRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallRight.name = "Wall_Right";
            wallRight.transform.position = new Vector3(7f, 2.5f, 0f);
            wallRight.transform.localScale = new Vector3(0.5f, 5f, 14f);
            wallRight.transform.SetParent(room.transform);
            SetColor(wallRight, new Color(0.16f, 0.17f, 0.19f));

            return room;
        }

        private UnityEngine.Camera SetupPlayerCamera()
        {
            UnityEngine.Camera cam = UnityEngine.Camera.main;
            if (cam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<UnityEngine.Camera>();
                camObj.tag = "MainCamera";
                camObj.AddComponent<AudioListener>();
            }

            cam.transform.position = new Vector3(0f, 1.6f, 0f);
            cam.transform.rotation = Quaternion.identity;

            if (cam.GetComponent<RoomCameraController>() == null)
            {
                cam.gameObject.AddComponent<RoomCameraController>();
            }

            if (cam.GetComponent<PlayerRaycaster>() == null)
            {
                cam.gameObject.AddComponent<PlayerRaycaster>();
            }

            return cam;
        }

        private RepairWorkbench CreateWorkbench(Transform parent)
        {
            GameObject wbObj = new GameObject("RepairWorkbench_Station");
            wbObj.transform.position = new Vector3(0f, 0f, 2.2f);
            wbObj.transform.SetParent(parent);

            // Столешница (верх на Y = 0.8)
            GameObject table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.name = "Workbench_Table";
            table.transform.position = new Vector3(0f, 0.7f, 2.2f);
            table.transform.localScale = new Vector3(2.4f, 0.2f, 1.2f);
            table.transform.SetParent(wbObj.transform);
            SetColor(table, new Color(0.25f, 0.27f, 0.3f));

            // Точка крепления детали ровно на поверхности стола
            GameObject mountObj = new GameObject("Part_Mount_Point");
            mountObj.transform.position = new Vector3(0f, 0.82f, 2.2f);
            mountObj.transform.SetParent(wbObj.transform);

            // Ножки стола
            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    leg.transform.position = new Vector3(x * 1f, 0.35f, 2.2f + z * 0.45f);
                    leg.transform.localScale = new Vector3(0.15f, 0.7f, 0.15f);
                    leg.transform.SetParent(wbObj.transform);
                    SetColor(leg, Color.black);
                }
            }

            RepairWorkbench workbench = wbObj.AddComponent<RepairWorkbench>();
            SetField(typeof(RepairWorkbench), workbench, "partMountPoint", mountObj.transform);

            wbObj.AddComponent<BoltMiniGame>();
            wbObj.AddComponent<WeldingMiniGame>();

            return workbench;
        }

        private MechInstance CreateMechInHangar(Transform parent)
        {
            GameObject mechRoot = new GameObject("Mech_Atlant_01");
            mechRoot.transform.position = new Vector3(0f, 0f, 5.5f);
            mechRoot.transform.SetParent(parent);

            MechInstance mech = mechRoot.AddComponent<MechInstance>();

            // 1. Торс
            GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            torso.name = "Torso_Core";
            torso.transform.position = new Vector3(0f, 2.8f, 5.5f);
            torso.transform.localScale = new Vector3(1.6f, 1.8f, 1.2f);
            torso.transform.SetParent(mechRoot.transform);
            SetColor(torso, new Color(0.2f, 0.45f, 0.7f));
            SetupMechPart(torso, "Тяжелый Бронекорпус", PartSlot.Torso, DamageType.Physical, DamageType.Physical, 20f, 60f);

            // 2. Голова / Кокпит
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Cockpit_Head";
            head.transform.position = new Vector3(0f, 3.9f, 5.4f);
            head.transform.localScale = new Vector3(0.8f, 0.6f, 0.8f);
            head.transform.SetParent(mechRoot.transform);
            SetColor(head, new Color(0.9f, 0.6f, 0.1f));
            SetupMechPart(head, "Командный Сенсорный Модуль", PartSlot.Head, DamageType.Plasma, DamageType.Plasma, 15f, 30f);

            // 3. Левая рука — Энерго-щит против огнестрела
            GameObject leftArm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftArm.name = "LeftArm_Shield";
            leftArm.transform.position = new Vector3(-1.3f, 2.7f, 5.3f);
            leftArm.transform.localScale = new Vector3(0.6f, 1.6f, 0.7f);
            leftArm.transform.SetParent(mechRoot.transform);
            SetColor(leftArm, new Color(0.2f, 0.7f, 0.9f));
            SetupMechPart(leftArm, "Тяжелый Щит 'Бастион'", PartSlot.LeftArm, DamageType.Physical, DamageType.Physical, 10f, 80f);

            // 4. Правая рука — Плазменное орудие против монстров
            GameObject rightArm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rightArm.name = "RightArm_PlasmaCannon";
            rightArm.transform.position = new Vector3(1.3f, 2.7f, 5.3f);
            rightArm.transform.localScale = new Vector3(0.5f, 0.9f, 0.5f);
            rightArm.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            rightArm.transform.SetParent(mechRoot.transform);
            SetColor(rightArm, new Color(0.85f, 0.2f, 0.2f));
            SetupMechPart(rightArm, "Плазменная Пушка 'Сверхновая'", PartSlot.RightArm, DamageType.Plasma, DamageType.Thermal, 60f, 20f);

            // 5. Ноги (аккуратно стоят на полу Y = 0)
            GameObject legLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            legLeft.name = "Leg_Left";
            legLeft.transform.position = new Vector3(-0.55f, 1.0f, 5.5f);
            legLeft.transform.localScale = new Vector3(0.5f, 2.0f, 0.6f);
            legLeft.transform.SetParent(mechRoot.transform);
            SetColor(legLeft, new Color(0.3f, 0.35f, 0.4f));
            SetupMechPart(legLeft, "Гидравлическая Опора (Л)", PartSlot.Legs, DamageType.Physical, DamageType.Physical, 10f, 40f);

            GameObject legRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            legRight.name = "Leg_Right";
            legRight.transform.position = new Vector3(0.55f, 1.0f, 5.5f);
            legRight.transform.localScale = new Vector3(0.5f, 2.0f, 0.6f);
            legRight.transform.SetParent(mechRoot.transform);
            SetColor(legRight, new Color(0.3f, 0.35f, 0.4f));
            SetupMechPart(legRight, "Гидравлическая Опора (П)", PartSlot.Legs, DamageType.Physical, DamageType.Physical, 10f, 40f);

            return mech;
        }

        private void SetupMechPart(GameObject obj, string partName, PartSlot slot, DamageType defenseType, DamageType attackType, float attackPow, float defensePow)
        {
            MechPartData pData = ScriptableObject.CreateInstance<MechPartData>();
            pData.partName = partName;
            pData.slot = slot;
            pData.resistanceType = defenseType;
            pData.attackType = attackType;
            pData.attackPower = attackPow;
            pData.defensePower = defensePow;
            pData.maxDurability = 100f;
            pData.repairCreditCost = 40;
            pData.repairAlloyCost = 20;

            MechPartInstance partInstance = obj.AddComponent<MechPartInstance>();
            partInstance.Initialize(pData, slot, 100f);
        }

        private void CreateDispatchConsole(Transform parent, MechInstance mech)
        {
            GameObject consoleGroup = new GameObject("Dispatch_Station_180Deg");
            consoleGroup.transform.position = new Vector3(0f, 0f, -4.5f);
            consoleGroup.transform.SetParent(parent);

            // Пульт управления
            GameObject desk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            desk.name = "Command_Desk";
            desk.transform.position = new Vector3(0f, 0.8f, -4.5f);
            desk.transform.localScale = new Vector3(3.2f, 0.3f, 1.4f);
            desk.transform.SetParent(consoleGroup.transform);
            SetColor(desk, new Color(0.2f, 0.22f, 0.25f));

            // Большой тактический голо-монитор
            GameObject monitor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            monitor.name = "Tactical_Radar_Screen";
            monitor.transform.position = new Vector3(0f, 1.8f, -4.9f);
            monitor.transform.localScale = new Vector3(2.6f, 1.4f, 0.1f);
            monitor.transform.SetParent(consoleGroup.transform);
            SetColor(monitor, new Color(0.05f, 0.35f, 0.45f));

            // 3D Кнопка на пульте: "ОТПРАВКА НА МИССИЮ"
            GameObject btn3DDeploy = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            btn3DDeploy.name = "3D_Btn_Deploy";
            btn3DDeploy.transform.position = new Vector3(-0.8f, 1.0f, -4.3f);
            btn3DDeploy.transform.localScale = new Vector3(0.25f, 0.1f, 0.25f);
            btn3DDeploy.transform.SetParent(consoleGroup.transform);
            SetColor(btn3DDeploy, new Color(0.9f, 0.2f, 0.2f));

            Clickable3DObject clickDeploy = btn3DDeploy.AddComponent<Clickable3DObject>();
            if (clickDeploy.onInteract == null) clickDeploy.onInteract = new UnityEngine.Events.UnityEvent();
            clickDeploy.onInteract.AddListener(() =>
            {
                if (DispatchManager.Instance != null && mech != null)
                {
                    DispatchManager.Instance.TryDeployMech(mech);
                }
            });

            // 3D Кнопка на пульте: "РАДАР / ВЫЗОВ УГРОЗЫ"
            GameObject btn3DScan = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            btn3DScan.name = "3D_Btn_Scan";
            btn3DScan.transform.position = new Vector3(0.8f, 1.0f, -4.3f);
            btn3DScan.transform.localScale = new Vector3(0.25f, 0.1f, 0.25f);
            btn3DScan.transform.SetParent(consoleGroup.transform);
            SetColor(btn3DScan, new Color(0.2f, 0.8f, 0.3f));

            Clickable3DObject clickScan = btn3DScan.AddComponent<Clickable3DObject>();
            if (clickScan.onInteract == null) clickScan.onInteract = new UnityEngine.Events.UnityEvent();
            clickScan.onInteract.AddListener(() =>
            {
                if (DispatchManager.Instance != null && !DispatchManager.Instance.HasActiveThreat)
                {
                    DispatchManager.Instance.SpawnNextThreat();
                }
            });
        }

        private void CreateUI(UnityEngine.Camera cam)
        {
            GameObject canvasObj = new GameObject("HUD_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            Font defaultFont = Font.CreateDynamicFontFromOSFont("Arial", 16);
            if (defaultFont == null)
            {
                defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            // HUDManager & DispatchUI
            HUDManager hud = canvasObj.AddComponent<HUDManager>();
            DispatchUI dispatchUI = canvasObj.AddComponent<DispatchUI>();

            // 1. Кнопка "◀ 180° [A / Q]" (Прижата к левому краю экрана)
            GameObject btnLeftObj = CreateAnchoredButton(
                canvasObj.transform, 
                "Btn_TurnLeft", 
                "◀ Диспетчерская\n(180° / A / Q)", 
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), 
                new Vector2(20f, 0f), new Vector2(180f, 110f), 
                defaultFont, new Color(0.15f, 0.35f, 0.6f, 0.95f)
            );

            // 2. Кнопка "180° ▶ [D / E]" (Прижата к правому краю экрана)
            GameObject btnRightObj = CreateAnchoredButton(
                canvasObj.transform, 
                "Btn_TurnRight", 
                "Диспетчерская ▶\n(180° / D / E)", 
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), 
                new Vector2(-20f, 0f), new Vector2(180f, 110f), 
                defaultFont, new Color(0.15f, 0.35f, 0.6f, 0.95f)
            );

            // 3. Панель Верстака (появляется только в режиме верстака)
            GameObject wbPanel = new GameObject("Workbench_Panel");
            wbPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform wbRect = wbPanel.AddComponent<RectTransform>();
            wbRect.anchorMin = new Vector2(0.5f, 0f);
            wbRect.anchorMax = new Vector2(0.5f, 0f);
            wbRect.pivot = new Vector2(0.5f, 0f);
            wbRect.anchoredPosition = new Vector2(0f, 25f);
            wbRect.sizeDelta = new Vector2(650f, 140f);

            Image wbBg = wbPanel.AddComponent<Image>();
            wbBg.color = new Color(0.1f, 0.12f, 0.16f, 0.92f);

            GameObject txtWbTitle = CreateAnchoredText(
                wbPanel.transform, "Txt_WbTitle", 
                "🔧 РЕЖИМ ВЕРСТАКА: Кликайте по болтам/сварке", 
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), 
                new Vector2(0f, -25f), new Vector2(600f, 35f), 
                18, defaultFont, Color.yellow
            );

            GameObject btnExitWb = CreateAnchoredButton(
                wbPanel.transform, "Btn_ExitWorkbench", 
                "❌ ВЫЙТИ В АНГАР (ESC)", 
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), 
                new Vector2(-150f, 40f), new Vector2(260f, 60f), 
                defaultFont, new Color(0.85f, 0.2f, 0.2f, 0.95f)
            );

            GameObject btnInstant = CreateAnchoredButton(
                wbPanel.transform, "Btn_InstantRepair", 
                "⚡ БЫСТРЫЙ РЕМОНТ (ПРОБЕЛ)", 
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), 
                new Vector2(150f, 40f), new Vector2(260f, 60f), 
                defaultFont, new Color(0.15f, 0.7f, 0.3f, 0.95f)
            );
            wbPanel.SetActive(false);

            // 4. Текст ресурсов (Верхний центр)
            GameObject txtResObj = CreateAnchoredText(
                canvasObj.transform, "Txt_Resources", 
                "КРЕДИТЫ: 500 ¢  |  СПЛАВ: 200 т.", 
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), 
                new Vector2(0f, -30f), new Vector2(600f, 45f), 
                22, defaultFont, Color.yellow
            );

            // 5. Прогресс сборки Егеря
            GameObject txtJaegerObj = CreateAnchoredText(
                canvasObj.transform, "Txt_Jaeger", 
                "ПРОЕКТ 'ЕГЕРЬ': 0% (Собрать 100% для победы)", 
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), 
                new Vector2(0f, -75f), new Vector2(600f, 35f), 
                16, defaultFont, Color.cyan
            );

            GameObject btnJaeger = CreateAnchoredButton(
                canvasObj.transform, "Btn_BuildJaeger", 
                "🔨 Собрать модуль Егеря (+25%) [300¢, 150т]", 
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), 
                new Vector2(0f, -125f), new Vector2(400f, 45f), 
                defaultFont, new Color(0.2f, 0.5f, 0.7f, 0.9f)
            );

            // 6. Подсказка ховера (Нижний центр)
            GameObject promptBgObj = new GameObject("Prompt_Box");
            promptBgObj.transform.SetParent(canvasObj.transform, false);
            RectTransform pRect = promptBgObj.AddComponent<RectTransform>();
            pRect.anchorMin = new Vector2(0.5f, 0f);
            pRect.anchorMax = new Vector2(0.5f, 0f);
            pRect.pivot = new Vector2(0.5f, 0f);
            pRect.anchoredPosition = new Vector2(0f, 160f);
            pRect.sizeDelta = new Vector2(750f, 50f);
            Image pImg = promptBgObj.AddComponent<Image>();
            pImg.color = new Color(0f, 0f, 0f, 0.6f);

            GameObject txtPromptObj = CreateAnchoredText(
                promptBgObj.transform, "Txt_HoverPrompt", 
                "", 
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), 
                Vector2.zero, Vector2.zero, 
                18, defaultFont, Color.white
            );

            // 7. Карточка активной угрозы (Dispatch)
            GameObject threatCard = new GameObject("ThreatCard");
            threatCard.transform.SetParent(canvasObj.transform, false);
            RectTransform tcRect = threatCard.AddComponent<RectTransform>();
            tcRect.anchorMin = new Vector2(0.5f, 0.5f);
            tcRect.anchorMax = new Vector2(0.5f, 0.5f);
            tcRect.pivot = new Vector2(0.5f, 0.5f);
            tcRect.anchoredPosition = new Vector2(0f, 40f);
            tcRect.sizeDelta = new Vector2(550f, 210f);

            Image cardBg = threatCard.AddComponent<Image>();
            cardBg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

            GameObject txtThreatName = CreateAnchoredText(
                threatCard.transform, "Txt_ThreatName", 
                "СКАНИРОВАНИЕ СЕКТОРА...", 
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), 
                new Vector2(0f, -30f), new Vector2(500f, 40f), 
                20, defaultFont, Color.red
            );

            GameObject txtThreatInfo = CreateAnchoredText(
                threatCard.transform, "Txt_ThreatDetails", 
                "Ожидание сигнатуры врага...", 
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 
                new Vector2(0f, 5f), new Vector2(500f, 70f), 
                16, defaultFont, Color.white
            );

            GameObject btnDeploy = CreateAnchoredButton(
                threatCard.transform, "Btn_Deploy", 
                "⚔ ОТПРАВИТЬ ЕГЕРЯ В БОЙ", 
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), 
                new Vector2(0f, 35f), new Vector2(340f, 50f), 
                defaultFont, new Color(0.85f, 0.2f, 0.2f, 0.95f)
            );

            // 8. Лог миссий и тревога адаптации
            GameObject txtLog = CreateAnchoredText(
                canvasObj.transform, "Txt_MissionLog", 
                "Добро пожаловать на базу, Командир! Отправляйте роботов в бой и чините повреждения на верстаке.", 
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), 
                new Vector2(0f, 110f), new Vector2(850f, 50f), 
                16, defaultFont, new Color(0.85f, 0.85f, 0.85f)
            );

            GameObject txtAdapt = CreateAnchoredText(
                canvasObj.transform, "Txt_AdaptationAlert", 
                "", 
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), 
                new Vector2(0f, -170f), new Vector2(800f, 40f), 
                17, defaultFont, new Color(1f, 0.55f, 0.1f)
            );

            // 9. Карточка активного боя (Mission Progress)
            GameObject missionCard = new GameObject("MissionProgressCard");
            missionCard.transform.SetParent(canvasObj.transform, false);
            RectTransform mRect = missionCard.AddComponent<RectTransform>();
            mRect.anchorMin = new Vector2(0.5f, 0.5f);
            mRect.anchorMax = new Vector2(0.5f, 0.5f);
            mRect.pivot = new Vector2(0.5f, 0.5f);
            mRect.anchoredPosition = new Vector2(0f, 40f);
            mRect.sizeDelta = new Vector2(550f, 160f);
            Image mBg = missionCard.AddComponent<Image>();
            mBg.color = new Color(0.18f, 0.1f, 0.1f, 0.96f);
            CreateAnchoredText(missionCard.transform, "Txt_MissTitle", "⚔ БОЕВАЯ ОПЕРАЦИЯ В ГОРОДЕ...", new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500f, 40f), 22, defaultFont, Color.yellow);
            CreateAnchoredText(missionCard.transform, "Txt_MissSub", "Егерь ведет перехват врага. Ожидание завершения боя...", new Vector2(0.5f, 0.35f), new Vector2(0.5f, 0.35f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500f, 40f), 16, defaultFont, Color.white);
            missionCard.SetActive(false);

            // 10. Экран победы (Victory Panel)
            GameObject victoryPanel = new GameObject("Victory_Panel");
            victoryPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform vRect = victoryPanel.AddComponent<RectTransform>();
            vRect.anchorMin = new Vector2(0.5f, 0.5f);
            vRect.anchorMax = new Vector2(0.5f, 0.5f);
            vRect.pivot = new Vector2(0.5f, 0.5f);
            vRect.anchoredPosition = Vector2.zero;
            vRect.sizeDelta = new Vector2(620f, 320f);
            Image vBg = victoryPanel.AddComponent<Image>();
            vBg.color = new Color(0.08f, 0.12f, 0.22f, 0.98f);

            CreateAnchoredText(victoryPanel.transform, "Txt_WinTitle", "🏆 ПОБЕДА!", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(500f, 50f), 34, defaultFont, Color.yellow);
            CreateAnchoredText(victoryPanel.transform, "Txt_WinDesc", "ПРОЕКТ 'ЕГЕРЬ' ЗАВЕРШЕН!\nГород спасен от вторжения!", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 15f), new Vector2(520f, 70f), 20, defaultFont, Color.white);
            GameObject btnRestart = CreateAnchoredButton(victoryPanel.transform, "Btn_Restart", "🔄 Играть снова", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(240f, 50f), defaultFont, new Color(0.2f, 0.7f, 0.3f, 0.95f));
            btnRestart.GetComponent<Button>().onClick.AddListener(() => UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex));
            victoryPanel.SetActive(false);

            // Связываем ссылки
            LinkHudFields(hud, btnLeftObj, btnRightObj, wbPanel, btnExitWb, btnInstant, txtResObj, txtJaegerObj, btnJaeger, txtPromptObj, victoryPanel, txtWbTitle);
            LinkDispatchFields(dispatchUI, threatCard, missionCard, txtThreatName, txtThreatInfo, btnDeploy, txtLog, txtAdapt);
        }

        private void LinkHudFields(HUDManager hud, GameObject bL, GameObject bR, GameObject wbPanel, GameObject bExit, GameObject bInstant, GameObject tRes, GameObject tJaeger, GameObject bJaeger, GameObject tPrompt, GameObject vPanel, GameObject tWbTitle)
        {
            var t = typeof(HUDManager);
            SetField(t, hud, "btnLookLeft", bL.GetComponent<Button>());
            SetField(t, hud, "btnLookRight", bR.GetComponent<Button>());
            SetField(t, hud, "txtLookLeft", bL.GetComponentInChildren<Text>());
            SetField(t, hud, "txtLookRight", bR.GetComponentInChildren<Text>());
            SetField(t, hud, "workbenchPanel", wbPanel);
            SetField(t, hud, "btnExitWorkbench", bExit.GetComponent<Button>());
            SetField(t, hud, "btnInstantRepair", bInstant.GetComponent<Button>());
            SetField(t, hud, "txtResources", tRes.GetComponent<Text>());
            SetField(t, hud, "txtJaegerProgress", tJaeger.GetComponent<Text>());
            SetField(t, hud, "btnBuildJaeger", bJaeger.GetComponent<Button>());
            SetField(t, hud, "txtHoverPrompt", tPrompt.GetComponent<Text>());
            SetField(t, hud, "victoryPanel", vPanel);
            SetField(t, hud, "txtWorkbenchTitle", tWbTitle.GetComponent<Text>());
        }

        private void LinkDispatchFields(DispatchUI dui, GameObject card, GameObject mCard, GameObject tName, GameObject tInfo, GameObject bDeploy, GameObject tLog, GameObject tAdapt)
        {
            var t = typeof(DispatchUI);
            SetField(t, dui, "activeThreatCard", card);
            SetField(t, dui, "missionProgressCard", mCard);
            SetField(t, dui, "txtThreatName", tName.GetComponent<Text>());
            SetField(t, dui, "txtThreatDetails", tInfo.GetComponent<Text>());
            SetField(t, dui, "btnDeployMech", bDeploy.GetComponent<Button>());
            SetField(t, dui, "txtMissionLog", tLog.GetComponent<Text>());
            SetField(t, dui, "txtAdaptationAlert", tAdapt.GetComponent<Text>());
        }

        private void SetField(System.Type type, object instance, string fieldName, object value)
        {
            var field = type.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (field != null) field.SetValue(instance, value);
        }

        private GameObject CreateAnchoredButton(Transform parent, string name, string label, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size, Font font, Color bgColor)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = aMin;
            rect.anchorMax = aMax;
            rect.pivot = pivot;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            Image img = btnObj.AddComponent<Image>();
            img.color = bgColor;

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.highlightedColor = bgColor * 1.3f;
            cb.pressedColor = bgColor * 0.7f;
            btn.colors = cb;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);

            RectTransform tRect = textObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = Vector2.zero;

            Text txt = textObj.AddComponent<Text>();
            txt.font = font;
            txt.text = label;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.fontSize = 15;

            return btnObj;
        }

        private GameObject CreateAnchoredText(Transform parent, string name, string text, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size, int fontSize, Font font, Color color)
        {
            GameObject txtObj = new GameObject(name);
            txtObj.transform.SetParent(parent, false);

            RectTransform rect = txtObj.AddComponent<RectTransform>();
            rect.anchorMin = aMin;
            rect.anchorMax = aMax;
            rect.pivot = pivot;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            Text txt = txtObj.AddComponent<Text>();
            txt.font = font;
            txt.text = text;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = color;
            txt.fontSize = fontSize;

            return txtObj;
        }

        private void SetColor(GameObject go, Color c)
        {
            Renderer r = go.GetComponent<Renderer>();
            if (r != null)
            {
                r.material.color = c;
            }
        }
    }
}
