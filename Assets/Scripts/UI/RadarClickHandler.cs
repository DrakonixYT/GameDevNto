using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HangarGame.UI
{
    public class RadarClickHandler : MonoBehaviour, IPointerClickHandler, IPointerDownHandler
    {
        public Action<Vector2> onRadarClick;

        private RectTransform rect;
        private Canvas rootCanvas;

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            rootCanvas = GetComponentInParent<Canvas>();
        }

        public void OnPointerDown(PointerEventData eventData) { }

        public void OnPointerClick(PointerEventData eventData)
        {
            var cam = eventData.pressEventCamera;
            if (cam == null && rootCanvas != null) cam = rootCanvas.worldCamera;
            if (cam == null) cam = UnityEngine.Camera.main;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, cam, out var pos))
                onRadarClick?.Invoke(pos);
        }
    }
}
