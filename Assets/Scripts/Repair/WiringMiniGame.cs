using System;
using System.Collections.Generic;
using UnityEngine;
using HangarGame.Interaction;
using HangarGame.Core;

namespace HangarGame.Repair
{
    public class WiringMiniGame : MonoBehaviour
    {
        public event Action OnWiringCompleted;

        private class WirePin
        {
            public GameObject obj;
            public int colorId; // 0 = Red, 1 = Blue, 2 = Yellow
            public bool isConnected;
            public Color color;
        }

        private List<WirePin> pins = new List<WirePin>();
        private WirePin selectedPin = null;
        private int connectionsRemaining = 3;

        public bool IsActive => pins.Count > 0 && connectionsRemaining > 0;

        public void StartMiniGameAt(Vector3 center)
        {
            Cleanup();

            connectionsRemaining = 3;
            Color[] colors = new Color[] { Color.red, Color.cyan, Color.yellow };

            // Создаем 3 левых пина и 3 правых пина
            for (int i = 0; i < 3; i++)
            {
                // Левый контакт
                Vector3 leftPos = center + new Vector3(-0.4f, 0.05f, (i - 1) * 0.22f);
                CreatePin(leftPos, i, colors[i], $"Pin_L_{i}");

                // Правый контакт (перемешан по Z для интереса)
                int shuffledZ = (i + 1) % 3;
                Vector3 rightPos = center + new Vector3(0.4f, 0.05f, (shuffledZ - 1) * 0.22f);
                CreatePin(rightPos, i, colors[i], $"Pin_R_{i}");
            }

            Debug.Log("<color=yellow>[WiringMiniGame]</color> Соедините перегоревшие контакты по цветам (Красный к Красному, Синий к Синему, Желтый к Желтому)!");
        }

        private void CreatePin(Vector3 pos, int colorId, Color c, string name)
        {
            GameObject pinObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pinObj.name = name;
            pinObj.transform.position = pos;
            pinObj.transform.localScale = new Vector3(0.12f, 0.05f, 0.12f);
            pinObj.transform.SetParent(transform);

            Renderer rend = pinObj.GetComponent<Renderer>();
            rend.material.color = c;

            WirePin pin = new WirePin { obj = pinObj, colorId = colorId, isConnected = false, color = c };
            pins.Add(pin);

            Clickable3DObject clickable = pinObj.AddComponent<Clickable3DObject>();
            clickable.onInteract.AddListener(() => OnPinClicked(pin));
        }

        private void OnPinClicked(WirePin pin)
        {
            if (pin.isConnected) return;

            HangarAudio.Instance?.PlayClick();

            if (selectedPin == null)
            {
                // Выбрали первый контакт
                selectedPin = pin;
                pin.obj.transform.localScale = new Vector3(0.16f, 0.08f, 0.16f); // Подсветка выбора
                Debug.Log($"[WiringMiniGame] Выбран контакт цвета: {pin.color}. Выберите парный контакт!");
            }
            else if (selectedPin == pin)
            {
                // Повторный клик — сброс выбора
                pin.obj.transform.localScale = new Vector3(0.12f, 0.05f, 0.12f);
                selectedPin = null;
            }
            else
            {
                // Клик по второму контакту
                if (selectedPin.colorId == pin.colorId)
                {
                    // Совпадение цвета!
                    selectedPin.isConnected = true;
                    pin.isConnected = true;

                    selectedPin.obj.GetComponent<Renderer>().material.color = Color.green;
                    pin.obj.GetComponent<Renderer>().material.color = Color.green;

                    selectedPin.obj.transform.localScale = new Vector3(0.12f, 0.03f, 0.12f);
                    pin.obj.transform.localScale = new Vector3(0.12f, 0.03f, 0.12f);

                    HangarAudio.Instance?.PlaySuccess();
                    connectionsRemaining--;

                    Debug.Log($"<color=green>[WiringMiniGame] Контакт замкнут! Осталось цепей: {connectionsRemaining}</color>");
                    selectedPin = null;

                    if (connectionsRemaining <= 0)
                    {
                        Debug.Log("<color=green>[WiringMiniGame] Вся электроника восстановлена!</color>");
                        OnWiringCompleted?.Invoke();
                    }
                }
                else
                {
                    // Ошибка цвета
                    HangarAudio.Instance?.PlayWarning();
                    Debug.LogWarning("[WiringMiniGame] Ошибка! Цвета не совпадают!");
                    selectedPin.obj.transform.localScale = new Vector3(0.12f, 0.05f, 0.12f);
                    selectedPin = null;
                }
            }
        }

        public void Cleanup()
        {
            foreach (var p in pins)
            {
                if (p.obj != null) Destroy(p.obj);
            }
            pins.Clear();
            selectedPin = null;
            connectionsRemaining = 0;
        }
    }
}
