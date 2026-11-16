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
            Debug.Log("<color=cyan>[PrototypeSceneBuilder] Генерация чистой 3D сцены ангара (8 структурированных папок)...</color>");

            // 1. --- 01_MANAGERS ---
            GameObject mgrGroup = new GameObject("--- 01_MANAGERS ---");
            GameManager gm = mgrGroup.AddComponent<GameManager>();
            DispatchManager dispatch = mgrGroup.AddComponent<DispatchManager>();
            EnemyAdaptationSystem adaptation = mgrGroup.AddComponent<EnemyAdaptationSystem>();
            JaegerProjectManager jaeger = mgrGroup.AddComponent<JaegerProjectManager>();
            HangarAudio audio = mgrGroup.AddComponent<HangarAudio>();
            HangarAtmosphere atmosphere = mgrGroup.AddComponent<HangarAtmosphere>();
            GameAutomatedTester tester = mgrGroup.AddComponent<GameAutomatedTester>();

            // EventSystem внутри Managers
            GameObject esObj = new GameObject("EventSystem");
            esObj.transform.SetParent(mgrGroup.transform, false);
            esObj.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            esObj.AddComponent<StandaloneInputModule>();
#endif

            // 2. Setup Player Camera
            UnityEngine.Camera cam = SetupPlayerCamera(mgrGroup.transform);

            // 3. --- 02_LIGHTING_AND_VFX ---
            CreateLightingAndVFX(atmosphere);

            // 4. --- 03_HANGAR_ROOM ---
            CreateHangarRoom();

            // 5. --- 04_MECH_ATLANT ---
            MechInstance mechAtlant = CreateMechAtlant();

            // 6. --- 05_JAEGER_FRAME ---
            CreateGiantJaegerFrame(jaeger);

            // 7. --- 06_WORKBENCH ---
            RepairWorkbench workbench = CreateWorkbench();

            // 8. --- 07_DISPATCH_CONSOLE --- (Содержит Diegetic WorldSpace Canvas)
            DispatchUI dispatchUI = CreateDispatchConsole(cam, mechAtlant);

            // 9. --- 08_MINIMAL_HUD --- (Только ненавязчивый 2D HUD)
            CreateMinimalHUD(cam, workbench);

            Debug.Log("<color=green>[PrototypeSceneBuilder] Сцена успешно собрана в строгой модульной иерархии!</color>");
        }

        private UnityEngine.Camera SetupPlayerCamera(Transform parent = null)
        {
            UnityEngine.Camera cam = UnityEngine.Camera.main;
            if (cam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<UnityEngine.Camera>();
                cam.tag = "MainCamera";
            }

            if (parent != null)
            {
                cam.transform.SetParent(parent, false);
            }

            cam.transform.position = new Vector3(0f, 1.6f, 0f);
            cam.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.08f, 0.11f);

            if (cam.GetComponent<RoomCameraController>() == null)
            {
                cam.gameObject.AddComponent<RoomCameraController>();
            }

            if (cam.GetComponent<CameraShaker>() == null)
            {
                cam.gameObject.AddComponent<CameraShaker>();
            }

            if (cam.GetComponent<PlayerRaycaster>() == null)
            {
                cam.gameObject.AddComponent<PlayerRaycaster>();
            }

            return cam;
        }

        private void CreateLightingAndVFX(HangarAtmosphere atmosphere)
        {
            GameObject lightGroup = new GameObject("--- 02_LIGHTING_AND_VFX ---");

            // Основной верхний теплый свет ангара
            GameObject dirObj = new GameObject("Directional Light");
            dirObj.transform.SetParent(lightGroup.transform, false);
            Light dirLight = dirObj.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.intensity = 1.15f;
            dirLight.color = new Color(0.96f, 0.98f, 1f);
            dirObj.transform.rotation = Quaternion.Euler(50f, -25f, 0f);

            // Аварийный красный маяк тревоги на потолке
            GameObject alarmObj = new GameObject("Emergency_Alarm_Light");
            alarmObj.transform.SetParent(lightGroup.transform, false);
            alarmObj.transform.position = new Vector3(0f, 4.8f, 0f);
            Light alarmLight = alarmObj.AddComponent<Light>();
            alarmLight.type = LightType.Point;
            alarmLight.color = new Color(1f, 0.1f, 0.1f);
            alarmLight.range = 28f;
            alarmLight.intensity = 0f;

            if (atmosphere != null)
            {
                SetField(typeof(HangarAtmosphere), atmosphere, "mainDirectionalLight", dirLight);
                SetField(typeof(HangarAtmosphere), atmosphere, "redAlarmLight", alarmLight);
            }

            // Рабочий прожектор над верстаком
            GameObject spotWb = new GameObject("Spotlight_Workbench");
            spotWb.transform.SetParent(lightGroup.transform, false);
            spotWb.transform.position = new Vector3(0f, 3.2f, 1.8f);
            spotWb.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
            Light lWb = spotWb.AddComponent<Light>();
            lWb.type = LightType.Spot;
            lWb.range = 7f;
            lWb.spotAngle = 60f;
            lWb.intensity = 2.2f;
            lWb.color = new Color(1f, 0.95f, 0.85f);

            // Прожектор подсветки боевого меха Атлант
            GameObject spotMech = new GameObject("Spotlight_Mech_Atlant");
            spotMech.transform.SetParent(lightGroup.transform, false);
            spotMech.transform.position = new Vector3(0f, 4.5f, 3.2f);
            spotMech.transform.rotation = Quaternion.Euler(35f, 0f, 0f);
            Light lMech = spotMech.AddComponent<Light>();
            lMech.type = LightType.Spot;
            lMech.range = 10f;
            lMech.spotAngle = 65f;
            lMech.intensity = 2.0f;
            lMech.color = new Color(0.85f, 0.92f, 1f);

            // Прожектор монтажного стапеля Егеря
            GameObject spotJaeger = new GameObject("Spotlight_Jaeger_Bay");
            spotJaeger.transform.SetParent(lightGroup.transform, false);
            spotJaeger.transform.position = new Vector3(3.8f, 4.8f, 2.8f);
            spotJaeger.transform.rotation = Quaternion.Euler(35f, 25f, 0f);
            Light lJaeger = spotJaeger.AddComponent<Light>();
            lJaeger.type = LightType.Spot;
            lJaeger.range = 11f;
            lJaeger.spotAngle = 70f;
            lJaeger.intensity = 2.4f;
            lJaeger.color = new Color(1f, 0.88f, 0.7f);
        }

        private void CreateHangarRoom()
        {
            GameObject roomGroup = new GameObject("--- 03_HANGAR_ROOM ---");

            // 1. Пол с разметкой
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor_Reinforced_Slab";
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(18f, 1f, 18f);
            floor.transform.SetParent(roomGroup.transform, false);
            SetColor(floor, new Color(0.18f, 0.2f, 0.23f));

            // Полосы безопасности на полу (желтые полосы)
            GameObject stripeL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripeL.name = "Safety_Stripe_Left";
            stripeL.transform.position = new Vector3(-2.2f, 0.01f, 3.5f);
            stripeL.transform.localScale = new Vector3(0.2f, 0.02f, 7f);
            stripeL.transform.SetParent(roomGroup.transform, false);
            SetColor(stripeL, new Color(0.9f, 0.75f, 0.1f));

            GameObject stripeR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripeR.name = "Safety_Stripe_Right";
            stripeR.transform.position = new Vector3(2.2f, 0.01f, 3.5f);
            stripeR.transform.localScale = new Vector3(0.2f, 0.02f, 7f);
            stripeR.transform.SetParent(roomGroup.transform, false);
            SetColor(stripeR, new Color(0.9f, 0.75f, 0.1f));

            // 2. Потолок
            GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Ceiling_Industrial_Girders";
            ceiling.transform.position = new Vector3(0f, 5.5f, 0f);
            ceiling.transform.localScale = new Vector3(18f, 0.5f, 18f);
            ceiling.transform.SetParent(roomGroup.transform, false);
            SetColor(ceiling, new Color(0.12f, 0.14f, 0.16f));

            // Подвесные мостовые краны под потолком
            GameObject craneRail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            craneRail.name = "Gantry_Crane_Beam";
            craneRail.transform.position = new Vector3(0f, 4.8f, 2.5f);
            craneRail.transform.localScale = new Vector3(17f, 0.4f, 0.8f);
            craneRail.transform.SetParent(roomGroup.transform, false);
            SetColor(craneRail, new Color(0.85f, 0.65f, 0.1f));

            // 3. Стены
            // Задняя стена (за мехами)
            GameObject wallBack = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallBack.name = "Wall_Back_Gate";
            wallBack.transform.position = new Vector3(0f, 2.5f, 8.5f);
            wallBack.transform.localScale = new Vector3(18f, 6f, 0.6f);
            wallBack.transform.SetParent(roomGroup.transform, false);
            SetColor(wallBack, new Color(0.14f, 0.16f, 0.19f));

            // Ворота ангара (акцентные створки)
            GameObject blastDoors = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blastDoors.name = "Blast_Gate_Panels";
            blastDoors.transform.position = new Vector3(0f, 2.4f, 8.25f);
            blastDoors.transform.localScale = new Vector3(7.5f, 4.8f, 0.1f);
            blastDoors.transform.SetParent(roomGroup.transform, false);
            SetColor(blastDoors, new Color(0.22f, 0.25f, 0.28f));

            // Передняя стена (за диспетчерской)
            GameObject wallFront = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallFront.name = "Wall_Front_Command";
            wallFront.transform.position = new Vector3(0f, 2.5f, -8.5f);
            wallFront.transform.localScale = new Vector3(18f, 6f, 0.6f);
            wallFront.transform.SetParent(roomGroup.transform, false);
            SetColor(wallFront, new Color(0.12f, 0.14f, 0.17f));

            // Боковые стены
            GameObject wallLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallLeft.name = "Wall_Left";
            wallLeft.transform.position = new Vector3(-8.8f, 2.5f, 0f);
            wallLeft.transform.localScale = new Vector3(0.6f, 6f, 18f);
            wallLeft.transform.SetParent(roomGroup.transform, false);
            SetColor(wallLeft, new Color(0.15f, 0.17f, 0.2f));

            GameObject wallRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallRight.name = "Wall_Right";
            wallRight.transform.position = new Vector3(8.8f, 2.5f, 0f);
            wallRight.transform.localScale = new Vector3(0.6f, 6f, 18f);
            wallRight.transform.SetParent(roomGroup.transform, false);
            SetColor(wallRight, new Color(0.15f, 0.17f, 0.2f));

            // Колонны поддержки
            CreatePillar(roomGroup.transform, new Vector3(-5.5f, 2.5f, 5.5f));
            CreatePillar(roomGroup.transform, new Vector3(5.5f, 2.5f, 5.5f));
            CreatePillar(roomGroup.transform, new Vector3(-5.5f, 2.5f, -5.5f));
            CreatePillar(roomGroup.transform, new Vector3(5.5f, 2.5f, -5.5f));
        }

        private void CreatePillar(Transform parent, Vector3 pos)
        {
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillar.name = "Support_Pillar";
            pillar.transform.position = pos;
            pillar.transform.localScale = new Vector3(0.8f, 5.5f, 0.8f);
            pillar.transform.SetParent(parent, false);
            SetColor(pillar, new Color(0.24f, 0.26f, 0.3f));
        }

        private MechInstance CreateMechAtlant()
        {
            GameObject mechRoot = new GameObject("--- 04_MECH_ATLANT ---");
            mechRoot.transform.position = new Vector3(0f, 0f, 5.5f);

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

            // 3. Левая рука — Энерго-щит
            GameObject leftArm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftArm.name = "LeftArm_Shield";
            leftArm.transform.position = new Vector3(-1.3f, 2.7f, 5.3f);
            leftArm.transform.localScale = new Vector3(0.6f, 1.6f, 0.7f);
            leftArm.transform.SetParent(mechRoot.transform);
            SetColor(leftArm, new Color(0.2f, 0.7f, 0.9f));
            SetupMechPart(leftArm, "Тяжелый Щит 'Бастион'", PartSlot.LeftArm, DamageType.Physical, DamageType.Physical, 10f, 80f);

            // 4. Правая рука — Плазменное орудие
            GameObject rightArm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rightArm.name = "RightArm_PlasmaCannon";
            rightArm.transform.position = new Vector3(1.3f, 2.7f, 5.3f);
            rightArm.transform.localScale = new Vector3(0.5f, 1.4f, 0.5f);
            rightArm.transform.SetParent(mechRoot.transform);
            SetColor(rightArm, new Color(0.9f, 0.3f, 0.1f));
            SetupMechPart(rightArm, "Плазменная Пушка 'Сверхновая'", PartSlot.RightArm, DamageType.Plasma, DamageType.Thermal, 60f, 20f);

            // 5. Ноги / Шасси
            GameObject legs = GameObject.CreatePrimitive(PrimitiveType.Cube);
            legs.name = "Legs_Chassis";
            legs.transform.position = new Vector3(0f, 0.9f, 5.5f);
            legs.transform.localScale = new Vector3(1.4f, 1.8f, 1.1f);
            legs.transform.SetParent(mechRoot.transform);
            SetColor(legs, new Color(0.25f, 0.28f, 0.32f));
            SetupMechPart(legs, "Гидравлическое Шасси 'Титан'", PartSlot.Legs, DamageType.Physical, DamageType.Physical, 15f, 50f);

            // Строительные леса вокруг Атланта
            GameObject scaffoldL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            scaffoldL.name = "Scaffold_Maintenance_L";
            scaffoldL.transform.position = new Vector3(-2.2f, 2.2f, 5.5f);
            scaffoldL.transform.localScale = new Vector3(0.2f, 4.4f, 2.4f);
            scaffoldL.transform.SetParent(mechRoot.transform);
            SetColor(scaffoldL, new Color(0.85f, 0.65f, 0.1f));

            GameObject scaffoldR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            scaffoldR.name = "Scaffold_Maintenance_R";
            scaffoldR.transform.position = new Vector3(2.2f, 2.2f, 5.5f);
            scaffoldR.transform.localScale = new Vector3(0.2f, 4.4f, 2.4f);
            scaffoldR.transform.SetParent(mechRoot.transform);
            SetColor(scaffoldR, new Color(0.85f, 0.65f, 0.1f));

            return mech;
        }

        private void SetupMechPart(GameObject obj, string partName, PartSlot slot, DamageType resType, DamageType atkType, float atkPow, float defPow)
        {
            MechPartInstance part = obj.AddComponent<MechPartInstance>();

            MechPartData data = ScriptableObject.CreateInstance<MechPartData>();
            data.partId = obj.name;
            data.partName = partName;
            data.slot = slot;
            data.resistanceType = resType;
            data.attackType = atkType;
            data.attackPower = atkPow;
            data.defensePower = defPow;
            data.maxDurability = 100f;
            data.repairCreditCost = 40;
            data.repairAlloyCost = 20;

            part.Initialize(data, slot, 100f);
        }

        private void CreateGiantJaegerFrame(JaegerProjectManager jaegerMgr)
        {
            GameObject frameRoot = new GameObject("--- 05_JAEGER_FRAME ---");
            frameRoot.transform.position = new Vector3(4.8f, 0f, 4.8f);

            // Массивные строительные леса и вышки стапеля
            GameObject towerL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            towerL.name = "Scaffold_Tower_L";
            towerL.transform.position = new Vector3(3.2f, 3.5f, 4.8f);
            towerL.transform.localScale = new Vector3(0.35f, 7f, 3.5f);
            towerL.transform.SetParent(frameRoot.transform);
            SetColor(towerL, new Color(0.9f, 0.7f, 0.15f));

            GameObject towerR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            towerR.name = "Scaffold_Tower_R";
            towerR.transform.position = new Vector3(6.4f, 3.5f, 4.8f);
            towerR.transform.localScale = new Vector3(0.35f, 7f, 3.5f);
            towerR.transform.SetParent(frameRoot.transform);
            SetColor(towerR, new Color(0.9f, 0.7f, 0.15f));

            // Базовый скелет Егеря (Ноги и тазовый узел)
            GameObject basePelvis = GameObject.CreatePrimitive(PrimitiveType.Cube);
            basePelvis.name = "Jaeger_Skeletal_Pelvis";
            basePelvis.transform.position = new Vector3(4.8f, 2.2f, 4.8f);
            basePelvis.transform.localScale = new Vector3(1.8f, 0.8f, 1.2f);
            basePelvis.transform.SetParent(frameRoot.transform);
            SetColor(basePelvis, new Color(0.2f, 0.22f, 0.25f));

            GameObject legL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            legL.name = "Jaeger_Skeletal_Leg_L";
            legL.transform.position = new Vector3(4.1f, 1.1f, 4.8f);
            legL.transform.localScale = new Vector3(0.7f, 2.2f, 0.8f);
            legL.transform.SetParent(frameRoot.transform);
            SetColor(legL, new Color(0.18f, 0.2f, 0.22f));

            GameObject legR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            legR.name = "Jaeger_Skeletal_Leg_R";
            legR.transform.position = new Vector3(5.5f, 1.1f, 4.8f);
            legR.transform.localScale = new Vector3(0.7f, 2.2f, 0.8f);
            legR.transform.SetParent(frameRoot.transform);
            SetColor(legR, new Color(0.18f, 0.2f, 0.22f));

            // Позвоночный столб
            GameObject spine = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spine.name = "Jaeger_Spine_Hydraulics";
            spine.transform.position = new Vector3(4.8f, 3.4f, 4.8f);
            spine.transform.localScale = new Vector3(0.4f, 1.4f, 0.4f);
            spine.transform.SetParent(frameRoot.transform);
            SetColor(spine, new Color(0.3f, 0.32f, 0.35f));

            // 4 МОДУЛЯ, КОТОРЫЕ МОНТИРУЮТСЯ ПО ХОДУ ИГРЫ:

            // 1. Термоядерный Реактор (Этап 1 - 25%)
            GameObject modReactor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            modReactor.name = "Mounted_Module_1_ReactorCore";
            modReactor.transform.position = new Vector3(4.8f, 3.4f, 4.5f);
            modReactor.transform.localScale = new Vector3(0.85f, 0.35f, 0.85f);
            modReactor.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            modReactor.transform.SetParent(frameRoot.transform);
            SetColor(modReactor, new Color(0.1f, 0.95f, 1f)); // Неоновый циан
            modReactor.SetActive(false);

            // 2. Титановый Силовой Экзоскелет (Этап 2 - 50%)
            GameObject modTorso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            modTorso.name = "Mounted_Module_2_TorsoArmor";
            modTorso.transform.position = new Vector3(4.8f, 3.5f, 4.8f);
            modTorso.transform.localScale = new Vector3(2.2f, 2.0f, 1.6f);
            modTorso.transform.SetParent(frameRoot.transform);
            SetColor(modTorso, new Color(0.25f, 0.45f, 0.65f)); // Тяжелая броня
            modTorso.SetActive(false);

            // 3. Тяжелый Ионный Излучатель (Этап 3 - 75%)
            GameObject modCannon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            modCannon.name = "Mounted_Module_3_IonCannon";
            modCannon.transform.position = new Vector3(6.3f, 4.2f, 4.7f);
            modCannon.transform.localScale = new Vector3(0.45f, 2.2f, 0.45f);
            modCannon.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            modCannon.transform.SetParent(frameRoot.transform);
            SetColor(modCannon, new Color(0.9f, 0.45f, 0.1f)); // Осадное орудие
            modCannon.SetActive(false);

            // 4. Нейро-Командный Шлем 'Апокалипсис' (Этап 4 - 100%)
            GameObject modHelm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            modHelm.name = "Mounted_Module_4_NeuralHelm";
            modHelm.transform.position = new Vector3(4.8f, 5.0f, 4.6f);
            modHelm.transform.localScale = new Vector3(0.9f, 0.7f, 0.9f);
            modHelm.transform.SetParent(frameRoot.transform);
            SetColor(modHelm, new Color(0.95f, 0.75f, 0.1f)); // Золотистый шлем
            modHelm.SetActive(false);

            // Передаем маркеры менеджеру Егеря
            Transform[] markers = new Transform[] { modReactor.transform, modTorso.transform, modCannon.transform, modHelm.transform };
            if (jaegerMgr != null)
            {
                jaegerMgr.SetStageVisualMarkers(markers, frameRoot);
            }
        }

        private RepairWorkbench CreateWorkbench()
        {
            GameObject wbGroup = new GameObject("--- 06_WORKBENCH ---");
            wbGroup.transform.position = new Vector3(0f, 0f, 2.2f);

            // Столешница
            GameObject tabletop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tabletop.name = "Workbench_Tabletop";
            tabletop.transform.position = new Vector3(0f, 0.76f, 2.2f);
            tabletop.transform.localScale = new Vector3(2.6f, 0.12f, 1.3f);
            tabletop.transform.SetParent(wbGroup.transform);
            SetColor(tabletop, new Color(0.22f, 0.24f, 0.27f));

            // Точка крепления детали
            GameObject mountObj = new GameObject("Part_Mount_Point");
            mountObj.transform.position = new Vector3(0f, 0.83f, 2.2f);
            mountObj.transform.SetParent(wbGroup.transform);

            // Тиски слева на столе
            GameObject vice = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vice.name = "Table_Vice";
            vice.transform.position = new Vector3(-1.1f, 0.88f, 2.2f);
            vice.transform.localScale = new Vector3(0.25f, 0.2f, 0.3f);
            vice.transform.SetParent(wbGroup.transform);
            SetColor(vice, new Color(0.7f, 0.2f, 0.2f));

            // Лампа на гибкой стойке
            GameObject lamp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lamp.name = "Halogen_Desk_Lamp";
            lamp.transform.position = new Vector3(1.1f, 1.15f, 2.5f);
            lamp.transform.localScale = new Vector3(0.08f, 0.6f, 0.08f);
            lamp.transform.SetParent(wbGroup.transform);
            SetColor(lamp, new Color(0.8f, 0.8f, 0.2f));

            // Ножки стола
            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    leg.name = $"Table_Leg_{x}_{z}";
                    leg.transform.position = new Vector3(x * 1.15f, 0.35f, 2.2f + z * 0.5f);
                    leg.transform.localScale = new Vector3(0.14f, 0.7f, 0.14f);
                    leg.transform.SetParent(wbGroup.transform);
                    SetColor(leg, Color.black);
                }
            }

            RepairWorkbench workbench = wbGroup.AddComponent<RepairWorkbench>();
            SetField(typeof(RepairWorkbench), workbench, "partMountPoint", mountObj.transform);

            wbGroup.AddComponent<BoltMiniGame>();
            wbGroup.AddComponent<WeldingMiniGame>();
            wbGroup.AddComponent<WiringMiniGame>();

            return workbench;
        }

        private DispatchUI CreateDispatchConsole(UnityEngine.Camera cam, MechInstance mech)
        {
            GameObject consoleGroup = new GameObject("--- 07_DISPATCH_CONSOLE ---");
            consoleGroup.transform.position = new Vector3(0f, 0f, -5.2f);
            consoleGroup.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            // Стол диспетчера
            GameObject desk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            desk.name = "Console_Desk";
            desk.transform.position = new Vector3(0f, 0.75f, -5.2f);
            desk.transform.localScale = new Vector3(3.6f, 0.15f, 1.5f);
            desk.transform.SetParent(consoleGroup.transform);
            SetColor(desk, new Color(0.18f, 0.2f, 0.24f));

            // Кресло оператора
            GameObject chair = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chair.name = "Operator_Chair";
            chair.transform.position = new Vector3(0f, 0.55f, -4.2f);
            chair.transform.localScale = new Vector3(0.7f, 0.8f, 0.7f);
            chair.transform.SetParent(consoleGroup.transform);
            SetColor(chair, new Color(0.1f, 0.1f, 0.12f));

            // Серверные стойки по бокам
            GameObject rackL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rackL.name = "Server_Rack_L";
            rackL.transform.position = new Vector3(-2.2f, 1.4f, -5.3f);
            rackL.transform.localScale = new Vector3(0.8f, 2.8f, 0.9f);
            rackL.transform.SetParent(consoleGroup.transform);
            SetColor(rackL, new Color(0.12f, 0.14f, 0.18f));

            GameObject rackR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rackR.name = "Server_Rack_R";
            rackR.transform.position = new Vector3(2.2f, 1.4f, -5.3f);
            rackR.transform.localScale = new Vector3(0.8f, 2.8f, 0.9f);
            rackR.transform.SetParent(consoleGroup.transform);
            SetColor(rackR, new Color(0.12f, 0.14f, 0.18f));

            // Большой монитор тактического командного терминала
            GameObject monitor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            monitor.name = "Tactical_Radar_Screen";
            monitor.transform.position = new Vector3(0f, 1.95f, -5.45f);
            monitor.transform.localScale = new Vector3(3.0f, 1.7f, 0.12f);
            monitor.transform.SetParent(consoleGroup.transform);
            SetColor(monitor, new Color(0.04f, 0.08f, 0.12f));

            // 3D Физические кнопки на пульте
            GameObject btn3DDeploy = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            btn3DDeploy.name = "3D_Btn_Deploy";
            btn3DDeploy.transform.position = new Vector3(-1.0f, 0.88f, -4.8f);
            btn3DDeploy.transform.localScale = new Vector3(0.3f, 0.1f, 0.3f);
            btn3DDeploy.transform.SetParent(consoleGroup.transform);
            SetColor(btn3DDeploy, new Color(0.95f, 0.2f, 0.2f));
            Clickable3DObject clickDeploy = btn3DDeploy.AddComponent<Clickable3DObject>();
            clickDeploy.ActionType = ClickableActionType.DeployMech;

            GameObject btn3DScan = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            btn3DScan.name = "3D_Btn_Scan";
            btn3DScan.transform.position = new Vector3(1.0f, 0.88f, -4.8f);
            btn3DScan.transform.localScale = new Vector3(0.3f, 0.1f, 0.3f);
            btn3DScan.transform.SetParent(consoleGroup.transform);
            SetColor(btn3DScan, new Color(0.2f, 0.85f, 0.35f));
            Clickable3DObject clickScan = btn3DScan.AddComponent<Clickable3DObject>();
            clickScan.ActionType = ClickableActionType.ScanRadar;

            // --- DIEGETIC WORLDSPACE CANVAS НА ЭКРАНЕ ТЕРМИНАЛА ---
            GameObject termCanvasObj = new GameObject("Terminal_WorldSpace_Canvas");
            termCanvasObj.transform.SetParent(monitor.transform, false);
            // Лицевая сторона монитора смотрит на игрока (внутренний local -Z монитора)
            termCanvasObj.transform.localPosition = new Vector3(0f, 0f, -0.65f);
            termCanvasObj.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            termCanvasObj.transform.localScale = new Vector3(0.0017f, 0.0017f, 1f);

            Canvas termCanvas = termCanvasObj.AddComponent<Canvas>();
            termCanvas.renderMode = RenderMode.WorldSpace;
            termCanvas.worldCamera = cam;

            RectTransform termRect = termCanvasObj.GetComponent<RectTransform>();
            termRect.sizeDelta = new Vector2(1650f, 950f);

            termCanvasObj.AddComponent<GraphicRaycaster>();

            Font defaultFont = Font.CreateDynamicFontFromOSFont("Arial", 16) ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Фоновая панель терминала
            GameObject bgPanel = new GameObject("Terminal_Screen_BG");
            bgPanel.transform.SetParent(termCanvasObj.transform, false);
            RectTransform bgRect = bgPanel.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            Image bgImg = bgPanel.AddComponent<Image>();
            bgImg.color = new Color(0.05f, 0.09f, 0.14f, 0.98f);

            // Шапка терминала
            GameObject txtHeader = CreateAnchoredText(
                bgPanel.transform, "Txt_TerminalStatus", 
                "🚨 ОБОРОННЫЙ КОМАНДНЫЙ ЦЕНТР // СЕКТОР-7 🚨", 
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), 
                new Vector2(0f, -35f), new Vector2(1500f, 50f), 
                28, defaultFont, Color.cyan
            );

            // ЛЕВАЯ КОЛОНКА: Радар, Угроза кайдзю, Адаптация
            GameObject txtThreatName = CreateAnchoredText(
                bgPanel.transform, "Txt_ThreatName", 
                "РАДАР СКАНИРУЕТ ГОРОДСКОЙ ПЕРИМЕТР", 
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), 
                new Vector2(40f, -100f), new Vector2(750f, 45f), 
                22, defaultFont, Color.red
            );

            GameObject txtThreatDetails = CreateAnchoredText(
                bgPanel.transform, "Txt_ThreatDetails", 
                "Ожидание приближения кайдзю... Держите роботов в полной боеготовности.", 
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), 
                new Vector2(40f, -165f), new Vector2(750f, 75f), 
                18, defaultFont, Color.white
            );

            GameObject txtAdaptAlert = CreateAnchoredText(
                bgPanel.transform, "Txt_AdaptationAlert", 
                "", 
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), 
                new Vector2(40f, -255f), new Vector2(750f, 50f), 
                17, defaultFont, new Color(1f, 0.55f, 0.15f)
            );

            // Радарная круговая сетка
            GameObject radarDeco = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            radarDeco.name = "Radar_Display_Circle";
            radarDeco.transform.position = new Vector3(-1.0f, 1.45f, -5.35f);
            radarDeco.transform.localScale = new Vector3(0.55f, 0.02f, 0.55f);
            radarDeco.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            radarDeco.transform.SetParent(consoleGroup.transform);
            SetColor(radarDeco, new Color(0.05f, 0.4f, 0.25f));

            // ПРАВАЯ КОЛОНКА: Статус робота, Проект Егерь, Логи боя
            GameObject txtMechStatus = CreateAnchoredText(
                bgPanel.transform, "Txt_MechStatusAlert", 
                "✔ СТАТУС: БОЕГОТОВЕН (Все 5 модулей смонтированы)", 
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), 
                new Vector2(-40f, -100f), new Vector2(750f, 45f), 
                20, defaultFont, new Color(0.2f, 1f, 0.4f)
            );

            // Секция проекта Егерь
            GameObject txtJaegerInfo = CreateAnchoredText(
                bgPanel.transform, "Txt_JaegerProjectInfo", 
                "ПРОЕКТ 'ЕГЕРЬ' (0/4): Термоядерный Реактор 'Гиперион' [150¢, 60т]", 
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), 
                new Vector2(-40f, -165f), new Vector2(750f, 60f), 
                19, defaultFont, Color.yellow
            );

            GameObject btnBuildJaeger = CreateAnchoredButton(
                bgPanel.transform, "Btn_BuildJaegerModule", 
                "🔨 НАЧАТЬ СБОРКУ МОДУЛЯ НА ВЕРСТАКЕ", 
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), 
                new Vector2(-40f, -245f), new Vector2(650f, 60f), 
                defaultFont, new Color(0.18f, 0.45f, 0.7f, 0.98f)
            );

            // Лог радиоэфира
            GameObject txtLog = CreateAnchoredText(
                bgPanel.transform, "Txt_MissionLog", 
                "Диспетчерская база готова. Выводите мехов на перехват и собирайте Егеря.", 
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), 
                new Vector2(0f, 185f), new Vector2(1500f, 60f), 
                18, defaultFont, new Color(0.85f, 0.9f, 0.95f)
            );

            // Тактическая панель QTE
            GameObject tacPanel = new GameObject("TacticalPromptPanel");
            tacPanel.transform.SetParent(bgPanel.transform, false);
            RectTransform tpRect = tacPanel.AddComponent<RectTransform>();
            tpRect.anchorMin = new Vector2(0.5f, 0f);
            tpRect.anchorMax = new Vector2(0.5f, 0f);
            tpRect.pivot = new Vector2(0.5f, 0f);
            tpRect.anchoredPosition = new Vector2(0f, 255f);
            tpRect.sizeDelta = new Vector2(1000f, 110f);

            Image tpBg = tacPanel.AddComponent<Image>();
            tpBg.color = new Color(0.7f, 0.15f, 0.15f, 0.95f);

            GameObject txtTacDir = CreateAnchoredText(
                tacPanel.transform, "Txt_TacDir", 
                "⚠ КРИТИЧЕСКИЙ МОМЕНТ БОЯ: Отдайте тактический приказ!", 
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), 
                new Vector2(0f, -20f), new Vector2(900f, 35f), 
                19, defaultFont, Color.yellow
            );

            GameObject txtTacTimer = CreateAnchoredText(
                tacPanel.transform, "Txt_TacTimer", 
                "⏱ 4.0с", 
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), 
                new Vector2(30f, 25f), new Vector2(250f, 40f), 
                20, defaultFont, Color.white
            );

            GameObject btnTacAct = CreateAnchoredButton(
                tacPanel.transform, "Btn_TacticalAction", 
                "⚡ АКТИВИРОВАТЬ ЭНЕРГОЩИТ (ПРОБЕЛ)", 
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), 
                new Vector2(-30f, 25f), new Vector2(550f, 55f), 
                defaultFont, new Color(0.15f, 0.8f, 0.35f, 0.98f)
            );
            tacPanel.SetActive(false);

            // КНОПКИ ВЫЛЕТА И СКАНИРОВАНИЯ НА ТЕРМИНАЛЕ
            GameObject btnDeploy = CreateAnchoredButton(
                bgPanel.transform, "Btn_Deploy", 
                "⚔ ВЫЛЕТ НА ПЕРЕХВАТ (SPACE)", 
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), 
                new Vector2(-280f, 75f), new Vector2(520f, 85f), 
                defaultFont, new Color(0.9f, 0.2f, 0.2f, 0.98f)
            );

            GameObject btnScan = CreateAnchoredButton(
                bgPanel.transform, "Btn_ScanRadar", 
                "📡 СКАНИРОВАТЬ РАДАР", 
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), 
                new Vector2(280f, 75f), new Vector2(520f, 85f), 
                defaultFont, new Color(0.15f, 0.65f, 0.4f, 0.98f)
            );

            // Добавляем DispatchUI
            DispatchUI dispatchUI = termCanvasObj.AddComponent<DispatchUI>();

            // Связываем поля DispatchUI через рефлексию
            var tDui = typeof(DispatchUI);
            SetField(tDui, dispatchUI, "txtTerminalStatus", txtHeader.GetComponent<Text>());
            SetField(tDui, dispatchUI, "txtThreatName", txtThreatName.GetComponent<Text>());
            SetField(tDui, dispatchUI, "txtThreatDetails", txtThreatDetails.GetComponent<Text>());
            SetField(tDui, dispatchUI, "txtMechStatusAlert", txtMechStatus.GetComponent<Text>());
            SetField(tDui, dispatchUI, "tacticalPromptPanel", tacPanel);
            SetField(tDui, dispatchUI, "txtTacticalDirective", txtTacDir.GetComponent<Text>());
            SetField(tDui, dispatchUI, "txtTacticalTimer", txtTacTimer.GetComponent<Text>());
            SetField(tDui, dispatchUI, "btnTacticalAction", btnTacAct.GetComponent<Button>());
            SetField(tDui, dispatchUI, "btnDeployMech", btnDeploy.GetComponent<Button>());
            SetField(tDui, dispatchUI, "btnScanRadar", btnScan.GetComponent<Button>());
            SetField(tDui, dispatchUI, "txtDeployBtnLabel", btnDeploy.GetComponentInChildren<Text>());
            SetField(tDui, dispatchUI, "txtJaegerProjectInfo", txtJaegerInfo.GetComponent<Text>());
            SetField(tDui, dispatchUI, "btnBuildJaegerModule", btnBuildJaeger.GetComponent<Button>());
            SetField(tDui, dispatchUI, "txtMissionLog", txtLog.GetComponent<Text>());
            SetField(tDui, dispatchUI, "txtAdaptationAlert", txtAdaptAlert.GetComponent<Text>());
            SetField(tDui, dispatchUI, "targetMech", mech);

            return dispatchUI;
        }

        private void CreateMinimalHUD(UnityEngine.Camera cam, RepairWorkbench workbench)
        {
            GameObject hudRoot = new GameObject("--- 08_MINIMAL_HUD ---");

            GameObject canvasObj = new GameObject("HUD_Canvas");
            canvasObj.transform.SetParent(hudRoot.transform, false);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            Font defaultFont = Font.CreateDynamicFontFromOSFont("Arial", 16) ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            HUDManager hud = canvasObj.AddComponent<HUDManager>();

            // 1. Кнопка поворота влево "◀ Диспетчерская (A / Q)"
            GameObject btnLeft = CreateAnchoredButton(
                canvasObj.transform, "Btn_TurnLeft", 
                "◀ Диспетчерская\n(180° / A / Q)", 
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), 
                new Vector2(20f, 0f), new Vector2(170f, 90f), 
                defaultFont, new Color(0.12f, 0.28f, 0.48f, 0.85f)
            );

            // 2. Кнопка поворота вправо "Диспетчерская ▶ (D / E)"
            GameObject btnRight = CreateAnchoredButton(
                canvasObj.transform, "Btn_TurnRight", 
                "Диспетчерская ▶\n(180° / D / E)", 
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), 
                new Vector2(-20f, 0f), new Vector2(170f, 90f), 
                defaultFont, new Color(0.12f, 0.28f, 0.48f, 0.85f)
            );

            // 3. Верхняя плашка ресурсов и сектора
            GameObject topBar = new GameObject("TopBar_Resources");
            topBar.transform.SetParent(canvasObj.transform, false);
            RectTransform tbRect = topBar.AddComponent<RectTransform>();
            tbRect.anchorMin = new Vector2(0.5f, 1f);
            tbRect.anchorMax = new Vector2(0.5f, 1f);
            tbRect.pivot = new Vector2(0.5f, 1f);
            tbRect.anchoredPosition = new Vector2(0f, -15f);
            tbRect.sizeDelta = new Vector2(700f, 45f);
            Image tbImg = topBar.AddComponent<Image>();
            tbImg.color = new Color(0.08f, 0.1f, 0.14f, 0.85f);

            GameObject txtRes = CreateAnchoredText(
                topBar.transform, "Txt_Resources", 
                "КРЕДИТЫ: 500 ¢   |   СПЛАВ: 200 т.", 
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), 
                Vector2.zero, Vector2.zero, 
                20, defaultFont, Color.yellow
            );

            // 4. Нижний ховер-промпт
            GameObject promptBox = new GameObject("Prompt_Box");
            promptBox.transform.SetParent(canvasObj.transform, false);
            RectTransform pbRect = promptBox.AddComponent<RectTransform>();
            pbRect.anchorMin = new Vector2(0.5f, 0f);
            pbRect.anchorMax = new Vector2(0.5f, 0f);
            pbRect.pivot = new Vector2(0.5f, 0f);
            pbRect.anchoredPosition = new Vector2(0f, 25f);
            pbRect.sizeDelta = new Vector2(800f, 45f);
            Image pbImg = promptBox.AddComponent<Image>();
            pbImg.color = new Color(0.04f, 0.05f, 0.07f, 0.75f);

            GameObject txtPrompt = CreateAnchoredText(
                promptBox.transform, "Txt_HoverPrompt", 
                "", 
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), 
                Vector2.zero, Vector2.zero, 
                17, defaultFont, Color.white
            );

            // 5. ПАНЕЛЬ ВЕРСТАКА (С 2-ОСЕВЫМ ВРАЩЕНИЕМ ДЕТАЛИ)
            GameObject wbPanel = new GameObject("Workbench_Panel");
            wbPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform wbRect = wbPanel.AddComponent<RectTransform>();
            wbRect.anchorMin = new Vector2(0.5f, 0f);
            wbRect.anchorMax = new Vector2(0.5f, 0f);
            wbRect.pivot = new Vector2(0.5f, 0f);
            wbRect.anchoredPosition = new Vector2(0f, 20f);
            wbRect.sizeDelta = new Vector2(980f, 180f);
            Image wbBg = wbPanel.AddComponent<Image>();
            wbBg.color = new Color(0.08f, 0.11f, 0.16f, 0.96f);

            GameObject txtWbTitle = CreateAnchoredText(
                wbPanel.transform, "Txt_WbTitle", 
                "🔧 РЕЖИМ ВЕРСТАКА: Осмотрите деталь и устраните дефекты", 
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), 
                new Vector2(0f, -20f), new Vector2(940f, 30f), 
                18, defaultFont, Color.yellow
            );

            GameObject txtModInfo = CreateAnchoredText(
                wbPanel.transform, "Txt_ModuleInfo", 
                "Клавиши: A/D/W/S или зажмите ПКМ для свободного вращения по 2 осям", 
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), 
                new Vector2(0f, -48f), new Vector2(940f, 25f), 
                15, defaultFont, Color.cyan
            );

            // Кнопки 2-осевого вращения детали
            GameObject btnRotYM = CreateAnchoredButton(wbPanel.transform, "Btn_RotYawMinus", "◀ Y", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-200f, 10f), new Vector2(75f, 40f), defaultFont, new Color(0.2f, 0.35f, 0.55f));
            GameObject btnRotYP = CreateAnchoredButton(wbPanel.transform, "Btn_RotYawPlus", "Y ▶", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-115f, 10f), new Vector2(75f, 40f), defaultFont, new Color(0.2f, 0.35f, 0.55f));
            GameObject btnRotPM = CreateAnchoredButton(wbPanel.transform, "Btn_RotPitchMinus", "▲ X", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-30f, 10f), new Vector2(75f, 40f), defaultFont, new Color(0.2f, 0.35f, 0.55f));
            GameObject btnRotPP = CreateAnchoredButton(wbPanel.transform, "Btn_RotPitchPlus", "▼ X", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(55f, 10f), new Vector2(75f, 40f), defaultFont, new Color(0.2f, 0.35f, 0.55f));
            GameObject btnRotRst = CreateAnchoredButton(wbPanel.transform, "Btn_RotReset", "↺ Сброс", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(150f, 10f), new Vector2(95f, 40f), defaultFont, new Color(0.35f, 0.35f, 0.4f));

            // Нижние кнопки действий
            GameObject btnExitWb = CreateAnchoredButton(wbPanel.transform, "Btn_ExitWorkbench", "❌ ВЫЙТИ В АНГАР (ESC)", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-320f, 35f), new Vector2(280f, 48f), defaultFont, new Color(0.85f, 0.2f, 0.2f, 0.95f));
            GameObject btnSwap = CreateAnchoredButton(wbPanel.transform, "Btn_SwapModule", "🔄 СМЕНИТЬ МОДУЛЬ (T)", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 35f), new Vector2(280f, 48f), defaultFont, new Color(0.2f, 0.55f, 0.85f, 0.95f));
            GameObject btnInstant = CreateAnchoredButton(wbPanel.transform, "Btn_InstantRepair", "⚡ БЫСТРЫЙ РЕМОНТ (SPACE)", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(320f, 35f), new Vector2(280f, 48f), defaultFont, new Color(0.15f, 0.7f, 0.3f, 0.95f));
            wbPanel.SetActive(false);

            // 6. Экран победы
            GameObject victoryPanel = new GameObject("Victory_Panel");
            victoryPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform vRect = victoryPanel.AddComponent<RectTransform>();
            vRect.anchorMin = new Vector2(0.5f, 0.5f);
            vRect.anchorMax = new Vector2(0.5f, 0.5f);
            vRect.pivot = new Vector2(0.5f, 0.5f);
            vRect.anchoredPosition = Vector2.zero;
            vRect.sizeDelta = new Vector2(700f, 360f);
            Image vBg = victoryPanel.AddComponent<Image>();
            vBg.color = new Color(0.06f, 0.1f, 0.18f, 0.98f);

            CreateAnchoredText(victoryPanel.transform, "Txt_WinTitle", "🏆 ТРИУМФ! ЕГЕРЬ 'ТИП-10' ПОСТРОЕН!", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -45f), new Vector2(650f, 55f), 30, defaultFont, Color.yellow);
            CreateAnchoredText(victoryPanel.transform, "Txt_WinDesc", "Все 4 ключевых узла смонтированы на стапеле.\nУгроза кайдзю полностью ликвидирована!", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(620f, 80f), 20, defaultFont, Color.white);
            GameObject btnRestart = CreateAnchoredButton(victoryPanel.transform, "Btn_Restart", "🔄 Играть снова", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 45f), new Vector2(260f, 55f), defaultFont, new Color(0.2f, 0.75f, 0.35f, 0.98f));
            btnRestart.GetComponent<Button>().onClick.AddListener(() => UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex));
            victoryPanel.SetActive(false);

            // Связывание ссылок в HUDManager
            var tHud = typeof(HUDManager);
            SetField(tHud, hud, "btnLookLeft", btnLeft.GetComponent<Button>());
            SetField(tHud, hud, "btnLookRight", btnRight.GetComponent<Button>());
            SetField(tHud, hud, "txtLookLeft", btnLeft.GetComponentInChildren<Text>());
            SetField(tHud, hud, "txtLookRight", btnRight.GetComponentInChildren<Text>());
            SetField(tHud, hud, "workbenchPanel", wbPanel);
            SetField(tHud, hud, "btnExitWorkbench", btnExitWb.GetComponent<Button>());
            SetField(tHud, hud, "btnInstantRepair", btnInstant.GetComponent<Button>());
            SetField(tHud, hud, "btnSwapModule", btnSwap.GetComponent<Button>());
            SetField(tHud, hud, "txtWorkbenchTitle", txtWbTitle.GetComponent<Text>());
            SetField(tHud, hud, "txtModuleInfo", txtModInfo.GetComponent<Text>());
            SetField(tHud, hud, "btnRotateYawMinus", btnRotYM.GetComponent<Button>());
            SetField(tHud, hud, "btnRotateYawPlus", btnRotYP.GetComponent<Button>());
            SetField(tHud, hud, "btnRotatePitchMinus", btnRotPM.GetComponent<Button>());
            SetField(tHud, hud, "btnRotatePitchPlus", btnRotPP.GetComponent<Button>());
            SetField(tHud, hud, "btnResetRotation", btnRotRst.GetComponent<Button>());
            SetField(tHud, hud, "txtResources", txtRes.GetComponent<Text>());
            SetField(tHud, hud, "txtHoverPrompt", txtPrompt.GetComponent<Text>());
            SetField(tHud, hud, "victoryPanel", victoryPanel);
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
                if (Application.isPlaying)
                {
                    r.material.color = c;
                }
                else
                {
                    Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                    mat.color = c;
                    r.sharedMaterial = mat;
                }
            }
        }
    }
}
