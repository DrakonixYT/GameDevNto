using System.Collections.Generic;
using HangarGame.Armory;
using HangarGame.CameraControl;
using HangarGame.Core;
using HangarGame.Dispatch;
using HangarGame.Interaction;
using HangarGame.Mechs;
using HangarGame.Repair;
using HangarGame.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HangarGame.Prototype
{
    public class PrototypeSceneBuilder : MonoBehaviour
    {
        public void BuildCompleteHangarPrototype()
        {
            Transform c85 = null, c104 = null;
            GameObject hf = GameObject.Find("HANGARFULL");
            if (hf == null)
            {
                GameObject fullPrefab = LoadPrefab("Assets/Prefabs/Base/HANGARFULL.prefab");
                if (fullPrefab != null)
                {
                    hf = Instantiate(fullPrefab);
                    hf.name = "HANGARFULL";
                }
            }

            if (hf != null)
            {
                c85 = hf.transform.Find("Cube.085");
                c104 = hf.transform.Find("Cube.104");
            }
            if (c85 == null) c85 = GameObject.Find("Cube.085")?.transform;
            if (c104 == null) c104 = GameObject.Find("Cube.104")?.transform;

            Vector3 podiumPos = GetPodiumPos(c85);
            Vector3 screenCenter = GetScreenCenter(c104);
            Vector3 mid = Vector3.Lerp(podiumPos, screenCenter, 0.48f);
            Vector3 camPos = new Vector3(podiumPos.x, screenCenter.y + 0.15f, mid.z);
            Vector3 dispatchPos = new Vector3(screenCenter.x, screenCenter.y, screenCenter.z - 1.32f);

            UnityEngine.Camera cam = CreateCameraRig(camPos, dispatchPos, 180f, 0f);
            var managers = CreateCoreManagers();
            CreateLighting(podiumPos, screenCenter, managers.alarmLight);
            CreateHangarRoom(hf);
            CreateMechsHangar(podiumPos);

            Vector3 wbPos = camPos + new Vector3(-1.75f, -0.75f, 0.2f);
            CreateWorkbench(wbPos);
            CreateJaegerStage(wbPos + new Vector3(0f, 0f, 1.6f), managers.jaeger);

            var consoleGrp = new GameObject("--- 07_DISPATCH_CONSOLE ---");
            BuildDispatchMonitors(consoleGrp.transform, c104, screenCenter, cam);

            var hudGrp = new GameObject("--- 08_MINIMAL_HUD ---");
            BuildHUD(hudGrp.transform);
        }

        private static Vector3 GetPodiumPos(Transform c85)
        {
            if (c85 == null) return new Vector3(20.59f, 4.50f, -6.38f);
            var r = c85.GetComponent<Renderer>();
            return r != null ? new Vector3(r.bounds.center.x, r.bounds.max.y, r.bounds.center.z) : c85.position + Vector3.up * 0.25f;
        }

        private static Vector3 GetScreenCenter(Transform c104)
        {
            if (c104 == null) return new Vector3(20.63f, 4.94f, -1.81f);
            var r = c104.GetComponent<Renderer>();
            return r != null ? r.bounds.center : c104.position;
        }

        private UnityEngine.Camera CreateCameraRig(Vector3 camPos, Vector3 dispatchPos, float hYaw, float dYaw)
        {
            GameObject camObj = GameObject.Find("Main Camera") ?? new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            camObj.transform.position = camPos;
            camObj.transform.rotation = Quaternion.Euler(0f, hYaw, 0f);

            var cam = ModelUtils.GetOrAdd<UnityEngine.Camera>(camObj);
            cam.fieldOfView = 65f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 200f;

            ModelUtils.GetOrAdd<AudioListener>(camObj);
            var rCam = ModelUtils.GetOrAdd<RoomCamera>(camObj);
            rCam.hangarPos = camPos;
            rCam.dispatchPos = dispatchPos;
            rCam.hangarYaw = hYaw;
            rCam.dispatchYaw = dYaw;

            ModelUtils.GetOrAdd<PlayerRaycaster>(camObj);
            return cam;
        }

        private (JaegerProject jaeger, HangarAlarmLight alarmLight) CreateCoreManagers()
        {
            var grp = new GameObject("--- 01_MANAGERS ---");
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.transform.SetParent(grp.transform, false);
                es.AddComponent<EventSystem>();
                GameInput.AddUIInputModule(es);
            }

            grp.AddComponent<GameManager>();
            var jaeger = grp.AddComponent<JaegerProject>();
            grp.AddComponent<DispatchSystem>();
            grp.AddComponent<HangarAudio>();
            var alarm = grp.AddComponent<HangarAlarmLight>();

            var armory = grp.AddComponent<ArmoryManager>();
            armory.SetWeaponPrefab("weapon_spear", LoadPrefab("Assets/Prefabs/Eger/EgerSpear.fbx"));
            armory.SetWeaponPrefab("shield_bastion", LoadPrefab("Assets/Prefabs/Eger/EgerShield.fbx"));

            return (jaeger, alarm);
        }

        private void CreateLighting(Vector3 podiumPos, Vector3 screenCenter, HangarAlarmLight alarmLight)
        {
            var grp = new GameObject("--- 02_LIGHTING_AND_VFX ---");
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.24f, 0.28f, 0.35f);

            CreateDirectional(grp.transform, "Directional_KeyLight", Quaternion.Euler(48f, -32f, 0f), new Color(0.95f, 0.95f, 1.0f), 1.15f, true);
            CreateDirectional(grp.transform, "Directional_FillLight", Quaternion.Euler(25f, 145f, 0f), new Color(0.25f, 0.35f, 0.5f), 0.55f, false);

            CreateSpotlight(grp.transform, "Spotlight_Mech_Hero", podiumPos + new Vector3(0f, 2.5f, 1.5f), Quaternion.Euler(40f, 180f, 0f), new Color(0.9f, 0.95f, 1.0f), 5.5f, 15f, 60f);
            CreateSpotlight(grp.transform, "Spotlight_Screen", screenCenter + new Vector3(0f, 2.0f, -1.2f), Quaternion.Euler(45f, 0f, 0f), new Color(0.6f, 0.8f, 1.0f), 2.5f, 8f, 70f);
            CreateSpotlight(grp.transform, "Spotlight_Jaeger", podiumPos + new Vector3(-2.5f, 2.8f, 2.5f), Quaternion.Euler(55f, 70f, 0f), new Color(1.0f, 0.7f, 0.3f), 3.0f, 10f, 55f);

            GameObject spAlarm = CreateSpotlight(grp.transform, "Spotlight_Alarm_Red", podiumPos + new Vector3(0f, 3.8f, 0f), Quaternion.Euler(90f, 0f, 0f), new Color(1f, 0.15f, 0.15f), 0f, 30f, 120f);
            alarmLight?.ConfigureLight(spAlarm.GetComponent<Light>());
        }

        private static void CreateDirectional(Transform parent, string name, Quaternion rot, Color c, float intensity, bool shadows)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.rotation = rot;
            Light l = go.AddComponent<Light>();
            l.type = LightType.Directional;
            l.color = c;
            l.intensity = intensity;
            if (shadows) l.shadows = LightShadows.Soft;
        }

        private static GameObject CreateSpotlight(Transform parent, string name, Vector3 pos, Quaternion rot, Color c, float intensity, float range, float spotAngle)
        {
            var sp = new GameObject(name);
            sp.transform.SetParent(parent, false);
            sp.transform.position = pos;
            sp.transform.rotation = rot;
            Light l = sp.AddComponent<Light>();
            l.type = LightType.Spot;
            l.color = c;
            l.intensity = intensity;
            l.range = range;
            l.spotAngle = spotAngle;
            l.shadows = LightShadows.Soft;
            return sp;
        }

        private void CreateHangarRoom(GameObject hf)
        {
            var grp = new GameObject("--- 03_HANGAR_ROOM ---");
            if (hf == null)
            {
                GameObject fullPrefab = LoadPrefab("Assets/Prefabs/Base/HANGARFULL.prefab");
                if (fullPrefab != null) Instantiate(fullPrefab, grp.transform).name = "HANGARFULL";
            }
        }

        private void CreateMechsHangar(Vector3 podiumPos)
        {
            var grp = new GameObject("--- 04_MECH_ATLANT ---");
            var mgr = grp.AddComponent<HangarMechManager>();

            CreatePrimitive(grp.transform, PrimitiveType.Cylinder, "Podium_Inner_Glow", podiumPos + new Vector3(0f, 0.02f, 0f), new Vector3(1.25f, 0.02f, 1.25f), new Color(0.15f, 0.75f, 0.95f), false);
            CreateBatteryStation(grp.transform, podiumPos + new Vector3(1.4f, -0.25f, 0.7f));

            mgr.Mechs.Add(BuildMechUnit(grp.transform, 0, "Страж-1 (Лёгкий)", new Color(0.15f, 0.75f, 0.85f), 85f, 18f, 95f, 0.92f, podiumPos));
            mgr.Mechs.Add(BuildMechUnit(grp.transform, 1, "Атлант-2 (Штурм)", new Color(0.2f, 0.45f, 0.85f), 120f, 26f, 62f, 1.0f, podiumPos));
            mgr.Mechs.Add(BuildMechUnit(grp.transform, 2, "Голиаф-3 (Тяжёлый)", new Color(0.85f, 0.5f, 0.15f), 175f, 34f, 40f, 1.12f, podiumPos));

            string[] bayMatPaths =
            {
                "Assets/Prefabs/Mech01/Mech01_Guard_Cyan.mat",
                "Assets/Prefabs/Mech01/Mech01_Atlant_Blue.mat",
                "Assets/Prefabs/Mech01/Mech01_Goliath_Orange.mat"
            };
            float[] bayScales = { 20f, 21.2f, 22.8f };
            Vector3 bayPos = podiumPos + new Vector3(0f, 0f, -2.9f);
            var bayList = new List<Transform>();

            for (int i = 0; i < 3; i++)
            {
                Transform b = BuildBayMechVisual(grp.transform, i, mgr.Mechs[i].mechName, bayScales[i], bayPos, bayMatPaths[i]);
                bayList.Add(b);
                if (b != null)
                {
                    b.gameObject.SetActive(i == 0);
                    mgr.Mechs[i].SetBayVisualRoot(b);
                }
            }
            mgr.ConfigureBayMechs(bayList);

            for (int i = 0; i < mgr.Mechs.Count; i++)
                mgr.Mechs[i].gameObject.SetActive(i == 0);
        }

        private Transform BuildBayMechVisual(Transform parent, int id, string mechName, float scale, Vector3 bayPos, string matPath)
        {
            GameObject mechPrefab = LoadPrefab("Assets/Prefabs/Mech01/Mech01.prefab");
            if (mechPrefab == null) return null;

            GameObject obj = Instantiate(mechPrefab, parent);
#if UNITY_EDITOR
            UnityEditor.PrefabUtility.UnpackPrefabInstance(obj, UnityEditor.PrefabUnpackMode.Completely, UnityEditor.InteractionMode.AutomatedAction);
#endif
            obj.name = $"Bay_Mech_{id}_{mechName}";
            obj.transform.position = bayPos;
            obj.transform.rotation = Quaternion.Euler(270f, 0f, 0f);
            obj.transform.localScale = Vector3.one * scale;

            foreach (var col in obj.GetComponentsInChildren<Collider>(true))
                ModelUtils.DestroySafe(col);

            ModelUtils.ApplyPrimaryMaterial(obj, LoadMaterial(matPath));
            SnapFeetToPodium(obj, bayPos);
            return obj.transform;
        }

        private void CreateBatteryStation(Transform parent, Vector3 pos)
        {
            var bRoot = new GameObject("Battery_Charging_Station");
            bRoot.transform.SetParent(parent, false);
            bRoot.transform.position = pos;

            CreatePrimitive(bRoot.transform, PrimitiveType.Cube, "Battery_Base", new Vector3(0f, 0.45f, 0f), new Vector3(0.85f, 0.9f, 0.85f), new Color(0.16f, 0.19f, 0.24f), true);
            GameObject core = CreatePrimitive(bRoot.transform, PrimitiveType.Cylinder, "Battery_Energy_Core", new Vector3(0f, 1.25f, 0f), new Vector3(0.45f, 0.7f, 0.45f), new Color(0.15f, 0.85f, 1.0f), true);
            CreatePrimitive(bRoot.transform, PrimitiveType.Cube, "Battery_Top_Cap", new Vector3(0f, 1.7f, 0f), new Vector3(0.75f, 0.25f, 0.75f), new Color(0.85f, 0.65f, 0.15f), true);

            ModelUtils.GetOrAdd<CapsuleCollider>(core);
            core.AddComponent<BatteryStation>();
        }

        private MechUnit BuildMechUnit(Transform parent, int id, string name, Color theme, float hp, float atk, float spd, float scaleMult, Vector3 podiumPos)
        {
            GameObject mechPrefab = LoadPrefab("Assets/Prefabs/Mech01/Mech01.prefab");
            GameObject obj = mechPrefab != null ? Instantiate(mechPrefab, parent) : new GameObject();
            if (mechPrefab == null) obj.transform.SetParent(parent, false);
#if UNITY_EDITOR
            else UnityEditor.PrefabUtility.UnpackPrefabInstance(obj, UnityEditor.PrefabUnpackMode.Completely, UnityEditor.InteractionMode.AutomatedAction);
#endif
            obj.name = $"Mech_{id}_{name}";
            obj.transform.position = podiumPos;
            obj.transform.rotation = Quaternion.Euler(270f, 0f, 0f);
            obj.transform.localScale = Vector3.one * 5.0f * scaleMult;

            MechUnit unit = ModelUtils.GetOrAdd<MechUnit>(obj);
            unit.id = id;
            unit.mechName = name;
            unit.themeColor = theme;
            unit.maxHp = hp;
            unit.currentHp = hp;
            unit.maxEnergy = 100f;
            unit.currentEnergy = 100f;
            unit.attackPower = atk;
            unit.moveSpeed = spd;
            unit.parts.Clear();

            Material mechMat = LoadMaterial("Assets/Prefabs/Holo.mat") ?? LoadMaterial("Assets/Prefabs/Mech01/Mech01.mat");
            ModelUtils.ApplyPrimaryMaterial(obj, mechMat);

            GameObject torsoObj = ExtractTorsoChild(obj);
            Transform tLegs = obj.transform.Find("LowerBody02");
            if (tLegs != null)
            {
                obj.transform.Find("LeftLegLow02")?.SetParent(tLegs, true);
                obj.transform.Find("RightLegLow02")?.SetParent(tLegs, true);
            }

            AddPartToUnit(unit, torsoObj, new MechPartData { partName = "Корпус (Торс)", slot = PartSlot.Torso, defensePower = 35f }, null, false);
            AddPartToUnit(unit, obj.transform.Find("HeadLow02")?.gameObject, new MechPartData { partName = "Голова (Сенсоры)", slot = PartSlot.Head, defensePower = 10f, repairCredits = 40, repairAlloy = 20 }, LoadPrefab("Assets/Prefabs/Mech01/BrokenParts/HeadBroken.fbx"), true);
            AddPartToUnit(unit, obj.transform.Find("LeftArmLow02")?.gameObject, new MechPartData { partName = "Левая рука (Щит)", slot = PartSlot.LeftArm, attackPower = 10f, defensePower = 20f, repairCredits = 45, repairAlloy = 25 }, LoadPrefab("Assets/Prefabs/Mech01/BrokenParts/LeftArmBroken.fbx"), true);
            AddPartToUnit(unit, obj.transform.Find("RightArmLow02")?.gameObject, new MechPartData { partName = "Правая рука (Орудие)", slot = PartSlot.RightArm, attackPower = 30f, defensePower = 5f, repairCredits = 50, repairAlloy = 25 }, LoadPrefab("Assets/Prefabs/Mech01/BrokenParts/RightArmBroken.fbx"), true);
            AddPartToUnit(unit, tLegs?.gameObject, new MechPartData { partName = "Шасси (Ноги)", slot = PartSlot.Legs, defensePower = 15f }, null, true);

            SnapFeetToPodium(obj, podiumPos);
            unit.EnsurePartsLoaded();
            unit.ApplyThemeColor();
            unit.SyncHpFromParts();
            return unit;
        }

        private GameObject ExtractTorsoChild(GameObject obj)
        {
            var torsoObj = new GameObject("Torso_Armor");
            torsoObj.transform.SetParent(obj.transform, false);

            var rootMF = obj.GetComponent<MeshFilter>();
            if (rootMF != null)
            {
                torsoObj.AddComponent<MeshFilter>().sharedMesh = rootMF.sharedMesh;
                ModelUtils.DestroySafe(rootMF);
            }
            var rootMR = obj.GetComponent<MeshRenderer>();
            if (rootMR != null)
            {
                torsoObj.AddComponent<MeshRenderer>().sharedMaterial = rootMR.sharedMaterial;
                ModelUtils.DestroySafe(rootMR);
            }
            var rootCol = obj.GetComponent<Collider>();
            if (rootCol != null) ModelUtils.DestroySafe(rootCol);
            return torsoObj;
        }

        private static void AddPartToUnit(MechUnit unit, GameObject target, MechPartData data, GameObject brokenPrefab, bool recurseColliders)
        {
            if (target == null) return;
            MechPart part = ModelUtils.GetOrAdd<MechPart>(target);
            part.Setup(data);
            AddLocalMeshColliders(target.transform, recurseColliders);

            var rend = target.GetComponent<Renderer>() ?? target.GetComponentInChildren<Renderer>();
            part.SetVisuals(rend, null);
            if (brokenPrefab != null) part.AttachBrokenVisual(brokenPrefab);
            unit.parts.Add(part);
        }

        private static void SnapFeetToPodium(GameObject obj, Vector3 podiumPos)
        {
            Bounds b = ModelUtils.CalcBounds(obj, false);
            if (b.size.sqrMagnitude > 0.0001f)
                obj.transform.position += podiumPos - new Vector3(b.center.x, b.min.y, b.center.z);
        }

        public static void AddLocalMeshColliders(Transform root, bool recurse)
        {
            if (root == null) return;

            var mf = root.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null && root.GetComponent<BoxCollider>() == null)
            {
                var box = root.gameObject.AddComponent<BoxCollider>();
                box.center = mf.sharedMesh.bounds.center;
                box.size = mf.sharedMesh.bounds.size;
            }

            var smr = root.GetComponent<SkinnedMeshRenderer>();
            if (smr != null && smr.sharedMesh != null && root.GetComponent<BoxCollider>() == null)
            {
                var box = root.gameObject.AddComponent<BoxCollider>();
                box.center = smr.sharedMesh.bounds.center;
                box.size = smr.sharedMesh.bounds.size;
            }

            if (!recurse) return;
            for (int i = 0; i < root.childCount; i++)
            {
                var ch = root.GetChild(i);
                if (ch.GetComponent<MechPart>() == null) AddLocalMeshColliders(ch, true);
            }
        }

        private void CreateJaegerStage(Vector3 bayPos, JaegerProject jaeger)
        {
            var grp = new GameObject("--- 05_JAEGER_FRAME ---");
            grp.transform.position = bayPos;
            grp.transform.rotation = Quaternion.Euler(0f, 270f, 0f);

            GameObject egerPrefab = LoadPrefab("Assets/Prefabs/Eger/Eger.prefab");
            if (jaeger == null) return;

            if (egerPrefab != null)
            {
                GameObject egerObj = Instantiate(egerPrefab, grp.transform);
                egerObj.name = "Eger";
                egerObj.transform.localPosition = Vector3.zero;
                egerObj.transform.localRotation = Quaternion.identity;

                jaeger.stageVisualParts = new[]
                {
                    egerObj.transform.Find("EgerHead"),
                    egerObj.transform.Find("EgerLeftArm"),
                    egerObj.transform.Find("EgerRightArm"),
                    egerObj.transform.Find("EgerShield"),
                    egerObj.transform.Find("EgerSpear")
                };
            }

            jaeger.stageWorkbenchPrefabs = new[]
            {
                LoadPrefab("Assets/Prefabs/Eger/EgerHead.fbx"),
                LoadPrefab("Assets/Prefabs/Eger/EgerLeftArm.fbx"),
                LoadPrefab("Assets/Prefabs/Eger/EgerRightArm.fbx"),
                LoadPrefab("Assets/Prefabs/Eger/EgerShield.fbx"),
                LoadPrefab("Assets/Prefabs/Eger/EgerSpear.fbx")
            };
            jaeger.platePrefab = LoadPrefab("Assets/Prefabs/Eger/Plate.fbx");
            jaeger.RefreshVisualStages();
        }

        private void CreateWorkbench(Vector3 wbPos)
        {
            var grp = new GameObject("--- 06_WORKBENCH ---");
            grp.transform.position = wbPos;
            grp.transform.rotation = Quaternion.Euler(0f, 270f, 0f);

            var wb = grp.AddComponent<RepairWorkbench>();
            wb.platePrefab = LoadPrefab("Assets/Prefabs/Eger/Plate.fbx");

            CreatePrimitive(grp.transform, PrimitiveType.Cube, "Tabletop", new Vector3(0f, 0.75f, 0f), new Vector3(2.4f, 0.12f, 1.1f), new Color(0.22f, 0.24f, 0.27f), true);
            CreatePrimitive(grp.transform, PrimitiveType.Cube, "Vice", new Vector3(-0.95f, 0.88f, 0f), new Vector3(0.25f, 0.2f, 0.3f), new Color(0.65f, 0.22f, 0.22f), true);
            CreatePrimitive(grp.transform, PrimitiveType.Cylinder, "Lamp", new Vector3(0.95f, 1.15f, 0.2f), new Vector3(0.08f, 0.6f, 0.08f), new Color(0.75f, 0.72f, 0.25f), true);

            for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2)
                    CreatePrimitive(grp.transform, PrimitiveType.Cube, $"Leg_{x}_{z}", new Vector3(x * 1.05f, 0.35f, z * 0.45f), new Vector3(0.12f, 0.7f, 0.12f), new Color(0.12f, 0.13f, 0.16f), true);

            BuildWorkbenchControlPanel3D(grp.transform);
        }

        public static void BuildWorkbenchControlPanel3D(Transform workbenchRoot)
        {
            if (workbenchRoot == null) return;
            var old = workbenchRoot.Find("Control_Panel_3D");
            if (old != null) ModelUtils.DestroySafe(old.gameObject);

            var panel = new GameObject("Control_Panel_3D");
            panel.transform.SetParent(workbenchRoot, false);
            panel.transform.localPosition = new Vector3(0f, 0.82f, -0.45f);
            panel.transform.localRotation = Quaternion.Euler(-28f, 0f, 0f);

            CreatePrimitive(panel.transform, PrimitiveType.Cube, "Panel_Tray", Vector3.zero, new Vector3(1.58f, 0.03f, 0.16f), new Color(0.10f, 0.13f, 0.17f), false);

            Color btnBlue = new Color(0.20f, 0.36f, 0.54f);
            AddBtn3D(panel.transform, -0.66f, WorkbenchButtonAction.RotateYawLeft, "< Y", "Повернуть влево", btnBlue, 0.14f);
            AddBtn3D(panel.transform, -0.49f, WorkbenchButtonAction.RotateYawRight, "Y >", "Повернуть вправо", btnBlue, 0.14f);
            AddBtn3D(panel.transform, -0.30f, WorkbenchButtonAction.RotatePitchUp, "^ P", "Наклон вверх", btnBlue, 0.14f);
            AddBtn3D(panel.transform, -0.13f, WorkbenchButtonAction.RotatePitchDown, "P v", "Наклон вниз", btnBlue, 0.14f);
            AddBtn3D(panel.transform,  0.08f, WorkbenchButtonAction.ResetRotation, "СБРОС", "Сбросить угол", new Color(0.32f, 0.37f, 0.44f), 0.18f);
            AddBtn3D(panel.transform,  0.34f, WorkbenchButtonAction.FinishWork, "ГОТОВО", "Завершить ремонт [Пробел]", new Color(0.18f, 0.52f, 0.34f), 0.24f);
            AddBtn3D(panel.transform,  0.62f, WorkbenchButtonAction.CancelWork, "ВЫХОД", "Вернуться в ангар [Esc]", new Color(0.62f, 0.22f, 0.22f), 0.22f);
        }

        private static void AddBtn3D(Transform panel, float localX, WorkbenchButtonAction action, string text, string prompt, Color color, float width)
        {
            GameObject btn = CreatePrimitive(panel, PrimitiveType.Cube, $"Btn3D_{action}", new Vector3(localX, 0.025f, 0f), new Vector3(width, 0.032f, 0.11f), color, true);
            btn.AddComponent<WorkbenchButton3D>().Init(action, text, prompt, color);

            var lblObj = new GameObject("Label");
            lblObj.transform.SetParent(btn.transform, false);
            lblObj.transform.localPosition = new Vector3(0f, 0.56f, 0f);
            lblObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            lblObj.transform.localScale = new Vector3(0.024f / Mathf.Max(0.05f, width), 0.22f, 1f);

            var tm = lblObj.AddComponent<TextMesh>();
            tm.text = text;
            tm.fontSize = 36;
            tm.characterSize = 0.34f;
            tm.fontStyle = FontStyle.Bold;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = Color.white;
        }

        public static void BuildDispatchMonitors(Transform consoleGroup, Transform centerScreen, Vector3 screenCenter, UnityEngine.Camera cam)
        {
            if (consoleGroup == null) return;

            Bounds cb = centerScreen != null && centerScreen.GetComponent<Renderer>() != null
                ? centerScreen.GetComponent<Renderer>().bounds
                : new Bounds(screenCenter, new Vector3(1.16f, 0.72f, 0.1f));

            float termScale = cb.size.x > 0.4f ? Mathf.Clamp((cb.size.x * 0.94f) / 1200f, 0.00072f, 0.00095f) : 0.0009f;
            Font baseFont = UIBuilder.GetFont(18);
            Font hiFont = UIBuilder.GetFont(28);

            BuildTerminalMonitor(consoleGroup, new Vector3(cb.center.x, cb.center.y, cb.min.z - 0.014f), termScale, cam, baseFont);
            BuildStatusMonitor(consoleGroup, new Vector3(cb.min.x, cb.center.y, cb.center.z), cam, hiFont);
            BuildArmoryMonitor(consoleGroup, new Vector3(cb.max.x, cb.center.y, cb.center.z), cam, hiFont);
        }

        private static Canvas CreateWorldCanvas(Transform parent, string name, Vector3 pos, Quaternion rot, float scale, Vector2 size, UnityEngine.Camera cam, int order)
        {
            Transform old = parent.Find(name);
            if (old != null) ModelUtils.DestroySafe(old.gameObject);

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = rot;
            go.transform.localScale = Vector3.one * scale;

            Canvas c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
            c.worldCamera = cam;
            c.sortingOrder = order;
            go.GetComponent<RectTransform>().sizeDelta = size;

            var gr = go.AddComponent<GraphicRaycaster>();
            gr.ignoreReversedGraphics = false;
            gr.blockingObjects = GraphicRaycaster.BlockingObjects.None;
            return c;
        }

        private static void BuildTerminalMonitor(Transform parent, Vector3 pos, float scale, UnityEngine.Camera cam, Font font)
        {
            Canvas c = CreateWorldCanvas(parent, "Terminal_Canvas", pos, Quaternion.identity, scale, new Vector2(1200f, 680f), cam, 4);
            GameObject bg = UIBuilder.Panel(c.transform, "BG", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, UITheme.BgDark, UITheme.Border);

            GameObject header = UIBuilder.Panel(bg.transform, "Header", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(1168f, 48f), UITheme.BgHeader, UITheme.Border);
            UIBuilder.Label(header.transform, "Txt_RadarTitle", "РАДАР ОБОРОНЫ   •   ПЕРИМЕТР ЧИСТ   •   БАЗА: 100%", new Vector2(0f, 0.5f), new Vector2(360f, 0f), new Vector2(680f, 36f), 19, UITheme.TextMain, font, FontStyle.Bold, TextAnchor.MiddleLeft);
            UIBuilder.Label(header.transform, "Txt_JaegerInfo", "Егерь 0/5", new Vector2(1f, 0.5f), new Vector2(-280f, 0f), new Vector2(160f, 32f), 17, UITheme.TextMuted, font, FontStyle.Bold, TextAnchor.MiddleRight);
            UIBuilder.Button(header.transform, "Btn_ScanThreat", "Вызвать волну", new Vector2(1f, 0.5f), new Vector2(-102f, 0f), new Vector2(180f, 34f), UITheme.AccentTeal, UITheme.Border, 16, font);

            GameObject radar = UIBuilder.Panel(bg.transform, "Radar_Screen", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 44f), new Vector2(1140f, 420f), UITheme.BgRecessed, UITheme.Border);
            radar.AddComponent<RadarClickHandler>();

            foreach (float x in new[] { -360f, -180f, 0f, 180f, 360f })
                UIBuilder.AddLine(radar.transform, new Vector2(x, 0f), new Vector2(1.5f, 412f), UITheme.RadarGrid);
            foreach (float y in new[] { -110f, 0f, 110f })
                UIBuilder.AddLine(radar.transform, new Vector2(0f, y), new Vector2(1132f, 1.5f), UITheme.RadarGrid);

            UIBuilder.AddLine(radar.transform, new Vector2(0f, -178f), new Vector2(1120f, 2f), new Color(UITheme.AccentTeal.r, UITheme.AccentTeal.g, UITheme.AccentTeal.b, 0.75f));

            var markers = new GameObject("Marker_Container");
            markers.transform.SetParent(radar.transform, false);
            var mRt = markers.AddComponent<RectTransform>();
            mRt.anchorMin = Vector2.zero;
            mRt.anchorMax = Vector2.one;
            mRt.sizeDelta = Vector2.zero;

            float[] xOff = { -386f, 0f, 386f };
            for (int i = 0; i < 3; i++)
            {
                GameObject slot = UIBuilder.Panel(bg.transform, $"SlotRoot_{i}", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(xOff[i], 96f), new Vector2(368f, 112f), UITheme.BgCard, UITheme.Border);
                UIBuilder.Button(slot.transform, $"Btn_MechSlot_{i}", $"Мех {i + 1}", new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(354f, 32f), UITheme.BgHeader, UITheme.Border, 15, font);
                UIBuilder.BarFill(slot.transform, "Bar_Hp", new Vector2(0.5f, 0.5f), new Vector2(-88f, 4f), new Vector2(168f, 10f), UITheme.BgRecessed, UITheme.AccentGreen);
                UIBuilder.BarFill(slot.transform, "Bar_En", new Vector2(0.5f, 0.5f), new Vector2(88f, 4f), new Vector2(168f, 10f), UITheme.BgRecessed, UITheme.AccentTeal);
                UIBuilder.Label(slot.transform, "Txt_Stats", "ХП 100%     ЭН 100%", new Vector2(0.5f, 0.5f), new Vector2(0f, -14f), new Vector2(350f, 20f), 13, UITheme.TextMuted, font);
                UIBuilder.Button(slot.transform, $"Btn_Shield_{i}", "Щит", new Vector2(0f, 0f), new Vector2(92f, 18f), new Vector2(166f, 26f), UITheme.AccentTeal, UITheme.Border, 14, font);
                UIBuilder.Button(slot.transform, $"Btn_Recall_{i}", "На базу", new Vector2(1f, 0f), new Vector2(-92f, 18f), new Vector2(166f, 26f), UITheme.AccentRed, UITheme.Border, 14, font);
            }

            UIBuilder.Label(bg.transform, "Txt_BattleLog", "Радар чист.", new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(1140f, 26f), 15, UITheme.TextMuted, font);

            ModelUtils.GetOrAdd<TerminalUI>(c.gameObject).BindFromHierarchy();
        }

        private static void BuildStatusMonitor(Transform parent, Vector3 leftPivot, UnityEngine.Camera cam, Font hiFont)
        {
            Quaternion rot = Quaternion.Euler(0f, -30f, 0f);
            Vector3 worldPos = leftPivot + rot * new Vector3(-0.186f, 0.004f, -0.014f);
            Canvas c = CreateWorldCanvas(parent, "Status_Canvas", worldPos, rot, 0.00052f, new Vector2(700f, 950f), cam, 5);

            GameObject bg = UIBuilder.Panel(c.transform, "BG", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, UITheme.BgDark, UITheme.Border);

            GameObject header = UIBuilder.Panel(bg.transform, "Header", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(660f, 68f), UITheme.BgHeader, UITheme.Border);
            UIBuilder.Label(header.transform, "Title", "СТАТУС БАЗЫ", new Vector2(0f, 0.5f), new Vector2(190f, 0f), new Vector2(340f, 44f), 30, UITheme.TextMain, hiFont, FontStyle.Bold, TextAnchor.MiddleLeft);
            UIBuilder.Label(header.transform, "Txt_Wave", "Спокойно", new Vector2(1f, 0.5f), new Vector2(-120f, 0f), new Vector2(210f, 38f), 22, UITheme.TextMuted, hiFont, FontStyle.Bold, TextAnchor.MiddleRight);

            GameObject baseCard = UIBuilder.Panel(bg.transform, "BaseCard", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -158f), new Vector2(660f, 142f), UITheme.BgCard, UITheme.Border);
            UIBuilder.Label(baseCard.transform, "Lbl", "ЦЕЛОСТНОСТЬ ПЕРИМЕТРА", new Vector2(0f, 1f), new Vector2(210f, -24f), new Vector2(390f, 32f), 22, UITheme.TextMuted, hiFont, FontStyle.Bold, TextAnchor.MiddleLeft);
            UIBuilder.Label(baseCard.transform, "Txt_BaseHp", "100%", new Vector2(1f, 1f), new Vector2(-78f, -24f), new Vector2(120f, 34f), 28, UITheme.AccentGreen, hiFont, FontStyle.Bold, TextAnchor.MiddleRight);
            UIBuilder.BarFill(baseCard.transform, "Bar_BaseHp", new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(616f, 22f), UITheme.BgRecessed, UITheme.AccentGreen);
            UIBuilder.Label(baseCard.transform, "Txt_Resources", "Кредиты: 220   •   Сплав: 95", new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(620f, 32f), 23, UITheme.AccentAmber, hiFont, FontStyle.Bold);

            UIBuilder.Label(bg.transform, "Txt_JaegerHeader", "ПРОЕКТ «ЕГЕРЬ»   0/5", new Vector2(0.5f, 1f), new Vector2(0f, -258f), new Vector2(650f, 40f), 26, UITheme.TextMain, hiFont, FontStyle.Bold);

            float[] yRows = { -330f, -424f, -518f, -612f, -706f };
            for (int i = 0; i < 5; i++)
            {
                GameObject row = UIBuilder.Panel(bg.transform, $"Stage_{i}", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, yRows[i]), new Vector2(660f, 80f), UITheme.BgCard, UITheme.Border);
                UIBuilder.Panel(row.transform, "Accent", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(10f, 62f), UITheme.Border);
                UIBuilder.Label(row.transform, "Txt_Stage", $"{i + 1}. Этап", new Vector2(0.5f, 0.5f), new Vector2(12f, 0f), new Vector2(610f, 46f), 24, UITheme.TextMain, hiFont, FontStyle.Bold, TextAnchor.MiddleLeft);
            }

            UIBuilder.Button(bg.transform, "Btn_BuildJaeger", "СОБРАТЬ МОДУЛЬ", new Vector2(0.5f, 0f), new Vector2(0f, 68f), new Vector2(660f, 84f), UITheme.AccentTeal, UITheme.Border, 25, hiFont);

            ModelUtils.GetOrAdd<StatusMonitorUI>(c.gameObject).BindFromHierarchy();
        }

        public static ArmoryUI BuildArmoryMonitor(Transform consoleGroup, Vector3 rightPivot, UnityEngine.Camera cam, Font hiFont)
        {
            Quaternion monRot = Quaternion.Euler(0f, 30f, 0f);
            Vector3 worldPos = rightPivot + monRot * new Vector3(0.186f, 0.004f, -0.014f);
            Canvas c = CreateWorldCanvas(consoleGroup, "Armory_Canvas", worldPos, monRot, 0.00052f, new Vector2(700f, 950f), cam, 5);

            GameObject bg = UIBuilder.Panel(c.transform, "BG", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, UITheme.BgDark, UITheme.Border);

            UIBuilder.Label(bg.transform, "Txt_Title", "ОРУЖЕЙНАЯ", new Vector2(0f, 1f), new Vector2(215f, -42f), new Vector2(380f, 44f), 32, UITheme.TextMain, hiFont, FontStyle.Bold, TextAnchor.MiddleLeft);
            UIBuilder.Button(bg.transform, "Btn_FocusMonitor", "КРУПНЕЕ", new Vector2(1f, 1f), new Vector2(-116f, -42f), new Vector2(188f, 48f), UITheme.AccentTeal, UITheme.Border, 22, hiFont);
            UIBuilder.Label(bg.transform, "Txt_ArmoryResources", "Кредиты: 220   •   Сплав: 95", new Vector2(0.5f, 1f), new Vector2(0f, -96f), new Vector2(650f, 38f), 25, UITheme.AccentAmber, hiFont, FontStyle.Bold);

            GameObject pMech = UIBuilder.Panel(bg.transform, "Panel_MechSelect", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 412f), new Vector2(664f, 804f), Color.clear);
            pMech.GetComponent<Image>().raycastTarget = false;
            UIBuilder.Label(pMech.transform, "Txt_Step1", "ВЫБЕРИТЕ РОБОТА", new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(640f, 36f), 25, UITheme.TextMuted, hiFont, FontStyle.Bold);

            float[] cardY = { -168f, -396f, -624f };
            for (int i = 0; i < 3; i++)
            {
                GameObject card = UIBuilder.Panel(pMech.transform, $"Btn_SelectMech_{i}", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, cardY[i]), new Vector2(648f, 208f), UITheme.BgCard, UITheme.Border);
                card.AddComponent<Button>();

                GameObject acc = UIBuilder.Panel(card.transform, "Accent", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(12f, 184f), UITheme.AccentTeal);
                acc.GetComponent<Image>().raycastTarget = false;

                Text info = UIBuilder.Label(card.transform, "Txt_Info", $"Робот {i + 1}", new Vector2(0.5f, 0.5f), new Vector2(16f, 14f), new Vector2(592f, 144f), 24, UITheme.TextMain, hiFont, FontStyle.Normal, TextAnchor.MiddleLeft);
                info.supportRichText = true;
                info.lineSpacing = 1.25f;

                UIBuilder.BarFill(card.transform, "Bar_Hp", new Vector2(0.5f, 0f), new Vector2(12f, 22f), new Vector2(596f, 12f), UITheme.BgRecessed, UITheme.AccentGreen);
            }

            GameObject pLoad = UIBuilder.Panel(bg.transform, "Panel_LoadoutMenu", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 412f), new Vector2(664f, 804f), Color.clear);
            pLoad.GetComponent<Image>().raycastTarget = false;

            UIBuilder.Button(pLoad.transform, "Btn_BackToMechs", "◀ К СПИСКУ", new Vector2(0f, 1f), new Vector2(102f, -32f), new Vector2(190f, 52f), UITheme.BgHeader, UITheme.Border, 23, hiFont);
            UIBuilder.Label(pLoad.transform, "Txt_SelectedMech", "СТРАЖ-1", new Vector2(1f, 1f), new Vector2(-220f, -32f), new Vector2(420f, 52f), 28, UITheme.TextMain, hiFont, FontStyle.Bold, TextAnchor.MiddleRight);

            GameObject sum = UIBuilder.Panel(pLoad.transform, "SummaryBox", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -96f), new Vector2(648f, 54f), UITheme.BgRecessed, UITheme.Border);
            sum.GetComponent<Image>().raycastTarget = false;
            UIBuilder.Label(sum.transform, "Txt_LoadoutSummary", "", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(628f, 44f), 19, UITheme.TextMuted, hiFont);

            UIBuilder.Button(pLoad.transform, "Btn_TabWeapons", "ОРУЖИЕ", new Vector2(0.5f, 1f), new Vector2(-164f, -162f), new Vector2(316f, 54f), UITheme.AccentTeal, UITheme.Border, 25, hiFont);
            UIBuilder.Button(pLoad.transform, "Btn_TabShields", "ЩИТЫ", new Vector2(0.5f, 1f), new Vector2(164f, -162f), new Vector2(316f, 54f), UITheme.BgCard, UITheme.Border, 25, hiFont);

            var listObj = new GameObject("ItemsContainer");
            listObj.transform.SetParent(pLoad.transform, false);
            var listRt = listObj.AddComponent<RectTransform>();
            listRt.anchorMin = new Vector2(0.5f, 1f);
            listRt.anchorMax = new Vector2(0.5f, 1f);
            listRt.pivot = new Vector2(0.5f, 1f);
            listRt.anchoredPosition = new Vector2(0f, -200f);
            listRt.sizeDelta = new Vector2(648f, 550f);

            var vl = listObj.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 12f;
            vl.childAlignment = TextAnchor.UpperCenter;
            vl.childControlWidth = false;
            vl.childControlHeight = false;
            vl.childForceExpandWidth = false;
            vl.childForceExpandHeight = false;

            UIBuilder.Label(pLoad.transform, "Txt_ArmoryFeedback", "", new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(640f, 34f), 22, UITheme.TextMain, hiFont);
            pLoad.SetActive(false);

            var ui = ModelUtils.GetOrAdd<ArmoryUI>(c.gameObject);
            ui.BindFromHierarchy();
            return ui;
        }

        public static HUDOverlay BuildHUD(Transform parent = null)
        {
            GameObject old = GameObject.Find("HUD_Canvas");
            if (old != null)
            {
                if (parent == null) parent = old.transform.parent;
                ModelUtils.DestroySafe(old);
            }

            var cObj = new GameObject("HUD_Canvas");
            if (parent != null) cObj.transform.SetParent(parent, false);
            cObj.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler sc = cObj.AddComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1920f, 1080f);
            cObj.AddComponent<GraphicRaycaster>();

            Font font = UIBuilder.GetFont(16);

            UIBuilder.Button(cObj.transform, "Btn_Left", "◄ Пульт (Q)", new Vector2(0f, 0.5f), new Vector2(82f, 0f), new Vector2(138f, 50f), UITheme.BgHeader, UITheme.Border, 15, font);
            UIBuilder.Button(cObj.transform, "Btn_Right", "Пульт (E) ►", new Vector2(1f, 0.5f), new Vector2(-82f, 0f), new Vector2(138f, 50f), UITheme.BgHeader, UITheme.Border, 15, font);

            GameObject top = UIBuilder.Panel(cObj.transform, "TopBar", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(860f, 40f), UITheme.BgHeader, UITheme.Border);
            UIBuilder.Label(top.transform, "Txt_Res", "Кредиты: 220   |   Сплав: 95   |   База: 100%   |   Егерь: 0/5", new Vector2(0.5f, 0.5f), new Vector2(-70f, 0f), new Vector2(680f, 32f), 16, UITheme.TextMain, font, FontStyle.Bold);
            UIBuilder.Label(top.transform, "Txt_Room", "Ангар", new Vector2(1f, 0.5f), new Vector2(-80f, 0f), new Vector2(130f, 32f), 15, UITheme.AccentTeal, font, FontStyle.Bold, TextAnchor.MiddleRight);

            GameObject pnlMech = UIBuilder.Panel(cObj.transform, "MechSelectPanel", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(780f, 46f), UITheme.BgHeader, UITheme.Border);
            UIBuilder.Button(pnlMech.transform, "Btn_PrevMech", "◄", new Vector2(0f, 0.5f), new Vector2(40f, 0f), new Vector2(62f, 34f), UITheme.BgCard, UITheme.Border, 16, font);
            UIBuilder.Button(pnlMech.transform, "Btn_NextMech", "►", new Vector2(1f, 0.5f), new Vector2(-40f, 0f), new Vector2(62f, 34f), UITheme.BgCard, UITheme.Border, 16, font);
            UIBuilder.Label(pnlMech.transform, "Txt_CurrentMech", "Страж-1 [1/3]", new Vector2(0.5f, 0.5f), new Vector2(-70f, 0f), new Vector2(450f, 36f), 16, UITheme.TextMain, font, FontStyle.Bold);
            UIBuilder.Button(pnlMech.transform, "Btn_RechargeBattery", "Зарядить", new Vector2(1f, 0.5f), new Vector2(-156f, 0f), new Vector2(136f, 34f), UITheme.AccentTeal, UITheme.Border, 15, font);

            GameObject pBox = UIBuilder.Panel(cObj.transform, "PromptBox", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(740f, 32f), new Color(UITheme.BgDark.r, UITheme.BgDark.g, UITheme.BgDark.b, 0.8f));
            pBox.GetComponent<Image>().raycastTarget = false;
            UIBuilder.Label(pBox.transform, "Txt_Prompt", "", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 28f), 14, UITheme.TextMuted, font);

            GameObject wb = UIBuilder.Panel(cObj.transform, "WorkbenchPanel", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -84f), new Vector2(680f, 60f), UITheme.BgHeader, UITheme.Border);
            UIBuilder.Label(wb.transform, "Txt_WbTitle", "Ремонт", new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(650f, 26f), 17, UITheme.AccentAmber, font, FontStyle.Bold);
            UIBuilder.Label(wb.transform, "Txt_WbInfo", "", new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(650f, 22f), 14, UITheme.TextMain, font);
            wb.SetActive(false);

            GameObject vic = UIBuilder.Panel(cObj.transform, "VictoryPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 260f), UITheme.BgDark, UITheme.Border);
            UIBuilder.Label(vic.transform, "Txt_Vic1", "Проект «Егерь» завершён", new Vector2(0.5f, 0.78f), Vector2.zero, new Vector2(580f, 44f), 26, UITheme.AccentAmber, font, FontStyle.Bold);
            UIBuilder.Label(vic.transform, "Txt_Vic2", "", new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(540f, 44f), 18, UITheme.TextMain, font);
            UIBuilder.Button(vic.transform, "Btn_Restart", "Начать заново", new Vector2(0.5f, 0f), new Vector2(0f, 44f), new Vector2(220f, 48f), UITheme.AccentGreen, UITheme.Border, 17, font);
            vic.SetActive(false);

            var hud = ModelUtils.GetOrAdd<HUDOverlay>(cObj);
            hud.BindFromHierarchy();
            return hud;
        }

        private static GameObject LoadPrefab(string path)
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
#else
            return null;
#endif
        }

        private static Material LoadMaterial(string path)
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
#else
            return null;
#endif
        }

        private static GameObject CreatePrimitive(Transform parent, PrimitiveType type, string name, Vector3 pos, Vector3 scale, Color c, bool keepCollider)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            if (!keepCollider) ModelUtils.DestroySafe(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = ModelUtils.CreateLitMaterial(c);
            return go;
        }
    }
}
