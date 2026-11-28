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
        public RepairWorkbench workbench;
        public GameManager game;
        public WorkbenchButtonAction action;
        public string label = "";
        public string prompt = "";
        public Color idleColor = new Color(0.16f, 0.25f, 0.36f);
        public Color hoverColor = new Color(0.32f, 0.72f, 0.88f);
        public Color readyColor = new Color(0.20f, 0.58f, 0.36f);

        private Renderer capRenderer;
        private Vector3 restLocalPos;
        private float pressTimer;
        private bool isHovered;

        private void Awake()
        {
            capRenderer = GetComponent<Renderer>();
            restLocalPos = transform.localPosition;
            game ??= GameManager.Instance ?? FindAnyObjectByType<GameManager>();
            workbench ??= game?.workbench ?? GetComponentInParent<RepairWorkbench>();
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

        public void Init(WorkbenchButtonAction btnAction, string btnLabel, string btnPrompt, Color baseColor, RepairWorkbench wb)
        {
            action = btnAction;
            label = btnLabel;
            prompt = btnPrompt;
            idleColor = baseColor;
            workbench = wb;
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
            if (action == WorkbenchButtonAction.FinishWork && workbench != null && workbench.IsWorkComplete)
                target = readyColor;
            if (isHovered)
                target = Color.Lerp(target, hoverColor, 0.55f);

            ModelUtils.SetColor(capRenderer, target);
        }

        public bool CanInteract() => gameObject.activeInHierarchy && workbench != null && workbench.HasItem;

        public string GetPrompt()
        {
            if (action == WorkbenchButtonAction.FinishWork && workbench != null)
                return workbench.IsWorkComplete ? "Готово — завершить" : "Ремонт не закончен";
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
            game?.PlaySfx(Sfx.Button3D);

            const float step = 22f;
            switch (action)
            {
                case WorkbenchButtonAction.RotateYawLeft: workbench.Rotate(-step, 0f); break;
                case WorkbenchButtonAction.RotateYawRight: workbench.Rotate(step, 0f); break;
                case WorkbenchButtonAction.RotatePitchUp: workbench.Rotate(0f, -step * 0.7f); break;
                case WorkbenchButtonAction.RotatePitchDown: workbench.Rotate(0f, step * 0.7f); break;
                case WorkbenchButtonAction.ResetRotation: workbench.ResetRotation(); break;
                case WorkbenchButtonAction.FinishWork:
                    if (workbench.IsWorkComplete) workbench.FinishWork();
                    else game?.PlaySfx(Sfx.Error);
                    break;
                case WorkbenchButtonAction.CancelWork: workbench.CancelWork(); break;
            }
        }

        public void OnInteractHold()
        {
            if (!CanInteract()) return;
            float delta = 95f * Time.deltaTime;

            switch (action)
            {
                case WorkbenchButtonAction.RotateYawLeft: pressTimer = 0.05f; workbench.Rotate(-delta, 0f); break;
                case WorkbenchButtonAction.RotateYawRight: pressTimer = 0.05f; workbench.Rotate(delta, 0f); break;
                case WorkbenchButtonAction.RotatePitchUp: pressTimer = 0.05f; workbench.Rotate(0f, -delta * 0.75f); break;
                case WorkbenchButtonAction.RotatePitchDown: pressTimer = 0.05f; workbench.Rotate(0f, delta * 0.75f); break;
            }
        }
    }
}
