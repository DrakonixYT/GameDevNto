using System.Collections.Generic;
using UnityEngine;

namespace HangarGame.Core
{
    public class HangarAudio : MonoBehaviour
    {
        public static HangarAudio Instance { get; private set; }

        private AudioSource audioSource;
        private Dictionary<string, AudioClip> proceduralClips = new Dictionary<string, AudioClip>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            GenerateProceduralAudioClips();
        }

        private void OnEnable()
        {
            EventBus.OnThreatAppeared += (n, a, w, p) => PlayAlarm();
            EventBus.OnEnemyAdapted += (msg) => PlayWarning();
            EventBus.OnThreatResolved += (msg) => PlaySuccess();
            EventBus.OnGameWon += () => PlayVictoryFanfare();
        }

        private void GenerateProceduralAudioClips()
        {
            proceduralClips["Click"] = CreateToneClip("Click", 800f, 0.05f, 0.4f);
            proceduralClips["Bolt"] = CreateToneClip("Bolt", 350f, 0.08f, 0.5f);
            proceduralClips["Weld"] = CreateNoiseClip("Weld", 0.15f, 0.3f);
            proceduralClips["Alarm"] = CreateAlarmKlaxonClip("Alarm", 1.2f);
            proceduralClips["Success"] = CreateMajorChordClip("Success", 440f, 0.35f);
            proceduralClips["Warning"] = CreateToneClip("Warning", 220f, 0.3f, 0.6f);
            proceduralClips["Victory"] = CreateMajorChordClip("Victory", 523.25f, 0.8f);
        }

        public void PlayClick() => PlayClip("Click");
        public void PlayBolt() => PlayClip("Bolt");
        public void PlayWeld() => PlayClip("Weld");
        public void PlayAlarm() => PlayClip("Alarm");
        public void PlayWarning() => PlayClip("Warning");
        public void PlaySuccess() => PlayClip("Success");
        public void PlayVictoryFanfare() => PlayClip("Victory");

        private void PlayClip(string name)
        {
            if (audioSource != null && proceduralClips.TryGetValue(name, out AudioClip clip))
            {
                audioSource.PlayOneShot(clip);
            }
        }

        #region Procedural Synthesis (Без внешних файлов, чистая математика звука)

        private AudioClip CreateToneClip(string name, float freq, float duration, float volume)
        {
            int sampleRate = 44100;
            int samplesCount = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samplesCount];

            for (int i = 0; i < samplesCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = 1f - (float)i / samplesCount; // Linear decay
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * volume;
            }

            AudioClip clip = AudioClip.Create(name, samplesCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateAlarmKlaxonClip(string name, float duration)
        {
            int sampleRate = 44100;
            int samplesCount = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samplesCount];

            for (int i = 0; i < samplesCount; i++)
            {
                float t = (float)i / sampleRate;
                float cycle = (t % 0.35f) / 0.35f;
                float freq = (cycle < 0.5f) ? 520f : 860f;
                float envelope = 0.5f + 0.5f * Mathf.Sin(t * Mathf.PI / duration);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.6f;
            }

            AudioClip clip = AudioClip.Create(name, samplesCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateDualToneClip(string name, float freq1, float freq2, float duration)
        {
            int sampleRate = 44100;
            int samplesCount = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samplesCount];

            for (int i = 0; i < samplesCount; i++)
            {
                float t = (float)i / sampleRate;
                float f = (t % 0.1f < 0.05f) ? freq1 : freq2;
                float envelope = 1f - (float)i / samplesCount;
                data[i] = Mathf.Sin(2f * Mathf.PI * f * t) * envelope * 0.5f;
            }

            AudioClip clip = AudioClip.Create(name, samplesCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateNoiseClip(string name, float duration, float volume)
        {
            int sampleRate = 44100;
            int samplesCount = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samplesCount];

            for (int i = 0; i < samplesCount; i++)
            {
                float noise = (Random.value * 2f - 1f);
                float envelope = 1f - (float)i / samplesCount;
                data[i] = noise * envelope * volume;
            }

            AudioClip clip = AudioClip.Create(name, samplesCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateMajorChordClip(string name, float rootFreq, float duration)
        {
            int sampleRate = 44100;
            int samplesCount = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samplesCount];

            float third = rootFreq * 1.25f; // Major third
            float fifth = rootFreq * 1.5f;  // Fifth

            for (int i = 0; i < samplesCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = 1f - (float)i / samplesCount;
                float tone = Mathf.Sin(2f * Mathf.PI * rootFreq * t) +
                             Mathf.Sin(2f * Mathf.PI * third * t) +
                             Mathf.Sin(2f * Mathf.PI * fifth * t);
                data[i] = (tone / 3f) * envelope * 0.5f;
            }

            AudioClip clip = AudioClip.Create(name, samplesCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        #endregion
    }
}
