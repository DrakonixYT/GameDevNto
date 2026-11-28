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
            Transform c85 = null;
            Transform c104 = null;
            GameObject hf = GameObject.Find("HANGARFULL");
            if (hf == null)
            {
                GameObject fullPrefab = LoadPrefab("Assets/Prefabs/Base/HANGARFULL.prefab");
                if (fullPrefab != null)
                {
                    hf = InstantiateModel(fullPrefab, null);
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
            CreateDispatchConsole(cam, screenCenter, c104);
            CreateHUD();
        }

        private Vector3 GetPodiumPos(Transform c85)
        {
            if (c85 == null) return new Vector3(20.59f, 4.50f, -6.38f);
            var r = c85.GetComponent<Renderer>();
            return r != null ? new Vector3(r.bounds.center.x, r.bounds.max.y, r.bounds.center.z) : c85.position + Vector3.up * 0.25f;
        }

        private Vector3 GetScreenCenter(Transform c104)
        {
            if (c104 == null) return new Vector3(20.63f, 4.94f, -1.81f);
            var r = c104.GetComponent<Renderer>();
            return r != null ? r.bounds.center : c104.position;
        }

        private UnityEngine.Camera CreateCameraRig(Vector3 camPos, Vector3 dispatchPos, float hYaw, float dYaw)
        {
            GameObject camObj = GameObject.Find("Main Camera");
            if (camObj == null)
            {
                camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
            }

            camObj.transform.position = camPos;
            camObj.transform.rotation = Quaternion.Euler(0f, hYaw, 0f);

            var cam = GetOrAdd<UnityEngine.Camera>(camObj);
            cam.fieldOfView = 65f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 200f;

            if (camObj.GetComponent<AudioListener>() == null) camObj.AddComponent<AudioListener>();
            var rCam = GetOrAdd<RoomCamera>(camObj);
            rCam.ConfigureAnchor(camPos, dispatchPos, hYaw, dYaw);

            if (camObj.GetComponent<PlayerRaycaster>() == null) camObj.AddComponent<PlayerRaycaster>();
            return cam;
        }

        private (JaegerProject jaeger, HangarAlarmLight alarmLight) CreateCoreManagers()
        {
            GameObject grp = new GameObject("--- 01_MANAGERS ---");
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem");
                es.transform.SetParent(grp.transform, false);
                es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                es.AddComponent<StandaloneInputModule>();
#endif
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
            GameObject grp = new GameObject("--- 02_LIGHTING_AND_VFX ---");
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.24f, 0.28f, 0.35f);

            GameObject sun = new GameObject("Directional_KeyLight");
            sun.transform.SetParent(grp.transform, false);
            sun.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            Light lSun = sun.AddComponent<Light>();
            lSun.type = LightType.Directional;
            lSun.color = new Color(0.95f, 0.95f, 1.0f);
            lSun.intensity = 1.15f;
            lSun.shadows = LightShadows.Soft;

            GameObject fill = new GameObject("Directional_FillLight");
            fill.transform.SetParent(grp.transform, false);
            fill.transform.rotation = Quaternion.Euler(25f, 145f, 0f);
            Light lFill = fill.AddComponent<Light>();
            lFill.type = LightType.Directional;
            lFill.color = new Color(0.25f, 0.35f, 0.5f);
            lFill.intensity = 0.55f;

            CreateSpotlight(grp.transform, "Spotlight_Mech_Hero", podiumPos + new Vector3(0f, 2.5f, 1.5f), Quaternion.Euler(40f, 180f, 0f), new Color(0.9f, 0.95f, 1.0f), 5.5f, 15f, 60f);
            CreateSpotlight(grp.transform, "Spotlight_Screen", screenCenter + new Vector3(0f, 2.0f, -1.2f), Quaternion.Euler(45f, 0f, 0f), new Color(0.6f, 0.8f, 1.0f), 2.5f, 8f, 70f);
            CreateSpotlight(grp.transform, "Spotlight_Jaeger", podiumPos + new Vector3(-2.5f, 2.8f, 2.5f), Quaternion.Euler(55f, 70f, 0f), new Color(1.0f, 0.7f, 0.3f), 3.0f, 10f, 55f);

            GameObject spAlarm = CreateSpotlight(grp.transform, "Spotlight_Alarm_Red", podiumPos + new Vector3(0f, 3.8f, 0f), Quaternion.Euler(90f, 0f, 0f), new Color(1f, 0.15f, 0.15f), 0f, 30f, 120f);
            if (alarmLight != null) alarmLight.ConfigureLight(spAlarm.GetComponent<Light>());
        }

        private GameObject CreateSpotlight(Transform parent, string name, Vector3 pos, Quaternion rot, Color c, float intensity, float range, float spotAngle)
        {
            GameObject sp = new GameObject(name);
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
            GameObject grp = new GameObject("--- 03_HANGAR_ROOM ---");
            if (hf == null)
            {
                GameObject fullPrefab = LoadPrefab("Assets/Prefabs/Base/HANGARFULL.prefab");
                if (fullPrefab != null)
                {
                    GameObject fullObj = InstantiateModel(fullPrefab, grp.transform);
                    fullObj.name = "HANGARFULL";
                }
            }
        }

        private void CreateMechsHangar(Vector3 podiumPos)
        {
            GameObject grp = new GameObject("--- 04_MECH_ATLANT ---");
            var mgr = grp.AddComponent<HangarMechManager>();

            CreateCyl(grp.transform, "Podium_Inner_Glow", podiumPos + new Vector3(0f, 0.02f, 0f), new Vector3(1.25f, 0.02f, 1.25f), new Color(0.15f, 0.75f, 0.95f));
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
            var bayList = new System.Collections.Generic.List<Transform>();
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

            GameObject obj = InstantiateModel(mechPrefab, parent);
#if UNITY_EDITOR
            UnityEditor.PrefabUtility.UnpackPrefabInstance(obj, UnityEditor.PrefabUnpackMode.Completely, UnityEditor.InteractionMode.AutomatedAction);
#endif
            obj.name = $"Bay_Mech_{id}_{mechName}";
            obj.transform.position = bayPos;
            obj.transform.rotation = Quaternion.Euler(270f, 0f, 0f);
            obj.transform.localScale = Vector3.one * scale;

            foreach (var col in obj.GetComponentsInChildren<Collider>(true))
                DestroyImmediate(col);

            Material mat = LoadMaterial(matPath);
            if (mat != null)
            {
                foreach (var r in obj.GetComponentsInChildren<Renderer>(true))
                {
                    var mats = r.sharedMaterials;
                    if (mats.Length > 0)
                    {
                        mats[0] = mat;
                        r.sharedMaterials = mats;
                    }
                }
            }

            SnapFeetToPodium(obj, bayPos);
            return obj.transform;
        }

        private void CreateBatteryStation(Transform parent, Vector3 pos)
        {
            GameObject bRoot = new GameObject("Battery_Charging_Station");
            bRoot.transform.SetParent(parent, false);
            bRoot.transform.position = pos;

            CreateBox(bRoot.transform, "Battery_Base", new Vector3(0f, 0.45f, 0f), new Vector3(0.85f, 0.9f, 0.85f), new Color(0.16f, 0.19f, 0.24f));
            GameObject core = CreateCyl(bRoot.transform, "Battery_Energy_Core", new Vector3(0f, 1.25f, 0f), new Vector3(0.45f, 0.7f, 0.45f), new Color(0.15f, 0.85f, 1.0f));
            CreateBox(bRoot.transform, "Battery_Top_Cap", new Vector3(0f, 1.7f, 0f), new Vector3(0.75f, 0.25f, 0.75f), new Color(0.85f, 0.65f, 0.15f));

            if (core.GetComponent<Collider>() == null) core.AddComponent<CapsuleCollider>();
            core.AddComponent<BatteryStation>();
        }

        private MechUnit BuildMechUnit(Transform parent, int id, string name, Color theme, float hp, float atk, float spd, float scaleMult, Vector3 podiumPos)
        {
            GameObject mechPrefab = LoadPrefab("Assets/Prefabs/Mech01/Mech01.prefab");
            GameObject obj;
            if (mechPrefab != null)
            {
                obj = InstantiateModel(mechPrefab, parent);
#if UNITY_EDITOR
                UnityEditor.PrefabUtility.UnpackPrefabInstance(obj, UnityEditor.PrefabUnpackMode.Completely, UnityEditor.InteractionMode.AutomatedAction);
#endif
            }
            else
            {
                obj = new GameObject();
                obj.transform.SetParent(parent, false);
            }

            obj.name = $"Mech_{id}_{name}";
            obj.transform.position = podiumPos;
            obj.transform.rotation = Quaternion.Euler(270f, 0f, 0f);
            obj.transform.localScale = Vector3.one * 5.0f * scaleMult;

            MechUnit unit = GetOrAdd<MechUnit>(obj);
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

            string[] matPaths =
            {
                "Assets/Prefabs/Mech01/Mech01_Guard_Cyan.mat",
                "Assets/Prefabs/Mech01/Mech01_Atlant_Blue.mat",
                "Assets/Prefabs/Mech01/Mech01_Goliath_Orange.mat"
            };
            Material holoMat = LoadMaterial("Assets/Prefabs/Holo.mat");
            Material mechMat = holoMat != null
                ? holoMat
                : LoadMaterial(id >= 0 && id < matPaths.Length ? matPaths[id] : "Assets/Prefabs/Mech01/Mech01.mat");
            if (mechMat != null)
            {
                foreach (var r in obj.GetComponentsInChildren<Renderer>(true))
                {
                    var mats = r.sharedMaterials;
                    if (mats.Length > 0)
                    {
                        mats[0] = mechMat;
                        r.sharedMaterials = mats;
                    }
                }
            }

            GameObject torsoObj = ExtractTorsoChild(obj);
            Transform tHead = obj.transform.Find("HeadLow02");
            Transform tArmL = obj.transform.Find("LeftArmLow02");
            Transform tArmR = obj.transform.Find("RightArmLow02");
            Transform tLegs = obj.transform.Find("LowerBody02");
            Transform tLegL = obj.transform.Find("LeftLegLow02");
            Transform tLegR = obj.transform.Find("RightLegLow02");

            if (tLegs != null)
            {
                if (tLegL != null) tLegL.SetParent(tLegs, true);
                if (tLegR != null) tLegR.SetParent(tLegs, true);
            }

            GameObject brokenHead = LoadPrefab("Assets/Prefabs/Mech01/BrokenParts/HeadBroken.fbx");
            GameObject brokenLeftArm = LoadPrefab("Assets/Prefabs/Mech01/BrokenParts/LeftArmBroken.fbx");
            GameObject brokenRightArm = LoadPrefab("Assets/Prefabs/Mech01/BrokenParts/RightArmBroken.fbx");

            AddPartToUnit(unit, torsoObj, new MechPartData { partName = "Корпус (Торс)", slot = PartSlot.Torso, defensePower = 35f }, null, false);
            AddPartToUnit(unit, tHead?.gameObject, new MechPartData { partName = "Голова (Сенсоры)", slot = PartSlot.Head, defensePower = 10f, repairCredits = 40, repairAlloy = 20 }, brokenHead, true);
            AddPartToUnit(unit, tArmL?.gameObject, new MechPartData { partName = "Левая рука (Щит)", slot = PartSlot.LeftArm, attackPower = 10f, defensePower = 20f, repairCredits = 45, repairAlloy = 25 }, brokenLeftArm, true);
            AddPartToUnit(unit, tArmR?.gameObject, new MechPartData { partName = "Правая рука (Орудие)", slot = PartSlot.RightArm, attackPower = 30f, defensePower = 5f, repairCredits = 50, repairAlloy = 25 }, brokenRightArm, true);
            AddPartToUnit(unit, tLegs?.gameObject, new MechPartData { partName = "Шасси (Ноги)", slot = PartSlot.Legs, defensePower = 15f }, null, true);

            SnapFeetToPodium(obj, podiumPos);
            unit.EnsurePartsLoaded();
            unit.ApplyThemeColor();
            unit.SyncHpFromParts();
            return unit;
        }

        private GameObject ExtractTorsoChild(GameObject obj)
        {
            GameObject torsoObj = new GameObject("Torso_Armor");
            torsoObj.transform.SetParent(obj.transform, false);

            var rootMF = obj.GetComponent<MeshFilter>();
            var rootMR = obj.GetComponent<MeshRenderer>();
            if (rootMF != null)
            {
                torsoObj.AddComponent<MeshFilter>().sharedMesh = rootMF.sharedMesh;
                DestroyImmediate(rootMF);
            }
            if (rootMR != null)
            {
                torsoObj.AddComponent<MeshRenderer>().sharedMaterial = rootMR.sharedMaterial;
                DestroyImmediate(rootMR);
            }
            var rootCol = obj.GetComponent<Collider>();
            if (rootCol != null) DestroyImmediate(rootCol);
            return torsoObj;
        }

        private void AddPartToUnit(MechUnit unit, GameObject target, MechPartData data, GameObject brokenPrefab, bool recurseColliders)
        {
            if (target == null) return;
            MechPart part = GetOrAdd<MechPart>(target);
            part.Setup(data);
            AddLocalMeshColliders(target.transform, recurseColliders);

            var rend = target.GetComponent<Renderer>();
            if (rend == null) rend = target.GetComponentInChildren<Renderer>();
            part.SetVisuals(rend, null);
            if (brokenPrefab != null) part.AttachBrokenVisual(brokenPrefab);

            unit.parts.Add(part);
        }

        private void SnapFeetToPodium(GameObject obj, Vector3 podiumPos)
        {
            var renderers = obj.GetComponentsInChildren<Renderer>(false);
            if (renderers.Length == 0) return;

            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
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
                if (ch.GetComponent<MechPart>() != null) continue;
                AddLocalMeshColliders(ch, true);
            }
        }

        private void CreateJaegerStage(Vector3 bayPos, JaegerProject jaeger)
        {
            GameObject grp = new GameObject("--- 05_JAEGER_FRAME ---");
            grp.transform.position = bayPos;
            grp.transform.rotation = Quaternion.Euler(0f, 270f, 0f);

            GameObject egerPrefab = LoadPrefab("Assets/Prefabs/Eger/Eger.prefab");
            Transform[] sceneParts = new Transform[5];

            if (egerPrefab != null)
            {
                GameObject egerObj = InstantiateModel(egerPrefab, grp.transform);
                egerObj.name = "Eger";
                egerObj.transform.localPosition = Vector3.zero;
                egerObj.transform.localRotation = Quaternion.identity;

                sceneParts[0] = egerObj.transform.Find("EgerHead");
                sceneParts[1] = egerObj.transform.Find("EgerLeftArm");
                sceneParts[2] = egerObj.transform.Find("EgerRightArm");
                sceneParts[3] = egerObj.transform.Find("EgerShield");
                sceneParts[4] = egerObj.transform.Find("EgerSpear");
            }

            GameObject[] stagePrefabs =
            {
                LoadPrefab("Assets/Prefabs/Eger/EgerHead.fbx"),
                LoadPrefab("Assets/Prefabs/Eger/EgerLeftArm.fbx"),
                LoadPrefab("Assets/Prefabs/Eger/EgerRightArm.fbx"),
                LoadPrefab("Assets/Prefabs/Eger/EgerShield.fbx"),
                LoadPrefab("Assets/Prefabs/Eger/EgerSpear.fbx")
            };
            GameObject plate = LoadPrefab("Assets/Prefabs/Eger/Plate.fbx");

            if (jaeger != null) jaeger.Configure(sceneParts, stagePrefabs, plate);
        }

        private void CreateWorkbench(Vector3 wbPos)
        {
            GameObject grp = new GameObject("--- 06_WORKBENCH ---");
            grp.transform.position = wbPos;
            grp.transform.rotation = Quaternion.Euler(0f, 270f, 0f);

            var wb = grp.AddComponent<RepairWorkbench>();
            wb.SetPlatePrefab(LoadPrefab("Assets/Prefabs/Eger/Plate.fbx"));

            CreateBox(grp.transform, "Tabletop", new Vector3(0f, 0.75f, 0f), new Vector3(2.4f, 0.12f, 1.1f), new Color(0.22f, 0.24f, 0.27f));
            CreateBox(grp.transform, "Vice", new Vector3(-0.95f, 0.88f, 0f), new Vector3(0.25f, 0.2f, 0.3f), new Color(0.7f, 0.2f, 0.2f));
            CreateCyl(grp.transform, "Lamp", new Vector3(0.95f, 1.15f, 0.2f), new Vector3(0.08f, 0.6f, 0.08f), new Color(0.8f, 0.8f, 0.2f));

            for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2)
                    CreateBox(grp.transform, $"Leg_{x}_{z}", new Vector3(x * 1.05f, 0.35f, z * 0.45f), new Vector3(0.12f, 0.7f, 0.12f), Color.black);

            BuildWorkbenchControlPanel3D(grp.transform);
        }

        public static void BuildWorkbenchControlPanel3D(Transform workbenchRoot)
        {
            if (workbenchRoot == null) return;
            var old = workbenchRoot.Find("Control_Panel_3D");
            if (old != null) DestroyImmediate(old.gameObject);

            GameObject panel = new GameObject("Control_Panel_3D");
            panel.transform.SetParent(workbenchRoot, false);
            panel.transform.localPosition = new Vector3(0f, 0.82f, -0.45f);
            panel.transform.localRotation = Quaternion.Euler(-28f, 0f, 0f);

            GameObject tray = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tray.name = "Panel_Tray";
            tray.transform.SetParent(panel.transform, false);
            tray.transform.localScale = new Vector3(1.58f, 0.03f, 0.16f);
            DestroyImmediate(tray.GetComponent<Collider>());
            ApplySimpleColor(tray, new Color(0.11f, 0.13f, 0.17f));

            AddBtn3D(panel.transform, -0.66f, WorkbenchButtonAction.RotateYawLeft, "< Y", "Вращать влево (удерживайте ЛКМ)", new Color(0.2f, 0.38f, 0.65f), 0.14f);
            AddBtn3D(panel.transform, -0.49f, WorkbenchButtonAction.RotateYawRight, "Y >", "Вращать вправо (удерживайте ЛКМ)", new Color(0.2f, 0.38f, 0.65f), 0.14f);
            AddBtn3D(panel.transform, -0.30f, WorkbenchButtonAction.RotatePitchUp, "^ P", "Наклон вверх (удерживайте ЛКМ)", new Color(0.2f, 0.38f, 0.65f), 0.14f);
            AddBtn3D(panel.transform, -0.13f, WorkbenchButtonAction.RotatePitchDown, "P v", "Наклон вниз (удерживайте ЛКМ)", new Color(0.2f, 0.38f, 0.65f), 0.14f);
            AddBtn3D(panel.transform,  0.08f, WorkbenchButtonAction.ResetRotation, "СБРОС", "Сбросить угол детали", new Color(0.38f, 0.42f, 0.5f), 0.18f);
            AddBtn3D(panel.transform,  0.34f, WorkbenchButtonAction.FinishWork, "ГОТОВО", "Завершить ремонт детали [Space]", new Color(0.18f, 0.62f, 0.32f), 0.24f);
            AddBtn3D(panel.transform,  0.62f, WorkbenchButtonAction.CancelWork, "ВЫХОД", "Вернуться в ангар [Esc]", new Color(0.68f, 0.22f, 0.22f), 0.22f);
        }

        private static void AddBtn3D(Transform panel, float localX, WorkbenchButtonAction action, string text, string prompt, Color color, float width)
        {
            GameObject btn = GameObject.CreatePrimitive(PrimitiveType.Cube);
            btn.name = $"Btn3D_{action}";
            btn.transform.SetParent(panel, false);
            btn.transform.localPosition = new Vector3(localX, 0.025f, 0f);
            btn.transform.localScale = new Vector3(width, 0.032f, 0.11f);
            ApplySimpleColor(btn, color);

            var wbBtn = btn.AddComponent<WorkbenchButton3D>();
            wbBtn.Setup(action, text, prompt, color);

            GameObject lblObj = new GameObject("Label");
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

        private void CreateDispatchConsole(UnityEngine.Camera cam, Vector3 screenCenter, Transform c104 = null)
        {
            GameObject grp = new GameObject("--- 07_DISPATCH_CONSOLE ---");
            GameObject cObj = new GameObject("Terminal_Canvas");
            cObj.transform.SetParent(grp.transform, false);
            cObj.transform.position = screenCenter + new Vector3(0f, 0f, -0.015f);
            cObj.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);

            Canvas c = cObj.AddComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
            c.worldCamera = cam;
            cObj.GetComponent<RectTransform>().sizeDelta = new Vector2(1280f, 720f);
            var gr = cObj.AddComponent<GraphicRaycaster>();
            gr.ignoreReversedGraphics = false;
            gr.blockingObjects = GraphicRaycaster.BlockingObjects.None;

            Font font = LoadDefaultFont();
            GameObject bg = CreatePanel(cObj.transform, "BG", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.04f, 0.07f, 0.12f, 0.98f));

            Text tTitle = CreateTxt(bg.transform, "Txt_RadarTitle", "Радар чист  |  База 100%", new Vector2(0f, 1f), new Vector2(28f, -28f), new Vector2(680f, 34f), 16, font, Color.cyan);
            tTitle.rectTransform.pivot = new Vector2(0f, 0.5f);
            tTitle.alignment = TextAnchor.MiddleLeft;
            tTitle.horizontalOverflow = HorizontalWrapMode.Overflow;

            Text tJaeger = CreateTxt(bg.transform, "Txt_JaegerInfo", "Егерь 0/5", new Vector2(1f, 1f), new Vector2(-320f, -20f), new Vector2(440f, 25f), 14, font, Color.yellow);
            Button bJaeger = CreateBtn(bg.transform, "Btn_BuildJaeger", "Собрать Егеря", new Vector2(1f, 1f), new Vector2(-320f, -50f), new Vector2(260f, 32f), font, new Color(0.18f, 0.45f, 0.7f));
            Button bScan = CreateBtn(bg.transform, "Btn_ScanThreat", "Вызвать волну", new Vector2(1f, 1f), new Vector2(-90f, -50f), new Vector2(150f, 32f), font, new Color(0.2f, 0.6f, 0.35f));

            GameObject rBox = CreatePanel(bg.transform, "Radar_Screen", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(920f, 430f), new Color(0.02f, 0.05f, 0.08f, 1f));
            RadarClickHandler rClick = rBox.AddComponent<RadarClickHandler>();

            GameObject mContainer = new GameObject("Marker_Container");
            mContainer.transform.SetParent(rBox.transform, false);
            var mRect = mContainer.AddComponent<RectTransform>();
            mRect.anchorMin = Vector2.zero;
            mRect.anchorMax = Vector2.one;
            mRect.sizeDelta = Vector2.zero;

            Button[] bSlots = new Button[3];
            Button[] bShields = new Button[3];
            Button[] bRecalls = new Button[3];
            Text[] tSlots = new Text[3];
            Image[] iSlots = new Image[3];

            float[] xOffsets = { -370f, 0f, 370f };
            for (int i = 0; i < 3; i++)
            {
                GameObject slotRoot = CreatePanel(bg.transform, $"SlotRoot_{i}", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(xOffsets[i], 85f), new Vector2(350f, 80f), new Color(0.08f, 0.12f, 0.16f, 0.95f));
                Button bMain = CreateBtn(slotRoot.transform, $"Btn_MechSlot_{i}", "", new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(340f, 44f), font, new Color(0.12f, 0.45f, 0.35f));
                bSlots[i] = bMain;
                iSlots[i] = bMain.GetComponent<Image>();
                tSlots[i] = bMain.GetComponentInChildren<Text>();
                bShields[i] = CreateBtn(slotRoot.transform, $"Btn_Shield_{i}", "ЩИТ", new Vector2(0f, 0f), new Vector2(85f, 14f), new Vector2(155f, 24f), font, new Color(0.2f, 0.5f, 0.75f));
                bRecalls[i] = CreateBtn(slotRoot.transform, $"Btn_Recall_{i}", "НА БАЗУ", new Vector2(1f, 0f), new Vector2(-85f, 14f), new Vector2(155f, 24f), font, new Color(0.75f, 0.25f, 0.2f));
            }

            Text tLog = CreateTxt(bg.transform, "Txt_BattleLog", "Радар чист.", new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(1200f, 25f), 14, font, new Color(0.85f, 0.9f, 0.95f));

            TerminalUI ui = cObj.AddComponent<TerminalUI>();
            ui.Configure(tTitle, tLog, tJaeger, bJaeger, bScan, rBox.GetComponent<RectTransform>(), rClick, mContainer.transform, bSlots, bShields, bRecalls, tSlots, iSlots);

            BuildArmoryMonitor(grp.transform, c104, cam, font);
        }

        private void CreateHUD()
        {
            GameObject grp = new GameObject("--- 08_MINIMAL_HUD ---");
            GameObject cObj = new GameObject("HUD_Canvas");
            cObj.transform.SetParent(grp.transform, false);

            cObj.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler sc = cObj.AddComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1920, 1080);
            cObj.AddComponent<GraphicRaycaster>();

            Font font = LoadDefaultFont();

            Button bLeft = CreateBtn(cObj.transform, "Btn_Left", "◀ Рубка (Q)", new Vector2(0f, 0.5f), new Vector2(25f, 0f), new Vector2(150f, 75f), font, new Color(0.12f, 0.28f, 0.48f, 0.85f));
            Button bRight = CreateBtn(cObj.transform, "Btn_Right", "Рубка ▶ (E)", new Vector2(1f, 0.5f), new Vector2(-25f, 0f), new Vector2(150f, 75f), font, new Color(0.12f, 0.28f, 0.48f, 0.85f));

            GameObject top = CreatePanel(cObj.transform, "TopBar", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -15f), new Vector2(980f, 45f), new Color(0.08f, 0.1f, 0.14f, 0.85f));
            Text tRes = CreateTxt(top.transform, "Txt_Res", "КРЕДИТЫ: 220 ¢   |   СПЛАВ: 95 т.   |   БАЗА: 100%", new Vector2(0.31f, 0.5f), Vector2.zero, new Vector2(540f, 40f), 17, font, Color.yellow);
            Text tRoom = CreateTxt(top.transform, "Txt_Room", "СЕКТОР: АНГАР", new Vector2(0.77f, 0.5f), Vector2.zero, new Vector2(400f, 40f), 14, font, Color.cyan);

            GameObject pnlMech = CreatePanel(cObj.transform, "MechSelectPanel", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 85f), new Vector2(880f, 55f), new Color(0.06f, 0.08f, 0.12f, 0.9f));
            Button bPrev = CreateBtn(pnlMech.transform, "Btn_PrevMech", "◀ Смена", new Vector2(0f, 0.5f), new Vector2(65f, 0f), new Vector2(105f, 42f), font, new Color(0.18f, 0.35f, 0.55f));
            Button bNext = CreateBtn(pnlMech.transform, "Btn_NextMech", "Смена ▶", new Vector2(1f, 0.5f), new Vector2(-65f, 0f), new Vector2(105f, 42f), font, new Color(0.18f, 0.35f, 0.55f));
            Text tCurMech = CreateTxt(pnlMech.transform, "Txt_CurrentMech", "РОБОТ [1/3]", new Vector2(0.5f, 0.5f), new Vector2(-95f, 0f), new Vector2(440f, 40f), 16, font, Color.cyan);
            Button bCharge = CreateBtn(pnlMech.transform, "Btn_RechargeBattery", "ЗАРЯДИТЬ", new Vector2(0.5f, 0.5f), new Vector2(230f, 0f), new Vector2(165f, 42f), font, new Color(0.15f, 0.65f, 0.85f));

            GameObject pBox = CreatePanel(cObj.transform, "PromptBox", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 25f), new Vector2(820f, 40f), new Color(0.04f, 0.05f, 0.07f, 0.75f));
            var pImg = pBox.GetComponent<Image>();
            if (pImg != null) pImg.raycastTarget = false;
            Text tPrompt = CreateTxt(pBox.transform, "Txt_Prompt", "", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(790f, 35f), 16, font, Color.white);

            GameObject wb = CreatePanel(cObj.transform, "WorkbenchPanel", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(860f, 75f), new Color(0.08f, 0.11f, 0.16f, 0.92f));
            Text tWbTitle = CreateTxt(wb.transform, "Txt_WbTitle", "ВЕРСТАК ТО", new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(840f, 28f), 17, font, Color.yellow);
            Text tWbInfo = CreateTxt(wb.transform, "Txt_WbInfo", "Используйте 3D кнопки на столе верстака или ПКМ", new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(840f, 24f), 14, font, Color.cyan);
            wb.SetActive(false);

            GameObject vic = CreatePanel(cObj.transform, "VictoryPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(750f, 320f), new Color(0.08f, 0.12f, 0.18f, 0.98f));
            CreateTxt(vic.transform, "Txt_Vic1", "ПОБЕДА! ПРОЕКТ 'ЕГЕРЬ' ЗАВЕРШЁН!", new Vector2(0.5f, 0.8f), Vector2.zero, new Vector2(700f, 50f), 28, font, Color.yellow);
            CreateTxt(vic.transform, "Txt_Vic2", "Егерь собран и готов защищать цитадель!", new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(620f, 60f), 20, font, Color.white);
            Button bRest = CreateBtn(vic.transform, "bRest", "Играть снова", new Vector2(0.5f, 0f), new Vector2(0f, 45f), new Vector2(260f, 55f), font, new Color(0.2f, 0.75f, 0.35f));
            vic.SetActive(false);

            HUDOverlay hud = cObj.AddComponent<HUDOverlay>();
            hud.Configure(tRes, tRoom, bLeft, bRight, pnlMech, bPrev, bNext, bCharge, tCurMech, tPrompt, wb, tWbTitle, tWbInfo, vic, bRest);
        }

        public static ArmoryUI BuildArmoryMonitor(Transform consoleGroup, Transform centerScreenTransform, UnityEngine.Camera cam, Font font)
        {
            var old = consoleGroup.Find("Armory_Canvas");
            if (old != null) DestroyImmediate(old.gameObject);

            Vector3 pivot;
            if (centerScreenTransform != null && centerScreenTransform.GetComponent<Renderer>() != null)
            {
                Bounds cb = centerScreenTransform.GetComponent<Renderer>().bounds;
                pivot = new Vector3(cb.max.x, cb.center.y, cb.center.z);
            }
            else
            {
                var term = consoleGroup.Find("Terminal_Canvas");
                Vector3 termPos = term != null ? term.position : new Vector3(-14.89f, 1.38f, 2.34f);
                pivot = new Vector3(termPos.x + 0.64f, termPos.y, termPos.z + 0.01f);
            }

            Quaternion monRot = Quaternion.Euler(0f, 30f, 0f);
            Vector3 worldPos = pivot + monRot * new Vector3(0.197f, 0.095f, -0.014f);

            GameObject cObj = new GameObject("Armory_Canvas");
            cObj.transform.SetParent(consoleGroup, false);
            cObj.transform.position = worldPos;
            cObj.transform.rotation = monRot;
            cObj.transform.localScale = new Vector3(0.00050f, 0.00050f, 0.00050f);

            Canvas c = cObj.AddComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
            c.worldCamera = cam;
            c.sortingOrder = 5;
            cObj.GetComponent<RectTransform>().sizeDelta = new Vector2(700f, 950f);

            var gr = cObj.AddComponent<GraphicRaycaster>();
            gr.ignoreReversedGraphics = false;
            gr.blockingObjects = GraphicRaycaster.BlockingObjects.None;

            Font hiFont = Font.CreateDynamicFontFromOSFont("Arial", 28);
            if (hiFont == null) hiFont = font;

            GameObject border = CreatePanel(cObj.transform, "FrameBorder", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.12f, 0.68f, 0.88f, 0.85f));
            var borderImg = border.GetComponent<Image>();
            if (borderImg != null) borderImg.raycastTarget = false;

            GameObject bg = CreatePanel(cObj.transform, "BG", Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-10f, -10f), new Color(0.03f, 0.06f, 0.11f, 0.98f));

            Text titleTxt = CreateTxt(bg.transform, "Txt_Title", "АРСЕНАЛ", new Vector2(0f, 1f), new Vector2(24f, -38f), new Vector2(380f, 44f), 34, hiFont, Color.cyan);
            titleTxt.rectTransform.pivot = new Vector2(0f, 0.5f);
            titleTxt.alignment = TextAnchor.MiddleLeft;
            titleTxt.fontStyle = FontStyle.Bold;

            Button focusBtn = CreateBtn(bg.transform, "Btn_FocusMonitor", "КРУПНЕЕ", new Vector2(1f, 1f), new Vector2(-114f, -38f), new Vector2(188f, 48f), hiFont, new Color(0.14f, 0.36f, 0.56f));
            Text focusTxt = focusBtn.GetComponentInChildren<Text>();
            if (focusTxt != null) { focusTxt.fontSize = 22; focusTxt.fontStyle = FontStyle.Bold; }

            Text resTxt = CreateTxt(bg.transform, "Txt_ArmoryResources", "220 ¢   |   95 т", new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(650f, 38f), 26, hiFont, new Color(1f, 0.85f, 0.25f));
            resTxt.fontStyle = FontStyle.Bold;

            GameObject pageMech = CreatePanel(bg.transform, "Panel_MechSelect", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 412f), new Vector2(664f, 804f), new Color(0f, 0f, 0f, 0f));
            var pmImg = pageMech.GetComponent<Image>();
            if (pmImg != null) pmImg.raycastTarget = false;

            Text step1Lbl = CreateTxt(pageMech.transform, "Txt_Step1", "ВЫБЕРИТЕ РОБОТА", new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(640f, 36f), 26, hiFont, new Color(0.68f, 0.90f, 1f));
            step1Lbl.fontStyle = FontStyle.Bold;

            Button[] mechBtns = new Button[3];
            Text[] mechTxts = new Text[3];
            Image[] mechCardImgs = new Image[3];
            Image[] mechAccentImgs = new Image[3];
            float[] cardY = { -168f, -396f, -624f };

            for (int i = 0; i < 3; i++)
            {
                GameObject card = CreatePanel(pageMech.transform, $"Btn_SelectMech_{i}", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, cardY[i]), new Vector2(648f, 208f), new Color(0.08f, 0.13f, 0.19f, 0.96f));
                Button cardBtn = card.AddComponent<Button>();
                Image cardImg = card.GetComponent<Image>();

                GameObject accent = CreatePanel(card.transform, "Accent", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(9f, 0f), new Vector2(14f, 192f), Color.cyan);
                var accImg = accent.GetComponent<Image>();
                if (accImg != null) accImg.raycastTarget = false;

                Text cardTxt = CreateTxt(card.transform, "Txt_Info", $"РОБОТ {i + 1}", new Vector2(0.5f, 0.5f), new Vector2(14f, 0f), new Vector2(598f, 184f), 25, hiFont, Color.white);
                cardTxt.alignment = TextAnchor.MiddleLeft;
                cardTxt.supportRichText = true;
                cardTxt.lineSpacing = 1.25f;

                mechBtns[i] = cardBtn;
                mechTxts[i] = cardTxt;
                mechCardImgs[i] = cardImg;
                mechAccentImgs[i] = accImg;
            }

            GameObject pageLoadout = CreatePanel(bg.transform, "Panel_LoadoutMenu", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 412f), new Vector2(664f, 804f), new Color(0f, 0f, 0f, 0f));
            var plImg = pageLoadout.GetComponent<Image>();
            if (plImg != null) plImg.raycastTarget = false;

            Button backBtn = CreateBtn(pageLoadout.transform, "Btn_BackToMechs", "◀ НАЗАД", new Vector2(0f, 1f), new Vector2(98f, -32f), new Vector2(180f, 52f), hiFont, new Color(0.18f, 0.30f, 0.45f));
            var backTxt = backBtn.GetComponentInChildren<Text>();
            if (backTxt != null) { backTxt.fontSize = 24; backTxt.fontStyle = FontStyle.Bold; }

            Text mechTitle = CreateTxt(pageLoadout.transform, "Txt_SelectedMech", "СТРАЖ-1", new Vector2(1f, 1f), new Vector2(-12f, -32f), new Vector2(440f, 52f), 30, hiFont, Color.yellow);
            mechTitle.rectTransform.pivot = new Vector2(1f, 0.5f);
            mechTitle.alignment = TextAnchor.MiddleRight;
            mechTitle.fontStyle = FontStyle.Bold;

            GameObject summaryBox = CreatePanel(pageLoadout.transform, "SummaryBox", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -96f), new Vector2(648f, 54f), new Color(0.06f, 0.11f, 0.17f, 0.95f));
            var sumImg = summaryBox.GetComponent<Image>();
            if (sumImg != null) sumImg.raycastTarget = false;
            Text summary = CreateTxt(summaryBox.transform, "Txt_LoadoutSummary", "", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(630f, 46f), 22, hiFont, new Color(0.75f, 0.95f, 1f));

            Button tabWeaponsBtn = CreateBtn(pageLoadout.transform, "Btn_TabWeapons", "ОРУЖИЕ", new Vector2(0.5f, 1f), new Vector2(-164f, -162f), new Vector2(316f, 54f), hiFont, new Color(0.14f, 0.52f, 0.72f));
            var twTxt = tabWeaponsBtn.GetComponentInChildren<Text>();
            if (twTxt != null) { twTxt.fontSize = 26; twTxt.fontStyle = FontStyle.Bold; }

            Button tabShieldsBtn = CreateBtn(pageLoadout.transform, "Btn_TabShields", "ЩИТЫ", new Vector2(0.5f, 1f), new Vector2(164f, -162f), new Vector2(316f, 54f), hiFont, new Color(0.11f, 0.16f, 0.22f));
            var tsTxt = tabShieldsBtn.GetComponentInChildren<Text>();
            if (tsTxt != null) { tsTxt.fontSize = 26; tsTxt.fontStyle = FontStyle.Bold; }

            GameObject listObj = new GameObject("ItemsContainer");
            listObj.transform.SetParent(pageLoadout.transform, false);
            var listRect = listObj.AddComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0.5f, 1f);
            listRect.anchorMax = new Vector2(0.5f, 1f);
            listRect.pivot = new Vector2(0.5f, 1f);
            listRect.anchoredPosition = new Vector2(0f, -206f);
            listRect.sizeDelta = new Vector2(648f, 540f);

            var vl = listObj.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 14f;
            vl.childAlignment = TextAnchor.UpperCenter;
            vl.childControlWidth = false;
            vl.childControlHeight = false;

            Text feedback = CreateTxt(pageLoadout.transform, "Txt_ArmoryFeedback", "", new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(640f, 34f), 22, hiFont, new Color(0.75f, 0.9f, 1f));
            pageLoadout.SetActive(false);

            var ui = GetOrAdd<ArmoryUI>(cObj);
            ui.ConfigureWorldMonitor(
                bg, resTxt, focusBtn, focusTxt,
                pageMech, mechBtns, mechTxts, mechCardImgs, mechAccentImgs,
                pageLoadout, backBtn, tabWeaponsBtn, tabShieldsBtn,
                mechTitle, summary, feedback, listObj.transform);
            return ui;
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T comp = go.GetComponent<T>();
            if (comp == null) comp = go.AddComponent<T>();
            return comp;
        }

        private GameObject LoadPrefab(string path)
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
#else
            return null;
