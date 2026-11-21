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
        [MenuItem("Tools/Bake Hangar Scene", false, 1)]
        [MenuItem("Pacific Rim/Bake Hangar Scene", false, 1)]
        public static void BakeIntoCurrentScene()
        {
            string scenePath = "Assets/Scenes/HangarMainScene.unity";
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.path != scenePath)
            {
                activeScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

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

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            CaptureGameView("C:/Users/skori/.gemini/antigravity/brain/9c28db05-5532-4b25-87af-9906cc3f22c9/game_view_hangar.png");
            Debug.Log("<color=green><b>[Pacific Rim]</b> Сцена успешно запечена и сохранена!</color>");
        }

        private static void CaptureGameView(string path)
        {
            var camObj = GameObject.Find("Main Camera");
            if (camObj == null) return;
            var cam = camObj.GetComponent<Camera>();
            if (cam == null) return;

            RenderTexture rt = new RenderTexture(1920, 1080, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;

            Texture2D tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            tex.Apply();

            cam.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);

            byte[] bytes = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            System.IO.File.WriteAllBytes(path, bytes);
        }
    }
}
#endif
