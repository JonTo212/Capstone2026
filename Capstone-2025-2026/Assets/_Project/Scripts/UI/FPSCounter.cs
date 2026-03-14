using TMPro;
using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    TextMeshProUGUI fpsCounterText;

    private float pollingTime = 0.2f;
    private float time;
    private int frameCount;

    // Update is called once per frame

    private void Start()
    {
        fpsCounterText = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        time += Time.deltaTime;

        frameCount++;

        if (time >= pollingTime)
        {
            int frameRate = Mathf.RoundToInt(frameCount/time);
            fpsCounterText.text = frameRate.ToString() + "FPS";
            
            time -= pollingTime;
            frameCount = 0;
        }
    }
}
