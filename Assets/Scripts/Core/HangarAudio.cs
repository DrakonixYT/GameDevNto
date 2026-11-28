using System.Collections.Generic;
using UnityEngine;

namespace HangarGame.Core
{
    public class HangarAudio : MonoBehaviour
    {
        public static HangarAudio Instance { get; private set; }

        [Header("Источники звука")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource ambientSource;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.85f;
        [SerializeField, Range(0f, 1f)] private float ambientVolume = 0.35f;
        [SerializeField] private bool synthFallbackIfEmpty = true;

        [Header("Интерфейс и верстак")]
        [SerializeField] private AudioClip clipUiClick;
        [SerializeField] private AudioClip clipButton3D;
        [SerializeField] private AudioClip clipBoltUnscrew;
        [SerializeField] private AudioClip clipWireConnect;
        [SerializeField] private AudioClip clipPlateInsert;
        [SerializeField] private AudioClip clipWireError;
        [SerializeField] private AudioClip clipRepairDone;

        [Header("Ангар, мехи и арсенал")]
        [SerializeField] private AudioClip clipMechSwap;
        [SerializeField] private AudioClip clipBatteryCharge;
        [SerializeField] private AudioClip clipWeaponBuy;
        [SerializeField] private AudioClip clipWeaponEquip;

        [Header("Радар, бой и Егерь")]
        [SerializeField] private AudioClip clipMechDeploy;
        [SerializeField] private AudioClip clipMechRecall;
        [SerializeField] private AudioClip clipShieldToggle;
        [SerializeField] private AudioClip clipAlarm;
        [SerializeField] private AudioClip clipJaegerStage;
        [SerializeField] private AudioClip clipVictory;
        [SerializeField] private AudioClip clipHangarAmbient;

        private readonly Dictionary<string, AudioClip> fallbackClips = new Dictionary<string, AudioClip>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (sfxSource == null)
                sfxSource = gameObject.AddComponent<AudioSource>();

            if (ambientSource == null)
            {
                ambientSource = gameObject.AddComponent<AudioSource>();
                ambientSource.loop = true;
                ambientSource.playOnAwake = false;
            }

            BuildFallbacks();
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

        public void PlayClick() => Play(clipUiClick, "Click");
        public void PlayButton3D() => Play(clipButton3D, "Button3D");
        public void PlayBolt() => Play(clipBoltUnscrew, "Bolt");
        public void PlayWire() => Play(clipWireConnect, "Click");
        public void PlayPlate() => Play(clipPlateInsert, "Bolt");
        public void PlayError() => Play(clipWireError, "Error");
        public void PlaySuccess() => Play(clipRepairDone, "Success");
        public void PlayMechSwap() => Play(clipMechSwap, "Swap");
        public void PlayBattery() => Play(clipBatteryCharge, "Success");
        public void PlayBuy() => Play(clipWeaponBuy, "Success");
        public void PlayEquip() => Play(clipWeaponEquip, "Bolt");
        public void PlayDeploy() => Play(clipMechDeploy, "Success");
        public void PlayRecall() => Play(clipMechRecall, "Bolt");
        public void PlayShield() => Play(clipShieldToggle, "Weld");
        public void PlayAlarm() => Play(clipAlarm, "Alarm");
        public void PlayJaegerStage() => Play(clipJaegerStage, "Success");
        public void PlayVictory() => Play(clipVictory, "Victory");

        private void Play(AudioClip userClip, string fallbackKey)
        {
            if (sfxSource == null) return;

            if (userClip != null)
            {
                sfxSource.PlayOneShot(userClip, sfxVolume);
                return;
            }

            if (synthFallbackIfEmpty && fallbackClips.TryGetValue(fallbackKey, out var fallback))
                sfxSource.PlayOneShot(fallback, sfxVolume);
        }

        private void BuildFallbacks()
        {
            fallbackClips["Click"] = SynthTone(760f, 0.045f, 0.25f);
            fallbackClips["Button3D"] = SynthTone(580f, 0.055f, 0.3f);
            fallbackClips["Bolt"] = SynthTone(310f, 0.09f, 0.35f);
            fallbackClips["Weld"] = SynthNoise(0.16f, 0.25f);
            fallbackClips["Swap"] = SynthSweep(180f, 340f, 0.28f, 0.28f);
            fallbackClips["Error"] = SynthTone(150f, 0.16f, 0.35f);
            fallbackClips["Alarm"] = SynthAlarm(0.85f);
            fallbackClips["Success"] = SynthChord(440f, 0.35f);
            fallbackClips["Victory"] = SynthChord(523.25f, 0.85f);
        }

        private AudioClip SynthTone(float freq, float dur, float vol)
        {
            int rate = 44100;
            int count = Mathf.CeilToInt(rate * dur);
            float[] samples = new float[count];
            for (int i = 0; i < count; i++)
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * (i / (float)rate)) * (1f - (float)i / count) * vol;

            var clip = AudioClip.Create("Tone", count, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip SynthSweep(float fStart, float fEnd, float dur, float vol)
        {
            int rate = 44100;
            int count = Mathf.CeilToInt(rate * dur);
            float[] samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)count;
                float f = Mathf.Lerp(fStart, fEnd, t);
                samples[i] = Mathf.Sin(2f * Mathf.PI * f * (i / (float)rate)) * Mathf.Sin(t * Mathf.PI) * vol;
            }
            var clip = AudioClip.Create("Sweep", count, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip SynthNoise(float dur, float vol)
        {
            int rate = 44100;
            int count = Mathf.CeilToInt(rate * dur);
            float[] samples = new float[count];
            for (int i = 0; i < count; i++)
                samples[i] = (Random.value * 2f - 1f) * (1f - (float)i / count) * vol;

            var clip = AudioClip.Create("Noise", count, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip SynthAlarm(float dur)
        {
            int rate = 44100;
            int count = Mathf.CeilToInt(rate * dur);
            float[] samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)rate;
                float freq = (t % 0.35f) < 0.175f ? 520f : 820f;
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.4f;
            }
            var clip = AudioClip.Create("Alarm", count, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip SynthChord(float root, float dur)
        {
            int rate = 44100;
            int count = Mathf.CeilToInt(rate * dur);
            float[] samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)rate;
                float s = Mathf.Sin(2f * Mathf.PI * root * t)
                        + Mathf.Sin(2f * Mathf.PI * root * 1.25f * t)
                        + Mathf.Sin(2f * Mathf.PI * root * 1.5f * t);
                samples[i] = (s / 3f) * (1f - (float)i / count) * 0.4f;
            }
            var clip = AudioClip.Create("Chord", count, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
