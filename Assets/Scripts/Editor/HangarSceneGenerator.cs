#if UNITY_EDITOR
using System.Collections.Generic;
using HangarGame.Armory;
using HangarGame.CameraControl;
using HangarGame.Core;
using HangarGame.Mechs;
using HangarGame.Prototype;
using HangarGame.Repair;
using HangarGame.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

            string[] objectsToClean =
            {
                "--- 01_MANAGERS ---", "--- 02_LIGHTING_AND_VFX ---", "--- 03_HANGAR_ROOM ---",
                "--- 04_MECH_ATLANT ---", "--- 05_JAEGER_FRAME ---", "--- 06_WORKBENCH ---",
                "--- 07_DISPATCH_CONSOLE ---", "--- 08_MINIMAL_HUD ---",
                "Hangar_Room", "--- CORE MANAGERS ---", "HUD_Canvas", "EventSystem", "Main Camera"
            };
            foreach (var objName in objectsToClean)
            {
                GameObject obj = GameObject.Find(objName);
                if (obj != null) Object.DestroyImmediate(obj);
            }

            GameObject builderObj = new GameObject("--- SCENE GENERATOR ---");
            PrototypeSceneBuilder builder = builderObj.AddComponent<PrototypeSceneBuilder>();
            builder.BuildCompleteHangarPrototype();
            Object.DestroyImmediate(builderObj);

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log("[Hangar] Сцена пересобрана и сохранена.");
        }

        [MenuItem("Tools/Upgrade Hangar Scene (Preserve Art)", false, 2)]
        public static void UpgradeCurrentScenePreserveArt()
        {
            var activeScene = OpenMainScene();

            WireManagersAndAlarmLight();
            WireMechsAndBrokenParts();
            WireWorkbenchAnd3DButtons();
            WireJaegerProjectFromScene();
            UpgradeHUDFor3DButtonsAndArmory();
            WireTerminalUI();

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log("[Hangar] Текущая сцена обновлена без потери моделей, света и материалов!");
        }

        private static Scene OpenMainScene()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.path != ScenePath)
                activeScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            return activeScene;
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T comp = go.GetComponent<T>();
            if (comp == null) comp = go.AddComponent<T>();
            return comp;
        }

        private static void WireManagersAndAlarmLight()
        {
            GameObject mgrs = GameObject.Find("--- 01_MANAGERS ---");
            if (mgrs == null) return;

            GetOrAdd<HangarAudio>(mgrs);
            var alarm = GetOrAdd<HangarAlarmLight>(mgrs);

            var alarmLightObj = GameObject.Find("Spotlight_Alarm_Red");
            if (alarmLightObj != null) alarm.ConfigureLight(alarmLightObj.GetComponent<Light>());
            EditorUtility.SetDirty(alarm);

            var gm = GetOrAdd<GameManager>(mgrs);
            var soGm = new SerializedObject(gm);
            soGm.FindProperty("startingCredits").intValue = 220;
            soGm.FindProperty("startingAlloy").intValue = 95;
            soGm.FindProperty("maxBaseHp").floatValue = 100f;
            soGm.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(gm);

            var disp = mgrs.GetComponent<HangarGame.Dispatch.DispatchSystem>();
            if (disp != null)
            {
                var soDisp = new SerializedObject(disp);
                soDisp.FindProperty("firstEnemyDelay").floatValue = 9f;
                soDisp.FindProperty("minSpawnInterval").floatValue = 18f;
                soDisp.FindProperty("maxSpawnInterval").floatValue = 27f;
                soDisp.FindProperty("workbenchTimerMultiplier").floatValue = 0.55f;
                soDisp.FindProperty("maxActiveEnemies").intValue = 4;
                soDisp.FindProperty("breachCreditsPenalty").intValue = 50;
                soDisp.FindProperty("breachAlloyPenalty").intValue = 25;
                soDisp.FindProperty("breachBaseDamage").floatValue = 25f;
                soDisp.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(disp);
            }

            var armory = GetOrAdd<ArmoryManager>(mgrs);
            armory.ResetCatalogFromDefaults();
            armory.SetWeaponPrefab("weapon_spear", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Eger/EgerSpear.fbx"));
            armory.SetWeaponPrefab("shield_bastion", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Eger/EgerShield.fbx"));
            EditorUtility.SetDirty(armory);
        }

        private static void WireMechsAndBrokenParts()
        {
            var mechMgr = Object.FindAnyObjectByType<HangarMechManager>();
            if (mechMgr == null) return;

            Transform bay0 = mechMgr.transform.Find("Bay_Mech_0_Страж-1");
            if (bay0 == null) bay0 = mechMgr.transform.Find("Mech_0_Страж-1 (Лёгкий) (1)");
            if (bay0 == null)
            {
                var allUnitsPre = Object.FindObjectsByType<MechUnit>(FindObjectsInactive.Include);
                foreach (var u in allUnitsPre)
                {
                    if (!mechMgr.Mechs.Contains(u))
                    {
                        bay0 = u.transform;
                        break;
                    }
                }
            }

            var allUnits = Object.FindObjectsByType<MechUnit>(FindObjectsInactive.Include);
            foreach (var u in allUnits)
            {
                if (!mechMgr.Mechs.Contains(u))
                {
                    foreach (var p in u.GetComponentsInChildren<MechPart>(true)) Object.DestroyImmediate(p);
                    foreach (var col in u.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(col);
                    Object.DestroyImmediate(u);
                }
            }

            Material holoMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/Holo.mat");
            GameObject brokenHead = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Mech01/BrokenParts/HeadBroken.fbx");
            GameObject brokenLeftArm = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Mech01/BrokenParts/LeftArmBroken.fbx");
            GameObject brokenRightArm = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Mech01/BrokenParts/RightArmBroken.fbx");

            float[] mechHp = { 85f, 120f, 175f };
            float[] mechAtk = { 18f, 26f, 34f };
            float[] mechSpd = { 95f, 62f, 40f };

            for (int i = 0; i < mechMgr.Mechs.Count; i++)
            {
                var mech = mechMgr.Mechs[i];
                if (mech == null) continue;
                if (i < mechHp.Length)
                {
                    mech.maxHp = mechHp[i];
                    mech.currentHp = mechHp[i];
                    mech.attackPower = mechAtk[i];
                    mech.moveSpeed = mechSpd[i];
                }
                if (holoMat != null) ApplyPrimaryMaterial(mech.gameObject, holoMat);
                mech.EnsurePartsLoaded();
                var h = mech.GetPartBySlot(PartSlot.Head);
                var l = mech.GetPartBySlot(PartSlot.LeftArm);
                var r = mech.GetPartBySlot(PartSlot.RightArm);

                if (h != null && brokenHead != null) h.AttachBrokenVisual(brokenHead);
                if (l != null && brokenLeftArm != null) l.AttachBrokenVisual(brokenLeftArm);
                if (r != null && brokenRightArm != null) r.AttachBrokenVisual(brokenRightArm);

                var battery = mech.transform.parent?.Find("Battery_Charging_Station/Battery_Energy_Core");
                if (battery != null && battery.GetComponent<BatteryStation>() == null)
                    battery.gameObject.AddComponent<BatteryStation>();

                EditorUtility.SetDirty(mech);
            }

            if (bay0 == null && mechMgr.Mechs.Count > 0 && mechMgr.Mechs[0] != null)
            {
                GameObject createdBay0 = Object.Instantiate(mechMgr.Mechs[0].gameObject, mechMgr.transform, false);
                createdBay0.name = "Bay_Mech_0_Страж-1";
                createdBay0.transform.localPosition = new Vector3(-15.07f, 1.7343613f, -5.07f);
                createdBay0.transform.localRotation = new Quaternion(0.7071068f, 0f, 0f, -0.7071068f);
                createdBay0.transform.localScale = Vector3.one * 20f;
                StripBayDecor(createdBay0);
                bay0 = createdBay0.transform;
            }

            if (bay0 != null)
            {
                bay0.name = "Bay_Mech_0_Страж-1";
                StripBayDecor(bay0.gameObject);
                Bounds bay0Bounds = CalcWorldBounds(bay0.gameObject);

                Material guardMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/Mech01/Mech01_Guard_Cyan.mat");
                Material atlantMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/Mech01/Mech01_Atlant_Blue.mat");
                Material goliathMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/Mech01/Mech01_Goliath_Orange.mat");
                if (guardMat != null) ApplyPrimaryMaterial(bay0.gameObject, guardMat);

                Transform bay1 = RebuildBayClone(mechMgr.transform, bay0, "Bay_Mech_1_Атлант-2", 1.06f, atlantMat, bay0Bounds.min.y);
                Transform bay2 = RebuildBayClone(mechMgr.transform, bay0, "Bay_Mech_2_Голиаф-3", 1.14f, goliathMat, bay0Bounds.min.y);

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

            var soMgr = new SerializedObject(mechMgr);
            soMgr.FindProperty("startFirstMechDamaged").boolValue = true;
            soMgr.FindProperty("firstMechStartHpRatio").floatValue = 0.58f;
            soMgr.ApplyModifiedPropertiesWithoutUndo();

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
            if (mat != null) ApplyPrimaryMaterial(clone, mat);

            Bounds b = CalcWorldBounds(clone);
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
            {
                if (tr != null && (tr.name == "Broken_Visual" || tr.name.StartsWith("Equipped_")))
                    Object.DestroyImmediate(tr.gameObject);
            }
        }

        private static void ApplyPrimaryMaterial(GameObject go, Material mat)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null) continue;
                var mats = r.sharedMaterials;
                if (mats.Length > 0)
                {
                    mats[0] = mat;
                    r.sharedMaterials = mats;
                }
            }
        }

        private static Bounds CalcWorldBounds(GameObject go)
        {
            var rends = go.GetComponentsInChildren<Renderer>(true);
            if (rends.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            return b;
        }

        private static void WireWorkbenchAnd3DButtons()
        {
            GameObject wbObj = GameObject.Find("--- 06_WORKBENCH ---");
            if (wbObj == null) return;

            var wb = GetOrAdd<RepairWorkbench>(wbObj);
            var plate = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Eger/Plate.fbx");
            wb.SetPlatePrefab(plate);
            wb.SetRestOffset(new Vector3(0f, 1.10f, 0f));
            EditorUtility.SetDirty(wb);

            var rCam = Object.FindAnyObjectByType<RoomCamera>();
            if (rCam != null)
            {
                rCam.ConfigureWorkbenchCamera(new Vector3(0f, 1.62f, -1.26f), 25f);
                EditorUtility.SetDirty(rCam);
            }

            PrototypeSceneBuilder.BuildWorkbenchControlPanel3D(wbObj.transform);
        }

        private static void WireJaegerProjectFromScene()
        {
            var jaeger = Object.FindAnyObjectByType<JaegerProject>();
            if (jaeger == null) return;

            GameObject egerRoot = GameObject.Find("Eger");
            if (egerRoot == null)
            {
                var frame = GameObject.Find("--- 05_JAEGER_FRAME ---");
                if (frame != null)
                {
                    var tr = frame.transform.Find("Eger");
                    if (tr == null) tr = frame.transform.Find("Jaeger_Chassis");
                    if (tr != null) egerRoot = tr.gameObject;
                }
            }
            if (egerRoot == null) return;

            Transform[] sceneParts =
            {
                egerRoot.transform.Find("EgerHead"),
                egerRoot.transform.Find("EgerLeftArm"),
                egerRoot.transform.Find("EgerRightArm"),
                egerRoot.transform.Find("EgerShield"),
                egerRoot.transform.Find("EgerSpear")
            };

            GameObject[] prefabs =
            {
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Eger/EgerHead.fbx"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Eger/EgerLeftArm.fbx"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Eger/EgerRightArm.fbx"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Eger/EgerShield.fbx"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Eger/EgerSpear.fbx")
            };
            GameObject plate = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Eger/Plate.fbx");

            jaeger.Configure(sceneParts, prefabs, plate);
            var soJg = new SerializedObject(jaeger);
            int[] creds = { 160, 210, 260, 330, 420 };
            int[] allys = { 70, 95, 120, 150, 190 };
            var cProp = soJg.FindProperty("stageCredits");
            var aProp = soJg.FindProperty("stageAlloy");
            if (cProp != null && aProp != null)
            {
                cProp.arraySize = creds.Length;
                aProp.arraySize = allys.Length;
                for (int i = 0; i < creds.Length; i++)
                {
                    cProp.GetArrayElementAtIndex(i).intValue = creds[i];
                    aProp.GetArrayElementAtIndex(i).intValue = allys[i];
                }
            }
            soJg.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(jaeger);

            var oldFrame = GameObject.Find("--- 05_JAEGER_FRAME ---");
            if (oldFrame != null && egerRoot.transform.parent != oldFrame.transform)
                oldFrame.SetActive(false);
        }

        private static void UpgradeHUDFor3DButtonsAndArmory()
        {
            var hudCanvas = GameObject.Find("HUD_Canvas");
            if (hudCanvas == null) return;

            Font font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var wbPanel = hudCanvas.transform.Find("WorkbenchPanel")?.gameObject;
            if (wbPanel != null)
            {
                string[] oldBtns = { "bYM", "bYP", "bPM", "bPP", "bRst", "bFin", "bSwap", "bExit" };
                foreach (var b in oldBtns)
                {
                    var child = wbPanel.transform.Find(b);
                    if (child != null) Object.DestroyImmediate(child.gameObject);
                }

                var wbRect = wbPanel.GetComponent<RectTransform>();
                wbRect.anchorMin = new Vector2(0.5f, 1f);
                wbRect.anchorMax = new Vector2(0.5f, 1f);
                wbRect.pivot = new Vector2(0.5f, 1f);
                wbRect.anchoredPosition = new Vector2(0f, -80f);
                wbRect.sizeDelta = new Vector2(860f, 75f);
            }

            Transform pnlMech = hudCanvas.transform.Find("MechSelectPanel");
            if (pnlMech != null)
            {
                var oldArmoryBtn = pnlMech.Find("Btn_Armory");
                if (oldArmoryBtn != null) Object.DestroyImmediate(oldArmoryBtn.gameObject);

                var pnlRect = pnlMech.GetComponent<RectTransform>();
                pnlRect.sizeDelta = new Vector2(880f, 55f);
                var bCharge = pnlMech.Find("Btn_RechargeBattery")?.GetComponent<RectTransform>();
                if (bCharge != null)
                {
                    bCharge.anchoredPosition = new Vector2(230f, 0f);
                    bCharge.sizeDelta = new Vector2(165f, 42f);
                }
                var tCur = pnlMech.Find("Txt_CurrentMech")?.GetComponent<RectTransform>();
                if (tCur != null) tCur.anchoredPosition = new Vector2(-95f, 0f);
            }

            var oldArmoryPanel = hudCanvas.transform.Find("ArmoryPanel");
            if (oldArmoryPanel != null) Object.DestroyImmediate(oldArmoryPanel.gameObject);
            var oldHudArmory = hudCanvas.GetComponent<ArmoryUI>();
            if (oldHudArmory != null) Object.DestroyImmediate(oldHudArmory);

            Text tRes = hudCanvas.transform.Find("TopBar/Txt_Res")?.GetComponent<Text>();
            Text tRoom = hudCanvas.transform.Find("TopBar/Txt_Room")?.GetComponent<Text>();
            if (tRes != null && tRoom == null)
            {
                tRes.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                tRes.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                tRes.rectTransform.anchoredPosition = Vector2.zero;
                tRes.rectTransform.sizeDelta = new Vector2(760f, 40f);
                tRes.alignment = TextAnchor.MiddleCenter;
                EditorUtility.SetDirty(tRes);
            }
            Button bLeft = hudCanvas.transform.Find("Btn_Left")?.GetComponent<Button>();
            Button bRight = hudCanvas.transform.Find("Btn_Right")?.GetComponent<Button>();
            Button bPrev = pnlMech?.Find("Btn_PrevMech")?.GetComponent<Button>();
            Button bNext = pnlMech?.Find("Btn_NextMech")?.GetComponent<Button>();
            Button bChg = pnlMech?.Find("Btn_RechargeBattery")?.GetComponent<Button>();
            Text tMech = pnlMech?.Find("Txt_CurrentMech")?.GetComponent<Text>();
            Text tPrompt = hudCanvas.transform.Find("PromptBox/Txt_Prompt")?.GetComponent<Text>();
            Text tWbTitle = wbPanel?.transform.Find("Txt_WbTitle")?.GetComponent<Text>();
            Text tWbInfo = wbPanel?.transform.Find("Txt_WbInfo")?.GetComponent<Text>();
            GameObject vic = hudCanvas.transform.Find("VictoryPanel")?.gameObject;
            Button bRest = vic != null ? vic.transform.Find("bRest")?.GetComponent<Button>() : null;

            var hud = GetOrAdd<HUDOverlay>(hudCanvas);
            hud.Configure(tRes, tRoom, bLeft, bRight, pnlMech?.gameObject, bPrev, bNext, bChg, tMech, tPrompt, wbPanel, tWbTitle, tWbInfo, vic, bRest);
            EditorUtility.SetDirty(hud);
        }

        private static void WireTerminalUI()
        {
            var termObj = GameObject.Find("Terminal_Canvas");
            if (termObj == null) return;

            var bg = termObj.transform.Find("BG");
            if (bg == null) return;

            var tTitle = bg.Find("Txt_RadarTitle")?.GetComponent<Text>();
            if (tTitle != null)
            {
                tTitle.rectTransform.anchorMin = new Vector2(0f, 1f);
                tTitle.rectTransform.anchorMax = new Vector2(0f, 1f);
                tTitle.rectTransform.pivot = new Vector2(0f, 0.5f);
                tTitle.rectTransform.anchoredPosition = new Vector2(28f, -28f);
                tTitle.rectTransform.sizeDelta = new Vector2(680f, 34f);
                tTitle.fontSize = 16;
                tTitle.alignment = TextAnchor.MiddleLeft;
                EditorUtility.SetDirty(tTitle);
            }
            var tLog = bg.Find("Txt_BattleLog")?.GetComponent<Text>();
            var tJaeger = bg.Find("Txt_JaegerInfo")?.GetComponent<Text>();
            var bJaeger = bg.Find("Btn_BuildJaeger")?.GetComponent<Button>();
            var bScan = bg.Find("Btn_ScanThreat")?.GetComponent<Button>();
            var rBox = bg.Find("Radar_Screen")?.GetComponent<RectTransform>();
            var rClick = rBox != null ? GetOrAdd<RadarClickHandler>(rBox.gameObject) : null;
            var mContainer = rBox != null ? rBox.Find("Marker_Container") : null;

            Button[] bSlots = new Button[3];
            Button[] bShields = new Button[3];
            Button[] bRecalls = new Button[3];
            Text[] tSlots = new Text[3];
            Image[] iSlots = new Image[3];

            for (int i = 0; i < 3; i++)
            {
                var slotRoot = bg.Find($"SlotRoot_{i}");
                if (slotRoot == null) continue;
                var bMain = slotRoot.Find($"Btn_MechSlot_{i}")?.GetComponent<Button>();
                bSlots[i] = bMain;
                if (bMain != null)
                {
                    var bRect = bMain.GetComponent<RectTransform>();
                    bRect.anchoredPosition = new Vector2(0f, -22f);
                    bRect.sizeDelta = new Vector2(340f, 44f);
                    iSlots[i] = bMain.GetComponent<Image>();
                    tSlots[i] = bMain.GetComponentInChildren<Text>();
                    if (tSlots[i] != null) tSlots[i].fontSize = 13;
                }
                bShields[i] = slotRoot.Find($"Btn_Shield_{i}")?.GetComponent<Button>();
                bRecalls[i] = slotRoot.Find($"Btn_Recall_{i}")?.GetComponent<Button>();
            }

            var ui = GetOrAdd<TerminalUI>(termObj);
            ui.Configure(tTitle, tLog, tJaeger, bJaeger, bScan, rBox, rClick, mContainer, bSlots, bShields, bRecalls, tSlots, iSlots);

            var termGr = termObj.GetComponent<GraphicRaycaster>();
            if (termGr != null)
            {
                termGr.ignoreReversedGraphics = false;
                termGr.blockingObjects = GraphicRaycaster.BlockingObjects.None;
            }
            EditorUtility.SetDirty(ui);

            var consoleGroup = GameObject.Find("--- 07_DISPATCH_CONSOLE ---");
            if (consoleGroup != null)
            {
                var hf = GameObject.Find("HANGARFULL");
                Transform c104 = hf != null ? hf.transform.Find("Cube.104") : GameObject.Find("Cube.104")?.transform;
                Font font = Font.CreateDynamicFontFromOSFont("Arial", 15);
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                ArmoryUI armoryUi = PrototypeSceneBuilder.BuildArmoryMonitor(consoleGroup.transform, c104, UnityEngine.Camera.main, font);
                EditorUtility.SetDirty(armoryUi);

                var rCam = Object.FindAnyObjectByType<RoomCamera>();
                if (rCam != null && armoryUi != null)
                {
                    Transform aTr = armoryUi.transform;
                    Vector3 focusPos = aTr.position + (-aTr.forward) * 0.72f;
                    rCam.ConfigureDispatchMonitorCamera(1.32f, focusPos, aTr.eulerAngles.y);
                    EditorUtility.SetDirty(rCam);
                }
            }
        }
    }
}
#endif
