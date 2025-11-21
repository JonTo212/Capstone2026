using System;
using UnityEngine;

public class NPCBase : Prop
{
    protected Transform playerRef;
    public event Action OnAnimComplete;

    protected virtual void Awake()
    {
        Init();
    }

    public virtual void PullIntoBag(Transform player)
    {
        playerRef = player;
        playerRef.GetComponent<PlayerInventory>().currentNPC = this;
    }

    public virtual void UseAbility()
    {

    }

    public virtual void Respawn()
    {

    }

    public virtual void RunAnim()
    {

    }

    public virtual void RunAnimEvent()
    {
        OnAnimComplete?.Invoke();
    }
}
