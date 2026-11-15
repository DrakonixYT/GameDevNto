using UnityEngine;
using UnityEngine.Events;

namespace HangarGame.Interaction
{
    [RequireComponent(typeof(Collider))]
    public class Clickable3DObject : MonoBehaviour, IInteractable
    {
        [Header("General")]
        [SerializeField] private string objectName = "3D Кнопка";
        [SerializeField] private string actionDescription = "Нажмите для активации";

        [Header("Event Callback")]
        [Tooltip("Сюда можно подключить ЛЮБУЮ функцию любого объекта из 100500 вариантов")]
        public UnityEvent onInteract;

        [Header("Visual Feedback")]
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color hoverColor = Color.yellow;
        [SerializeField] private Color pressColor = Color.green;

        private Material propertyBlockMaterial;
        private Vector3 originalLocalPos;
        private bool isPressed = false;

        private void Awake()
        {
            if (targetRenderer == null)
            {
                targetRenderer = GetComponent<Renderer>();
            }

            if (targetRenderer != null)
            {
                // Создаем инстанс материала для подсветки
                targetRenderer.material.color = normalColor;
            }

            originalLocalPos = transform.localPosition;
        }

        public void OnHoverEnter()
        {
            if (targetRenderer != null && !isPressed)
            {
                targetRenderer.material.color = hoverColor;
            }
        }

        public void OnHoverExit()
        {
            if (targetRenderer != null && !isPressed)
            {
                targetRenderer.material.color = normalColor;
            }
        }

        public void OnInteract()
        {
            // Визуальный отклик — кратковременное вдавливание кнопки
            StopAllCoroutines();
            StartCoroutine(PressFeedbackRoutine());

            // Вызов подключенной функции
            onInteract?.Invoke();
            Debug.Log($"<color=cyan>[Clickable3DObject]</color> Активирован 3D объект: {objectName}");
        }

        private System.Collections.IEnumerator PressFeedbackRoutine()
        {
            isPressed = true;
            if (targetRenderer != null)
            {
                targetRenderer.material.color = pressColor;
            }

            // Вдавливание внутрь на 5 см
            transform.localPosition = originalLocalPos - transform.up * 0.05f;

            yield return new WaitForSeconds(0.12f);

            transform.localPosition = originalLocalPos;
            isPressed = false;

            if (targetRenderer != null)
            {
                targetRenderer.material.color = normalColor;
            }
        }

        public string GetInteractionPrompt()
        {
            return $"{objectName} — {actionDescription}";
        }
    }
}
