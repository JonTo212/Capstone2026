using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameRestart : MonoBehaviour
{
    private InputAction SpecialAction;
    public bool Special => SpecialAction.WasReleasedThisFrame();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    private void Awake()
    {
        var map = InputSystem.actions;
        SpecialAction = map.FindAction("Special");
    }

    private void OnEnable()
    {
        SpecialAction.Enable();
    }

    private void OnDisable()
    {
        SpecialAction.Disable();
    }


    // Update is called once per frame
    void Update()
    {

        //restart gmae when R is pressed
        if (Special)
        {
            print("restart");

            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
