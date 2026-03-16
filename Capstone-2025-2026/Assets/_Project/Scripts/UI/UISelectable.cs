using FMODUnity;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class UISelectable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler
{
    private static UISelectable _hoveredElement;
    private static UISelectable _lastHovered;

    private StudioEventEmitter _emitter;

    private void Awake()
    {
        _emitter = GetComponentInParent<StudioEventEmitter>();
    }

    private void Update()
    {
        //if mouse moves while hovering, snap selection back to this element
        if (_hoveredElement == this && Mouse.current != null && Mouse.current.delta.ReadValue() != Vector2.zero)
            if (EventSystem.current.currentSelectedGameObject != gameObject)
                EventSystem.current.SetSelectedGameObject(gameObject);

        //if nothing is selected and no element is hovered, restore from last hovered and clear
        if (_hoveredElement == null && _lastHovered == this)
        {
            bool controllerInput = Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame;
            bool keyboardInput = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;

            if (controllerInput || keyboardInput)
            {
                if (EventSystem.current.currentSelectedGameObject != gameObject)
                    EventSystem.current.SetSelectedGameObject(gameObject);
                _lastHovered = null;
            }
        }
    }

    //on mouse enter, select
    public void OnPointerEnter(PointerEventData eventData)
    {
        _hoveredElement = this;
        _lastHovered = this;
        EventSystem.current.SetSelectedGameObject(gameObject);
        PlayerActions.Instance.RumbleFor(0.1f, 0.2f, 0.1f);
    }

    //on mouse exit, deselect
    public void OnPointerExit(PointerEventData eventData)
    {
        if (_hoveredElement == this)
            _hoveredElement = null;
        EventSystem.current.SetSelectedGameObject(null);
    }

    public void OnSelect(BaseEventData eventData)
    {
        PlayerActions.Instance.RumbleFor(0.1f, 0.2f, 0.1f);
    
        if (_hoveredElement != this)
            PlayHoverSound();
    }

    private void PlayHoverSound()
    {
        if (_emitter != null)
            _emitter.Play();
    }
}