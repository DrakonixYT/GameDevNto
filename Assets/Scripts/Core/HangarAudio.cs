using System;
using System.Collections.Generic;
using UnityEngine;

namespace HangarGame.Core
{
    public enum Sfx
    {
        Click,
        Button3D,
        Bolt,
        Wire,
        Plate,
        Error,
        Success,
        Swap,
        Battery,
        Buy,
        Equip,
        Deploy,
        Recall,
        Shield,
        Alarm,
        JaegerStage,
        Victory
    }

    [Serializable]
    public struct SfxClipEntry
    {
        public Sfx id;
        public AudioClip clip;
    }

    public class HangarAudio : MonoBehaviour
    {
        public static HangarAudio Instance { get; private set; }

        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource ambientSource;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.85f;
        [SerializeField, Range(0f, 1f)] private float ambientVolume = 0.35f;
        [SerializeField] private AudioClip ambientClip;
        [SerializeField] private List<SfxClipEntry> clips = new List<SfxClipEntry>();

        private readonly Dictionary<Sfx, AudioClip> clipMap = new Dictionary<Sfx, AudioClip>();
        private AudioClip clickTone;
        private AudioClip alertTone;

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

            foreach (var entry in clips)
                if (entry.clip != null) clipMap[entry.id] = entry.clip;

            clickTone = CreateTone(620f, 0.05f, 0.22f);
            alertTone = CreateTone(420f, 0.22f, 0.30f);
        }

        private void Start()
        {
            if (ambientClip != null && ambientSource != null)
            {
                ambientSource.clip = ambientClip;
                ambientSource.volume = ambientVolume;
                ambientSource.Play();
            }
        }

        public static void Play(Sfx id) => Instance?.PlayOneShot(id);

        private void PlayOneShot(Sfx id)
        {
            if (sfxSource == null) return;
            if (clipMap.TryGetValue(id, out var custom) && custom != null)
            {
                sfxSource.PlayOneShot(custom, sfxVolume);
                return;
            }

            bool isAlert = id == Sfx.Alarm || id == Sfx.Error;
            sfxSource.pitch = isAlert ? 0.9f : UnityEngine.Random.Range(0.95f, 1.06f);
            sfxSource.PlayOneShot(isAlert ? alertTone : clickTone, sfxVolume);
        }

        private static AudioClip CreateTone(float freq, float dur, float vol)
        {
            const int rate = 44100;
            int count = Mathf.CeilToInt(rate * dur);
            float[] data = new float[count];
            for (int i = 0; i < count; i++)
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * (i / (float)rate)) * (1f - i / (float)count) * vol;

            var clip = AudioClip.Create("Tone", count, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
