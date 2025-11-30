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
    Activated,
    Deactivated,
    PlayerInteracting,
    InBag
}

public interface INPC
{
    void SetPlayerRef(Transform player);
    void UseAbility();
    void StopAbility();
    void OnCaptureStart();
    void OnCaptureComplete();
    void OnReleaseStart();
    void OnReleaseComplete();
    void SwitchNPCState(NPCState newState);
    NPCState CurrentNPCState { get; }
}
