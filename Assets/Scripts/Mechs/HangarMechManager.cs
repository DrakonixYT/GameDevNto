using System;
using System.Collections;
using System.Collections.Generic;
using HangarGame.Core;
using UnityEngine;

namespace HangarGame.Mechs
{
    public class HangarMechManager : MonoBehaviour
    {
        public static HangarMechManager Instance { get; private set; }

        public List<MechUnit> Mechs = new List<MechUnit>();

        [Header("Анимация смены больших мехов в ангаре")]
        [SerializeField] private List<Transform> bayMechs = new List<Transform>();
        [SerializeField] private Animator podiumAnimator;
        [SerializeField] private float exitDuration = 0.24f;
        [SerializeField] private float enterDuration = 0.38f;
        [SerializeField] private float verticalLiftOffset = 0.85f;
        [SerializeField] private float horizontalSlideOffset = 0.45f;
        [SerializeField] private float bayVerticalLiftOffset = 3.6f;
        [SerializeField] private float bayHorizontalSlideOffset = 1.8f;
        [SerializeField] private float entryYawTwist = 18f;

        [Header("Начальное состояние")]
        [SerializeField] private bool startFirstMechDamaged = true;
        [SerializeField, Range(0.3f, 1f)] private float firstMechStartHpRatio = 0.58f;

        public int CurrentIndex { get; private set; }
        public MechUnit CurrentMech => (Mechs.Count > 0 && CurrentIndex >= 0 && CurrentIndex < Mechs.Count) ? Mechs[CurrentIndex] : null;
        public bool IsAnimatingSwap { get; private set; }

        public event Action<int> OnMechSwitched;

        private Coroutine swapCoroutine;
        private readonly Dictionary<Transform, (Vector3 pos, Quaternion rot, Vector3 scl)> bayRestPoses = new Dictionary<Transform, (Vector3, Quaternion, Vector3)>();

        public void ConfigureBayMechs(List<Transform> bayList)
        {
            bayMechs = bayList ?? new List<Transform>();
            bayRestPoses.Clear();
            CaptureBayPoses();
        }

        private void CaptureBayPoses()
        {
            if (bayMechs == null) return;
            foreach (var b in bayMechs)
            {
                if (b != null && !bayRestPoses.ContainsKey(b))
                    bayRestPoses[b] = (b.localPosition, b.localRotation, b.localScale);
            }
        }

        private void RestoreBayPose(Transform b)
        {
            if (b != null && bayRestPoses.TryGetValue(b, out var pose))
            {
                b.localPosition = pose.pos;
                b.localRotation = pose.rot;
                b.localScale = pose.scl;
            }
        }

        private bool HasBayMechs()
        {
            if (bayMechs == null || bayMechs.Count == 0) return false;
            for (int i = 0; i < bayMechs.Count; i++)
                if (bayMechs[i] != null) return true;
            return false;
        }

        private void Awake()
        {
            Instance = this;

            if (Mechs.Count == 0)
            {
                var found = GetComponentsInChildren<MechUnit>(true);
                Mechs.AddRange(found);
            }

            foreach (var m in Mechs)
            {
                if (m == null) continue;
                m.CaptureRestPose();
                m.EnsurePartsLoaded();
            }

            CaptureBayPoses();
            UpdatePodiumDisplay();
        }

        private void Start()
        {
            if (startFirstMechDamaged && Mechs.Count > 0 && Mechs[0] != null)
            {
                Mechs[0].SyncPartsFromHp(firstMechStartHpRatio);
                Mechs[0].currentEnergy = 75f;
            }
            if (Mechs.Count > 1 && Mechs[1] != null)
                Mechs[1].currentEnergy = 85f;
            if (Mechs.Count > 2 && Mechs[2] != null)
                Mechs[2].currentEnergy = 55f;
        }

        public void NextMech()
        {
            if (Mechs.Count <= 1) return;
            SwitchTo((CurrentIndex + 1) % Mechs.Count, 1);
        }

        public void PrevMech()
        {
            if (Mechs.Count <= 1) return;
            SwitchTo((CurrentIndex - 1 + Mechs.Count) % Mechs.Count, -1);
        }

        public void SelectMech(int idx)
        {
            if (idx < 0 || idx >= Mechs.Count || idx == CurrentIndex) return;
            SwitchTo(idx, idx > CurrentIndex ? 1 : -1);
        }