#endif
        }

        private Material LoadMaterial(string path)
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
#else
            return null;
#endif
        }

        private GameObject InstantiateModel(GameObject prefab, Transform parent)
        {
#if UNITY_EDITOR
            return (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent);
#else
            return Instantiate(prefab, parent);
#endif
        }

        private Font LoadDefaultFont()
        {
            Font f = Font.CreateDynamicFontFromOSFont("Arial", 16);
            if (f == null) f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return f;
        }

        private static void ApplySimpleColor(GameObject go, Color c)
        {
            Renderer r = go.GetComponent<Renderer>();
            if (r == null) return;
            Shader s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Standard");
            Material m = new Material(s);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            m.color = c;
            r.sharedMaterial = m;
        }

        private GameObject CreateBox(Transform parent, string name, Vector3 pos, Vector3 scale, Color c)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.transform.SetParent(parent, false);
            ApplySimpleColor(go, c);
            return go;
        }

        private GameObject CreateCyl(Transform parent, string name, Vector3 pos, Vector3 scale, Color c)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.transform.SetParent(parent, false);
            ApplySimpleColor(go, c);
            return go;
        }

        public static GameObject CreatePanel(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size, Color c)
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

        public static Text CreateTxt(Transform parent, string name, string text, Vector2 a, Vector2 pos, Vector2 size, int fontSize, Font font, Color c)
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
            t.raycastTarget = false;
            return t;
        }

        public static Button CreateBtn(Transform parent, string name, string label, Vector2 a, Vector2 pos, Vector2 size, Font font, Color bg)
        {
            GameObject p = CreatePanel(parent, name, a, a, pos, size, bg);
            Button b = p.AddComponent<Button>();
            CreateTxt(p.transform, "Text", label, new Vector2(0.5f, 0.5f), Vector2.zero, size, 15, font, Color.white);
            return b;
        }
    }
}
