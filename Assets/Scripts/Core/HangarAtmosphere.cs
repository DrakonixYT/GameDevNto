using UnityEngine;
using HangarGame.Core;

namespace HangarGame.Core
{
    public class HangarAtmosphere : MonoBehaviour
    {
        public static HangarAtmosphere Instance { get; private set; }

        [Header("Lighting References")]
        [SerializeField] private Light mainDirectionalLight;
        [SerializeField] private Light redAlarmLight;

        [Header("Alarm Settings")]
        [SerializeField] private float alarmPulseSpeed = 4f;
        [SerializeField] private bool isAlarmActive = false;

        private Color normalDirColor = new Color(0.95f, 0.98f, 1f);
        private float normalDirIntensity = 1.2f;

        private void Awake()
        {
            Instance = this;

            if (mainDirectionalLight == null)
            {
                Light[] lights = FindObjectsOfType<Light>();
                foreach (var l in lights)
                {
                    if (l.type == LightType.Directional)
                    {
                        mainDirectionalLight = l;
                        break;
                    }
                }
            }

            if (mainDirectionalLight != null)
            {
                normalDirColor = mainDirectionalLight.color;
                normalDirIntensity = mainDirectionalLight.intensity;
            }

            if (redAlarmLight == null)
            {
                GameObject alarmObj = new GameObject("Emergency_Alarm_Light");
                alarmObj.transform.position = new Vector3(0f, 4.5f, 0f);
                redAlarmLight = alarmObj.AddComponent<Light>();
                redAlarmLight.type = LightType.Point;
                redAlarmLight.color = Color.red;
                redAlarmLight.range = 25f;
                redAlarmLight.intensity = 0f;
            }
        }

        private void OnEnable()
        {
            EventBus.OnThreatAppeared += HandleThreatAppeared;
            EventBus.OnThreatResolved += HandleThreatResolved;
            EventBus.OnGameWon += HandleVictory;
        }

        private void OnDisable()
        {
            EventBus.OnThreatAppeared -= HandleThreatAppeared;
            EventBus.OnThreatResolved -= HandleThreatResolved;
            EventBus.OnGameWon -= HandleVictory;
        }

        private void Update()
        {
            if (isAlarmActive && redAlarmLight != null)
            {
                // Пульсирующий красный свет аварийной тревоги
                float pulse = (Mathf.Sin(Time.time * alarmPulseSpeed) + 1f) * 0.5f;
                redAlarmLight.intensity = Mathf.Lerp(1f, 5f, pulse);
            }
        }

        private void HandleThreatAppeared(string name, DamageType atk, DamageType weak, float power)
        {
            SetAlarm(true);
        }

        private void HandleThreatResolved(string log)
        {
            SetAlarm(false);
        }

        private void HandleVictory()
        {
            SetAlarm(false);
            if (mainDirectionalLight != null)
            {
                mainDirectionalLight.color = new Color(1f, 0.9f, 0.6f); // Теплый триумфальный свет
                mainDirectionalLight.intensity = 1.5f;
            }
        }

        public void SetAlarm(bool active)
        {
            isAlarmActive = active;

            if (mainDirectionalLight != null)
            {
                mainDirectionalLight.color = active ? new Color(0.3f, 0.35f, 0.5f) : normalDirColor;
                mainDirectionalLight.intensity = active ? 0.4f : normalDirIntensity;
            }

            if (redAlarmLight != null)
            {
                redAlarmLight.intensity = active ? 3f : 0f;
            }
        }
    }
}
