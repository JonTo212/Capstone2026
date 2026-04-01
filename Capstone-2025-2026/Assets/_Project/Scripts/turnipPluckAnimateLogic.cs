using UnityEngine;

public class turnipPluckAnimateLogic : MonoBehaviour
{
    [SerializeField] private BreakablePluckupProp breakablePluckupPropScript;
    [SerializeField] private Animator animator;


    [SerializeField] private bool isPlayingAnimation = false;

    [SerializeField] private bool animationOverride = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        breakablePluckupPropScript = GetComponent<BreakablePluckupProp>();
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
       if (breakablePluckupPropScript.hasBeenPlucked || animationOverride )
        {
            if (isPlayingAnimation != true)
            {
                animator.Play("PluckoutTurnipBreak");
                isPlayingAnimation = true;
            }
        }
    }
}
