using System.Collections.Generic;
using UnityEngine;

namespace HangarGame.Core
{
    public class HangarAudioLight : MonoBehaviour
    {
        public static HangarAudioLight Instance { get; private set; }
        [SerializeField] private Light redAlarmLight;
        private AudioSource src;
        private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        private bool isAlarm = false;

        private void Awake()
        {
            Instance = this;
            src = gameObject.AddComponent<AudioSource>();
            clips["Click"] = SynthTone(750f, 0.05f, 0.4f);
            clips["Bolt"] = SynthTone(320f, 0.1f, 0.5f);
            clips["Weld"] = SynthNoise(0.18f, 0.35f);
            clips["Alarm"] = SynthAlarm(1.0f);
            clips["Success"] = SynthChord(440f, 0.4f);
            clips["Victory"] = SynthChord(523.25f, 0.9f);
        }

        private void Update()
        {
            if (isAlarm && redAlarmLight != null)
                redAlarmLight.intensity = Mathf.PingPong(Time.time * 6f, 4.5f) + 0.8f;
        }

        public void SetAlarm(bool act)
        {
            isAlarm = act;
            if (redAlarmLight != null) redAlarmLight.intensity = act ? 3f : 0f;
            if (act) Play("Alarm");
        }

        public void PlayClick() => Play("Click");
        public void PlayBolt() => Play("Bolt");
        public void PlayWeld() => Play("Weld");
        public void PlaySuccess() => Play("Success");
        public void PlayVictory() => Play("Victory");

        private void Play(string id) { if (clips.TryGetValue(id, out var c) && src != null) src.PlayOneShot(c); }

        private AudioClip SynthTone(float freq, float dur, float vol)
        {
            int r = 44100, cnt = Mathf.CeilToInt(r * dur); float[] d = new float[cnt];
            for (int i = 0; i < cnt; i++) d[i] = Mathf.Sin(2f * Mathf.PI * freq * (i / (float)r)) * (1f - (float)i / cnt) * vol;
            var c = AudioClip.Create("T", cnt, 1, r, false); c.SetData(d, 0); return c;
        }

        private AudioClip SynthNoise(float dur, float vol)
        {
            int r = 44100, cnt = Mathf.CeilToInt(r * dur); float[] d = new float[cnt];
            for (int i = 0; i < cnt; i++) d[i] = (Random.value * 2f - 1f) * (1f - (float)i / cnt) * vol;
            var c = AudioClip.Create("N", cnt, 1, r, false); c.SetData(d, 0); return c;
        }

        private AudioClip SynthAlarm(float dur)
        {
            int r = 44100, cnt = Mathf.CeilToInt(r * dur); float[] d = new float[cnt];
            for (int i = 0; i < cnt; i++) d[i] = Mathf.Sin(2f * Mathf.PI * (((i / (float)r % 0.35f) < 0.175f) ? 540f : 860f) * (i / (float)r)) * 0.55f;
            var c = AudioClip.Create("A", cnt, 1, r, false); c.SetData(d, 0); return c;
        }

        private AudioClip SynthChord(float root, float dur)
        {
            int r = 44100, cnt = Mathf.CeilToInt(r * dur); float[] d = new float[cnt];
            for (int i = 0; i < cnt; i++) { float t = i / (float)r; d[i] = (Mathf.Sin(2f * Mathf.PI * root * t) + Mathf.Sin(2f * Mathf.PI * root * 1.25f * t) + Mathf.Sin(2f * Mathf.PI * root * 1.5f * t)) / 3f * (1f - (float)i / cnt) * 0.5f; }
            var c = AudioClip.Create("C", cnt, 1, r, false); c.SetData(d, 0); return c;
        }
    }
}
