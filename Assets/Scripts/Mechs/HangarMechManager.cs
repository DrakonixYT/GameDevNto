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
        public List<Transform> bayMechs = new List<Transform>();
        public Animator podiumAnimator;

        public float exitDuration = 1.0f;
        public float enterDuration = 1.0f;
        public float verticalLiftOffset = 0.9f;
        public float bayVerticalLiftOffset = 9.5f;
        public bool startFirstMechDamaged = true;
        [Range(0.3f, 1f)] public float firstMechStartHpRatio = 0.58f;

        public int CurrentIndex { get; private set; }
        public MechUnit CurrentMech => (CurrentIndex >= 0 && CurrentIndex < Mechs.Count) ? Mechs[CurrentIndex] : null;
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
            foreach (var b in bayMechs)
                if (b != null && !bayRestPoses.ContainsKey(b))
                    bayRestPoses[b] = (b.localPosition, b.localRotation, b.localScale);
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
            foreach (var b in bayMechs)
                if (b != null) return true;
            return false;
        }

        private void Awake()
        {
            Instance = this;
            if (Mechs.Count == 0)
                Mechs.AddRange(GetComponentsInChildren<MechUnit>(true));

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
            if (Mechs.Count > 1 && Mechs[1] != null) Mechs[1].currentEnergy = 85f;
            if (Mechs.Count > 2 && Mechs[2] != null) Mechs[2].currentEnergy = 55f;
        }

        public void NextMech()
        {
            if (Mechs.Count > 1) SwitchTo((CurrentIndex + 1) % Mechs.Count);
        }

        public void PrevMech()
        {
            if (Mechs.Count > 1) SwitchTo((CurrentIndex - 1 + Mechs.Count) % Mechs.Count);
        }

        public void SelectMech(int idx)
        {
            if (idx >= 0 && idx < Mechs.Count && idx != CurrentIndex)
                SwitchTo(idx);
        }

        private void SwitchTo(int targetIdx)
        {
            int prevIdx = CurrentIndex;
            MechUnit prevMech = CurrentMech;
            CurrentIndex = targetIdx;
            MechUnit nextMech = CurrentMech;

            HangarAudio.Play(Sfx.Swap);
            if (podiumAnimator != null) podiumAnimator.SetTrigger("Swap");

            if (!Application.isPlaying || !gameObject.activeInHierarchy)
            {
                UpdatePodiumDisplay();
                OnMechSwitched?.Invoke(CurrentIndex);
                return;
            }

            if (swapCoroutine != null)
            {
                StopCoroutine(swapCoroutine);
                IsAnimatingSwap = false;
                UpdatePodiumDisplay();
            }

            if (HasBayMechs())
            {
                UpdateForegroundHoloDisplay();
                Transform prevBay = prevIdx >= 0 && prevIdx < bayMechs.Count ? bayMechs[prevIdx] : null;
                Transform nextBay = targetIdx >= 0 && targetIdx < bayMechs.Count ? bayMechs[targetIdx] : null;
                bool showNextBay = nextMech != null && !nextMech.isDeployed;
                swapCoroutine = StartCoroutine(SwapElevatorRoutine(prevBay, showNextBay ? nextBay : null));
            }
            else
            {
                swapCoroutine = StartCoroutine(SwapHoloRoutine(prevMech, nextMech));
            }

            OnMechSwitched?.Invoke(CurrentIndex);
        }

        private void UpdateForegroundHoloDisplay()
        {
            for (int i = 0; i < Mechs.Count; i++)
            {
                if (Mechs[i] == null) continue;
                bool show = i == CurrentIndex && !Mechs[i].isDeployed;
                Mechs[i].gameObject.SetActive(show);
                if (show) Mechs[i].RestoreRestPose();
            }
        }

        private IEnumerator SwapElevatorRoutine(Transform outgoing, Transform incoming)
        {
            IsAnimatingSwap = true;

            if (outgoing != null && outgoing != incoming && outgoing.gameObject.activeSelf)
            {
                if (!bayRestPoses.ContainsKey(outgoing))
                    bayRestPoses[outgoing] = (outgoing.localPosition, outgoing.localRotation, outgoing.localScale);
                var basePose = bayRestPoses[outgoing];
                yield return SlideVertical(outgoing, basePose.pos, basePose.pos - Vector3.up * bayVerticalLiftOffset, exitDuration);
                RestoreBayPose(outgoing);
                outgoing.gameObject.SetActive(false);
            }

            foreach (var b in bayMechs)
                if (b != null && b != incoming) b.gameObject.SetActive(false);

            if (incoming != null)
            {
                if (!bayRestPoses.ContainsKey(incoming))
                    bayRestPoses[incoming] = (incoming.localPosition, incoming.localRotation, incoming.localScale);
                var basePose = bayRestPoses[incoming];
                Vector3 startPos = basePose.pos - Vector3.up * bayVerticalLiftOffset;
                incoming.localPosition = startPos;
                incoming.localRotation = basePose.rot;
                incoming.localScale = basePose.scl;
                incoming.gameObject.SetActive(true);
                yield return SlideVertical(incoming, startPos, basePose.pos, enterDuration);
                RestoreBayPose(incoming);
            }

            IsAnimatingSwap = false;
            swapCoroutine = null;
        }

        private IEnumerator SwapHoloRoutine(MechUnit outgoing, MechUnit incoming)
        {
            IsAnimatingSwap = true;

            if (outgoing != null && outgoing != incoming && outgoing.gameObject.activeSelf)
            {
                outgoing.CaptureRestPose();
                yield return SlideVertical(outgoing.transform, outgoing.RestLocalPos, outgoing.RestLocalPos - Vector3.up * verticalLiftOffset, exitDuration);
                outgoing.RestoreRestPose();
                outgoing.gameObject.SetActive(false);
            }

            foreach (var m in Mechs)
                if (m != null && m != incoming) m.gameObject.SetActive(false);

            if (incoming != null && !incoming.isDeployed)
            {
                incoming.CaptureRestPose();
                Vector3 startPos = incoming.RestLocalPos - Vector3.up * verticalLiftOffset;
                incoming.transform.localPosition = startPos;
                incoming.transform.localRotation = incoming.RestLocalRot;
                incoming.transform.localScale = incoming.RestLocalScale;
                incoming.gameObject.SetActive(true);
                yield return SlideVertical(incoming.transform, startPos, incoming.RestLocalPos, enterDuration);
                incoming.RestoreRestPose();
            }

            IsAnimatingSwap = false;
            swapCoroutine = null;
        }

        private static IEnumerator SlideVertical(Transform target, Vector3 from, Vector3 to, float duration)
        {
            float dur = Mathf.Max(0.05f, duration);
            float t = 0f;
            while (t < dur && target != null)
            {
                t += Time.deltaTime;
                target.localPosition = Vector3.Lerp(from, to, Mathf.Clamp01(t / dur));
                yield return null;
            }
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
                bool showBay = i == CurrentIndex && i < Mechs.Count && Mechs[i] != null && !Mechs[i].isDeployed;
                bayMechs[i].gameObject.SetActive(showBay);
                if (showBay) RestoreBayPose(bayMechs[i]);
            }
        }

        public MechUnit GetMechById(int id) => Mechs.Find(m => m.id == id);
    }
}
