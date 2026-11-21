using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HangarGame.Core;
using HangarGame.Mechs;
using HangarGame.CameraControl;
using HangarGame.Repair;
using HangarGame.Dispatch;
using HangarGame.UI;
using HangarGame.Interaction;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HangarGame.Prototype
{
    public class PrototypeSceneBuilder : MonoBehaviour
    {
        public void BuildCompleteHangarPrototype()
        {
            UnityEngine.Camera cam = CreateCameraRig();
            CreateCoreManagers();
            CreateLightingAndAtmosphere();
            CreateHangarRoom();
            CreateMechsHangar();
            CreateJaegerFrame();
            CreateWorkbench();
            CreateDispatchConsole(cam);
            CreateHUD();
        }

        private UnityEngine.Camera CreateCameraRig()
        {
            GameObject camObj = GameObject.Find("Main Camera");
            if (camObj == null)
            {
                camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
            }
            camObj.transform.position = new Vector3(0f, 1.6f, -0.6f);
            camObj.transform.rotation = Quaternion.identity;

            UnityEngine.Camera cam = camObj.GetComponent<UnityEngine.Camera>();
            if (cam == null) cam = camObj.AddComponent<UnityEngine.Camera>();
            cam.fieldOfView = 65f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 200f;

            if (camObj.GetComponent<AudioListener>() == null)
            {
                camObj.AddComponent<AudioListener>();
            }

            if (camObj.GetComponent<RoomCamera>() == null)
            {
                camObj.AddComponent<RoomCamera>();
            }

            if (camObj.GetComponent<PlayerRaycaster>() == null)
            {
                camObj.AddComponent<PlayerRaycaster>();
            }

            return cam;
        }

        private void CreateCoreManagers()
        {
            GameObject grp = new GameObject("--- 01_MANAGERS ---");
            grp.transform.position = Vector3.zero;
            grp.transform.rotation = Quaternion.identity;

            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem");
                es.transform.SetParent(grp.transform, false);
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
#if ENABLE_INPUT_SYSTEM
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
            }

            grp.AddComponent<GameManager>();
            grp.AddComponent<JaegerProject>();
            grp.AddComponent<DispatchSystem>();
            grp.AddComponent<HangarAudioLight>();
        }

        private void CreateLightingAndAtmosphere()
        {
            GameObject grp = new GameObject("--- 02_LIGHTING_AND_VFX ---");
            grp.transform.position = Vector3.zero;
            grp.transform.rotation = Quaternion.identity;

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
            Light lFillLight = fill.AddComponent<Light>();
            lFillLight.type = LightType.Directional;
            lFillLight.color = new Color(0.25f, 0.35f, 0.5f);
            lFillLight.intensity = 0.55f;
            lFillLight.shadows = LightShadows.None;

            HangarAudioLight audioLight = FindFirstObjectByType<HangarAudioLight>();

            GameObject alarm = new GameObject("Alarm_Red_Light");
            alarm.transform.SetParent(grp.transform, false);
            alarm.transform.position = new Vector3(0f, 6.2f, 0f);
            Light lAlarm = alarm.AddComponent<Light>();
            lAlarm.type = LightType.Point;
            lAlarm.color = Color.red;
            lAlarm.range = 35f;
            lAlarm.intensity = 0f;

            if (audioLight != null)
            {
                var f = typeof(HangarAudioLight).GetField("redAlarmLight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (f != null) f.SetValue(audioLight, lAlarm);
            }

            CreateSpotlight(grp.transform, "Spotlight_Mech_Hero", new Vector3(0f, 6.0f, 2.4f), Quaternion.Euler(46f, 0f, 0f), 3.4f, 70f, new Color(0.95f, 0.98f, 1f));
            CreateSpotlight(grp.transform, "Spotlight_Workbench", new Vector3(-2.6f, 3.2f, 1.8f), Quaternion.Euler(60f, 0f, 0f), 3.6f, 65f, new Color(1f, 0.92f, 0.78f));
            CreateSpotlight(grp.transform, "Spotlight_Jaeger", new Vector3(4.5f, 5.5f, 1.8f), Quaternion.Euler(45f, 25f, 0f), 2.8f, 65f, new Color(1f, 0.85f, 0.6f));
            CreateSpotlight(grp.transform, "Spotlight_Dispatch", new Vector3(0f, 3.4f, -3.4f), Quaternion.Euler(50f, 180f, 0f), 2.5f, 75f, new Color(0.7f, 0.9f, 1f));

            GameObject wbFill = new GameObject("PointLight_Workbench");
            wbFill.transform.SetParent(grp.transform, false);
            wbFill.transform.position = new Vector3(-2.6f, 1.6f, 1.8f);
            Light lFill = wbFill.AddComponent<Light>();
            lFill.type = LightType.Point;
            lFill.range = 5f;
            lFill.intensity = 2.0f;
            lFill.color = new Color(1f, 0.95f, 0.85f);

            GameObject batFill = new GameObject("PointLight_BatteryStation");
            batFill.transform.SetParent(grp.transform, false);
            batFill.transform.position = new Vector3(2.4f, 1.4f, 3.0f);
            Light lBat = batFill.AddComponent<Light>();
            lBat.type = LightType.Point;
            lBat.range = 5f;
            lBat.intensity = 2.4f;
            lBat.color = new Color(0.2f, 0.85f, 1.0f);
        }

        private void CreateSpotlight(Transform parent, string name, Vector3 pos, Quaternion rot, float intensity, float angle, Color col)
        {
            GameObject sp = new GameObject(name);
            sp.transform.SetParent(parent, false);
            sp.transform.position = pos;
            sp.transform.rotation = rot;
            Light l = sp.AddComponent<Light>();
            l.type = LightType.Spot;
            l.range = 14f;
            l.spotAngle = angle;
            l.intensity = intensity;
            l.color = col;
        }

        private GameObject LoadAsset(string path)
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
#else
            return null;
#endif
        }

        private Material LoadMaterial(string path)
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<Material>(path);
#else
            return null;
#endif
        }

        private GameObject InstantiateModel(GameObject prefab, Transform parent)
        {
            if (prefab == null) return null;
#if UNITY_EDITOR
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            if (instance == null) instance = Instantiate(prefab, parent);
            else PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            return instance;
#else
            return Instantiate(prefab, parent);
#endif
        }

        private void CreateHangarRoom()
        {
            GameObject grp = new GameObject("--- 03_HANGAR_ROOM ---");
            grp.transform.position = Vector3.zero;
            grp.transform.rotation = Quaternion.identity;

            GameObject fullPrefab = LoadAsset("Assets/Prefabs/Base/HANGARFULL.prefab");
            GameObject hangarPrefab = LoadAsset("Assets/Prefabs/Hangar/Hangar.prefab");

            if (fullPrefab != null)
            {
                GameObject fullObj = InstantiateModel(fullPrefab, grp.transform);
                fullObj.name = "Hangar_Full_Base";
                fullObj.transform.localScale = Vector3.one * 2.8f;
                fullObj.transform.position = Vector3.zero;

                Transform pWall = fullObj.transform.Find("Plane");
                if (pWall != null) pWall.gameObject.SetActive(false);

                Material baseMat = LoadMaterial("Assets/Prefabs/Base/Base.mat");
                var rends = fullObj.GetComponentsInChildren<Renderer>(false);
                if (rends.Length > 0)
                {
                    Bounds b = rends[0].bounds;
                    for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                    Vector3 offset = fullObj.transform.position - b.center;
                    fullObj.transform.position = new Vector3(offset.x, -b.min.y, offset.z);
                    if (baseMat != null)
                    {
                        foreach (var r in rends) r.sharedMaterial = baseMat;
                    }
                }
            }

            if (hangarPrefab != null)
            {
                GameObject hObj = InstantiateModel(hangarPrefab, grp.transform);
                hObj.name = "Hangar_Gantry_Rig";
                hObj.transform.localScale = Vector3.one * 1.35f;

                Transform wall1 = hObj.transform.Find("Cube.086");
                if (wall1 != null) wall1.gameObject.SetActive(false);
                Transform wall2 = hObj.transform.Find("Plane.008");
                if (wall2 != null) wall2.gameObject.SetActive(false);

                Material hangarMat = LoadMaterial("Assets/Prefabs/Hangar/Hangar.mat");
                var rends = hObj.GetComponentsInChildren<Renderer>(false);
                if (rends.Length > 0)
                {
                    Bounds b = rends[0].bounds;
                    for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                    Vector3 offset = hObj.transform.position - b.center;
                    hObj.transform.position = new Vector3(offset.x, -b.min.y + 3.8f, offset.z + 4.2f);
                    if (hangarMat != null)
                    {
                        foreach (var r in rends) r.sharedMaterial = hangarMat;
                    }
                }
            }

            CreateBox(grp.transform, "Floor", new Vector3(0f, -0.5f, 0f), new Vector3(24f, 1f, 24f), new Color(0.18f, 0.2f, 0.23f));
            CreateBox(grp.transform, "Ceiling", new Vector3(0f, 6.5f, 0f), new Vector3(24f, 0.5f, 24f), new Color(0.12f, 0.14f, 0.16f));
            CreateBox(grp.transform, "Crane_Beam", new Vector3(0f, 5.5f, 2.5f), new Vector3(22f, 0.5f, 1.0f), new Color(0.85f, 0.65f, 0.1f));
            CreateBox(grp.transform, "Wall_Back", new Vector3(0f, 3.2f, 9.5f), new Vector3(24f, 7.5f, 0.6f), new Color(0.14f, 0.16f, 0.19f));
            CreateBox(grp.transform, "Blast_Doors", new Vector3(0f, 3.0f, 9.2f), new Vector3(9f, 6.0f, 0.1f), new Color(0.22f, 0.25f, 0.28f));
            CreateBox(grp.transform, "Wall_Front", new Vector3(0f, 3.2f, -9.5f), new Vector3(24f, 7.5f, 0.6f), new Color(0.12f, 0.14f, 0.17f));
            CreateBox(grp.transform, "Wall_Left", new Vector3(-11.5f, 3.2f, 0f), new Vector3(0.6f, 7.5f, 24f), new Color(0.15f, 0.17f, 0.2f));
            CreateBox(grp.transform, "Wall_Right", new Vector3(11.5f, 3.2f, 0f), new Vector3(0.6f, 7.5f, 24f), new Color(0.15f, 0.17f, 0.2f));

            GameObject fl = new GameObject("Hangar_Floor_Collider");
            fl.transform.SetParent(grp.transform, false);
            fl.transform.localPosition = new Vector3(0f, -0.2f, 0f);
            var col = fl.AddComponent<BoxCollider>();
            col.size = new Vector3(60f, 0.4f, 60f);
        }

        private void CreateMechsHangar()
        {
            GameObject grp = new GameObject("--- 04_MECH_ATLANT ---");
            grp.transform.position = Vector3.zero;
            grp.transform.rotation = Quaternion.identity;

            var mgr = grp.AddComponent<HangarMechManager>();

            Vector3 podiumPos = new Vector3(0f, 0f, 4.2f);
            CreateCyl(grp.transform, "Podium_Pedestal", podiumPos + new Vector3(0f, 0.12f, 0f), new Vector3(6.5f, 0.24f, 6.5f), new Color(0.18f, 0.21f, 0.25f));
            CreateCyl(grp.transform, "Podium_Inner_Glow", podiumPos + new Vector3(0f, 0.16f, 0f), new Vector3(5.5f, 0.04f, 5.5f), new Color(0.15f, 0.75f, 0.95f));

            CreateBatteryStation(grp.transform, new Vector3(2.8f, 0f, 2.8f));

            var m0 = BuildMechUnit(grp.transform, 0, "Страж-1 (Лёгкий)", new Color(0.15f, 0.75f, 0.85f), 80f, 32f, 85f, 0.92f, podiumPos);
            var m1 = BuildMechUnit(grp.transform, 1, "Атлант-2 (Штурм)", new Color(0.2f, 0.45f, 0.85f), 110f, 45f, 60f, 1.0f, podiumPos);
            var m2 = BuildMechUnit(grp.transform, 2, "Голиаф-3 (Тяжёлый)", new Color(0.85f, 0.5f, 0.15f), 160f, 62f, 42f, 1.12f, podiumPos);

            mgr.Mechs.Add(m0);
            mgr.Mechs.Add(m1);
            mgr.Mechs.Add(m2);

            m0.gameObject.SetActive(true);
            m1.gameObject.SetActive(false);
            m2.gameObject.SetActive(false);
        }

        private void CreateBatteryStation(Transform parent, Vector3 pos)
        {
            GameObject bRoot = new GameObject("Battery_Charging_Station");
            bRoot.transform.SetParent(parent, false);
            bRoot.transform.position = pos;

            CreateBox(bRoot.transform, "Battery_Base", new Vector3(0f, 0.45f, 0f), new Vector3(0.85f, 0.9f, 0.85f), new Color(0.16f, 0.19f, 0.24f));
            GameObject core = CreateCyl(bRoot.transform, "Battery_Energy_Core", new Vector3(0f, 1.25f, 0f), new Vector3(0.45f, 0.7f, 0.45f), new Color(0.15f, 0.85f, 1.0f));
            CreateBox(bRoot.transform, "Battery_Top_Cap", new Vector3(0f, 1.7f, 0f), new Vector3(0.75f, 0.25f, 0.75f), new Color(0.85f, 0.65f, 0.15f));

            var col = core.GetComponent<Collider>();
            if (col == null) col = core.AddComponent<CapsuleCollider>();

            core.AddComponent<BatteryStation>();
        }

        private MechUnit BuildMechUnit(Transform parent, int id, string name, Color theme, float hp, float atk, float spd, float scaleMult, Vector3 podiumPos)
        {
            GameObject mechPrefab = LoadAsset("Assets/Prefabs/Mech01/Mech01.prefab");
            GameObject obj;
            if (mechPrefab != null)
            {
                obj = InstantiateModel(mechPrefab, parent);
            }
            else
            {
                obj = new GameObject();
                obj.transform.SetParent(parent, false);
            }
            obj.name = $"Mech_{id}_{name}";
            obj.transform.position = podiumPos + new Vector3(-0.55f, 1.8f * scaleMult, 0f);
            obj.transform.rotation = Quaternion.Euler(270f, 0f, 180f);
            obj.transform.localScale = Vector3.one * 15f * scaleMult;

            MechUnit unit = obj.GetComponent<MechUnit>();
            if (unit == null) unit = obj.AddComponent<MechUnit>();
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

            Material mechMat = LoadMaterial("Assets/Prefabs/Mech01/Mech01.mat");
            Material tintedMat = mechMat != null ? new Material(mechMat) : null;
            if (tintedMat != null)
            {
                tintedMat.color = theme;
                foreach (var r in obj.GetComponentsInChildren<Renderer>(true))
                {
                    r.sharedMaterial = tintedMat;
                }
            }

            Transform tTorso = obj.transform;
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

            GameObject brokenHeadPrefab = LoadAsset("Assets/Prefabs/Mech01/BrokenParts/HeadBroken.fbx");
            GameObject brokenLeftArmPrefab = LoadAsset("Assets/Prefabs/Mech01/BrokenParts/LeftArmBroken.fbx");
            GameObject brokenRightArmPrefab = LoadAsset("Assets/Prefabs/Mech01/BrokenParts/RightArmBroken.fbx");

            var pTorso = SetupPartComponent(tTorso.gameObject, new MechPartData
            {
                partName = $"{name}: Корпус",
                slot = PartSlot.Torso,
                attackType = DamageType.Physical,
                attackPower = 15f,
                defenseType = DamageType.Physical,
                defensePower = 60f
            }, null, Vector3.zero, null, false);

            var pHead = SetupPartComponent(tHead != null ? tHead.gameObject : null, new MechPartData
            {
                partName = $"{name}: Сенсорная Рубка",
                slot = PartSlot.Head,
                attackType = DamageType.Plasma,
                attackPower = 10f,
                defenseType = DamageType.Plasma,
                defensePower = 30f
            }, brokenHeadPrefab, new Vector3(5.03f, -0.57f, 0.27f), mechMat, true);

            var pArmL = SetupPartComponent(tArmL != null ? tArmL.gameObject : null, new MechPartData
            {
                partName = $"{name}: Энергощит",
                slot = PartSlot.LeftArm,
                attackType = DamageType.Physical,
                attackPower = 10f,
                defenseType = DamageType.Physical,
                defensePower = 70f
            }, brokenLeftArmPrefab, new Vector3(7.06f, 0.02f, 0.42f), mechMat, true);

            var pArmR = SetupPartComponent(tArmR != null ? tArmR.gameObject : null, new MechPartData
            {
                partName = $"{name}: Пушка",
                slot = PartSlot.RightArm,
                attackType = DamageType.Plasma,
                attackPower = atk,
                defenseType = DamageType.Thermal,
                defensePower = 20f
            }, brokenRightArmPrefab, new Vector3(4.15f, 0.02f, 0.42f), mechMat, true);

            var pLegs = SetupPartComponent(tLegs != null ? tLegs.gameObject : null, new MechPartData
            {
                partName = $"{name}: Приводное Шасси",
                slot = PartSlot.Legs,
                attackType = DamageType.Physical,
                attackPower = 10f,
                defenseType = DamageType.Physical,
                defensePower = 50f
            }, null, Vector3.zero, null, true);

            if (pTorso != null) unit.parts.Add(pTorso);
            if (pHead != null) unit.parts.Add(pHead);
            if (pArmL != null) unit.parts.Add(pArmL);
            if (pArmR != null) unit.parts.Add(pArmR);
            if (pLegs != null) unit.parts.Add(pLegs);

            unit.SyncHpFromParts();
            return unit;
        }

        private MechPart SetupPartComponent(GameObject target, MechPartData data, GameObject brokenPrefab, Vector3 brokenOffset, Material brokenMat = null, bool recurseMeshColliders = true)
        {
            if (target == null) return null;
            MechPart p = target.GetComponent<MechPart>();
            if (p == null) p = target.AddComponent<MechPart>();
            p.Setup(data);

            AddLocalMeshColliders(target.transform, recurseMeshColliders);

            if (brokenPrefab != null)
            {
                GameObject brokenObj = InstantiateModel(brokenPrefab, target.transform);
                brokenObj.name = "Broken_Visual";
                brokenObj.transform.localPosition = brokenOffset;
                brokenObj.transform.localRotation = Quaternion.identity;

                if (brokenMat != null)
                {
                    foreach (var br in brokenObj.GetComponentsInChildren<Renderer>(true))
                    {
                        br.sharedMaterial = brokenMat;
                    }
                }
                foreach (var col in brokenObj.GetComponentsInChildren<Collider>(true))
                {
                    DestroyImmediate(col);
                }
                brokenObj.SetActive(false);

                GameObject pristineHolder = null;
                var r = target.GetComponentInChildren<Renderer>();
                if (r != null)
                {
                    pristineHolder = r.gameObject;
                }
                else
                {
                    pristineHolder = target;
                }

                p.SetVisuals(pristineHolder, brokenObj);
            }

            return p;
        }

        private void AddLocalMeshColliders(Transform root, bool recurse)
        {
            if (root == null) return;
            var mf = root.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                var box = root.GetComponent<BoxCollider>();
                if (box == null) box = root.gameObject.AddComponent<BoxCollider>();
                box.center = mf.sharedMesh.bounds.center;
                box.size = mf.sharedMesh.bounds.size;
            }
            if (recurse)
            {
                for (int i = 0; i < root.childCount; i++)
                {
                    var child = root.GetChild(i);
                    if (child.GetComponent<MechPart>() != null) continue;
                    AddLocalMeshColliders(child, true);
                }
            }
        }

        private void CreateJaegerFrame()
        {
            GameObject grp = new GameObject("--- 05_JAEGER_FRAME ---");
            grp.transform.position = Vector3.zero;
            grp.transform.rotation = Quaternion.identity;

            Vector3 bayPos = new Vector3(4.5f, 0f, 3.2f);
            Quaternion bayRot = Quaternion.Euler(0f, -30f, 0f);

            CreateBox(grp.transform, "Tower_L", bayPos + new Vector3(-1.6f, 3.5f, 0f), new Vector3(0.35f, 7f, 3.5f), new Color(0.9f, 0.7f, 0.15f));
            CreateBox(grp.transform, "Tower_R", bayPos + new Vector3(1.6f, 3.5f, 0f), new Vector3(0.35f, 7f, 3.5f), new Color(0.9f, 0.7f, 0.15f));

            GameObject egerPrefab = LoadAsset("Assets/Prefabs/Eger/Eger.fbx");
            GameObject platePrefab = LoadAsset("Assets/Prefabs/Eger/Plate.fbx");

            GameObject egerObj;
            if (egerPrefab != null)
            {
                egerObj = InstantiateModel(egerPrefab, grp.transform);
                egerObj.name = "Jaeger_Titan_Chassis";
                egerObj.transform.position = bayPos + new Vector3(0f, 0.1f, 0f);
                egerObj.transform.rotation = bayRot;
                egerObj.transform.localScale = Vector3.one * 1.5f;
            }
            else
            {
                egerObj = new GameObject("Jaeger_Titan_Chassis");
                egerObj.transform.SetParent(grp.transform, false);
                egerObj.transform.position = bayPos + new Vector3(0f, 0.1f, 0f);
            }

            Transform[] markers = new Transform[4];
            Vector3[] platePositions = {
                new Vector3(0f, 1.3f, 0.1f),
                new Vector3(0f, 1.8f, 0.25f),
                new Vector3(0.45f, 2.1f, 0.1f),
                new Vector3(0f, 2.5f, 0.15f)
            };
            Vector3[] plateRotations = {
                Vector3.zero,
                new Vector3(12f, 0f, 0f),
                new Vector3(0f, 15f, 0f),
                Vector3.zero
            };
            Color[] stageColors = {
                Color.cyan,
                new Color(0.2f, 0.5f, 0.9f),
                new Color(0.95f, 0.45f, 0.1f),
                new Color(1f, 0.85f, 0.2f)
            };

            for (int i = 0; i < 4; i++)
            {
                GameObject markerRoot = new GameObject($"Jaeger_Plate_Stage_{i + 1}");
                markerRoot.transform.SetParent(egerObj.transform, false);
                markerRoot.transform.localPosition = platePositions[i];
                markerRoot.transform.localRotation = Quaternion.Euler(plateRotations[i]);

                if (platePrefab != null)
                {
                    GameObject pObj = InstantiateModel(platePrefab, markerRoot.transform);
                    pObj.name = "Model";
                    pObj.transform.localPosition = new Vector3(5.19f, -1.46f, 0.40f);
                    pObj.transform.localRotation = Quaternion.identity;
                    pObj.transform.localScale = Vector3.one * 0.85f;
                    Material stageMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                    stageMat.color = stageColors[i];
                    foreach (var r in pObj.GetComponentsInChildren<Renderer>(true))
                    {
                        r.sharedMaterial = stageMat;
                    }
                }
                else
                {
                    CreateBox(markerRoot.transform, "Box", Vector3.zero, new Vector3(0.6f, 0.2f, 0.6f), stageColors[i]);
                }
                markerRoot.SetActive(false);
                markers[i] = markerRoot.transform;
            }

            if (JaegerProject.Instance != null)
            {
                JaegerProject.Instance.SetMarkers(markers);
                if (platePrefab != null) JaegerProject.Instance.SetPlatePrefab(platePrefab);
            }
        }

        private void CreateWorkbench()
        {
            GameObject grp = new GameObject("--- 06_WORKBENCH ---");
            grp.transform.position = new Vector3(-2.6f, 0f, 1.8f);
            grp.transform.rotation = Quaternion.Euler(0f, 25f, 0f);
            grp.AddComponent<RepairWorkbench>();

            CreateBox(grp.transform, "Tabletop", new Vector3(0f, 0.75f, 0f), new Vector3(2.4f, 0.12f, 1.1f), new Color(0.22f, 0.24f, 0.27f));
            CreateBox(grp.transform, "Vice", new Vector3(-0.95f, 0.88f, 0f), new Vector3(0.25f, 0.2f, 0.3f), new Color(0.7f, 0.2f, 0.2f));
            CreateCyl(grp.transform, "Lamp", new Vector3(0.95f, 1.15f, 0.2f), new Vector3(0.08f, 0.6f, 0.08f), new Color(0.8f, 0.8f, 0.2f));

            for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2)
                    CreateBox(grp.transform, $"Leg_{x}_{z}", new Vector3(x * 1.05f, 0.35f, z * 0.45f), new Vector3(0.12f, 0.7f, 0.12f), Color.black);
        }

        private void CreateDispatchConsole(UnityEngine.Camera cam)
        {
            GameObject grp = new GameObject("--- 07_DISPATCH_CONSOLE ---");
            grp.transform.position = Vector3.zero;
            grp.transform.rotation = Quaternion.identity;

            CreateBox(grp.transform, "Desk", new Vector3(0f, 0.75f, -4.8f), new Vector3(3.6f, 0.15f, 1.5f), new Color(0.18f, 0.2f, 0.24f));
            CreateBox(grp.transform, "Chair", new Vector3(0f, 0.55f, -3.6f), new Vector3(0.7f, 0.8f, 0.7f), new Color(0.1f, 0.1f, 0.12f));
            CreateBox(grp.transform, "Rack_L", new Vector3(-2.2f, 1.4f, -4.9f), new Vector3(0.8f, 2.8f, 0.9f), new Color(0.12f, 0.14f, 0.18f));
            CreateBox(grp.transform, "Rack_R", new Vector3(2.2f, 1.4f, -4.9f), new Vector3(0.8f, 2.8f, 0.9f), new Color(0.12f, 0.14f, 0.18f));

            CreateBox(grp.transform, "Monitor_Bezel", new Vector3(0f, 1.85f, -5.05f), new Vector3(3.0f, 1.65f, 0.15f), new Color(0.04f, 0.08f, 0.12f));

            GameObject cObj = new GameObject("Terminal_Canvas");
            cObj.transform.SetParent(grp.transform, false);
            cObj.transform.position = new Vector3(0f, 1.85f, -4.96f);
            cObj.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            cObj.transform.localScale = new Vector3(0.00135f, 0.00135f, 1f);

            Canvas c = cObj.AddComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
            c.worldCamera = cam;
            cObj.GetComponent<RectTransform>().sizeDelta = new Vector2(1920f, 1050f);
            cObj.AddComponent<GraphicRaycaster>();

            Font font = Font.CreateDynamicFontFromOSFont("Arial", 16) ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject bg = CreatePanel(cObj.transform, "BG", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.04f, 0.07f, 0.12f, 0.98f));

            Text tTitle = CreateTxt(bg.transform, "Txt_RadarTitle", "СЕКТОР-7: РАДАР СКАНИРУЕТ ПЕРИМЕТР", new Vector2(0f, 1f), new Vector2(50f, -40f), new Vector2(850f, 50f), 24, font, Color.cyan);
            Text tJaeger = CreateTxt(bg.transform, "Txt_JaegerInfo", "ПРОЕКТ 'ЕГЕРЬ' (0/4)", new Vector2(1f, 1f), new Vector2(-480f, -30f), new Vector2(600f, 35f), 18, font, Color.yellow);
            Button bJaeger = CreateBtn(bg.transform, "Btn_BuildJaeger", "🔨 СОБРАТЬ МОДУЛЬ НА ВЕРСТАКЕ", new Vector2(1f, 1f), new Vector2(-480f, -70f), new Vector2(400f, 45f), font, new Color(0.18f, 0.45f, 0.7f));
            Button bScan = CreateBtn(bg.transform, "Btn_ScanThreat", "📡 ВЫЗВАТЬ ВОЛНУ", new Vector2(1f, 1f), new Vector2(-150f, -70f), new Vector2(220f, 45f), font, new Color(0.2f, 0.6f, 0.35f));

            GameObject rBox = CreatePanel(bg.transform, "Radar_Screen", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 55f), new Vector2(1300f, 620f), new Color(0.02f, 0.05f, 0.08f, 1f));
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

            float[] xOffsets = { -560f, 0f, 560f };
            for (int i = 0; i < 3; i++)
            {
                GameObject slotRoot = CreatePanel(bg.transform, $"SlotRoot_{i}", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(xOffsets[i], 125f), new Vector2(530f, 115f), new Color(0.08f, 0.12f, 0.16f, 0.95f));

                Button bMain = CreateBtn(slotRoot.transform, $"Btn_MechSlot_{i}", "", new Vector2(0.5f, 1f), new Vector2(0f, -38f), new Vector2(520f, 70f), font, new Color(0.12f, 0.45f, 0.35f));
                bSlots[i] = bMain;
                iSlots[i] = bMain.GetComponent<Image>();
                tSlots[i] = bMain.GetComponentInChildren<Text>();

                Button bShield = CreateBtn(slotRoot.transform, $"Btn_Shield_{i}", "🛡 ЩИТ", new Vector2(0f, 0f), new Vector2(130f, 20f), new Vector2(250f, 35f), font, new Color(0.2f, 0.5f, 0.75f));
                Button bRecall = CreateBtn(slotRoot.transform, $"Btn_Recall_{i}", "🔙 НА БАЗУ", new Vector2(1f, 0f), new Vector2(-130f, 20f), new Vector2(250f, 35f), font, new Color(0.75f, 0.25f, 0.2f));

                bShields[i] = bShield;
                bRecalls[i] = bRecall;
            }

            Text tLog = CreateTxt(bg.transform, "Txt_BattleLog", "База готова к дежурству. Радар чист.", new Vector2(0.5f, 0f), new Vector2(0f, 35f), new Vector2(1800f, 40f), 19, font, new Color(0.85f, 0.9f, 0.95f));

            TerminalUI ui = cObj.AddComponent<TerminalUI>();
            SetRef(ui, "txtRadarTitle", tTitle);
            SetRef(ui, "txtBattleLog", tLog);
            SetRef(ui, "txtJaegerInfo", tJaeger);
            SetRef(ui, "btnBuildJaeger", bJaeger);
            SetRef(ui, "btnScanThreat", bScan);
            SetRef(ui, "radarArea", rBox.GetComponent<RectTransform>());
            SetRef(ui, "radarClick", rClick);
            SetRef(ui, "markerContainer", mContainer.transform);
            SetRef(ui, "btnMechSlots", bSlots);
            SetRef(ui, "btnShieldSlots", bShields);
            SetRef(ui, "btnRecallSlots", bRecalls);
            SetRef(ui, "txtMechSlots", tSlots);
            SetRef(ui, "imgMechSlots", iSlots);
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

            Button bLeft = CreateBtn(cObj.transform, "Btn_Left", "◀ Рубка (Q)", new Vector2(0f, 0.5f), new Vector2(25f, 0f), new Vector2(150f, 75f), font, new Color(0.12f, 0.28f, 0.48f, 0.85f));
            Button bRight = CreateBtn(cObj.transform, "Btn_Right", "Рубка ▶ (E)", new Vector2(1f, 0.5f), new Vector2(-25f, 0f), new Vector2(150f, 75f), font, new Color(0.12f, 0.28f, 0.48f, 0.85f));

            GameObject top = CreatePanel(cObj.transform, "TopBar", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -15f), new Vector2(850f, 45f), new Color(0.08f, 0.1f, 0.14f, 0.85f));
            Text tRes = CreateTxt(top.transform, "Txt_Res", "КРЕДИТЫ: 500 ¢   |   СПЛАВ: 200 т.", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800f, 40f), 20, font, Color.yellow);

            GameObject pnlMech = CreatePanel(cObj.transform, "MechSelectPanel", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 85f), new Vector2(920f, 55f), new Color(0.06f, 0.08f, 0.12f, 0.9f));
            Button bPrev = CreateBtn(pnlMech.transform, "Btn_PrevMech", "◀ Смена", new Vector2(0f, 0.5f), new Vector2(65f, 0f), new Vector2(110f, 42f), font, new Color(0.18f, 0.35f, 0.55f));
            Button bNext = CreateBtn(pnlMech.transform, "Btn_NextMech", "Смена ▶", new Vector2(1f, 0.5f), new Vector2(-65f, 0f), new Vector2(110f, 42f), font, new Color(0.18f, 0.35f, 0.55f));
            Button bCharge = CreateBtn(pnlMech.transform, "Btn_RechargeBattery", "⚡ ЗАРЯДИТЬ БАТАРЕЮ", new Vector2(0.5f, 0.5f), new Vector2(250f, 0f), new Vector2(200f, 42f), font, new Color(0.15f, 0.65f, 0.85f));
            Text tCurMech = CreateTxt(pnlMech.transform, "Txt_CurrentMech", "РОБОТ [1/3]: Страж-1 (Лёгкий)", new Vector2(0.5f, 0.5f), new Vector2(-100f, 0f), new Vector2(480f, 40f), 17, font, Color.cyan);

            GameObject pBox = CreatePanel(cObj.transform, "PromptBox", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 25f), new Vector2(800f, 40f), new Color(0.04f, 0.05f, 0.07f, 0.75f));
            Text tPrompt = CreateTxt(pBox.transform, "Txt_Prompt", "", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(780f, 35f), 16, font, Color.white);

            GameObject wb = CreatePanel(cObj.transform, "WorkbenchPanel", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(980f, 180f), new Color(0.08f, 0.11f, 0.16f, 0.96f));
            Text tWbTitle = CreateTxt(wb.transform, "Txt_WbTitle", "🔧 ВЕРСТАК ТО", new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(940f, 30f), 18, font, Color.yellow);
            Text tWbInfo = CreateTxt(wb.transform, "Txt_WbInfo", "Вращение A/D/W/S или ПКМ", new Vector2(0.5f, 1f), new Vector2(0f, -48f), new Vector2(940f, 25f), 15, font, Color.cyan);

            Button bYM = CreateBtn(wb.transform, "bYM", "◀ Y", new Vector2(0.5f, 0.5f), new Vector2(-200f, 10f), new Vector2(75f, 40f), font, new Color(0.2f, 0.35f, 0.55f));
            Button bYP = CreateBtn(wb.transform, "bYP", "Y ▶", new Vector2(0.5f, 0.5f), new Vector2(-115f, 10f), new Vector2(75f, 40f), font, new Color(0.2f, 0.35f, 0.55f));
            Button bPM = CreateBtn(wb.transform, "bPM", "▲ X", new Vector2(0.5f, 0.5f), new Vector2(-30f, 10f), new Vector2(75f, 40f), font, new Color(0.2f, 0.35f, 0.55f));
            Button bPP = CreateBtn(wb.transform, "bPP", "▼ X", new Vector2(0.5f, 0.5f), new Vector2(55f, 10f), new Vector2(75f, 40f), font, new Color(0.2f, 0.35f, 0.55f));
            Button bRst = CreateBtn(wb.transform, "bRst", "↺ Сброс", new Vector2(0.5f, 0.5f), new Vector2(150f, 10f), new Vector2(95f, 40f), font, new Color(0.35f, 0.35f, 0.4f));

            Button bExit = CreateBtn(wb.transform, "bExit", "❌ ВЫЙТИ В АНГАР (ESC)", new Vector2(0.5f, 0f), new Vector2(-320f, 35f), new Vector2(280f, 48f), font, new Color(0.85f, 0.2f, 0.2f));
            Button bSwap = CreateBtn(wb.transform, "bSwap", "🔄 СМЕНИТЬ МОДУЛЬ", new Vector2(0.5f, 0f), new Vector2(0f, 35f), new Vector2(280f, 48f), font, new Color(0.2f, 0.55f, 0.85f));
            Button bFin = CreateBtn(wb.transform, "bFin", "⚡ ЗАВЕРШИТЬ (SPACE)", new Vector2(0.5f, 0f), new Vector2(320f, 35f), new Vector2(280f, 48f), font, new Color(0.15f, 0.7f, 0.3f));
            wb.SetActive(false);

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
            SetRef(hud, "mechSelectPanel", pnlMech);
            SetRef(hud, "btnPrevMech", bPrev);
            SetRef(hud, "btnNextMech", bNext);
            SetRef(hud, "btnRechargeBattery", bCharge);
            SetRef(hud, "txtCurrentMech", tCurMech);
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
    }
}
