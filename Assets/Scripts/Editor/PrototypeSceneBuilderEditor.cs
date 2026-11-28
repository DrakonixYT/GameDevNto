#if UNITY_EDITOR
using HangarGame.Prototype;
using UnityEditor;
using UnityEngine;

namespace HangarGame.EditorTools
{
    [CustomEditor(typeof(PrototypeSceneBuilder))]
    public class PrototypeSceneBuilderEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            GUILayout.Space(10);

            GUI.backgroundColor = new Color(0.25f, 0.65f, 0.95f);
            if (GUILayout.Button("Обновить текущую сцену (Сохраняя арт)", GUILayout.Height(38)))
            {
                HangarSceneGenerator.UpgradeCurrentScenePreserveArt();
            }

            GUILayout.Space(4);
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.4f);
            if (GUILayout.Button("Сгенерировать сцену с нуля (Резервный сброс)", GUILayout.Height(36)))
            {
                HangarSceneGenerator.BakeIntoCurrentScene();
            }
            GUI.backgroundColor = Color.white;
        }
    }
}
#endif
