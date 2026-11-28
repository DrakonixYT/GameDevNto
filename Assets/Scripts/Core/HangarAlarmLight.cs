using UnityEngine;

namespace HangarGame.Core
{
    public class HangarAlarmLight : MonoBehaviour
    {
        public static HangarAlarmLight Instance { get; private set; }

        [SerializeField] private Light redAlarmLight;
        [SerializeField] private float minIntensity = 0.8f;
        [SerializeField] private float maxIntensity = 5.0f;
        [SerializeField] private float pulseSpeed = 6f;

        private bool isAlarmActive;

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            if (!isAlarmActive || redAlarmLight == null) return;
            redAlarmLight.intensity = minIntensity + Mathf.PingPong(Time.time * pulseSpeed, maxIntensity - minIntensity);
        }

        public void ConfigureLight(Light targetLight)
        {
            redAlarmLight = targetLight;
            if (redAlarmLight != null && !isAlarmActive)
                redAlarmLight.intensity = 0f;
        }

        public void SetAlarm(bool active)
        {
            if (isAlarmActive == active) return;
            isAlarmActive = active;

            if (redAlarmLight != null)
                redAlarmLight.intensity = active ? minIntensity : 0f;

            if (active)
                HangarAudio.Instance?.PlayAlarm();
        }
    }
}
