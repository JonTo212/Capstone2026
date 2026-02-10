using NodeCanvas.Tasks.Actions;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Zelda: BotW/TotK style third-person camera system
/// Features: Free-look with mouse, manual zoom on input, smooth ghost transform tracking
/// </summary>
public class ZeldaCameraController : MonoBehaviour
{
    [SerializeField] private PlayerActions input;

    [Header("Target Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 targetOffset = new Vector3(0, 1.5f, 0);

    [Header("Camera Distance")]
    [SerializeField] private float defaultDistance = 5f;
    [SerializeField] private float minDistance = 2f;
    [SerializeField] private float maxDistance = 10f;
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float zoomSmoothTime = 0.1f;

    [Header("Rotation Settings")]
    [SerializeField] private float mouseSensitivity = 3f;
    [SerializeField] private float minVerticalAngle = -30f;
    [SerializeField] private float maxVerticalAngle = 70f;
    [SerializeField] private float rotationSmoothTime = 0.05f;

    [Header("Position Smoothing")]
    [SerializeField] private float positionSmoothTime = 0.1f;

    [Header("Collision")]
    [SerializeField] private bool handleCollision = true;
    [SerializeField] private float collisionBuffer = 0.2f;
    [SerializeField] private LayerMask collisionLayers = ~0;
    [SerializeField] private float collisionZoomInTime = 0.1f;
    [SerializeField] private float collisionZoomOutTime = 0.8f;

    //ghost transform -> what the camera tracks
    private Vector3 ghostPosition;
    private Quaternion ghostRotation;

    //rotation
    private float currentYaw = 0f;
    private float currentPitch = 0f;
    private float targetYaw = 0f;
    private float targetPitch = 0f;

    //distance
    private float currentDistance;
    private float targetDistance;
    private float zoomVelocity;

    //collision
    private float collisionDistance;
    private float collisionVelocity;
    private float collisionSmoothTime;

    //smoothing
    private Vector3 positionVelocity;
    private Vector3 rotationVelocity;

    //input
    private bool hasInput = false;

    private void Start()
    {
        currentDistance = defaultDistance;
        targetDistance = defaultDistance;
        collisionDistance = defaultDistance;

        Vector3 currentRotation = transform.eulerAngles;
        currentYaw = currentRotation.y;
        currentPitch = currentRotation.x;

        if (currentPitch > 180f)
            currentPitch -= 360f;

        targetYaw = currentYaw;
        targetPitch = currentPitch;

        UpdateGhostTransform();
        transform.position = ghostPosition;
        transform.rotation = ghostRotation;
    }

    private void Update()
    {
        HandleInput();
    }

    private void LateUpdate()
    {
        if (target == null) return;

        UpdateGhostTransform();
        SmoothCameraToGhost();
    }

    private void HandleInput()
    { 
        float mouseX = input.LookInput.x * mouseSensitivity;
        float mouseY = input.LookInput.y * mouseSensitivity;

        hasInput = Mathf.Abs(mouseX) > 0.001f || Mathf.Abs(mouseY) > 0.001f || Mathf.Abs(input.MoveInput.sqrMagnitude) > 0.001f;
        print(hasInput);

        targetYaw += mouseX;
        targetPitch -= mouseY;
        targetPitch = Mathf.Clamp(targetPitch, minVerticalAngle, maxVerticalAngle);
    }

    private bool colliding;//temp
    private void UpdateGhostTransform()
    {
        //rotation
        currentYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref rotationVelocity.y, rotationSmoothTime);
        currentPitch = Mathf.SmoothDampAngle(currentPitch, targetPitch, ref rotationVelocity.x, rotationSmoothTime);

        //desired rotation
        ghostRotation = Quaternion.Euler(currentPitch, currentYaw, 0f);

        //desired position before collision, including offset
        Vector3 targetPosition = target.position + targetOffset;
        Vector3 desiredPosition = targetPosition - (ghostRotation * Vector3.forward * currentDistance);

        if (handleCollision)
        {
            Vector3 direction = desiredPosition - targetPosition;
            float distance = direction.magnitude;


            if (Physics.Raycast(targetPosition, direction.normalized, out RaycastHit hit, distance, collisionLayers))
            {
                //slightly in front of collision point
                float hitDistance = hit.distance - collisionBuffer;
                colliding = true;
                collisionSmoothTime = collisionZoomInTime;
                collisionDistance = Mathf.SmoothDamp(collisionDistance, hitDistance, ref collisionVelocity, collisionSmoothTime);
            }
            else
            {
                if(colliding)
                {
                    collisionSmoothTime = collisionZoomOutTime;
                    colliding = false;
                }

                //ease with input only
                if (hasInput)
                {
                    collisionDistance = Mathf.SmoothDamp(collisionDistance, distance, ref collisionVelocity, collisionSmoothTime);
                }
                else
                {
                    collisionVelocity = 0f;
                }
            }

            ghostPosition = targetPosition + direction.normalized * collisionDistance;
        }
        else
        {
            ghostPosition = desiredPosition;
            collisionDistance = desiredPosition.magnitude;
        }
    }

    private void SmoothCameraToGhost()
    {
        transform.position = Vector3.SmoothDamp(transform.position, ghostPosition, ref positionVelocity, positionSmoothTime);
        transform.rotation = ghostRotation;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void SetRotation(float yaw, float pitch)
    {
        targetYaw = yaw;
        currentYaw = yaw;
        targetPitch = Mathf.Clamp(pitch, minVerticalAngle, maxVerticalAngle);
        currentPitch = targetPitch;
    }

    public void SetDistance(float distance)
    {
        targetDistance = Mathf.Clamp(distance, minDistance, maxDistance);
        currentDistance = targetDistance;
    }

    public void AddYaw(float degrees)
    {
        targetYaw += degrees;
    }

    public void AddPitch(float degrees)
    {
        targetPitch += degrees;
        targetPitch = Mathf.Clamp(targetPitch, minVerticalAngle, maxVerticalAngle);
    }

    public Vector3 GetGhostPosition() => ghostPosition;
    public Quaternion GetGhostRotation() => ghostRotation;
    public float GetCurrentDistance() => currentDistance;
    public float GetCurrentYaw() => currentYaw;
    public float GetCurrentPitch() => currentPitch;
}