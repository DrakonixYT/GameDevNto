using UnityEngine;

namespace HangarGame.Core
{
    public class HangarAudio : MonoBehaviour
    {
        public static HangarAudio Instance { get; private set; }

        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource ambientSource;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.85f;
        [SerializeField, Range(0f, 1f)] private float ambientVolume = 0.35f;
        [SerializeField] private bool useFallbackBeep = true;

        [Header("Звуковые слоты")]
        [SerializeField] private AudioClip clipUiClick;
        [SerializeField] private AudioClip clipButton3D;
        [SerializeField] private AudioClip clipBoltUnscrew;
        [SerializeField] private AudioClip clipWireConnect;
        [SerializeField] private AudioClip clipPlateInsert;
        [SerializeField] private AudioClip clipWireError;
        [SerializeField] private AudioClip clipRepairDone;
        [SerializeField] private AudioClip clipMechSwap;
        [SerializeField] private AudioClip clipBatteryCharge;
        [SerializeField] private AudioClip clipWeaponBuy;
        [SerializeField] private AudioClip clipWeaponEquip;
        [SerializeField] private AudioClip clipMechDeploy;
        [SerializeField] private AudioClip clipMechRecall;
        [SerializeField] private AudioClip clipShieldToggle;
        [SerializeField] private AudioClip clipAlarm;
        [SerializeField] private AudioClip clipJaegerStage;
        [SerializeField] private AudioClip clipVictory;
        [SerializeField] private AudioClip clipHangarAmbient;

        private AudioClip cachedBeep;
        private AudioClip cachedAlarmBeep;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
            if (ambientSource == null)
            {
                ambientSource = gameObject.AddComponent<AudioSource>();
                ambientSource.loop = true;
                ambientSource.playOnAwake = false;
            }

            cachedBeep = CreateTone(620f, 0.05f, 0.25f);
            cachedAlarmBeep = CreateTone(440f, 0.25f, 0.35f);
        }

        private void Start()
        {
            if (clipHangarAmbient != null && ambientSource != null)
            {
                ambientSource.clip = clipHangarAmbient;
                ambientSource.volume = ambientVolume;
                ambientSource.Play();
            }
        }

        public void PlayClick() => Play(clipUiClick);
        public void PlayButton3D() => Play(clipButton3D);
        public void PlayBolt() => Play(clipBoltUnscrew);
        public void PlayWire() => Play(clipWireConnect);
        public void PlayPlate() => Play(clipPlateInsert);
        public void PlayError() => Play(clipWireError);
        public void PlaySuccess() => Play(clipRepairDone);
        public void PlayMechSwap() => Play(clipMechSwap);
        public void PlayBattery() => Play(clipBatteryCharge);
        public void PlayBuy() => Play(clipWeaponBuy);
        public void PlayEquip() => Play(clipWeaponEquip);
        public void PlayDeploy() => Play(clipMechDeploy);
        public void PlayRecall() => Play(clipMechRecall);
        public void PlayShield() => Play(clipShieldToggle);
        public void PlayAlarm() => Play(clipAlarm, true);
        public void PlayJaegerStage() => Play(clipJaegerStage);
        public void PlayVictory() => Play(clipVictory);

        private void Play(AudioClip clip, bool isAlarm = false)
        {
            if (sfxSource == null) return;
            AudioClip target = clip != null ? clip : (useFallbackBeep ? (isAlarm ? cachedAlarmBeep : cachedBeep) : null);
            if (target != null) sfxSource.PlayOneShot(target, sfxVolume);
        }

        private static AudioClip CreateTone(float freq, float dur, float vol)
        {
            const int rate = 44100;
            int count = Mathf.CeilToInt(rate * dur);
            float[] data = new float[count];
            for (int i = 0; i < count; i++)
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * (i / (float)rate)) * (1f - i / (float)count) * vol;

            var clip = AudioClip.Create("SfxFallback", count, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
