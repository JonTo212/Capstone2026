using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SocialPlatforms.GameCenter;

public static class DrawDebugShape
{
    public static void DrawCircle()
    {

    }

    public enum DebugShapeDrawMode
    {
        Center,
        Edge,
    }

    public static void DrawRectangle(Vector3 center, Vector3 forward, Vector3 right, float xy, Color color, DebugShapeDrawMode drawMode)
    {
        Vector3 topRight;
        Vector3 topLeft;
        Vector3 bottomRight;
        Vector3 bottomLeft;

        if(drawMode == DebugShapeDrawMode.Center)
        {
            topRight = center + (right * xy / 2) + (forward * xy / 2);
            topLeft = center + (-right * xy / 2) + (forward * xy / 2);
            bottomRight = center + (-forward * xy / 2) + (right * xy / 2);
            bottomLeft = center + (-forward * xy / 2) + (-right * xy / 2);
        }
        else
        {
            topRight = center + (right * xy / 2) + (forward * xy);
            topLeft = center + (-right * xy / 2) + (forward * xy);
            bottomRight = center + (right * xy / 2);
            bottomLeft = center +(-right * xy / 2);
        }

        Gizmos.color = color;

        Debug.DrawLine(topRight, topLeft);
        Debug.DrawLine(topRight, bottomRight);
        Debug.DrawLine(topLeft, bottomLeft);
        Debug.DrawLine(bottomLeft, bottomRight);
    }

    public static void DrawRectangle(Vector3 center, Vector3 forward, Vector3 right, float x, float z, Color color, DebugShapeDrawMode drawMode)
    {
        Vector3 topRight;
        Vector3 topLeft;
        Vector3 bottomRight;
        Vector3 bottomLeft;

        if (drawMode == DebugShapeDrawMode.Center)
        {
            topRight = center + (right * x / 2) + (forward * z / 2);
            topLeft = center + (-right * x / 2) + (forward * z / 2);
            bottomRight = center + (-forward * z / 2) + (right * x / 2);
            bottomLeft = center + (-forward * z / 2) + (-right * x / 2);
        }
        else
        {
            topRight = center + (right * x / 2) + (forward * z);
            topLeft = center + (-right * x / 2) + (forward * z);
            bottomRight = center + (right * x / 2);
            bottomLeft = center + (-right * x / 2);
        }

        Gizmos.color = color;

        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(topLeft, bottomLeft);
        Gizmos.DrawLine(bottomLeft, bottomRight);
    }

    public static void DrawCube(Vector3 center, Vector3 forward, Vector3 right, float size, Color color)
    {
        Vector3 localUp = Vector3.Cross(forward, right);

        Vector3 topFace = center + (localUp * size/2);
        Vector3 bottomFace = center + (-localUp * size/2);
        Vector3 frontFace = center + (forward * size/2);
        Vector3 backFace = center + (-forward * size/2);
        Vector3 rightFace = center + (right * size/2);
        Vector3 leftFace = center + (-right * size/2);

        DrawRectangle(topFace, forward, right, size, color, DebugShapeDrawMode.Center);
        DrawRectangle(bottomFace, forward, right, size, color, DebugShapeDrawMode.Center);
        DrawRectangle(rightFace, forward, localUp, size, color, DebugShapeDrawMode.Center);
        DrawRectangle(leftFace, forward, localUp, size, color, DebugShapeDrawMode.Center);
        DrawRectangle(frontFace, localUp, right, size, color, DebugShapeDrawMode.Center);
        DrawRectangle(backFace, localUp, right, size, color, DebugShapeDrawMode.Center);
    }

    public static void DrawCube(Vector3 center, Vector3 forward, Vector3 right, Vector3 size, Color color, DebugShapeDrawMode drawMode)
    {
        Vector3 localUp = Vector3.Cross(forward, right);

        if(drawMode == DebugShapeDrawMode.Center)
        {
            Vector3 topFace = center + (localUp * size.y / 2);
            Vector3 bottomFace = center + (-localUp * size.y / 2);
            Vector3 frontFace = center + (forward * size.z / 2);
            Vector3 backFace = center + (-forward * size.z / 2);
            Vector3 rightFace = center + (right * size.x / 2);
            Vector3 leftFace = center + (-right * size.x / 2);

            DrawRectangle(topFace, forward, right, size.x, size.z, color, DebugShapeDrawMode.Center);
            DrawRectangle(bottomFace, forward, right, size.x, size.z, color, DebugShapeDrawMode.Center);
            DrawRectangle(rightFace, forward, localUp, size.y, size.z, color, DebugShapeDrawMode.Center);
            DrawRectangle(leftFace, forward, localUp, size.y, size.z, color, DebugShapeDrawMode.Center);
            DrawRectangle(frontFace, localUp, right, size.x, size.y, color, DebugShapeDrawMode.Center);
            DrawRectangle(backFace, localUp, right, size.x, size.y, color, DebugShapeDrawMode.Center);
        }
        else
        {
            Vector3 topFace = center + (localUp * size.y / 2);
            Vector3 bottomFace = center + (-localUp * size.y / 2);
            Vector3 frontFace = center;
            Vector3 backFace = center;
            Vector3 rightFace = center + (right * size.x / 2);
            Vector3 leftFace = center + (-right * size.x / 2);

            DrawRectangle(topFace, forward, right, size.x, size.z, color, DebugShapeDrawMode.Edge);
            DrawRectangle(bottomFace, forward, right, size.x, size.z, color, DebugShapeDrawMode.Edge);
            DrawRectangle(rightFace, forward, localUp, size.y, size.z, color, DebugShapeDrawMode.Edge);
            DrawRectangle(leftFace, forward, localUp, size.y, size.z, color, DebugShapeDrawMode.Edge);
            DrawRectangle(frontFace, localUp, right, size.x, size.y, color, DebugShapeDrawMode.Center);
            DrawRectangle(backFace, localUp, right, size.x, size.y, color, DebugShapeDrawMode.Center);
        }
    }
}
