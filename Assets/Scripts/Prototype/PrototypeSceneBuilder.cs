using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HangarGame.Core;
using HangarGame.Interaction;
using HangarGame.Mechs;
using HangarGame.Repair;
using HangarGame.Dispatch;
using HangarGame.CameraControl;
using HangarGame.UI;

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
                GameObject fullPrefab = LoadAsset("Assets/Prefabs/Base/HANGARFULL.prefab");
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

            Vector3 podiumPos = new Vector3(20.59f, 4.50f, -6.38f);
            if (c85 != null)
            {
                var r85 = c85.GetComponent<Renderer>();
                if (r85 != null)
                {
                    podiumPos = new Vector3(r85.bounds.center.x, r85.bounds.max.y, r85.bounds.center.z);
                }
                else
                {
                    podiumPos = c85.position + Vector3.up * 0.25f;
                }
            }

            Vector3 screenCenter = new Vector3(20.63f, 4.94f, -1.81f);
            if (c104 != null)
            {
                var r104 = c104.GetComponent<Renderer>();
                if (r104 != null)
                {
                    screenCenter = r104.bounds.center;
                }
                else
                {
                    screenCenter = c104.position;
                }
            }

            Vector3 mid = Vector3.Lerp(podiumPos, screenCenter, 0.48f);
            Vector3 camPos = new Vector3(podiumPos.x, screenCenter.y + 0.15f, mid.z);
            float hYaw = 180f;
            float dYaw = 0f;

            UnityEngine.Camera cam = CreateCameraRig(camPos, hYaw, dYaw);
            CreateCoreManagers();
            CreateLightingAndAtmosphere(podiumPos, screenCenter);
            CreateHangarRoom(hf);
            CreateMechsHangar(podiumPos);
            Vector3 wbPos = camPos + new Vector3(-1.75f, -0.75f, 0.2f);
            CreateWorkbench(wbPos);
            CreateJaegerFrame(wbPos + new Vector3(0f, 0f, 1.5f));
            CreateDispatchConsole(cam, screenCenter);
            CreateHUD();
        }

        private UnityEngine.Camera CreateCameraRig(Vector3 camPos, float hYaw, float dYaw)
        {
            GameObject camObj = GameObject.Find("Main Camera");
            if (camObj == null)
            {
                camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
            }
            camObj.transform.position = camPos;
            camObj.transform.rotation = Quaternion.Euler(0f, hYaw, 0f);

            UnityEngine.Camera cam = camObj.GetComponent<UnityEngine.Camera>();
            if (cam == null) cam = camObj.AddComponent<UnityEngine.Camera>();
            cam.fieldOfView = 65f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 200f;

            if (camObj.GetComponent<AudioListener>() == null)
            {
                camObj.AddComponent<AudioListener>();
            }

            RoomCamera rCam = camObj.GetComponent<RoomCamera>();
            if (rCam == null) rCam = camObj.AddComponent<RoomCamera>();
            rCam.ConfigureAnchor(camPos, camPos, hYaw, dYaw);

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

            if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
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

        private void CreateLightingAndAtmosphere(Vector3 podiumPos, Vector3 screenCenter)
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

            HangarAudioLight audioLight = FindAnyObjectByType<HangarAudioLight>();

            GameObject spMech = CreateSpotlight(grp.transform, "Spotlight_Mech_Hero", podiumPos + new Vector3(0f, 2.5f, 1.5f), Quaternion.Euler(40f, 180f, 0f), new Color(0.9f, 0.95f, 1.0f), 5.5f, 15f, 60f);
            if (audioLight != null) SetRef(audioLight, "mechSpotlight", spMech.GetComponent<Light>());

            GameObject spScreen = CreateSpotlight(grp.transform, "Spotlight_Screen", screenCenter + new Vector3(0f, 2.0f, -1.2f), Quaternion.Euler(45f, 0f, 0f), new Color(0.6f, 0.8f, 1.0f), 2.5f, 8f, 70f);

            GameObject spJ = CreateSpotlight(grp.transform, "Spotlight_Jaeger", podiumPos + new Vector3(-2.5f, 2.8f, 2.5f), Quaternion.Euler(55f, 70f, 0f), new Color(1.0f, 0.7f, 0.3f), 3.0f, 10f, 55f);
            if (audioLight != null) SetRef(audioLight, "jaegerSpotlight", spJ.GetComponent<Light>());
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

        private GameObject LoadAsset(string path)
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

        private void CreateHangarRoom(GameObject hf)
        {
            GameObject grp = new GameObject("--- 03_HANGAR_ROOM ---");
            grp.transform.position = Vector3.zero;
            grp.transform.rotation = Quaternion.identity;

            if (hf == null)
            {
                GameObject fullPrefab = LoadAsset("Assets/Prefabs/Base/HANGARFULL.prefab");
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
            grp.transform.position = Vector3.zero;
            grp.transform.rotation = Quaternion.identity;

            var mgr = grp.AddComponent<HangarMechManager>();

            CreateCyl(grp.transform, "Podium_Inner_Glow", podiumPos + new Vector3(0f, 0.02f, 0f), new Vector3(1.25f, 0.02f, 1.25f), new Color(0.15f, 0.75f, 0.95f));

            Vector3 batteryPos = podiumPos + new Vector3(1.4f, -0.25f, 0.7f);
            CreateBatteryStation(grp.transform, batteryPos);

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
            obj.transform.position = podiumPos;
            obj.transform.rotation = Quaternion.Euler(270f, 0f, 0f);
            obj.transform.localScale = Vector3.one * 5.0f * scaleMult;

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
                if (tintedMat.HasProperty("_BaseColor")) tintedMat.SetColor("_BaseColor", theme);
                if (tintedMat.HasProperty("_Color")) tintedMat.color = theme;
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

            GameObject brokenHead = LoadAsset("Assets/Prefabs/Mech01/HeadLow02.fbx");
            GameObject brokenArmL = LoadAsset("Assets/Prefabs/Mech01/LeftArmLow02.fbx");
            GameObject brokenArmR = LoadAsset("Assets/Prefabs/Mech01/RightArmLow02.fbx");
            GameObject brokenLegs = LoadAsset("Assets/Prefabs/Mech01/LowerBody02.fbx");

            Material brokenMat = LoadMaterial("Assets/Prefabs/Mech01/Materials/brokenMat.mat");
            if (brokenMat == null)
            {
                brokenMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                brokenMat.color = new Color(0.35f, 0.35f, 0.35f);
            }

            var pTorso = SetupPartComponent(tTorso.gameObject, new MechPartData
            {
                partName = "Корпус (Торс)",
                slot = PartSlot.Torso,
                attackPower = 0f,
                defensePower = 35f,
                repairCredits = 60,
                repairAlloy = 30
            }, null, Vector3.zero, null, false);
            if (pTorso != null) unit.parts.Add(pTorso);

            var pHead = SetupPartComponent(tHead != null ? tHead.gameObject : null, new MechPartData
            {
                partName = "Голова (Сенсоры)",
                slot = PartSlot.Head,
                attackPower = 0f,
                defensePower = 10f,
                repairCredits = 40,
                repairAlloy = 20
            }, brokenHead, Vector3.zero, brokenMat);
            if (pHead != null) unit.parts.Add(pHead);

            var pArmL = SetupPartComponent(tArmL != null ? tArmL.gameObject : null, new MechPartData
            {
                partName = "Левая рука (Щит)",
                slot = PartSlot.LeftArm,
                attackPower = 10f,
                defensePower = 20f,
                repairCredits = 45,
                repairAlloy = 25
            }, brokenArmL, Vector3.zero, brokenMat);
            if (pArmL != null) unit.parts.Add(pArmL);

            var pArmR = SetupPartComponent(tArmR != null ? tArmR.gameObject : null, new MechPartData
            {
                partName = "Правая рука (Орудие)",
                slot = PartSlot.RightArm,
                attackPower = 30f,
                defensePower = 5f,
                repairCredits = 50,
                repairAlloy = 25
            }, brokenArmR, Vector3.zero, brokenMat);
            if (pArmR != null) unit.parts.Add(pArmR);

            var pLegs = SetupPartComponent(tLegs != null ? tLegs.gameObject : null, new MechPartData
            {
                partName = "Шасси (Ноги)",
                slot = PartSlot.Legs,
                attackPower = 0f,
                defensePower = 15f,
                repairCredits = 40,
                repairAlloy = 20
            }, brokenLegs, Vector3.zero, brokenMat);
            if (pLegs != null) unit.parts.Add(pLegs);

            var activeRends = obj.GetComponentsInChildren<Renderer>(false);
            if (activeRends.Length > 0)
            {
                Bounds b = activeRends[0].bounds;
                for (int i = 1; i < activeRends.Length; i++) b.Encapsulate(activeRends[i].bounds);
                Vector3 shift = podiumPos - new Vector3(b.center.x, b.min.y, b.center.z);
                obj.transform.position += shift;
            }

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
                AddLocalMeshColliders(brokenObj.transform, true);
                brokenObj.SetActive(false);

                var r = target.GetComponent<Renderer>() ?? target.GetComponentInChildren<Renderer>();
                p.SetVisuals(r, brokenObj);
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
            var smr = root.GetComponent<SkinnedMeshRenderer>();
            if (smr != null && smr.sharedMesh != null)
            {
                var box = root.GetComponent<BoxCollider>();
                if (box == null) box = root.gameObject.AddComponent<BoxCollider>();
                box.center = smr.sharedMesh.bounds.center;
                box.size = smr.sharedMesh.bounds.size;
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

        private void CreateJaegerFrame(Vector3 bayPos)
        {
            GameObject grp = new GameObject("--- 05_JAEGER_FRAME ---");
            grp.transform.position = bayPos;
            grp.transform.rotation = Quaternion.Euler(0f, 270f, 0f);

            GameObject egerPrefab = LoadAsset("Assets/Prefabs/Eger/Eger.fbx");
            GameObject platePrefab = LoadAsset("Assets/Prefabs/Eger/Plate.fbx");

            Material egerMat = LoadMaterial("Assets/Prefabs/Eger/Materials/EgerMat.mat");
            if (egerMat == null)
            {
                egerMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                egerMat.color = new Color(0.35f, 0.38f, 0.44f);
            }

            if (egerPrefab != null)
            {
                GameObject eObj = InstantiateModel(egerPrefab, grp.transform);
                eObj.name = "Jaeger_Chassis";
                eObj.transform.localPosition = Vector3.zero;
                eObj.transform.localRotation = Quaternion.identity;
                eObj.transform.localScale = Vector3.one * 1.0f;
                foreach (var r in eObj.GetComponentsInChildren<Renderer>(true))
                {
                    r.sharedMaterial = egerMat;
                }
            }
            else
            {
                CreateBox(grp.transform, "Gantry_L", new Vector3(-1.8f, 2.5f, 0f), new Vector3(0.35f, 5.0f, 0.35f), new Color(0.3f, 0.35f, 0.4f));
                CreateBox(grp.transform, "Gantry_R", new Vector3(1.8f, 2.5f, 0f), new Vector3(0.35f, 5.0f, 0.35f), new Color(0.3f, 0.35f, 0.4f));
                CreateBox(grp.transform, "Gantry_Top", new Vector3(0f, 5.0f, 0f), new Vector3(3.95f, 0.35f, 0.35f), new Color(0.85f, 0.65f, 0.15f));
            }

            Transform[] markers = new Transform[4];
            Vector3[] stageOffsets = {
                new Vector3(0f, 0.45f, 0f),
                new Vector3(0f, 1.25f, 0.05f),
                new Vector3(-0.6f, 0.95f, 0.15f),
                new Vector3(0f, 1.85f, -0.05f)
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
                markerRoot.transform.SetParent(grp.transform, false);
                markerRoot.transform.localPosition = stageOffsets[i];
                markerRoot.transform.localRotation = Quaternion.identity;

                if (platePrefab != null)
                {
                    GameObject pObj = InstantiateModel(platePrefab, markerRoot.transform);
                    pObj.name = "Plate_Mesh";
                    pObj.transform.localPosition = new Vector3(5.19f, -1.46f, 0.40f);
                    pObj.transform.localRotation = Quaternion.identity;
                    pObj.transform.localScale = Vector3.one * 1.0f;
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

        private void CreateWorkbench(Vector3 wbPos)
        {
            GameObject grp = new GameObject("--- 06_WORKBENCH ---");
            grp.transform.position = wbPos;
            grp.transform.rotation = Quaternion.Euler(0f, 270f, 0f);
            grp.AddComponent<RepairWorkbench>();

            CreateBox(grp.transform, "Tabletop", new Vector3(0f, 0.75f, 0f), new Vector3(2.4f, 0.12f, 1.1f), new Color(0.22f, 0.24f, 0.27f));
            CreateBox(grp.transform, "Vice", new Vector3(-0.95f, 0.88f, 0f), new Vector3(0.25f, 0.2f, 0.3f), new Color(0.7f, 0.2f, 0.2f));
            CreateCyl(grp.transform, "Lamp", new Vector3(0.95f, 1.15f, 0.2f), new Vector3(0.08f, 0.6f, 0.08f), new Color(0.8f, 0.8f, 0.2f));

            for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2)
                    CreateBox(grp.transform, $"Leg_{x}_{z}", new Vector3(x * 1.05f, 0.35f, z * 0.45f), new Vector3(0.12f, 0.7f, 0.12f), Color.black);
        }

        private void CreateDispatchConsole(UnityEngine.Camera cam, Vector3 screenCenter)
        {
            GameObject grp = new GameObject("--- 07_DISPATCH_CONSOLE ---");
            grp.transform.position = Vector3.zero;
            grp.transform.rotation = Quaternion.identity;

            GameObject cObj = new GameObject("Terminal_Canvas");
            cObj.transform.SetParent(grp.transform, false);
            cObj.transform.position = screenCenter + new Vector3(0f, 0f, -0.015f);
            cObj.transform.rotation = Quaternion.identity;
            cObj.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);

            Canvas c = cObj.AddComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
            c.worldCamera = cam;
            cObj.GetComponent<RectTransform>().sizeDelta = new Vector2(1280f, 720f);
            var gr = cObj.AddComponent<GraphicRaycaster>();
            gr.ignoreReversedGraphics = false;

            Font font = Font.CreateDynamicFontFromOSFont("Arial", 16) ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject bg = CreatePanel(cObj.transform, "BG", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.04f, 0.07f, 0.12f, 0.98f));

            Text tTitle = CreateTxt(bg.transform, "Txt_RadarTitle", "СЕКТОР-7: РАДАР СКАНИРУЕТ ПЕРИМЕТР", new Vector2(0f, 1f), new Vector2(40f, -30f), new Vector2(700f, 35f), 17, font, Color.cyan);
            tTitle.horizontalOverflow = HorizontalWrapMode.Overflow;

            Text tJaeger = CreateTxt(bg.transform, "Txt_JaegerInfo", "ПРОЕКТ 'ЕГЕРЬ' (0/4)", new Vector2(1f, 1f), new Vector2(-320f, -20f), new Vector2(380f, 25f), 14, font, Color.yellow);
            Button bJaeger = CreateBtn(bg.transform, "Btn_BuildJaeger", "🔨 СОБРАТЬ МОДУЛЬ НА ВЕРСТАКЕ", new Vector2(1f, 1f), new Vector2(-320f, -50f), new Vector2(260f, 32f), font, new Color(0.18f, 0.45f, 0.7f));
            Button bScan = CreateBtn(bg.transform, "Btn_ScanThreat", "📡 ВЫЗВАТЬ ВОЛНУ", new Vector2(1f, 1f), new Vector2(-90f, -50f), new Vector2(150f, 32f), font, new Color(0.2f, 0.6f, 0.35f));

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

                Button bMain = CreateBtn(slotRoot.transform, $"Btn_MechSlot_{i}", "", new Vector2(0.5f, 1f), new Vector2(0f, -25f), new Vector2(340f, 48f), font, new Color(0.12f, 0.45f, 0.35f));
                bSlots[i] = bMain;
                iSlots[i] = bMain.GetComponent<Image>();
                tSlots[i] = bMain.GetComponentInChildren<Text>();

                Button bShield = CreateBtn(slotRoot.transform, $"Btn_Shield_{i}", "🛡 ЩИТ", new Vector2(0f, 0f), new Vector2(85f, 14f), new Vector2(155f, 24f), font, new Color(0.2f, 0.5f, 0.75f));
                Button bRecall = CreateBtn(slotRoot.transform, $"Btn_Recall_{i}", "🔙 НА БАЗУ", new Vector2(1f, 0f), new Vector2(-85f, 14f), new Vector2(155f, 24f), font, new Color(0.75f, 0.25f, 0.2f));

                bShields[i] = bShield;
                bRecalls[i] = bRecall;
            }

            Text tLog = CreateTxt(bg.transform, "Txt_BattleLog", "База готова к дежурству. Радар чист.", new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(1200f, 25f), 14, font, new Color(0.85f, 0.9f, 0.95f));

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
            Button bPM = CreateBtn(wb.transform, "bPM", "▲ P", new Vector2(0.5f, 0.5f), new Vector2(-30f, 10f), new Vector2(75f, 40f), font, new Color(0.2f, 0.35f, 0.55f));
            Button bPP = CreateBtn(wb.transform, "bPP", "P ▼", new Vector2(0.5f, 0.5f), new Vector2(55f, 10f), new Vector2(75f, 40f), font, new Color(0.2f, 0.35f, 0.55f));
            Button bRst = CreateBtn(wb.transform, "bRst", "Сброс", new Vector2(0.5f, 0.5f), new Vector2(145f, 10f), new Vector2(85f, 40f), font, new Color(0.35f, 0.38f, 0.45f));

            Button bFin = CreateBtn(wb.transform, "bFin", "✔ Готово (Пробел)", new Vector2(0.5f, 0.5f), new Vector2(270f, 10f), new Vector2(145f, 40f), font, new Color(0.2f, 0.65f, 0.35f));
            Button bSwap = CreateBtn(wb.transform, "bSwap", "🔄 Замена", new Vector2(0.5f, 0.5f), new Vector2(405f, 10f), new Vector2(105f, 40f), font, new Color(0.45f, 0.35f, 0.65f));
            Button bExit = CreateBtn(wb.transform, "bExit", "✖ Выход (Esc)", new Vector2(0.5f, 0.5f), new Vector2(505f, 10f), new Vector2(110f, 40f), font, new Color(0.65f, 0.25f, 0.25f));
            wb.SetActive(false);

            GameObject vic = CreatePanel(cObj.transform, "VictoryPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(750f, 320f), new Color(0.08f, 0.12f, 0.18f, 0.98f));
            CreateTxt(vic.transform, "Txt_Vic1", "ПОБЕДА! ПРОЕКТ 'ЕГЕРЬ' ЗАВЕРШЁН!", new Vector2(0.5f, 0.8f), Vector2.zero, new Vector2(700f, 50f), 28, font, Color.yellow);
            CreateTxt(vic.transform, "Txt_Vic2", "Тяжелый страж собран и готов защищать цитадель. Земля спасена от вторжения кайдзю!", new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(620f, 60f), 20, font, Color.white);
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
            t.raycastTarget = false;
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
