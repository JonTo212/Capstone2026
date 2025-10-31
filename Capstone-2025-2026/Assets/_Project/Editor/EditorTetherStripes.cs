using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TetherStripeParameters))]
public class EditorTetherStripes : Editor
{
    public override void OnInspectorGUI()
    {
        TetherStripeParameters selected = target as TetherStripeParameters;
    }
}
