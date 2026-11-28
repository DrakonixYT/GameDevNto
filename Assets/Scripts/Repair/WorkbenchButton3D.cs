using HangarGame.Core;
using HangarGame.Interaction;
using UnityEngine;

namespace HangarGame.Repair
{
    public enum WorkbenchButtonAction
    {
        RotateYawLeft,
        RotateYawRight,
        RotatePitchUp,
        RotatePitchDown,
        ResetRotation,
        FinishWork,
        CancelWork
    }

    public class WorkbenchButton3D : MonoBehaviour, IInteractable
    {
        [SerializeField] private WorkbenchButtonAction action;
        [SerializeField] private string label = "";
        [SerializeField] private string prompt = "";
        [SerializeField] private Color idleColor = new Color(0.16f, 0.25f, 0.36f);
        [SerializeField] private Color hoverColor = new Color(0.32f, 0.72f, 0.88f);
        [SerializeField] private Color readyColor = new Color(0.20f, 0.58f, 0.36f);
        [SerializeField] private float continuousSpeed = 95f;
        [SerializeField] private float stepAngle = 22f;

        private Renderer capRenderer;
        private Vector3 restLocalPos;
        private float pressTimer;
        private bool isHovered;

        private void Awake()
        {
            capRenderer = GetComponent<Renderer>();
            restLocalPos = transform.localPosition;
        }

        private void Update()
        {
            if (pressTimer > 0f)
            {
                pressTimer -= Time.deltaTime;
                transform.localPosition = restLocalPos - new Vector3(0f, 0.012f, 0f);
            }
            else
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, restLocalPos, Time.deltaTime * 16f);
            }

            RefreshColor();
        }

        public void Setup(WorkbenchButtonAction btnAction, string btnLabel, string btnPrompt, Color baseColor)
        {
            action = btnAction;
            label = btnLabel;
            prompt = btnPrompt;
            idleColor = baseColor;
            restLocalPos = transform.localPosition;
            capRenderer = GetComponent<Renderer>();

            var tm = GetComponentInChildren<TextMesh>();
            if (tm != null) tm.text = btnLabel;

            RefreshColor();
        }

        private void RefreshColor()
        {
            if (capRenderer == null || !Application.isPlaying) return;

            Color target = idleColor;
            if (action == WorkbenchButtonAction.FinishWork && RepairWorkbench.Instance != null && RepairWorkbench.Instance.IsWorkComplete)
                target = readyColor;
            if (isHovered)
                target = Color.Lerp(target, hoverColor, 0.55f);

            ModelUtils.SetColor(capRenderer, target);
        }

        public bool CanInteract()
        {
            return gameObject.activeInHierarchy && RepairWorkbench.Instance != null && RepairWorkbench.Instance.HasItem;
        }

        public string GetPrompt()
        {
            if (action == WorkbenchButtonAction.FinishWork && RepairWorkbench.Instance != null)
                return RepairWorkbench.Instance.IsWorkComplete ? "Готово — завершить" : "Ремонт не закончен";
            return string.IsNullOrEmpty(prompt) ? label : prompt;
        }

        public void OnHover(bool hovered)
        {
            isHovered = hovered;
            RefreshColor();
        }

        public void OnInteract()
        {
            if (!CanInteract()) return;
            pressTimer = 0.1f;
            HangarAudio.Instance?.PlayButton3D();

            var wb = RepairWorkbench.Instance;
            switch (action)
            {
                case WorkbenchButtonAction.RotateYawLeft: wb.RotateYaw(-stepAngle); break;
                case WorkbenchButtonAction.RotateYawRight: wb.RotateYaw(stepAngle); break;
                case WorkbenchButtonAction.RotatePitchUp: wb.RotatePitch(-stepAngle * 0.7f); break;
                case WorkbenchButtonAction.RotatePitchDown: wb.RotatePitch(stepAngle * 0.7f); break;
                case WorkbenchButtonAction.ResetRotation: wb.ResetRotation(); break;
                case WorkbenchButtonAction.FinishWork:
                    if (wb.IsWorkComplete) wb.FinishWork();
                    else HangarAudio.Instance?.PlayError();
                    break;
                case WorkbenchButtonAction.CancelWork: wb.CancelWork(); break;
            }
        }

        public void OnInteractHold()
        {
            if (!CanInteract()) return;
            var wb = RepairWorkbench.Instance;
            float delta = continuousSpeed * Time.deltaTime;

            switch (action)
            {
                case WorkbenchButtonAction.RotateYawLeft: pressTimer = 0.05f; wb.RotateYaw(-delta); break;
                case WorkbenchButtonAction.RotateYawRight: pressTimer = 0.05f; wb.RotateYaw(delta); break;
                case WorkbenchButtonAction.RotatePitchUp: pressTimer = 0.05f; wb.RotatePitch(-delta * 0.75f); break;
                case WorkbenchButtonAction.RotatePitchDown: pressTimer = 0.05f; wb.RotatePitch(delta * 0.75f); break;
            }
        }
    }
}
