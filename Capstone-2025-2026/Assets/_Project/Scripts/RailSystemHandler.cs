using System.Collections.Generic;
using UnityEngine;

public class RailSystemHandler : MonoBehaviour
{
    private List<Rail> rails = new List<Rail>();
    private Prop movableObject;
    private Vector3 movableObjectLastPos;

    private void Awake()
    {
        foreach(var child in transform)
        {
            if (TryGetComponent(out Rail rail))
                rails.Add(rail);
        }
    }

    public void ConstrainToRails()
    {
        


        movableObjectLastPos = movableObject.transform.position;
    }
}
