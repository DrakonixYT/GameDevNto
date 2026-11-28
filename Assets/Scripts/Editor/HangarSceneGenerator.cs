#if UNITY_EDITOR
using System.Collections.Generic;
using HangarGame.Armory;
using HangarGame.CameraControl;
using HangarGame.Core;
using HangarGame.Dispatch;
using HangarGame.Mechs;
using HangarGame.Prototype;
using HangarGame.Repair;
using HangarGame.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HangarGame.EditorTools
{
    public static class HangarSceneGenerator
    {
        private const string ScenePath = "Assets/Scenes/HangarMainScene.unity";

        [MenuItem("Tools/Bake Hangar Scene (Full Reset)", false, 1)]
        [MenuItem("Pacific Rim/Bake Hangar Scene", false, 1)]
        public static void BakeIntoCurrentScene()
        {
            var activeScene = OpenMainScene();

            string[] roots =
            {
                "--- 01_MANAGERS ---", "--- 02_LIGHTING_AND_VFX ---", "--- 03_HANGAR_ROOM ---",
                "--- 04_MECH_ATLANT ---", "--- 05_JAEGER_FRAME ---", "--- 06_WORKBENCH ---",
                "--- 07_DISPATCH_CONSOLE ---", "--- 08_MINIMAL_HUD ---",
                "Hangar_Room", "--- CORE MANAGERS ---", "HUD_Canvas", "EventSystem", "Main Camera"
            };
            foreach (var objName in roots)
            {
                GameObject obj = GameObject.Find(objName);
                if (obj != null) Object.DestroyImmediate(obj);
            }

            var builderObj = new GameObject("--- SCENE GENERATOR ---");
            builderObj.AddComponent<PrototypeSceneBuilder>().BuildCompleteHangarPrototype();
            Object.DestroyImmediate(builderObj);

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log("[Hangar] Сцена пересобрана и сохранена.");
        }

        [MenuItem("Tools/Upgrade Hangar Scene (Preserve Art)", false, 2)]
        public static void UpgradeCurrentScenePreserveArt()
        {
            var activeScene = OpenMainScene();

            SetupManagers();
            SetupMechs();
            SetupWorkbench();
            SetupJaeger();
            SetupUIAndCamera();

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log("[Hangar] Сцена обновлена с сохранением моделей и материалов.");
        }

        private static Scene OpenMainScene()
        {
            var activeScene = SceneManager.GetActiveScene();
            return activeScene.path == ScenePath
                ? activeScene
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        private static void SetupManagers()
        {
            GameObject mgrs = GameObject.Find("--- 01_MANAGERS ---");
            if (mgrs == null) return;

            ModelUtils.GetOrAdd<HangarAudio>(mgrs);
            var alarm = ModelUtils.GetOrAdd<HangarAlarmLight>(mgrs);
            var alarmLightObj = GameObject.Find("Spotlight_Alarm_Red");
            if (alarmLightObj != null) alarm.ConfigureLight(alarmLightObj.GetComponent<Light>());
            EditorUtility.SetDirty(alarm);

            var gm = ModelUtils.GetOrAdd<GameManager>(mgrs);
            gm.startingCredits = 220;
            gm.startingAlloy = 95;
            gm.maxBaseHp = 100f;
            EditorUtility.SetDirty(gm);

            var disp = mgrs.GetComponent<DispatchSystem>();
            if (disp != null)
            {
                disp.firstEnemyDelay = 9f;
                disp.minSpawnInterval = 18f;
                disp.maxSpawnInterval = 27f;
                disp.workbenchTimerMultiplier = 0.55f;
                disp.maxActiveEnemies = 4;
                disp.breachCreditsPenalty = 50;
                disp.breachAlloyPenalty = 25;
                disp.breachBaseDamage = 25f;
                disp.combatEngageRange = 185f;
                disp.holdToFightRange = 145f;
                disp.enemyAggroRange = 210f;
                EditorUtility.SetDirty(disp);
            }

            var armory = ModelUtils.GetOrAdd<ArmoryManager>(mgrs);
            armory.ResetCatalogFromDefaults();
            armory.SetWeaponPrefab("weapon_spear", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Eger/EgerSpear.fbx"));
            armory.SetWeaponPrefab("shield_bastion", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Eger/EgerShield.fbx"));
            EditorUtility.SetDirty(armory);
        }

        private static void SetupMechs()
        {
            var mechMgr = Object.FindAnyObjectByType<HangarMechManager>();
            if (mechMgr == null) return;

            Transform bay0 = mechMgr.transform.Find("Bay_Mech_0_Страж-1")
                ?? mechMgr.transform.Find("Mech_0_Страж-1 (Лёгкий) (1)");

            foreach (var u in Object.FindObjectsByType<MechUnit>(FindObjectsInactive.Include))
            {
                if (mechMgr.Mechs.Contains(u)) continue;
                if (bay0 == null) bay0 = u.transform;
                foreach (var p in u.GetComponentsInChildren<MechPart>(true)) Object.DestroyImmediate(p);
                foreach (var col in u.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(col);
                Object.DestroyImmediate(u);
            }

            Material holoMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/Holo.mat");
            GameObject brokenHead = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Mech01/BrokenParts/HeadBroken.fbx");
            GameObject brokenLeft = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Mech01/BrokenParts/LeftArmBroken.fbx");
            GameObject brokenRight = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Mech01/BrokenParts/RightArmBroken.fbx");

            float[] hp = { 85f, 120f, 175f };
            float[] atk = { 18f, 26f, 34f };
            float[] spd = { 95f, 62f, 40f };

            for (int i = 0; i < mechMgr.Mechs.Count; i++)
            {
                var mech = mechMgr.Mechs[i];
                if (mech == null) continue;
                if (i < hp.Length)
                {
                    mech.maxHp = hp[i];
                    mech.currentHp = hp[i];
                    mech.attackPower = atk[i];
                    mech.moveSpeed = spd[i];
                }
                ModelUtils.ApplyPrimaryMaterial(mech.gameObject, holoMat);
                mech.EnsurePartsLoaded();
                mech.GetPartBySlot(PartSlot.Head)?.AttachBrokenVisual(brokenHead);
                mech.GetPartBySlot(PartSlot.LeftArm)?.AttachBrokenVisual(brokenLeft);
                mech.GetPartBySlot(PartSlot.RightArm)?.AttachBrokenVisual(brokenRight);

                var battery = mech.transform.parent?.Find("Battery_Charging_Station/Battery_Energy_Core");
                if (battery != null) ModelUtils.GetOrAdd<BatteryStation>(battery.gameObject);
                EditorUtility.SetDirty(mech);
            }

            if (bay0 == null && mechMgr.Mechs.Count > 0 && mechMgr.Mechs[0] != null)
            {
                GameObject created = Object.Instantiate(mechMgr.Mechs[0].gameObject, mechMgr.transform, false);
                created.name = "Bay_Mech_0_Страж-1";
                created.transform.localPosition = new Vector3(-15.07f, 1.734f, -5.07f);
                created.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                created.transform.localScale = Vector3.one * 20f;
                StripBayDecor(created);
                bay0 = created.transform;
            }

            if (bay0 != null)
            {
                bay0.name = "Bay_Mech_0_Страж-1";
                StripBayDecor(bay0.gameObject);
                Bounds bay0Bounds = ModelUtils.CalcBounds(bay0.gameObject, true);

                ModelUtils.ApplyPrimaryMaterial(bay0.gameObject, AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/Mech01/Mech01_Guard_Cyan.mat"));
                Transform bay1 = RebuildBayClone(mechMgr.transform, bay0, "Bay_Mech_1_Атлант-2", 1.06f, AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/Mech01/Mech01_Atlant_Blue.mat"), bay0Bounds.min.y);
                Transform bay2 = RebuildBayClone(mechMgr.transform, bay0, "Bay_Mech_2_Голиаф-3", 1.14f, AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/Mech01/Mech01_Goliath_Orange.mat"), bay0Bounds.min.y);

                var bayList = new List<Transform> { bay0, bay1, bay2 };
                for (int i = 0; i < bayList.Count; i++)
                {
                    if (bayList[i] != null) bayList[i].gameObject.SetActive(i == 0);
                    if (i < mechMgr.Mechs.Count && mechMgr.Mechs[i] != null)
                    {
                        mechMgr.Mechs[i].SetBayVisualRoot(bayList[i]);
                        EditorUtility.SetDirty(mechMgr.Mechs[i]);
                    }
                }
                mechMgr.ConfigureBayMechs(bayList);
            }

            mechMgr.startFirstMechDamaged = true;
            mechMgr.firstMechStartHpRatio = 0.58f;
            mechMgr.exitDuration = 1.0f;
            mechMgr.enterDuration = 1.0f;
            mechMgr.verticalLiftOffset = 0.9f;
            mechMgr.bayVerticalLiftOffset = 9.5f;
            EditorUtility.SetDirty(mechMgr);
        }

        private static Transform RebuildBayClone(Transform parent, Transform source, string cloneName, float scaleMult, Material mat, float floorMinY)
        {
            string prefix = cloneName.Length >= 10 ? cloneName.Substring(0, 10) : cloneName;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var ch = parent.GetChild(i);
                if (ch != source && (ch.name == cloneName || ch.name.StartsWith(prefix)))
                    Object.DestroyImmediate(ch.gameObject);
            }

            GameObject clone = Object.Instantiate(source.gameObject, parent, false);
            clone.name = cloneName;
            clone.transform.localPosition = source.localPosition;
            clone.transform.localRotation = source.localRotation;
            clone.transform.localScale = source.localScale * scaleMult;
            StripBayDecor(clone);
            ModelUtils.ApplyPrimaryMaterial(clone, mat);

            Bounds b = ModelUtils.CalcBounds(clone, true);
            if (b.size.sqrMagnitude > 0.0001f)
                clone.transform.position += new Vector3(0f, floorMinY - b.min.y, 0f);

            clone.SetActive(false);
            return clone.transform;
        }

        private static void StripBayDecor(GameObject go)
        {
            foreach (var u in go.GetComponentsInChildren<MechUnit>(true)) Object.DestroyImmediate(u);
            foreach (var p in go.GetComponentsInChildren<MechPart>(true)) Object.DestroyImmediate(p);
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            foreach (var tr in go.GetComponentsInChildren<Transform>(true))
                if (tr != null && (tr.name == "Broken_Visual" || tr.name.StartsWith("Equipped_")))
                    Object.DestroyImmediate(tr.gameObject);
        }

        private static void SetupWorkbench()
        {
            GameObject wbObj = GameObject.Find("--- 06_WORKBENCH ---");
            if (wbObj == null) return;

            var wb = ModelUtils.GetOrAdd<RepairWorkbench>(wbObj);
            wb.platePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Eger/Plate.fbx");
            wb.tableRestOffset = new Vector3(0f, 1.00f, -0.04f);
            wb.normalizedPartExtent = 0.64f;
            EditorUtility.SetDirty(wb);

            PrototypeSceneBuilder.BuildWorkbenchControlPanel3D(wbObj.transform);
        }

        private static void SetupJaeger()
        {
            var jaeger = Object.FindAnyObjectByType<JaegerProject>();
            if (jaeger == null) return;

            var frame = GameObject.Find("--- 05_JAEGER_FRAME ---");
            GameObject egerRoot = GameObject.Find("Eger")
                ?? (frame != null ? (frame.transform.Find("Eger")?.gameObject ?? frame.transform.Find("Jaeger_Chassis")?.gameObject) : null);
            if (egerRoot == null) return;

            jaeger.stageVisualParts = new[]
            {
                egerRoot.transform.Find("EgerHead"),
                egerRoot.transform.Find("EgerLeftArm"),
                egerRoot.transform.Find("EgerRightArm"),
                egerRoot.transform.Find("EgerShield"),
                egerRoot.transform.Find("EgerSpear")
            };
            jaeger.stageWorkbenchPrefabs = new[]
            {
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Eger/EgerHead.fbx"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Eger/EgerLeftArm.fbx"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Eger/EgerRightArm.fbx"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Eger/EgerShield.fbx"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Eger/EgerSpear.fbx")
            };
            jaeger.platePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Eger/Plate.fbx");
            jaeger.RefreshVisualStages();
            EditorUtility.SetDirty(jaeger);

            if (frame != null && egerRoot.transform.parent != frame.transform)
                frame.SetActive(false);
        }

        private static void SetupUIAndCamera()
        {
            HUDOverlay hud = PrototypeSceneBuilder.BuildHUD();
            if (hud != null) EditorUtility.SetDirty(hud);

            GameObject consoleGroup = GameObject.Find("--- 07_DISPATCH_CONSOLE ---");
            if (consoleGroup == null) return;

            var hf = GameObject.Find("HANGARFULL");
            Transform c85 = hf != null ? hf.transform.Find("Cube.085") : GameObject.Find("Cube.085")?.transform;
            Transform c104 = hf != null ? hf.transform.Find("Cube.104") : GameObject.Find("Cube.104")?.transform;

            Vector3 screenCenter = c104 != null && c104.GetComponent<Renderer>() != null
                ? c104.GetComponent<Renderer>().bounds.center
                : consoleGroup.transform.position;

            PrototypeSceneBuilder.BuildDispatchMonitors(consoleGroup.transform, c104, screenCenter, UnityEngine.Camera.main);

            var rCam = Object.FindAnyObjectByType<RoomCamera>();
            if (rCam != null)
            {
                if (c85 != null && c104 != null)
                {
                    Vector3 mid = Vector3.Lerp(c85.position, c104.position, 0.45f) + Vector3.up * 0.75f;
                    rCam.hangarPos = mid;
                    rCam.hangarYaw = Quaternion.LookRotation(c85.position - mid).eulerAngles.y;

                    var tCanvas = GameObject.Find("Terminal_Canvas");
                    Vector3 sPoint = tCanvas != null ? tCanvas.transform.position : c104.position + Vector3.up * 0.34f;
                    rCam.dispatchDistance = 1.32f;
                    rCam.dispatchPos = new Vector3(sPoint.x, sPoint.y, sPoint.z - rCam.dispatchDistance);
                    rCam.dispatchYaw = 0f;
                }

                var armoryUi = Object.FindAnyObjectByType<ArmoryUI>();
                if (armoryUi != null)
                {
                    Transform aTr = armoryUi.transform;
                    rCam.armoryFocusPos = aTr.position - aTr.forward * 0.58f;
                    rCam.armoryFocusYaw = aTr.eulerAngles.y;
                }

                rCam.wbCameraOffset = new Vector3(0f, 1.54f, -1.24f);
                rCam.wbCameraPitch = 22f;
                EditorUtility.SetDirty(rCam);
            }
        }
    }
}
#endif
