using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using HangarGame.Core;
using HangarGame.CameraControl;
using HangarGame.Interaction;
using HangarGame.Mechs;
using HangarGame.Repair;
using HangarGame.Dispatch;
using HangarGame.UI;

namespace HangarGame.Prototype
{
    public class PrototypeSceneBuilder : MonoBehaviour
    {
        [ContextMenu("Build Prototype Hangar Scene")]
        public void BuildCompleteHangarPrototype()
        {
            Debug.Log("<color=cyan>[PrototypeSceneBuilder] Генерация чистой 3D сцены (8 модульных папок)...</color>");

            // 1. MANAGERS
            GameObject mgr = new GameObject("--- 01_MANAGERS ---");
            mgr.AddComponent<GameManager>();
            mgr.AddComponent<DispatchSystem>();
            mgr.AddComponent<JaegerProject>();
            HangarAudioLight audioLight = mgr.AddComponent<HangarAudioLight>();

            GameObject es = new GameObject("EventSystem");
            es.transform.SetParent(mgr.transform, false);
            es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif

            // Camera
            UnityEngine.Camera cam = UnityEngine.Camera.main;
            if (cam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<UnityEngine.Camera>();
                cam.tag = "MainCamera";
            }
            cam.transform.SetParent(mgr.transform, false);
            cam.transform.position = new Vector3(0f, 1.6f, 0f);
            cam.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.08f, 0.11f);

            if (cam.GetComponent<RoomCamera>() == null) cam.gameObject.AddComponent<RoomCamera>();
            if (cam.GetComponent<PlayerRaycaster>() == null) cam.gameObject.AddComponent<PlayerRaycaster>();

            // 2. LIGHTING
            CreateLighting(audioLight);

            // 3. ROOM
            CreateHangarRoom();

            // 4. COMBAT MECH ATLANT
            CreateMechAtlant();

            // 5. JAEGER FRAME
            CreateJaegerFrame();

            // 6. WORKBENCH
            CreateWorkbench();

            // 7. DISPATCH CONSOLE
            CreateDispatchConsole(cam);

            // 8. MINIMAL HUD
            CreateHUD();

            Debug.Log("<color=green>[PrototypeSceneBuilder] Сцена собрана идеально!</color>");
        }

