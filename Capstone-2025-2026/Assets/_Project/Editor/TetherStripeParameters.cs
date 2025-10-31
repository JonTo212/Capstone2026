using UnityEditor;
using UnityEngine;

public class TetherStripeParameters : MonoBehaviour
{
    public void SetLineCount(float lineCount)
    {
        Shader.SetGlobalFloat("_HiddenLineCount", lineCount);
    }

    public void SetLineThickness(float lineThickness)
    {
        Shader.SetGlobalFloat("_HiddenLineThickness", lineThickness);
    }
}
