using System;
using System.Collections.Generic;
using UnityEngine;
using HangarGame.Core;
using HangarGame.Mechs;
using HangarGame.Interaction;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HangarGame.Repair
{
    public class RepairWorkbench : MonoBehaviour
    {
        public static RepairWorkbench Instance { get; private set; }
        public MechPart CurrentPart { get; private set; }
        public GameObject CurrentJaegerModule { get; private set; }
        public string CurrentItemName => CurrentPart != null ? CurrentPart.Data.partName : currentJaegerName;
        public bool HasItem => CurrentPart != null || CurrentJaegerModule != null;
        public bool IsJaegerModule => CurrentJaegerModule != null;

        private float targetYaw = 180f, targetPitch = 0f, curYaw = 180f, curPitch = 0f;
        private string currentJaegerName;
        private Action onJaegerDone;
        private readonly List<GameObject> activeMiniNodes = new List<GameObject>();
        private int nodesRemaining = 0;

        private void Awake() => Instance = this;

        private void Update()
        {
            if (!HasItem) return;

            // Hotkeys
            bool esc = false, space = false, tKey = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                esc = Keyboard.current.escapeKey.wasPressedThisFrame;
                space = Keyboard.current.spaceKey.wasPressedThisFrame;
                tKey = Keyboard.current.tKey.wasPressedThisFrame;
            }
#else
            esc = Input.GetKeyDown(KeyCode.Escape); space = Input.GetKeyDown(KeyCode.Space); tKey = Input.GetKeyDown(KeyCode.T);
#endif
            if (esc) CancelWork(); else if (space) FinishWork(); else if (tKey && CurrentPart != null) CycleModule();

            // 2-Axis Rotation
            float h = 0f, v = 0f;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) h -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) h += 1f;
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) v += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) v -= 1f;
            }
            if (Mouse.current != null && (Mouse.current.rightButton.isPressed || Mouse.current.middleButton.isPressed))
            {
                Vector2 d = Mouse.current.delta.ReadValue();
                targetYaw += d.x * 0.4f; targetPitch = Mathf.Clamp(targetPitch - d.y * 0.4f, -70f, 70f);
            }
#else
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) h -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h += 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v -= 1f;
            if (Input.GetMouseButton(1) || Input.GetMouseButton(2))
            {
                targetYaw += Input.GetAxis("Mouse X") * 4f; targetPitch = Mathf.Clamp(targetPitch - Input.GetAxis("Mouse Y") * 4f, -70f, 70f);
            }
#endif
            targetYaw += h * 90f * Time.deltaTime;
            targetPitch = Mathf.Clamp(targetPitch - v * 90f * Time.deltaTime, -70f, 70f);
            curYaw = Mathf.Lerp(curYaw, targetYaw, Time.deltaTime * 12f);
            curPitch = Mathf.Lerp(curPitch, targetPitch, Time.deltaTime * 12f);

            var t = CurrentPart != null ? CurrentPart.transform : (CurrentJaegerModule != null ? CurrentJaegerModule.transform : null);
            if (t != null) t.rotation = Quaternion.Euler(curPitch, curYaw, 0f);
        }

        public void RotateYaw(float d) => targetYaw += d;
        public void RotatePitch(float d) => targetPitch = Mathf.Clamp(targetPitch + d, -70f, 70f);
        public void ResetRotation() { targetYaw = 180f; targetPitch = 0f; }

        public void ReceivePart(MechPart p)
        {
            if (HasItem) return;
            CurrentPart = p; p.IsOnWorkbench = true;
            ResetRotation(); GameManager.Instance?.SwitchView(RoomView.WorkbenchFocus);
            StartCoroutine(SlideToWorkbench(p.transform, new Vector3(0f, 1.05f, 2.2f), Quaternion.Euler(0f, 180f, 0f), () =>
            {
                SpawnMiniNodes(new Color(1f, 0.8f, 0.1f), "Болт фиксатора");
            }));
        }

        public void ReceiveJaegerModule(GameObject mod, string name, Action onDone)
        {
            if (HasItem) return;
            CurrentJaegerModule = mod; currentJaegerName = name; onJaegerDone = onDone;
            ResetRotation(); GameManager.Instance?.SwitchView(RoomView.WorkbenchFocus);
            mod.transform.position = new Vector3(0f, 2.2f, 2.2f);
            StartCoroutine(SlideToWorkbench(mod.transform, new Vector3(0f, 1.05f, 2.2f), Quaternion.Euler(0f, 180f, 0f), () =>
            {
                SpawnMiniNodes(new Color(0.2f, 0.8f, 1f), "Калибровочный контакт");
            }));
        }

        private System.Collections.IEnumerator SlideToWorkbench(Transform t, Vector3 targetPos, Quaternion targetRot, Action onDone)
        {
            Vector3 sP = t.position; Quaternion sR = t.rotation;
            float el = 0f, d = 0.28f;
            while (el < d && t != null) { el += Time.deltaTime; float p = Mathf.SmoothStep(0f, 1f, el / d); t.position = Vector3.Lerp(sP, targetPos, p); t.rotation = Quaternion.Slerp(sR, targetRot, p); yield return null; }
            if (t != null) { t.position = targetPos; t.rotation = targetRot; }
            onDone?.Invoke();
        }

        private void SpawnMiniNodes(Color c, string prompt)
        {
            ClearNodes(); nodesRemaining = 3;
            for (int i = 0; i < 3; i++)
            {
                var n = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                n.transform.position = new Vector3((i - 1) * 0.45f, 0.88f, 2.2f);
                n.transform.localScale = new Vector3(0.12f, 0.04f, 0.12f);
                n.GetComponent<Renderer>().material.color = c;
                var clk = n.AddComponent<ClickableObject>();
                clk.promptText = $"{prompt}: кликните для монтажа";
                clk.onClick = () => { HangarAudioLight.Instance?.PlayBolt(); activeMiniNodes.Remove(n); Destroy(n); if (--nodesRemaining <= 0) FinishWork(); };
                activeMiniNodes.Add(n);
            }
        }

        public void CycleModule()
        {
            if (CurrentPart == null) return;
            CurrentPart.Setup(ModuleCatalog.GetNextModule(CurrentPart.Data));
            HangarAudioLight.Instance?.PlayBolt();
        }

        public void FinishWork()
        {
            ClearNodes();
            if (CurrentJaegerModule != null)
            {
                Destroy(CurrentJaegerModule); CurrentJaegerModule = null;
                onJaegerDone?.Invoke(); HangarAudioLight.Instance?.PlaySuccess();
                GameManager.Instance?.SwitchView(RoomView.Hangar);
            }
            else if (CurrentPart != null)
            {
                GameManager.Instance?.TrySpend(CurrentPart.Data.repairCredits, CurrentPart.Data.repairAlloy);
                CurrentPart.RepairFull(); CurrentPart.ReturnToSocket(); CurrentPart = null;
                HangarAudioLight.Instance?.PlaySuccess();
                GameManager.Instance?.SwitchView(RoomView.Hangar);
            }
        }

        public void CancelWork()
        {
            ClearNodes();
            if (CurrentJaegerModule != null) { Destroy(CurrentJaegerModule); CurrentJaegerModule = null; }
            if (CurrentPart != null) { CurrentPart.ReturnToSocket(); CurrentPart = null; }
            GameManager.Instance?.SwitchView(RoomView.Hangar);
        }

        private void ClearNodes() { foreach (var n in activeMiniNodes) if (n != null) Destroy(n); activeMiniNodes.Clear(); }
    }
}
