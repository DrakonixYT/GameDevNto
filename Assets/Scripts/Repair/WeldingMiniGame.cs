using System;
using System.Collections.Generic;
using UnityEngine;
using HangarGame.Interaction;

namespace HangarGame.Repair
{
    public class WeldingMiniGame : MonoBehaviour
    {
        public event Action OnWeldingCompleted;

        [Header("Welding Seam")]
        [SerializeField] private int seamPointsCount = 5;
        [SerializeField] private float seamLength = 0.6f;

        private List<GameObject> seamNodes = new List<GameObject>();
        private int nodesWelded = 0;

        public bool IsActive => seamNodes.Count > 0;

        public void StartMiniGame(Transform workbenchTable)
        {
            Cleanup();

            nodesWelded = 0;
            Vector3 startPos = workbenchTable.position + new Vector3(-seamLength * 0.5f, 0.05f, 0f);

            for (int i = 0; i < seamPointsCount; i++)
            {
                float t = i / (float)(seamPointsCount - 1);
                Vector3 nodePos = startPos + new Vector3(t * seamLength, 0f, Mathf.Sin(t * Mathf.PI) * 0.08f);

                GameObject nodeObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                nodeObj.name = $"WeldNode_{i + 1}";
                nodeObj.transform.position = nodePos;
                nodeObj.transform.localScale = new Vector3(0.06f, 0.03f, 0.06f);
                nodeObj.transform.SetParent(transform);

                Renderer rend = nodeObj.GetComponent<Renderer>();
                rend.material.color = new Color(1f, 0.4f, 0f); // Раскаленный оранжевый шов

                Clickable3DObject clickable = nodeObj.AddComponent<Clickable3DObject>();
                clickable.onInteract.AddListener(() => OnNodeWelded(nodeObj));

                seamNodes.Add(nodeObj);
            }

            Debug.Log($"[WeldingMiniGame] Заварите трещину брони, пройдясь горелкой по {seamPointsCount} точкам!");
        }

        private void OnNodeWelded(GameObject nodeObj)
        {
            if (!seamNodes.Contains(nodeObj)) return;

            Renderer rend = nodeObj.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = new Color(0.2f, 0.8f, 1f); // Остывший заваренный металл
            }

            // Убираем кликабельность заваренной точки
            Destroy(nodeObj.GetComponent<Clickable3DObject>());

            nodesWelded++;
            Debug.Log($"[WeldingMiniGame] Сварка шва: {nodesWelded}/{seamPointsCount}");

            if (nodesWelded >= seamPointsCount)
            {
                Debug.Log("<color=green>[WeldingMiniGame] Сварочный шов герметичен! Прочность восстановлена!</color>");
                OnWeldingCompleted?.Invoke();
            }
        }

        public void Cleanup()
        {
            foreach (var n in seamNodes)
            {
                if (n != null) Destroy(n);
            }
            seamNodes.Clear();
            nodesWelded = 0;
        }
    }
}
