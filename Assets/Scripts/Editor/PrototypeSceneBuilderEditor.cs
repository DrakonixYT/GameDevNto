#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using HangarGame.Prototype;

namespace HangarGame.EditorTools
{
    [CustomEditor(typeof(PrototypeSceneBuilder))]
    public class PrototypeSceneBuilderEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            GUILayout.Space(10);
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.4f);
            if (GUILayout.Button("Сгенерировать сцену (Bake)", GUILayout.Height(45)))
            {
                HangarSceneGenerator.BakeIntoCurrentScene();
            }
            GUI.backgroundColor = Color.white;
        }
    }
}
#endif
