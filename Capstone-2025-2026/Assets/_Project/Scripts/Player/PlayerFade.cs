using UnityEngine;

public class PlayerFade : MonoBehaviour
{
    [SerializeField] private ZeldaCameraController cameraController;
    [SerializeField] private PlayerFadeData player;

    [Header("Fade Settings")]
    [SerializeField] private float fadeStartDistance = 3f;
    [SerializeField] private float fadeEndDistance = 1f;
    [SerializeField] private float minAlpha = 0f;

    private void Update()
    {
        float distance = Vector3.Distance(Camera.main.transform.position, transform.position);
        float alpha = Mathf.InverseLerp(fadeEndDistance, fadeStartDistance, distance);

        foreach (Material material in player.Materials)
        {
            if (material.HasProperty("_Opacity"))
            {
                material.SetFloat("_Opacity", Mathf.Lerp(minAlpha, 1f, alpha));
            }
        }
    }
}