using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Simple on-screen virtual joystick (drag-based). Background + handle
/// are child RectTransforms under this one; Direction is a normalized-ish
/// Vector2 (magnitude 0..1, analog — partial tilt moves slower) read by
/// PlayerController every frame. Involves no world raycasts, so unlike a
/// tap-to-move scheme it can never conflict with Canvas UI buttons.
/// </summary>
public class Joystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public RectTransform background;
    public RectTransform handle;
    public float handleRange = 60f;

    public Vector2 Direction { get; private set; }

    public void OnPointerDown(PointerEventData eventData) => OnDrag(eventData);

    public void OnDrag(PointerEventData eventData)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                background, eventData.position, eventData.pressEventCamera, out var localPoint))
            return;

        localPoint = Vector2.ClampMagnitude(localPoint, handleRange);
        if (handle != null) handle.anchoredPosition = localPoint;
        Direction = localPoint / handleRange;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (handle != null) handle.anchoredPosition = Vector2.zero;
        Direction = Vector2.zero;
    }
}