        private void SwitchTo(int targetIdx, int direction)
        {
            int prevIdx = CurrentIndex;
            MechUnit prevMech = CurrentMech;
            CurrentIndex = targetIdx;
            MechUnit nextMech = CurrentMech;

            HangarAudio.Instance?.PlayMechSwap();
            if (podiumAnimator != null) podiumAnimator.SetTrigger("Swap");

            if (Application.isPlaying && gameObject.activeInHierarchy)
            {
                if (swapCoroutine != null)
                {
                    StopCoroutine(swapCoroutine);
                    SnapAllToRest();
                }

                if (HasBayMechs())
                {
                    UpdateForegroundHoloDisplay();
                    Transform prevBay = (prevIdx >= 0 && prevIdx < bayMechs.Count) ? bayMechs[prevIdx] : null;
                    Transform nextBay = (targetIdx >= 0 && targetIdx < bayMechs.Count) ? bayMechs[targetIdx] : null;
                    bool showNextBay = nextMech != null && !nextMech.isDeployed;
                    swapCoroutine = StartCoroutine(AnimateBaySwapRoutine(prevBay, showNextBay ? nextBay : null, direction));
                }
                else
                {
                    swapCoroutine = StartCoroutine(AnimateSwapRoutine(prevMech, nextMech, direction));
                }
            }
            else
            {
                UpdatePodiumDisplay();
            }

            OnMechSwitched?.Invoke(CurrentIndex);
        }

        private void UpdateForegroundHoloDisplay()
        {
            for (int i = 0; i < Mechs.Count; i++)
            {
                if (Mechs[i] == null) continue;
                bool show = (i == CurrentIndex) && !Mechs[i].isDeployed;
                Mechs[i].gameObject.SetActive(show);
                if (show) Mechs[i].RestoreRestPose();
            }
        }

        private void SnapAllToRest()
        {
            IsAnimatingSwap = false;
            UpdatePodiumDisplay();
        }

        private IEnumerator AnimateBaySwapRoutine(Transform outgoing, Transform incoming, int dir)
        {
            IsAnimatingSwap = true;

            if (outgoing != null && outgoing != incoming && outgoing.gameObject.activeSelf)
            {
                if (!bayRestPoses.ContainsKey(outgoing))
                    bayRestPoses[outgoing] = (outgoing.localPosition, outgoing.localRotation, outgoing.localScale);
                var basePose = bayRestPoses[outgoing];

                Vector3 outPos = basePose.pos + new Vector3(-dir * bayHorizontalSlideOffset, -bayVerticalLiftOffset, 0f);
                Quaternion outRot = Quaternion.Euler(0f, -dir * entryYawTwist * 0.4f, 0f) * basePose.rot;
                Vector3 outScale = basePose.scl * 0.85f;

                float t = 0f;
                while (t < exitDuration)
                {
                    t += Time.deltaTime;
                    float k = t / Mathf.Max(0.01f, exitDuration);
                    float ease = k * k;
                    outgoing.localPosition = Vector3.Lerp(basePose.pos, outPos, ease);
                    outgoing.localRotation = Quaternion.Slerp(basePose.rot, outRot, ease);
                    outgoing.localScale = Vector3.Lerp(basePose.scl, outScale, ease);
                    yield return null;
                }

                RestoreBayPose(outgoing);
                outgoing.gameObject.SetActive(false);
            }

            for (int i = 0; i < bayMechs.Count; i++)
            {
                if (bayMechs[i] != null && bayMechs[i] != incoming)
                    bayMechs[i].gameObject.SetActive(false);
            }

            if (incoming != null)
            {
                if (!bayRestPoses.ContainsKey(incoming))
                    bayRestPoses[incoming] = (incoming.localPosition, incoming.localRotation, incoming.localScale);
                var basePose = bayRestPoses[incoming];

                Vector3 startPos = basePose.pos + new Vector3(dir * bayHorizontalSlideOffset, -bayVerticalLiftOffset, 0f);
                Quaternion startRot = Quaternion.Euler(0f, dir * entryYawTwist, 0f) * basePose.rot;
                Vector3 startScale = basePose.scl * 0.85f;

                incoming.localPosition = startPos;
                incoming.localRotation = startRot;
                incoming.localScale = startScale;
                incoming.gameObject.SetActive(true);

                float t = 0f;
                while (t < enterDuration)
                {
                    t += Time.deltaTime;
                    float k = Mathf.Clamp01(t / Mathf.Max(0.01f, enterDuration));
                    float ease = 1f - Mathf.Pow(1f - k, 3f);
                    float liftBounce = Mathf.Sin(k * Mathf.PI) * 0.22f;

                    incoming.localPosition = Vector3.Lerp(startPos, basePose.pos, ease) + new Vector3(0f, liftBounce, 0f);
                    incoming.localRotation = Quaternion.Slerp(startRot, basePose.rot, ease);
                    incoming.localScale = Vector3.Lerp(startScale, basePose.scl, ease);
                    yield return null;
                }

                RestoreBayPose(incoming);
            }

            IsAnimatingSwap = false;
            swapCoroutine = null;
        }

