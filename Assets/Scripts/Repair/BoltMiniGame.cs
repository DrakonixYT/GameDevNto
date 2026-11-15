using System;
using System.Collections.Generic;
using UnityEngine;
using HangarGame.Interaction;

namespace HangarGame.Repair
{
    public class BoltMiniGame : MonoBehaviour
    {
        public event Action OnAllBoltsCompleted;

        [Header("Bolt Settings")]
        [SerializeField] private int totalBolts = 4;
        [SerializeField] private float boltRadius = 0.4f;

        private List<GameObject> activeBolts = new List<GameObject>();
        private int boltsRemaining = 0;

        public bool IsActive => activeBolts.Count > 0;

        public void StartMiniGame(Transform workbenchTable)
        {
            Cleanup();

            boltsRemaining = totalBolts;
            for (int i = 0; i < totalBolts; i++)
            {
                float angle = (i / (float)totalBolts) * Mathf.PI * 2f;
                Vector3 offset = new Vector3(Mathf.Cos(angle) * boltRadius, 0.05f, Mathf.Sin(angle) * boltRadius);
                Vector3 boltPos = workbenchTable.position + offset;

                GameObject boltObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                boltObj.name = $"Bolt_{i + 1}";
                boltObj.transform.position = boltPos;
                boltObj.transform.localScale = new Vector3(0.08f, 0.02f, 0.08f);
                boltObj.transform.SetParent(transform);

                Renderer rend = boltObj.GetComponent<Renderer>();
                rend.material.color = new Color(0.8f, 0.8f, 0.2f);

                Clickable3DObject clickable = boltObj.AddComponent<Clickable3DObject>();
                clickable.onInteract.AddListener(() => OnBoltClicked(boltObj));

                activeBolts.Add(boltObj);
            }

            Debug.Log($"[BoltMiniGame] Открутите {totalBolts} крепежных болта!");
        }

        private void OnBoltClicked(GameObject boltObj)
        {
            if (!activeBolts.Contains(boltObj)) return;

            activeBolts.Remove(boltObj);
            boltsRemaining--;

            // Эффект откручивания
            Destroy(boltObj);
            Debug.Log($"[BoltMiniGame] Болт выкручен! Осталось: {boltsRemaining}");

            if (boltsRemaining <= 0)
            {
                Debug.Log("<color=green>[BoltMiniGame] Все болты успешно выкручены и заменены!</color>");
                OnAllBoltsCompleted?.Invoke();
            }
        }

        public void Cleanup()
        {
            foreach (var b in activeBolts)
            {
                if (b != null) Destroy(b);
            }
            activeBolts.Clear();
            boltsRemaining = 0;
        }
    }
}
