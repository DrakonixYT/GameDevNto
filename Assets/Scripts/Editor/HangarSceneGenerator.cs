#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using HangarGame.Prototype;

namespace HangarGame.EditorTools
{
    public static class HangarSceneGenerator
    {
        [MenuItem("Pacific Rim/🚀 1. Сгенерировать сцену прямо сюда (Bake)")]
        public static void BakeIntoCurrentScene()
        {
            string[] objectsToClean = {
                "--- 01_MANAGERS ---", "--- 02_LIGHTING_AND_VFX ---", "--- 03_HANGAR_ROOM ---",
                "--- 04_MECH_ATLANT ---", "--- 05_JAEGER_FRAME ---", "--- 06_WORKBENCH ---",
                "--- 07_DISPATCH_CONSOLE ---", "--- 08_MINIMAL_HUD ---",
                "Hangar_Room", "--- CORE MANAGERS ---", "HUD_Canvas", "EventSystem", "Main Camera"
            };
            foreach (var name in objectsToClean)
            {
                GameObject obj = GameObject.Find(name);
                if (obj != null) Object.DestroyImmediate(obj);
            }

            GameObject builderObj = new GameObject("--- SCENE GENERATOR ---");
            PrototypeSceneBuilder builder = builderObj.AddComponent<PrototypeSceneBuilder>();
            builder.BuildCompleteHangarPrototype();
            Object.DestroyImmediate(builderObj);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Debug.Log("<color=green><b>[Pacific Rim]</b> Сцена успешно запечена и сохранена!</color>");
        }

        [MenuItem("Pacific Rim/💾 2. Пересоздать файл сцены 'HangarMainScene'")]
        public static void GenerateSceneMenu()
        {
            BakeIntoCurrentScene();
        }
    }
}
#endif
