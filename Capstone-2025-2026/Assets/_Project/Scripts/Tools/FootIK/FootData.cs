using UnityEngine;

public enum FootState
{
    Grounded,
    Released
}


public class FootData
{
    public FootState State;

    public Vector3 FromPosition;
    public Vector3 ToPosition;

    public Quaternion FromRotation;
    public Quaternion ToRotation;

    public float StrideStartTime;
    public float IKWeight;

    public float IKWeightSmoothdampVelocity;
    public Vector3 PositionSmoothdampVelocity;

    public Vector3 GroundNormal;

    public void Initialize(Vector3 startPos, Quaternion startRot, FootState defaultState = FootState.Grounded, float defaultWeight = 1f)
    {
        FromPosition = startPos;
        ToPosition = startPos;
        FromRotation = startRot;
        ToRotation = startRot;
        State = defaultState;
        IKWeight = defaultWeight;
        IKWeightSmoothdampVelocity = 0f;
        PositionSmoothdampVelocity = Vector3.zero;
        GroundNormal = Vector3.up;
        StrideStartTime = Time.time;
    }
}
