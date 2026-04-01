using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Collider))]
public class PostProcessCrossfadeTrigger : MonoBehaviour
{
    [Header("Volumes")]
    [Tooltip("The local Volume whose weight this trigger controls")]
    public Volume localVolume;

    [Header("Crossfade Settings")]
    [Tooltip("Duration of the crossfade in seconds")]
    public float crossfadeDuration = 2.0f;

    [Tooltip("Tag of the object that triggers the crossfade (usually the player)")]
    public string triggerTag = "Player";

    private Coroutine _currentCrossfade;

    private void Start()
    {
        // Local volume starts fully active
        if (localVolume) localVolume.weight = 1f;
    }

    //entering local -> fade local volume in
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(triggerTag)) return;

        StartCrossfade(targetWeight: 1f);
    }

    //exiting the local zone -> fade local volume out
    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(triggerTag)) return;

        StartCrossfade(targetWeight: 0f);
    }

    private void StartCrossfade(float targetWeight)
    {
        if (_currentCrossfade != null)
            StopCoroutine(_currentCrossfade);

        _currentCrossfade = StartCoroutine(Crossfade(targetWeight));
    }

    private IEnumerator Crossfade(float targetWeight)
    {
        if (!localVolume) yield break;

        float elapsed = 0f;
        float startWeight = localVolume.weight;

        while (elapsed < crossfadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / crossfadeDuration);

            localVolume.weight = Mathf.SmoothStep(startWeight, targetWeight, t);
            yield return null;
        }

        localVolume.weight = targetWeight;
        _currentCrossfade = null;
    }
}