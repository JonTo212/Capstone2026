using UnityEngine;

public class GianDandilionAnimation : MonoBehaviour
{

    public float minSpeed = 0.8f;
    public float maxSpeed = 1.2f;

    void Start()
    {
        Animator anim = GetComponent<Animator>();
        anim.speed = Random.Range(minSpeed, maxSpeed);
    }

}
