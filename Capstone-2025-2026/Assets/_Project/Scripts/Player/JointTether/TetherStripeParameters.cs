using UnityEditor;
using UnityEngine;

public class TetherStripeParameters : MonoBehaviour
{
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
