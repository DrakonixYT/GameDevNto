using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using UnityEditor;
#endif
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
            GameObject mgr = new GameObject("--- 01_MANAGERS ---");
            mgr.transform.position = Vector3.zero;
            mgr.transform.rotation = Quaternion.identity;
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

            UnityEngine.Camera cam = UnityEngine.Camera.main;
            if (cam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<UnityEngine.Camera>();
                cam.tag = "MainCamera";
            }
            cam.transform.SetParent(mgr.transform, false);
            cam.transform.localPosition = new Vector3(0f, 1.6f, -0.6f);
            cam.transform.localRotation = Quaternion.identity;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.08f, 0.11f);

            if (cam.GetComponent<RoomCamera>() == null) cam.gameObject.AddComponent<RoomCamera>();
            if (cam.GetComponent<PlayerRaycaster>() == null) cam.gameObject.AddComponent<PlayerRaycaster>();

            CreateLighting(audioLight);
            CreateHangarRoom();
            CreateMechsHangar();
            CreateJaegerFrame();
            CreateWorkbench();
            CreateDispatchConsole(cam);
            CreateHUD();
        }

        private void CreateLighting(HangarAudioLight audioLight)
        {
            GameObject grp = new GameObject("--- 02_LIGHTING_AND_VFX ---");
            grp.transform.position = Vector3.zero; grp.transform.rotation = Quaternion.identity;

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

            CreateSpotlight(grp.transform, "Spotlight_Workbench", new Vector3(0f, 2.8f, 1.0f), Quaternion.Euler(42f, 0f, 0f), 3.5f);
            CreateSpotlight(grp.transform, "Spotlight_Mech", new Vector3(0f, 4.5f, 3.2f), Quaternion.Euler(35f, 0f, 0f), 2.0f);
            CreateSpotlight(grp.transform, "Spotlight_Jaeger", new Vector3(3.8f, 4.8f, 2.8f), Quaternion.Euler(35f, 25f, 0f), 2.4f);

            GameObject wbFill = new GameObject("PointLight_Workbench");
            wbFill.transform.SetParent(grp.transform, false);
            wbFill.transform.position = new Vector3(0f, 2.2f, 1.8f);
            Light lFill = wbFill.AddComponent<Light>();
            lFill.type = LightType.Point;
            lFill.range = 7f;
            lFill.intensity = 2.2f;
            lFill.color = new Color(1f, 0.96f, 0.9f);
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
            return instance;
#else
            return Instantiate(prefab, parent);
#endif
        }

        private void CreateHangarRoom()
        {
            GameObject grp = new GameObject("--- 03_HANGAR_ROOM ---");
            grp.transform.position = Vector3.zero; grp.transform.rotation = Quaternion.identity;

            GameObject fullPrefab = LoadAsset("Assets/Prefabs/Base/HANGARFULL.prefab");
            GameObject hangarPrefab = LoadAsset("Assets/Prefabs/Hangar/Hangar.prefab");

            if (fullPrefab != null)
            {
                GameObject fullObj = InstantiateModel(fullPrefab, grp.transform);
                fullObj.name = "Hangar_Full_Base";
                fullObj.transform.localPosition = new Vector3(13.87f, 0f, 0f);
                fullObj.transform.localRotation = Quaternion.identity;
            }

            if (hangarPrefab != null)
            {
                GameObject hObj = InstantiateModel(hangarPrefab, grp.transform);
                hObj.name = "Hangar_Gantry_Rig";
                hObj.transform.localPosition = new Vector3(23.14f, 0f, 0f);
                hObj.transform.localRotation = Quaternion.identity;
            }

            if (fullPrefab == null && hangarPrefab == null)
            {
                CreateBox(grp.transform, "Floor", new Vector3(0f, -0.5f, 0f), new Vector3(18f, 1f, 18f), new Color(0.18f, 0.2f, 0.23f));
                CreateBox(grp.transform, "Ceiling", new Vector3(0f, 5.5f, 0f), new Vector3(18f, 0.5f, 18f), new Color(0.12f, 0.14f, 0.16f));
                CreateBox(grp.transform, "Crane_Beam", new Vector3(0f, 4.8f, 2.5f), new Vector3(17f, 0.4f, 0.8f), new Color(0.85f, 0.65f, 0.1f));
                CreateBox(grp.transform, "Wall_Back", new Vector3(0f, 2.5f, 8.5f), new Vector3(18f, 6f, 0.6f), new Color(0.14f, 0.16f, 0.19f));
                CreateBox(grp.transform, "Blast_Doors", new Vector3(0f, 2.4f, 8.25f), new Vector3(7.5f, 4.8f, 0.1f), new Color(0.22f, 0.25f, 0.28f));
                CreateBox(grp.transform, "Wall_Front", new Vector3(0f, 2.5f, -8.5f), new Vector3(18f, 6f, 0.6f), new Color(0.12f, 0.14f, 0.17f));
                CreateBox(grp.transform, "Wall_Left", new Vector3(-8.8f, 2.5f, 0f), new Vector3(0.6f, 6f, 18f), new Color(0.15f, 0.17f, 0.2f));
                CreateBox(grp.transform, "Wall_Right", new Vector3(8.8f, 2.5f, 0f), new Vector3(0.6f, 6f, 18f), new Color(0.15f, 0.17f, 0.2f));
            }

            GameObject fl = new GameObject("Hangar_Floor_Collider");
            fl.transform.SetParent(grp.transform, false);
            fl.transform.localPosition = new Vector3(0f, -0.2f, 0f);
            var col = fl.AddComponent<BoxCollider>();
            col.size = new Vector3(50f, 0.4f, 50f);
        }

        private void CreateMechsHangar()
        {
            GameObject grp = new GameObject("--- 04_MECH_ATLANT ---");
            grp.transform.position = Vector3.zero; grp.transform.rotation = Quaternion.identity;

            var mgr = grp.AddComponent<HangarMechManager>();

            CreateCyl(grp.transform, "Podium_Pedestal", new Vector3(0f, 0.1f, 5.5f), new Vector3(4f, 0.18f, 4f), new Color(0.22f, 0.25f, 0.28f));
            CreateBox(grp.transform, "Scaffold_L", new Vector3(-2.2f, 2.2f, 5.5f), new Vector3(0.2f, 4.4f, 2.4f), new Color(0.85f, 0.65f, 0.1f));
            CreateBox(grp.transform, "Scaffold_R", new Vector3(2.2f, 2.2f, 5.5f), new Vector3(0.2f, 4.4f, 2.4f), new Color(0.85f, 0.65f, 0.1f));

            var m0 = BuildMechUnit(grp.transform, 0, "Страж-1 (Лёгкий)", new Color(0.15f, 0.75f, 0.85f), 80f, 32f, 85f, 0.88f);
            var m1 = BuildMechUnit(grp.transform, 1, "Атлант-2 (Штурм)", new Color(0.2f, 0.45f, 0.85f), 110f, 45f, 60f, 1.0f);
            var m2 = BuildMechUnit(grp.transform, 2, "Голиаф-3 (Тяжёлый)", new Color(0.85f, 0.5f, 0.15f), 160f, 62f, 42f, 1.18f);

            mgr.Mechs.Add(m0);
            mgr.Mechs.Add(m1);
            mgr.Mechs.Add(m2);

            m0.gameObject.SetActive(true);
            m1.gameObject.SetActive(false);
            m2.gameObject.SetActive(false);
        }

        private MechUnit BuildMechUnit(Transform parent, int id, string name, Color theme, float hp, float atk, float spd, float scaleMult)
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
            obj.transform.position = new Vector3(0f, 2.5f * scaleMult, 5.5f);
            obj.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            obj.transform.localScale = Vector3.one * scaleMult;

            MechUnit unit = obj.GetComponent<MechUnit>();
            if (unit == null) unit = obj.AddComponent<MechUnit>();
            unit.id = id;
            unit.mechName = name;
            unit.themeColor = theme;
            unit.maxHp = hp;
            unit.currentHp = hp;
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
                    r.material = tintedMat;
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
            }, null, Vector3.zero);

            var pHead = SetupPartComponent(tHead != null ? tHead.gameObject : null, new MechPartData
            {
                partName = $"{name}: Сенсорная Рубка",
                slot = PartSlot.Head,
                attackType = DamageType.Plasma,
                attackPower = 10f,
                defenseType = DamageType.Plasma,
                defensePower = 30f
            }, brokenHeadPrefab, new Vector3(5.03f, -0.57f, 0.27f), mechMat);

            var pArmL = SetupPartComponent(tArmL != null ? tArmL.gameObject : null, new MechPartData
            {
                partName = $"{name}: Энергощит",
                slot = PartSlot.LeftArm,
                attackType = DamageType.Physical,
                attackPower = 10f,
                defenseType = DamageType.Physical,
                defensePower = 70f
            }, brokenLeftArmPrefab, new Vector3(7.06f, 0.02f, 0.42f), mechMat);

            var pArmR = SetupPartComponent(tArmR != null ? tArmR.gameObject : null, new MechPartData
            {
                partName = $"{name}: Пушка",
                slot = PartSlot.RightArm,
                attackType = DamageType.Plasma,
                attackPower = atk,
                defenseType = DamageType.Thermal,
                defensePower = 20f
            }, brokenRightArmPrefab, new Vector3(4.15f, 0.02f, 0.42f), mechMat);

            var pLegs = SetupPartComponent(tLegs != null ? tLegs.gameObject : null, new MechPartData
            {
                partName = $"{name}: Приводное Шасси",
                slot = PartSlot.Legs,
                attackType = DamageType.Physical,
                attackPower = 10f,
                defenseType = DamageType.Physical,
                defensePower = 50f
            }, null, Vector3.zero);

            if (pTorso != null) unit.parts.Add(pTorso);
            if (pHead != null) unit.parts.Add(pHead);
            if (pArmL != null) unit.parts.Add(pArmL);
            if (pArmR != null) unit.parts.Add(pArmR);
            if (pLegs != null) unit.parts.Add(pLegs);

            return unit;
        }

        private MechPart SetupPartComponent(GameObject target, MechPartData data, GameObject brokenPrefab, Vector3 brokenOffset, Material brokenMat = null)
        {
            if (target == null) return null;
            MechPart p = target.GetComponent<MechPart>();
            if (p == null) p = target.AddComponent<MechPart>();
            p.Setup(data);

            AddFittedCollider(target);

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
                var r = target.GetComponent<Renderer>();
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

        private BoxCollider AddFittedCollider(GameObject go)
        {
            BoxCollider box = go.GetComponent<BoxCollider>();
            if (box == null) box = go.AddComponent<BoxCollider>();
            var rends = go.GetComponentsInChildren<Renderer>(true);
            if (rends.Length > 0)
            {
                Bounds b = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                box.center = go.transform.InverseTransformPoint(b.center);
                Vector3 s = go.transform.lossyScale;
                box.size = new Vector3(
                    Mathf.Abs(s.x) > 0.001f ? b.size.x / Mathf.Abs(s.x) : b.size.x,
                    Mathf.Abs(s.y) > 0.001f ? b.size.y / Mathf.Abs(s.y) : b.size.y,
                    Mathf.Abs(s.z) > 0.001f ? b.size.z / Mathf.Abs(s.z) : b.size.z
                );
            }
            else
            {
                box.center = Vector3.zero;
                box.size = Vector3.one;
            }
            return box;
        }

        private void CreateJaegerFrame()
        {
            GameObject grp = new GameObject("--- 05_JAEGER_FRAME ---");
            grp.transform.position = Vector3.zero; grp.transform.rotation = Quaternion.identity;

            CreateBox(grp.transform, "Tower_L", new Vector3(3.2f, 3.5f, 4.8f), new Vector3(0.35f, 7f, 3.5f), new Color(0.9f, 0.7f, 0.15f));
            CreateBox(grp.transform, "Tower_R", new Vector3(6.4f, 3.5f, 4.8f), new Vector3(0.35f, 7f, 3.5f), new Color(0.9f, 0.7f, 0.15f));

            GameObject egerPrefab = LoadAsset("Assets/Prefabs/Eger/Eger.fbx");
            GameObject platePrefab = LoadAsset("Assets/Prefabs/Eger/Plate.fbx");

            GameObject egerObj;
            if (egerPrefab != null)
            {
                egerObj = InstantiateModel(egerPrefab, grp.transform);
                egerObj.name = "Jaeger_Titan_Chassis";
                egerObj.transform.position = new Vector3(4.8f, 0.1f, 4.8f);
                egerObj.transform.rotation = Quaternion.Euler(0f, -40f, 0f);
                egerObj.transform.localScale = Vector3.one * 1.5f;
            }
            else
            {
                egerObj = new GameObject("Jaeger_Titan_Chassis");
                egerObj.transform.SetParent(grp.transform, false);
                egerObj.transform.position = new Vector3(4.8f, 0.1f, 4.8f);
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
                    foreach (var r in pObj.GetComponentsInChildren<Renderer>(true))
                    {
                        r.material.color = stageColors[i];
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
            grp.transform.position = Vector3.zero; grp.transform.rotation = Quaternion.identity;
            grp.AddComponent<RepairWorkbench>();

            CreateBox(grp.transform, "Tabletop", new Vector3(0f, 0.75f, 2.3f), new Vector3(2.4f, 0.12f, 1.1f), new Color(0.22f, 0.24f, 0.27f));
            CreateBox(grp.transform, "Vice", new Vector3(-0.95f, 0.88f, 2.3f), new Vector3(0.25f, 0.2f, 0.3f), new Color(0.7f, 0.2f, 0.2f));
            CreateCyl(grp.transform, "Lamp", new Vector3(0.95f, 1.15f, 2.5f), new Vector3(0.08f, 0.6f, 0.08f), new Color(0.8f, 0.8f, 0.2f));

            for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2)
                    CreateBox(grp.transform, $"Leg_{x}_{z}", new Vector3(x * 1.05f, 0.35f, 2.3f + z * 0.45f), new Vector3(0.12f, 0.7f, 0.12f), Color.black);
        }

        private void CreateDispatchConsole(UnityEngine.Camera cam)
        {
            GameObject grp = new GameObject("--- 07_DISPATCH_CONSOLE ---");
            grp.transform.position = Vector3.zero; grp.transform.rotation = Quaternion.identity;

            CreateBox(grp.transform, "Desk", new Vector3(0f, 0.75f, -5.2f), new Vector3(3.6f, 0.15f, 1.5f), new Color(0.18f, 0.2f, 0.24f));
            CreateBox(grp.transform, "Chair", new Vector3(0f, 0.55f, -4.0f), new Vector3(0.7f, 0.8f, 0.7f), new Color(0.1f, 0.1f, 0.12f));
            CreateBox(grp.transform, "Rack_L", new Vector3(-2.2f, 1.4f, -5.3f), new Vector3(0.8f, 2.8f, 0.9f), new Color(0.12f, 0.14f, 0.18f));
            CreateBox(grp.transform, "Rack_R", new Vector3(2.2f, 1.4f, -5.3f), new Vector3(0.8f, 2.8f, 0.9f), new Color(0.12f, 0.14f, 0.18f));

            CreateBox(grp.transform, "Monitor_Bezel", new Vector3(0f, 1.85f, -5.45f), new Vector3(2.8f, 1.55f, 0.15f), new Color(0.04f, 0.08f, 0.12f));

            GameObject cObj = new GameObject("Terminal_Canvas");
            cObj.transform.SetParent(grp.transform, false);
            cObj.transform.position = new Vector3(0f, 1.85f, -5.36f);
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
            rBox.AddComponent<Button>();
            RadarClickHandler rClick = rBox.AddComponent<RadarClickHandler>();

            GameObject mContainer = new GameObject("Marker_Container");
            mContainer.transform.SetParent(rBox.transform, false);
            var mRect = mContainer.AddComponent<RectTransform>();
            mRect.anchorMin = Vector2.zero; mRect.anchorMax = Vector2.one; mRect.sizeDelta = Vector2.zero;

            Button[] bSlots = new Button[3];
            Text[] tSlots = new Text[3];
            Image[] iSlots = new Image[3];

            float[] xOffsets = { -560f, 0f, 560f };
            for (int i = 0; i < 3; i++)
            {
                Button b = CreateBtn(bg.transform, $"Btn_MechSlot_{i}", "", new Vector2(0.5f, 0f), new Vector2(xOffsets[i], 125f), new Vector2(530f, 115f), font, new Color(0.12f, 0.45f, 0.35f));
                bSlots[i] = b;
                iSlots[i] = b.GetComponent<Image>();
                tSlots[i] = b.GetComponentInChildren<Text>();
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

            GameObject pnlMech = CreatePanel(cObj.transform, "MechSelectPanel", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 85f), new Vector2(750f, 55f), new Color(0.06f, 0.08f, 0.12f, 0.9f));
            Button bPrev = CreateBtn(pnlMech.transform, "Btn_PrevMech", "◀ Смена", new Vector2(0f, 0.5f), new Vector2(65f, 0f), new Vector2(110f, 42f), font, new Color(0.18f, 0.35f, 0.55f));
            Button bNext = CreateBtn(pnlMech.transform, "Btn_NextMech", "Смена ▶", new Vector2(1f, 0.5f), new Vector2(-65f, 0f), new Vector2(110f, 42f), font, new Color(0.18f, 0.35f, 0.55f));
            Text tCurMech = CreateTxt(pnlMech.transform, "Txt_CurrentMech", "РОБОТ [1/3]: Страж-1 (Лёгкий)", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(460f, 40f), 18, font, Color.cyan);

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