        private void CreateLighting(HangarAudioLight audioLight)
        {
            GameObject grp = new GameObject("--- 02_LIGHTING_AND_VFX ---");

            GameObject dir = new GameObject("Directional Light");
            dir.transform.SetParent(grp.transform, false);
            Light lDir = dir.AddComponent<Light>();
            lDir.type = LightType.Directional;
            lDir.intensity = 1.15f;
            lDir.color = new Color(0.96f, 0.98f, 1f);
            dir.transform.rotation = Quaternion.Euler(50f, -25f, 0f);

            GameObject alarm = new GameObject("Emergency_Alarm_Light");
            alarm.transform.SetParent(grp.transform, false);
            alarm.transform.position = new Vector3(0f, 4.8f, 0f);
            Light lAlarm = alarm.AddComponent<Light>();
            lAlarm.type = LightType.Point;
            lAlarm.color = Color.red;
            lAlarm.range = 28f;
            lAlarm.intensity = 0f;

            if (audioLight != null)
            {
                var f = typeof(HangarAudioLight).GetField("redAlarmLight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (f != null) f.SetValue(audioLight, lAlarm);
            }

            CreateSpotlight(grp.transform, "Spotlight_Workbench", new Vector3(0f, 3.2f, 1.8f), Quaternion.Euler(55f, 0f, 0f), 2.2f);
            CreateSpotlight(grp.transform, "Spotlight_Mech", new Vector3(0f, 4.5f, 3.2f), Quaternion.Euler(35f, 0f, 0f), 2.0f);
            CreateSpotlight(grp.transform, "Spotlight_Jaeger", new Vector3(3.8f, 4.8f, 2.8f), Quaternion.Euler(35f, 25f, 0f), 2.4f);
        }

        private void CreateSpotlight(Transform parent, string name, Vector3 pos, Quaternion rot, float intensity)
        {
            GameObject sp = new GameObject(name);
            sp.transform.SetParent(parent, false);
            sp.transform.position = pos;
            sp.transform.rotation = rot;
            Light l = sp.AddComponent<Light>();
            l.type = LightType.Spot;
            l.range = 10f;
            l.spotAngle = 65f;
            l.intensity = intensity;
        }

        private void CreateHangarRoom()
        {
            GameObject grp = new GameObject("--- 03_HANGAR_ROOM ---");

            CreateBox(grp.transform, "Floor", new Vector3(0f, -0.5f, 0f), new Vector3(18f, 1f, 18f), new Color(0.18f, 0.2f, 0.23f));
            CreateBox(grp.transform, "Ceiling", new Vector3(0f, 5.5f, 0f), new Vector3(18f, 0.5f, 18f), new Color(0.12f, 0.14f, 0.16f));
            CreateBox(grp.transform, "Crane_Beam", new Vector3(0f, 4.8f, 2.5f), new Vector3(17f, 0.4f, 0.8f), new Color(0.85f, 0.65f, 0.1f));

            CreateBox(grp.transform, "Wall_Back", new Vector3(0f, 2.5f, 8.5f), new Vector3(18f, 6f, 0.6f), new Color(0.14f, 0.16f, 0.19f));
            CreateBox(grp.transform, "Blast_Doors", new Vector3(0f, 2.4f, 8.25f), new Vector3(7.5f, 4.8f, 0.1f), new Color(0.22f, 0.25f, 0.28f));
            CreateBox(grp.transform, "Wall_Front", new Vector3(0f, 2.5f, -8.5f), new Vector3(18f, 6f, 0.6f), new Color(0.12f, 0.14f, 0.17f));
            CreateBox(grp.transform, "Wall_Left", new Vector3(-8.8f, 2.5f, 0f), new Vector3(0.6f, 6f, 18f), new Color(0.15f, 0.17f, 0.2f));
            CreateBox(grp.transform, "Wall_Right", new Vector3(8.8f, 2.5f, 0f), new Vector3(0.6f, 6f, 18f), new Color(0.15f, 0.17f, 0.2f));

            CreateBox(grp.transform, "Pillar_1", new Vector3(-5.5f, 2.5f, 5.5f), new Vector3(0.8f, 5.5f, 0.8f), new Color(0.24f, 0.26f, 0.3f));
            CreateBox(grp.transform, "Pillar_2", new Vector3(5.5f, 2.5f, 5.5f), new Vector3(0.8f, 5.5f, 0.8f), new Color(0.24f, 0.26f, 0.3f));
            CreateBox(grp.transform, "Pillar_3", new Vector3(-5.5f, 2.5f, -5.5f), new Vector3(0.8f, 5.5f, 0.8f), new Color(0.24f, 0.26f, 0.3f));
            CreateBox(grp.transform, "Pillar_4", new Vector3(5.5f, 2.5f, -5.5f), new Vector3(0.8f, 5.5f, 0.8f), new Color(0.24f, 0.26f, 0.3f));
        }

        private void CreateMechAtlant()
        {
            GameObject grp = new GameObject("--- 04_MECH_ATLANT ---");
            grp.transform.position = new Vector3(0f, 0f, 5.5f);
            grp.AddComponent<CombatMech>();

            SetupPart(grp.transform, "Torso_Core", PrimitiveType.Cube, new Vector3(0f, 2.8f, 5.5f), new Vector3(1.6f, 1.8f, 1.2f), new Color(0.2f, 0.45f, 0.7f),
                new MechPartData { partName = "Тяжелый Бронекорпус", slot = PartSlot.Torso, attackType = DamageType.Physical, attackPower = 20f, defenseType = DamageType.Physical, defensePower = 60f });

            SetupPart(grp.transform, "Cockpit_Head", PrimitiveType.Sphere, new Vector3(0f, 3.9f, 5.4f), new Vector3(0.8f, 0.6f, 0.8f), new Color(0.9f, 0.6f, 0.1f),
                new MechPartData { partName = "Командный Сенсорный Модуль", slot = PartSlot.Head, attackType = DamageType.Plasma, attackPower = 15f, defenseType = DamageType.Plasma, defensePower = 30f });

            SetupPart(grp.transform, "LeftArm_Shield", PrimitiveType.Cube, new Vector3(-1.3f, 2.7f, 5.3f), new Vector3(0.6f, 1.6f, 0.7f), new Color(0.2f, 0.7f, 0.9f),
                new MechPartData { partName = "Тяжелый Щит 'Бастион'", slot = PartSlot.LeftArm, attackType = DamageType.Physical, attackPower = 10f, defenseType = DamageType.Physical, defensePower = 80f });

            SetupPart(grp.transform, "RightArm_PlasmaCannon", PrimitiveType.Cylinder, new Vector3(1.3f, 2.7f, 5.3f), new Vector3(0.5f, 1.4f, 0.5f), new Color(0.9f, 0.3f, 0.1f),
                new MechPartData { partName = "Плазменная Пушка 'Сверхновая'", slot = PartSlot.RightArm, attackType = DamageType.Plasma, attackPower = 60f, defenseType = DamageType.Thermal, defensePower = 20f });

            SetupPart(grp.transform, "Legs_Chassis", PrimitiveType.Cube, new Vector3(0f, 0.9f, 5.5f), new Vector3(1.4f, 1.8f, 1.1f), new Color(0.25f, 0.28f, 0.32f),
                new MechPartData { partName = "Гидравлическое Шасси 'Титан'", slot = PartSlot.Legs, attackType = DamageType.Physical, attackPower = 15f, defenseType = DamageType.Physical, defensePower = 50f });

            CreateBox(grp.transform, "Scaffold_L", new Vector3(-2.2f, 2.2f, 5.5f), new Vector3(0.2f, 4.4f, 2.4f), new Color(0.85f, 0.65f, 0.1f));
            CreateBox(grp.transform, "Scaffold_R", new Vector3(2.2f, 2.2f, 5.5f), new Vector3(0.2f, 4.4f, 2.4f), new Color(0.85f, 0.65f, 0.1f));
        }

        private void SetupPart(Transform parent, string name, PrimitiveType prim, Vector3 pos, Vector3 scale, Color color, MechPartData data)
        {
            GameObject obj = GameObject.CreatePrimitive(prim);
            obj.name = name;
            obj.transform.position = pos;
            obj.transform.localScale = scale;
            obj.transform.SetParent(parent, false);
            SetColor(obj, color);

            MechPart p = obj.AddComponent<MechPart>();
            p.Setup(data);
        }

        private void CreateJaegerFrame()
        {
            GameObject grp = new GameObject("--- 05_JAEGER_FRAME ---");
            grp.transform.position = new Vector3(4.8f, 0f, 4.8f);

            CreateBox(grp.transform, "Tower_L", new Vector3(3.2f, 3.5f, 4.8f), new Vector3(0.35f, 7f, 3.5f), new Color(0.9f, 0.7f, 0.15f));
            CreateBox(grp.transform, "Tower_R", new Vector3(6.4f, 3.5f, 4.8f), new Vector3(0.35f, 7f, 3.5f), new Color(0.9f, 0.7f, 0.15f));

            CreateBox(grp.transform, "Pelvis", new Vector3(4.8f, 2.2f, 4.8f), new Vector3(1.8f, 0.8f, 1.2f), new Color(0.2f, 0.22f, 0.25f));
            CreateBox(grp.transform, "Leg_L", new Vector3(4.1f, 1.1f, 4.8f), new Vector3(0.7f, 2.2f, 0.8f), new Color(0.18f, 0.2f, 0.22f));
            CreateBox(grp.transform, "Leg_R", new Vector3(5.5f, 1.1f, 4.8f), new Vector3(0.7f, 2.2f, 0.8f), new Color(0.18f, 0.2f, 0.22f));
            CreateCyl(grp.transform, "Spine", new Vector3(4.8f, 3.4f, 4.8f), new Vector3(0.4f, 1.4f, 0.4f), new Color(0.3f, 0.32f, 0.35f));

            // 4 Модуля стапеля
            GameObject m1 = CreateCyl(grp.transform, "Stage1_Reactor", new Vector3(4.8f, 3.4f, 4.5f), new Vector3(0.85f, 0.35f, 0.85f), Color.cyan);
            m1.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            m1.SetActive(false);

            GameObject m2 = CreateBox(grp.transform, "Stage2_Torso", new Vector3(4.8f, 3.5f, 4.8f), new Vector3(2.2f, 2.0f, 1.6f), new Color(0.25f, 0.45f, 0.65f));
            m2.SetActive(false);

            GameObject m3 = CreateCyl(grp.transform, "Stage3_Cannon", new Vector3(6.3f, 4.2f, 4.7f), new Vector3(0.45f, 2.2f, 0.45f), new Color(0.9f, 0.45f, 0.1f));
            m3.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            m3.SetActive(false);

            GameObject m4 = CreateBox(grp.transform, "Stage4_Helm", new Vector3(4.8f, 5.0f, 4.6f), new Vector3(0.9f, 0.7f, 0.9f), new Color(0.95f, 0.75f, 0.1f));
            m4.SetActive(false);

            JaegerProject.Instance?.SetMarkers(new Transform[] { m1.transform, m2.transform, m3.transform, m4.transform });
        }

        private void CreateWorkbench()
        {
            GameObject grp = new GameObject("--- 06_WORKBENCH ---");
            grp.transform.position = new Vector3(0f, 0f, 2.2f);
            grp.AddComponent<RepairWorkbench>();

            CreateBox(grp.transform, "Tabletop", new Vector3(0f, 0.76f, 2.2f), new Vector3(2.6f, 0.12f, 1.3f), new Color(0.22f, 0.24f, 0.27f));
            CreateBox(grp.transform, "Vice", new Vector3(-1.1f, 0.88f, 2.2f), new Vector3(0.25f, 0.2f, 0.3f), new Color(0.7f, 0.2f, 0.2f));
            CreateCyl(grp.transform, "Lamp", new Vector3(1.1f, 1.15f, 2.5f), new Vector3(0.08f, 0.6f, 0.08f), new Color(0.8f, 0.8f, 0.2f));

            for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2)
                    CreateBox(grp.transform, $"Leg_{x}_{z}", new Vector3(x * 1.15f, 0.35f, 2.2f + z * 0.5f), new Vector3(0.14f, 0.7f, 0.14f), Color.black);
        }

