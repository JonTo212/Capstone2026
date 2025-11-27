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
    void AttachToPlayer(Transform player);
    void UseAbility();
    void RunCaptureAnim();
    void SwitchNPCState(NPCState newState);
    void OnEnteredPlayerBag();
    void RemoveFromPlayerBag();
    NPCState CurrentNPCState { get; }
}
