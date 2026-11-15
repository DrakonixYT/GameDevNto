using System;
using System.Collections.Generic;
using UnityEngine;
using HangarGame.Interaction;
using HangarGame.Core;

namespace HangarGame.Repair
{
    public class BoltMiniGame : MonoBehaviour
    {
        public event Action OnAllBoltsCompleted;

        [Header("Bolt Settings")]
        [SerializeField] private int totalBolts = 4;
        [SerializeField] private float boltRadius = 0.45f;

        private List<GameObject> activeBolts = new List<GameObject>();
        private int boltsRemaining = 0;

        public bool IsActive => activeBolts.Count > 0;

        public void StartMiniGame(Transform workbenchTable)
        {
            Vector3 center = workbenchTable != null ? workbenchTable.position + Vector3.up * 0.35f : new Vector3(0f, 1.15f, 2.2f);
            StartMiniGameAt(center);
        }

        public void StartMiniGameAt(Vector3 center)
        {
            Cleanup();

            boltsRemaining = totalBolts;
            for (int i = 0; i < totalBolts; i++)
            {
                float angle = (i / (float)totalBolts) * Mathf.PI * 2f;
                Vector3 offset = new Vector3(Mathf.Cos(angle) * boltRadius, 0f, Mathf.Sin(angle) * boltRadius);
                Vector3 boltPos = center + offset;

                GameObject boltObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                boltObj.name = $"Bolt_{i + 1}";
                boltObj.transform.position = boltPos;
                // Крупные, заметные болты с золотистым оттенком
                boltObj.transform.localScale = new Vector3(0.18f, 0.06f, 0.18f);
                boltObj.transform.SetParent(transform);

                Renderer rend = boltObj.GetComponent<Renderer>();
                rend.material.color = new Color(1f, 0.85f, 0.1f); // Яркое золото

                // Увеличенный коллайдер для удобного клика мышью
                Collider col = boltObj.GetComponent<Collider>();
                if (col == null) boltObj.AddComponent<BoxCollider>();

                Clickable3DObject clickable = boltObj.AddComponent<Clickable3DObject>();
                clickable.onInteract.AddListener(() => OnBoltClicked(boltObj));

                activeBolts.Add(boltObj);
            }

            Debug.Log($"<color=yellow>[BoltMiniGame]</color> Кликните по {totalBolts} золотистым болтам на верстаке!");
        }

        private void OnBoltClicked(GameObject boltObj)
        {
            if (!activeBolts.Contains(boltObj)) return;

            activeBolts.Remove(boltObj);
            boltsRemaining--;

            HangarAudio.Instance?.PlayBolt();
            Destroy(boltObj);
            Debug.Log($"[BoltMiniGame] Болт выкручен! Осталось: {boltsRemaining}");

            if (boltsRemaining <= 0)
            {
                Debug.Log("<color=green>[BoltMiniGame] Все болты закручены!</color>");
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
