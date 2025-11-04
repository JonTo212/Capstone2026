using System.Collections.Generic;
using UnityEngine;

public class BoxGrabPointGenerator : MonoBehaviour, IGrabPointGenerator
{
    public List<Transform> GeneratePoints(Collider col, int rows, int columns)
    {
        List<Transform> points = new List<Transform>();
        BoxCollider box = col as BoxCollider;

        //get object center and scale (bounds don't have scale)
        Vector3 center = box.transform.TransformPoint(box.center);
        Vector3 scaledSize = Vector3.Scale(box.size, box.transform.lossyScale);

        //calculate half-size in each direction
        Vector3 halfRight = box.transform.right * scaledSize.x * 0.5f;
        Vector3 halfUp = box.transform.up * scaledSize.y * 0.5f;
        Vector3 halfForward = box.transform.forward * scaledSize.z * 0.5f;

        //generate points: right, left, top, bottom, front, back
        points.AddRange(GenerateFacePoints(center + halfRight, box.transform.forward, box.transform.up, scaledSize.y, scaledSize.z, rows, columns, box.transform.right));
        points.AddRange(GenerateFacePoints(center - halfRight, -box.transform.forward, box.transform.up, scaledSize.y, scaledSize.z, rows, columns, -box.transform.right));
        points.AddRange(GenerateFacePoints(center + halfUp, box.transform.right, box.transform.forward, scaledSize.z, scaledSize.x, rows, columns, box.transform.up));
        points.AddRange(GenerateFacePoints(center - halfUp, -box.transform.right, box.transform.forward, scaledSize.z, scaledSize.x, rows, columns, -box.transform.up));
        points.AddRange(GenerateFacePoints(center + halfForward, box.transform.right, box.transform.up, scaledSize.y, scaledSize.x, rows, columns, box.transform.forward));
        points.AddRange(GenerateFacePoints(center - halfForward, -box.transform.right, box.transform.up, scaledSize.y, scaledSize.x, rows, columns, -box.transform.forward));

        return points;
    }

    private List<Transform> GenerateFacePoints(Vector3 faceCenter, Vector3 horizontalDir, Vector3 verticalDir, float faceHeight, float faceWidth, int rows, int columns, Vector3 faceNormal)
    {
        List<Transform> points = new List<Transform>();

        float cellHeight = faceHeight / rows;
        float cellWidth = faceWidth / columns;

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                //calculate offsets to center the points in each cell
                float verticalOffset = (i + 0.5f) * cellHeight - faceHeight * 0.5f;
                float horizontalOffset = (j + 0.5f) * cellWidth - faceWidth * 0.5f;
                Vector3 point = faceCenter + verticalDir * verticalOffset + horizontalDir * horizontalOffset;

                //make new grab point object, orient it to face normal, parent it to the prop
                GameObject newPoint = new GameObject("GrabPoint");
                newPoint.transform.position = point;
                newPoint.transform.rotation = Quaternion.LookRotation(faceNormal);
                newPoint.transform.SetParent(transform);
                points.Add(newPoint.transform);
            }
        }

        return points;
    }
}