        private IEnumerator AnimateSwapRoutine(MechUnit outgoing, MechUnit incoming, int dir)
        {
            IsAnimatingSwap = true;

            if (outgoing != null && outgoing != incoming && outgoing.gameObject.activeSelf)
            {
                outgoing.CaptureRestPose();
                Vector3 basePos = outgoing.RestLocalPos;
                Quaternion baseRot = outgoing.RestLocalRot;
                Vector3 baseScale = outgoing.RestLocalScale;

                Vector3 outPos = basePos + new Vector3(-dir * horizontalSlideOffset, -verticalLiftOffset, 0f);
                Quaternion outRot = Quaternion.Euler(0f, -dir * entryYawTwist * 0.5f, 0f) * baseRot;
                Vector3 outScale = baseScale * 0.72f;

                float t = 0f;
                while (t < exitDuration)
                {
                    t += Time.deltaTime;
                    float k = t / Mathf.Max(0.01f, exitDuration);
                    float ease = k * k;
                    outgoing.transform.localPosition = Vector3.Lerp(basePos, outPos, ease);
                    outgoing.transform.localRotation = Quaternion.Slerp(baseRot, outRot, ease);
                    outgoing.transform.localScale = Vector3.Lerp(baseScale, outScale, ease);
                    yield return null;
                }

                outgoing.RestoreRestPose();
                outgoing.gameObject.SetActive(false);
            }

            for (int i = 0; i < Mechs.Count; i++)
            {
                if (Mechs[i] != null && Mechs[i] != incoming)
                    Mechs[i].gameObject.SetActive(false);
            }

            if (incoming != null && !incoming.isDeployed)
            {
                incoming.CaptureRestPose();
                Vector3 basePos = incoming.RestLocalPos;
                Quaternion baseRot = incoming.RestLocalRot;
                Vector3 baseScale = incoming.RestLocalScale;

                Vector3 startPos = basePos + new Vector3(dir * horizontalSlideOffset, -verticalLiftOffset, 0f);
                Quaternion startRot = Quaternion.Euler(0f, dir * entryYawTwist, 0f) * baseRot;
                Vector3 startScale = baseScale * 0.75f;

                incoming.transform.localPosition = startPos;
                incoming.transform.localRotation = startRot;
                incoming.transform.localScale = startScale;
                incoming.gameObject.SetActive(true);

                float t = 0f;
                while (t < enterDuration)
                {
                    t += Time.deltaTime;
                    float k = Mathf.Clamp01(t / Mathf.Max(0.01f, enterDuration));
                    float ease = 1f - Mathf.Pow(1f - k, 3f);
                    float liftBounce = Mathf.Sin(k * Mathf.PI) * 0.06f;

                    incoming.transform.localPosition = Vector3.Lerp(startPos, basePos, ease) + new Vector3(0f, liftBounce, 0f);
                    incoming.transform.localRotation = Quaternion.Slerp(startRot, baseRot, ease);
                    incoming.transform.localScale = Vector3.Lerp(startScale, baseScale, ease);
                    yield return null;
                }

                incoming.RestoreRestPose();
            }

            IsAnimatingSwap = false;
            swapCoroutine = null;
        }

        public void RechargeCurrentMech()
        {
            if (CurrentMech != null && !CurrentMech.isDeployed)
                CurrentMech.RechargeBattery();
        }

        public void UpdatePodiumDisplay()
        {
            UpdateForegroundHoloDisplay();
            for (int i = 0; i < bayMechs.Count; i++)
            {
                if (bayMechs[i] == null) continue;
                bool showBay = (i == CurrentIndex) && (i < Mechs.Count && Mechs[i] != null && !Mechs[i].isDeployed);
                bayMechs[i].gameObject.SetActive(showBay);
                if (showBay) RestoreBayPose(bayMechs[i]);
            }
        }

        public MechUnit GetMechById(int id) => Mechs.Find(m => m.id == id);
    }
}
