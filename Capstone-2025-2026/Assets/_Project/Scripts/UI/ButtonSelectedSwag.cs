using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonSelectedSwag : MonoBehaviour
{
    private Vector3 currentScale;
    public Vector3 desiredScale;
    public float rotationStrength = 20f;
    private bool selected = false;

    void Awake()
    {
        currentScale = transform.localScale;
    }
    void Update()
    {
        if(selected)transform.localRotation = Quaternion.Euler(0,0, rotationStrength * Mathf.Sin(Time.time));
    }

    public void ButtonSelect()
    {
        selected = true;
        transform.localScale = desiredScale;
    }

    public void ButtonDeselect()
    {
        selected = false;
        transform.localScale = currentScale;
        transform.localRotation = Quaternion.Euler(Vector3.zero);
    }

}
