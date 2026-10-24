using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VelocityRivals
{
    /// <summary>Keep keyboard/controller focus visible even on long menu pages.</summary>
    public sealed class MenuFocusReveal : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public Graphic Accent;
        bool selected, hovered;
        public void OnSelect(BaseEventData data)
        {
            selected = true; Refresh(); Reveal();
        }
        public void OnDeselect(BaseEventData data) { selected = false; Refresh(); }
        public void OnPointerEnter(PointerEventData data) { hovered = true; Refresh(); }
        public void OnPointerExit(PointerEventData data) { hovered = false; Refresh(); }
        void Refresh() { if (Accent) Accent.enabled = selected || hovered; }
        public void Reveal()
        {
            var scroll = GetComponentInParent<ScrollRect>();
            if (!scroll || !scroll.viewport || !scroll.content) return;
            Canvas.ForceUpdateCanvases();
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, transform);
            Rect view = scroll.viewport.rect;
            float offset = bounds.max.y > view.yMax ? view.yMax - bounds.max.y : bounds.min.y < view.yMin ? view.yMin - bounds.min.y : 0;
            scroll.StopMovement();
            scroll.content.anchoredPosition += Vector2.up * offset;
        }
    }
}
