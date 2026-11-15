using System;
using System.Collections.Generic;
using UnityEngine;
using HangarGame.Interaction;
using HangarGame.Core;

namespace HangarGame.Repair
{
    public class WeldingMiniGame : MonoBehaviour
    {
        public event Action OnWeldingCompleted;

        [Header("Welding Seam")]
        [SerializeField] private int seamPointsCount = 5;
        [SerializeField] private float seamLength = 0.7f;

        private List<GameObject> seamNodes = new List<GameObject>();
        private int nodesWelded = 0;

        public bool IsActive => seamNodes.Count > 0;

        public void StartMiniGame(Transform workbenchTable)
        {
            Vector3 center = workbenchTable != null ? workbenchTable.position + Vector3.up * 0.35f : new Vector3(0f, 1.15f, 2.2f);
            StartMiniGameAt(center);
        }

        public void StartMiniGameAt(Vector3 center)
        {
            Cleanup();

            nodesWelded = 0;
            Vector3 startPos = center + new Vector3(-seamLength * 0.5f, 0.05f, 0f);

            for (int i = 0; i < seamPointsCount; i++)
            {
                float t = i / (float)(seamPointsCount - 1);
                Vector3 nodePos = startPos + new Vector3(t * seamLength, 0f, Mathf.Sin(t * Mathf.PI) * 0.1f);

                GameObject nodeObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                nodeObj.name = $"WeldNode_{i + 1}";
                nodeObj.transform.position = nodePos;
                // Крупные заметные раскаленные сферы
                nodeObj.transform.localScale = new Vector3(0.14f, 0.1f, 0.14f);
                nodeObj.transform.SetParent(transform);

                Renderer rend = nodeObj.GetComponent<Renderer>();
                rend.material.color = new Color(1f, 0.35f, 0f); // Раскаленный оранжевый

                Clickable3DObject clickable = nodeObj.AddComponent<Clickable3DObject>();
                clickable.onInteract.AddListener(() => OnNodeWelded(nodeObj));

                seamNodes.Add(nodeObj);
            }

            Debug.Log($"<color=orange>[WeldingMiniGame]</color> Кликните по {seamPointsCount} раскаленным точкам шва для сварки!");
        }

        private void OnNodeWelded(GameObject nodeObj)
        {
            if (!seamNodes.Contains(nodeObj)) return;

            Renderer rend = nodeObj.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = new Color(0.2f, 0.85f, 1f); // Заваренный холодный металл
            }

            HangarAudio.Instance?.PlayWeld();

            Destroy(nodeObj.GetComponent<Clickable3DObject>());

            nodesWelded++;
            Debug.Log($"[WeldingMiniGame] Сварка шва: {nodesWelded}/{seamPointsCount}");

            if (nodesWelded >= seamPointsCount)
            {
                Debug.Log("<color=green>[WeldingMiniGame] Шов заварен!</color>");
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
