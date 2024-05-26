using UnityEngine;
using UnityEngine.EventSystems;

public abstract class UIClickable : MonoBehaviour, IPointerDownHandler {

	protected abstract void OnPressed();

	public void OnPointerDown(PointerEventData eventData) => OnPressed();
}
