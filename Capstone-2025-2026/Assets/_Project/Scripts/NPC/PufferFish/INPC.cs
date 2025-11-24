using System;
using UnityEngine;

[Serializable]
public enum NPCType
{
    Mama,
    Baby
}

public enum NPCState
{
    Idle,
    UsingAbility,
    Attached,
    Disturbed
}

public interface INPC
{
    void AttachObject(Transform attachedObj);
    void UseAbility();
    void RunCaptureAnim();
    void SwitchNPCState(NPCState newState);
    event Action OnAnimComplete;
    NPCState CurrentNPCState { get; }
}
