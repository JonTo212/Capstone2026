using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class TetherStripeParameters : MonoBehaviour
{
    [SerializeField] float hiddenStripeScrollSpeed = -2f;
    [SerializeField] float hiddenStripeThickness = 0f;
    [SerializeField] float hiddenStripeLineAmount = 50f;
    [ColorUsage(true, true)]
    [SerializeField] Color inactiveEmmssive = Color.yellow;
    [ColorUsage(true, true)]
    [SerializeField] Color activeEmmissive = Color.green;
    [SerializeField] MeshRenderer[] attachmentPoints;
    private void Start()
    {
        SetStripeCount(hiddenStripeLineAmount);
        SetStripeScrollSpeed(hiddenStripeScrollSpeed);
        SetStripeThickness(hiddenStripeThickness);
    }

    private void OnValidate()
    {
        SetStripeCount(hiddenStripeLineAmount);
        SetStripeScrollSpeed(hiddenStripeScrollSpeed);
        SetStripeThickness(hiddenStripeThickness);
    }

    public void SetStripeCount(float lineCount)
    {
        Shader.SetGlobalFloat("_HiddenStripeLineCount", lineCount);
    }

    public void SetStripeThickness(float lineThickness)
    {
        Shader.SetGlobalFloat("_HiddenStripeThickness", lineThickness);
    }

    public void SetStripeScrollSpeed(float lineScrollSpeed)
    {
        Shader.SetGlobalFloat("_HiddenStripeScrollSpeed", lineScrollSpeed);
    }
}