        private void CreateDispatchConsole(UnityEngine.Camera cam)
        {
            GameObject grp = new GameObject("--- 07_DISPATCH_CONSOLE ---");
            grp.transform.position = new Vector3(0f, 0f, -5.2f);
            grp.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            CreateBox(grp.transform, "Desk", new Vector3(0f, 0.75f, -5.2f), new Vector3(3.6f, 0.15f, 1.5f), new Color(0.18f, 0.2f, 0.24f));
            CreateBox(grp.transform, "Chair", new Vector3(0f, 0.55f, -4.2f), new Vector3(0.7f, 0.8f, 0.7f), new Color(0.1f, 0.1f, 0.12f));
            CreateBox(grp.transform, "Rack_L", new Vector3(-2.2f, 1.4f, -5.3f), new Vector3(0.8f, 2.8f, 0.9f), new Color(0.12f, 0.14f, 0.18f));
            CreateBox(grp.transform, "Rack_R", new Vector3(2.2f, 1.4f, -5.3f), new Vector3(0.8f, 2.8f, 0.9f), new Color(0.12f, 0.14f, 0.18f));

            // Monitor & Diegetic WorldSpace Canvas
            GameObject monitor = CreateBox(grp.transform, "Tactical_Radar_Screen", new Vector3(0f, 1.95f, -5.45f), new Vector3(3.0f, 1.7f, 0.12f), new Color(0.04f, 0.08f, 0.12f));

            GameObject cObj = new GameObject("Terminal_Canvas");
            cObj.transform.SetParent(monitor.transform, false);
            cObj.transform.localPosition = new Vector3(0f, 0f, -0.65f);
            cObj.transform.localScale = new Vector3(0.0017f, 0.0017f, 1f);

            Canvas c = cObj.AddComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
            c.worldCamera = cam;
            cObj.GetComponent<RectTransform>().sizeDelta = new Vector2(1650f, 950f);
            cObj.AddComponent<GraphicRaycaster>();

            Font font = Font.CreateDynamicFontFromOSFont("Arial", 16) ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // UI Elements
            GameObject bg = CreatePanel(cObj.transform, "BG", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.05f, 0.09f, 0.14f, 0.98f));

