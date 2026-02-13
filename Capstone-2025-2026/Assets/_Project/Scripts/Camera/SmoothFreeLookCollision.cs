using Unity.Cinemachine;
using UnityEngine;

public class OrbitalCollisionHandler : MonoBehaviour
{
    [Header("Collision Settings")]
    [SerializeField] private LayerMask collisionLayers;
    [SerializeField] private float spherecastRadius = 0.3f;
    [SerializeField] private float minDistance = 0.5f;
    [SerializeField] private float collisionPadding = 0.1f;

    [Header("Smoothing")]
    [SerializeField] private float fadeInTime = 0.15f;  // Time to pull back when collision detected
    [SerializeField] private float fadeOutTime = 0.3f;  // Time to return to normal when collision clears

    private CinemachineBrain brain;
    private CinemachineCamera vcam;
    private Vector3 correctionOffset;
    private Vector3 correctionVelocity;

    private void Awake()
    {
        vcam = GetComponent<CinemachineCamera>();
        brain = FindFirstObjectByType<CinemachineBrain>();
    }

    private void OnEnable()
    {
        if (brain != null)
            CinemachineCore.CameraUpdatedEvent.AddListener(OnCameraUpdate);
    }

    private void OnDisable()
    {
        if (brain != null)
            CinemachineCore.CameraUpdatedEvent.RemoveListener(OnCameraUpdate);

        correctionOffset = Vector3.zero;
        correctionVelocity = Vector3.zero;
    }

    private void OnCameraUpdate(CinemachineBrain eventBrain)
    {
        if (eventBrain != brain || vcam == null || vcam.Follow == null)
            return;

        //only runs if virtual cam is active
        if (brain.ActiveVirtualCamera as CinemachineVirtualCameraBase != vcam as CinemachineVirtualCameraBase)
            return;

        Transform camTransform = brain.transform;
        Vector3 targetPos = vcam.Follow.position;
        Vector3 currentCamPos = camTransform.position;
        Vector3 direction = currentCamPos - targetPos;
        float currentDist = direction.magnitude;

        if (currentDist < 0.01f)
            return;

        Vector3 dirNorm = direction / currentDist;
        Vector3 targetCorrection = Vector3.zero;
        bool isColliding = false;

        //calculate distance back from the colliding object
        if (Physics.SphereCast(targetPos, spherecastRadius, dirNorm, out RaycastHit hit, currentDist, collisionLayers, QueryTriggerInteraction.Ignore))
        {
            float safeDistance = Mathf.Max(hit.distance - spherecastRadius - collisionPadding, minDistance);

            float pullback = currentDist - safeDistance;
            targetCorrection = -dirNorm * pullback;
            isColliding = true;
        }

        //smooth based on colliding or exiting collision
        float smoothTime = isColliding ? fadeInTime : fadeOutTime;
        correctionOffset = Vector3.SmoothDamp(correctionOffset, targetCorrection, ref correctionVelocity, smoothTime);
        camTransform.position += correctionOffset;
    }
}