            Text tTitle = CreateTxt(bg.transform, "Txt_ThreatTitle", "СЕКТОР-7: РАДАР СКАНИРУЕТ ПЕРИМЕТР", new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1500f, 50f), 26, font, Color.cyan);
            Text tDetails = CreateTxt(bg.transform, "Txt_ThreatDetails", "Угроз нет. Проводите ТО робота на верстаке.", new Vector2(0f, 1f), new Vector2(40f, -110f), new Vector2(750f, 75f), 18, font, Color.white);
            Text tAdapt = CreateTxt(bg.transform, "Txt_Adaptation", "", new Vector2(0f, 1f), new Vector2(40f, -195f), new Vector2(750f, 45f), 17, font, new Color(1f, 0.55f, 0.15f));

            Text tStatus = CreateTxt(bg.transform, "Txt_MechStatus", "✔ СТАТУС: БОЕГОТОВЕН (Все 5 модулей смонтированы)", new Vector2(1f, 1f), new Vector2(-40f, -110f), new Vector2(750f, 45f), 20, font, Color.green);
            Text tJaeger = CreateTxt(bg.transform, "Txt_JaegerInfo", "ПРОЕКТ 'ЕГЕРЬ' (0/4)", new Vector2(1f, 1f), new Vector2(-40f, -170f), new Vector2(750f, 55f), 19, font, Color.yellow);
            Button bJaeger = CreateBtn(bg.transform, "Btn_BuildJaeger", "🔨 СОБРАТЬ МОДУЛЬ НА ВЕРСТАКЕ", new Vector2(1f, 1f), new Vector2(-40f, -240f), new Vector2(650f, 55f), font, new Color(0.18f, 0.45f, 0.7f));

            Text tLog = CreateTxt(bg.transform, "Txt_MissionLog", "База готова к дежурству.", new Vector2(0.5f, 0f), new Vector2(0f, 185f), new Vector2(1500f, 55f), 18, font, new Color(0.85f, 0.9f, 0.95f));

            Button bDeploy = CreateBtn(bg.transform, "Btn_Deploy", "⚔ ВЫЛЕТ НА ПЕРЕХВАТ (SPACE)", new Vector2(0.5f, 0f), new Vector2(-280f, 75f), new Vector2(520f, 85f), font, new Color(0.9f, 0.2f, 0.2f));
            Button bScan = CreateBtn(bg.transform, "Btn_Scan", "📡 СКАНИРОВАТЬ РАДАР", new Vector2(0.5f, 0f), new Vector2(280f, 75f), new Vector2(520f, 85f), font, new Color(0.15f, 0.65f, 0.4f));

            GameObject qte = CreatePanel(bg.transform, "QtePanel", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 255f), new Vector2(1000f, 110f), new Color(0.7f, 0.15f, 0.15f));
            Text tQteTimer = CreateTxt(qte.transform, "Txt_QteTimer", "⏱ 3.5с", new Vector2(0f, 0.5f), new Vector2(30f, 0f), new Vector2(250f, 40f), 20, font, Color.white);
            Button bQte = CreateBtn(qte.transform, "Btn_Qte", "⚡ АКТИВИРОВАТЬ ЭНЕРГОЩИТ (ПРОБЕЛ)", new Vector2(1f, 0.5f), new Vector2(-30f, 0f), new Vector2(550f, 55f), font, new Color(0.15f, 0.8f, 0.35f));
            qte.SetActive(false);

            TerminalUI ui = cObj.AddComponent<TerminalUI>();
            SetRef(ui, "txtThreatTitle", tTitle);
            SetRef(ui, "txtThreatDetails", tDetails);
            SetRef(ui, "txtMechStatus", tStatus);
            SetRef(ui, "txtMissionLog", tLog);
            SetRef(ui, "txtAdaptation", tAdapt);
            SetRef(ui, "txtJaegerInfo", tJaeger);
            SetRef(ui, "btnDeploy", bDeploy);
            SetRef(ui, "txtDeployLabel", bDeploy.GetComponentInChildren<Text>());
            SetRef(ui, "btnScan", bScan);
            SetRef(ui, "btnBuildJaeger", bJaeger);
            SetRef(ui, "qtePanel", qte);
            SetRef(ui, "txtQteTimer", tQteTimer);
            SetRef(ui, "btnQteAction", bQte);

            // 3D Physical buttons on desk
            GameObject b3d1 = CreateCyl(grp.transform, "3D_Btn_Deploy", new Vector3(-1.0f, 0.88f, -4.8f), new Vector3(0.3f, 0.1f, 0.3f), Color.red);
            b3d1.AddComponent<ClickableObject>().onClick = () => DispatchSystem.Instance?.TryDeploy();
            b3d1.GetComponent<ClickableObject>().promptText = "Кнопка вылета: отправить робота в бой";

            GameObject b3d2 = CreateCyl(grp.transform, "3D_Btn_Scan", new Vector3(1.0f, 0.88f, -4.8f), new Vector3(0.3f, 0.1f, 0.3f), Color.green);
            b3d2.AddComponent<ClickableObject>().onClick = () => DispatchSystem.Instance?.SpawnThreat();
            b3d2.GetComponent<ClickableObject>().promptText = "Радар: сканировать сектор на наличие кайдзю";
        }

        private void CreateHUD()
        {
            GameObject grp = new GameObject("--- 08_MINIMAL_HUD ---");
            GameObject cObj = new GameObject("HUD_Canvas");
            cObj.transform.SetParent(grp.transform, false);

            Canvas c = cObj.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler sc = cObj.AddComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1920, 1080);
            cObj.AddComponent<GraphicRaycaster>();

            Font font = Font.CreateDynamicFontFromOSFont("Arial", 16) ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            Button bLeft = CreateBtn(cObj.transform, "Btn_Left", "◀ Диспетчерская\n(180° / A / Q)", new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(170f, 90f), font, new Color(0.12f, 0.28f, 0.48f, 0.85f));
            Button bRight = CreateBtn(cObj.transform, "Btn_Right", "Диспетчерская ▶\n(180° / D / E)", new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(170f, 90f), font, new Color(0.12f, 0.28f, 0.48f, 0.85f));

            GameObject top = CreatePanel(cObj.transform, "TopBar", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -15f), new Vector2(700f, 45f), new Color(0.08f, 0.1f, 0.14f, 0.85f));
            Text tRes = CreateTxt(top.transform, "Txt_Res", "КРЕДИТЫ: 500 ¢   |   СПЛАВ: 200 т.", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(680f, 40f), 20, font, Color.yellow);

            GameObject pBox = CreatePanel(cObj.transform, "PromptBox", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 25f), new Vector2(800f, 45f), new Color(0.04f, 0.05f, 0.07f, 0.75f));
            Text tPrompt = CreateTxt(pBox.transform, "Txt_Prompt", "", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(780f, 40f), 17, font, Color.white);

            // Workbench panel
            GameObject wb = CreatePanel(cObj.transform, "WorkbenchPanel", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(980f, 180f), new Color(0.08f, 0.11f, 0.16f, 0.96f));
            Text tWbTitle = CreateTxt(wb.transform, "Txt_WbTitle", "🔧 ВЕРСТАК ТО", new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(940f, 30f), 18, font, Color.yellow);
            Text tWbInfo = CreateTxt(wb.transform, "Txt_WbInfo", "Вращение A/D/W/S или ПКМ", new Vector2(0.5f, 1f), new Vector2(0f, -48f), new Vector2(940f, 25f), 15, font, Color.cyan);

            Button bYM = CreateBtn(wb.transform, "bYM", "◀ Y", new Vector2(0.5f, 0.5f), new Vector2(-200f, 10f), new Vector2(75f, 40f), font, new Color(0.2f, 0.35f, 0.55f));
            Button bYP = CreateBtn(wb.transform, "bYP", "Y ▶", new Vector2(0.5f, 0.5f), new Vector2(-115f, 10f), new Vector2(75f, 40f), font, new Color(0.2f, 0.35f, 0.55f));
            Button bPM = CreateBtn(wb.transform, "bPM", "▲ X", new Vector2(0.5f, 0.5f), new Vector2(-30f, 10f), new Vector2(75f, 40f), font, new Color(0.2f, 0.35f, 0.55f));
            Button bPP = CreateBtn(wb.transform, "bPP", "▼ X", new Vector2(0.5f, 0.5f), new Vector2(55f, 10f), new Vector2(75f, 40f), font, new Color(0.2f, 0.35f, 0.55f));
            Button bRst = CreateBtn(wb.transform, "bRst", "↺ Сброс", new Vector2(0.5f, 0.5f), new Vector2(150f, 10f), new Vector2(95f, 40f), font, new Color(0.35f, 0.35f, 0.4f));

            Button bExit = CreateBtn(wb.transform, "bExit", "❌ ВЫЙТИ В АНГАР (ESC)", new Vector2(0.5f, 0f), new Vector2(-320f, 35f), new Vector2(280f, 48f), font, new Color(0.85f, 0.2f, 0.2f));
            Button bSwap = CreateBtn(wb.transform, "bSwap", "🔄 СМЕНИТЬ МОДУЛЬ (T)", new Vector2(0.5f, 0f), new Vector2(0f, 35f), new Vector2(280f, 48f), font, new Color(0.2f, 0.55f, 0.85f));
            Button bFin = CreateBtn(wb.transform, "bFin", "⚡ ЗАВЕРШИТЬ (SPACE)", new Vector2(0.5f, 0f), new Vector2(320f, 35f), new Vector2(280f, 48f), font, new Color(0.15f, 0.7f, 0.3f));
            wb.SetActive(false);

            // Victory panel
            GameObject vic = CreatePanel(cObj.transform, "VictoryPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700f, 360f), new Color(0.06f, 0.1f, 0.18f, 0.98f));
            CreateTxt(vic.transform, "tWin", "🏆 ТРИУМФ! ЕГЕРЬ ПОСТРОЕН!", new Vector2(0.5f, 1f), new Vector2(0f, -45f), new Vector2(650f, 55f), 30, font, Color.yellow);
            CreateTxt(vic.transform, "tWinD", "Земля спасена от вторжения кайдзю!", new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(620f, 60f), 20, font, Color.white);
            Button bRest = CreateBtn(vic.transform, "bRest", "🔄 Играть снова", new Vector2(0.5f, 0f), new Vector2(0f, 45f), new Vector2(260f, 55f), font, new Color(0.2f, 0.75f, 0.35f));
            bRest.onClick.AddListener(() => UnityEngine.SceneManagement.SceneManager.LoadScene(0));
            vic.SetActive(false);

            HUDOverlay hud = cObj.AddComponent<HUDOverlay>();
            SetRef(hud, "txtResources", tRes);
            SetRef(hud, "btnTurnLeft", bLeft);
            SetRef(hud, "btnTurnRight", bRight);
            SetRef(hud, "txtHoverPrompt", tPrompt);
            SetRef(hud, "wbPanel", wb);
            SetRef(hud, "txtWbTitle", tWbTitle);
            SetRef(hud, "txtWbInfo", tWbInfo);
            SetRef(hud, "btnRotYM", bYM);
            SetRef(hud, "btnRotYP", bYP);
            SetRef(hud, "btnRotPM", bPM);
            SetRef(hud, "btnRotPP", bPP);
            SetRef(hud, "btnResetRotation", bRst);
            SetRef(hud, "btnFinish", bFin);
            SetRef(hud, "btnSwap", bSwap);
            SetRef(hud, "btnExit", bExit);
            SetRef(hud, "victoryPanel", vic);
        }

        #region Helpers

        private GameObject CreateBox(Transform parent, string name, Vector3 pos, Vector3 scale, Color c)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.transform.SetParent(parent, false);
            SetColor(go, c);
            return go;
        }

        private GameObject CreateCyl(Transform parent, string name, Vector3 pos, Vector3 scale, Color c)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.transform.SetParent(parent, false);
            SetColor(go, c);
            return go;
        }

        private GameObject CreatePanel(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size, Color c)
        {
            GameObject p = new GameObject(name);
            p.transform.SetParent(parent, false);
            RectTransform r = p.AddComponent<RectTransform>();
            r.anchorMin = aMin;
            r.anchorMax = aMax;
            r.pivot = aMin == aMax ? new Vector2(0.5f, 0.5f) : Vector2.zero;
            r.anchoredPosition = pos;
            r.sizeDelta = size;
            p.AddComponent<Image>().color = c;
            return p;
        }

        private Text CreateTxt(Transform parent, string name, string text, Vector2 a, Vector2 pos, Vector2 size, int fontSize, Font font, Color c)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform r = go.AddComponent<RectTransform>();
            r.anchorMin = a;
            r.anchorMax = a;
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = pos;
            r.sizeDelta = size;
            Text t = go.AddComponent<Text>();
            t.font = font;
            t.text = text;
            t.fontSize = fontSize;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = c;
            return t;
        }

        private Button CreateBtn(Transform parent, string name, string label, Vector2 a, Vector2 pos, Vector2 size, Font font, Color bg)
        {
            GameObject p = CreatePanel(parent, name, a, a, pos, size, bg);
            Button b = p.AddComponent<Button>();
            CreateTxt(p.transform, "Text", label, new Vector2(0.5f, 0.5f), Vector2.zero, size, 15, font, Color.white);
            return b;
        }

        private void SetColor(GameObject go, Color c)
        {
            Renderer r = go.GetComponent<Renderer>();
            if (r != null)
            {
                Material m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                m.color = c;
                r.sharedMaterial = m;
            }
        }

        private void SetRef(object target, string field, object val)
        {
            var f = target.GetType().GetField(field, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (f != null) f.SetValue(target, val);
        }

        #endregion
    }
}
