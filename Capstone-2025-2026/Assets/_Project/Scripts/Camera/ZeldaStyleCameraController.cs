using UnityEngine;

public class ZeldaCameraController : MonoBehaviour
{
    [SerializeField] private PlayerActions input;

    [Header("Target Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 targetOffset = new Vector3(0, 1.5f, 0);
    [SerializeField] private Vector2 screenOffset;

    [Header("Camera Distance")]
    [SerializeField] private float defaultDistance = 5f;
    [SerializeField] private float minDistance = 2f;
    private float currentMaxDistance;

    [Header("Rotation Settings")]
    [SerializeField] private float mouseXSensitivity = 1f;
    [SerializeField] private float mouseYSensitivity = 1f;
    [SerializeField] private float controllerXSensitivityMultiplier = 5f;
    [SerializeField] private float controllerYSensitivityMultiplier = 5f;
    [SerializeField] private float minVerticalAngle = -30f;
    [SerializeField] private float maxVerticalAngle = 70f;
    [SerializeField] private float rotationSmoothTime = 0f; //ANY NON-ZERO VALUE BREAKS ROTATION IF YOU SWEEP WITH YOUR MOUSE VERY QUICKLY

    [Header("Position Smoothing")]
    [SerializeField] private Vector3 positionDamping;

    [Header("Collision")]
    [SerializeField] private bool handleCollision = true;
    [SerializeField] private float collisionBuffer = 0.2f;
    [SerializeField] private float cameraRadius = 0.3f;
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

    //collision
    private float collisionDistance;
    private float collisionVelocity;
    private float collisionSmoothTime;
    private float previousTargetDistance;
    private bool colliding;

    //smoothing
    private Vector3 positionVelocity;
    private Vector3 rotationVelocity;
    private Vector3 smoothedTargetPosition;
    private float? overrideSmoothTime = null;
    private float? pitchSmoothOverride = null;

    //input
    private bool hasInput = false;
    private bool yAxisLocked = false;
    private Camera cam;

    private void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
            cam = Camera.main;

        currentDistance = defaultDistance;
        targetDistance = defaultDistance;
        collisionDistance = defaultDistance;
        previousTargetDistance = defaultDistance;
        collisionSmoothTime = collisionZoomInTime;

        Vector3 currentRotation = transform.eulerAngles;
        currentYaw = currentRotation.y;
        currentPitch = currentRotation.x;

        if (currentPitch > 180f)
            currentPitch -= 360f;

        targetYaw = currentYaw;
        targetPitch = currentPitch;

        smoothedTargetPosition = target.position + targetOffset;

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
        float mouseX = input.LookInput.x;
        float mouseY = input.LookInput.y;

        if (input.CurrentDevice.Equals(PlayerActions.InputType.MouseKeyboard))
        {
            mouseX *= mouseXSensitivity;
            mouseY *= mouseYSensitivity;
        }
        else if(input.CurrentDevice.Equals(PlayerActions.InputType.Controller))
        {
            mouseX *= mouseXSensitivity * controllerXSensitivityMultiplier;
            mouseY *= mouseYSensitivity * controllerYSensitivityMultiplier;
        }

        hasInput = Mathf.Abs(mouseX) > 0.001f || Mathf.Abs(mouseY) > 0.001f || Mathf.Abs(input.MoveInput.sqrMagnitude) > 0.001f;

        targetYaw += mouseX;

        if (!yAxisLocked)
        {
            targetPitch -= mouseY;
            targetPitch = Mathf.Clamp(targetPitch, minVerticalAngle, maxVerticalAngle);
        }
    }

    private void UpdateGhostTransform()
    {
        //rotation
        currentYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref rotationVelocity.y, rotationSmoothTime);
        float smoothTime = pitchSmoothOverride.HasValue ? pitchSmoothOverride.Value : rotationSmoothTime;
        currentPitch = Mathf.SmoothDampAngle(currentPitch, targetPitch, ref rotationVelocity.x, smoothTime);

        //desired rotation
        ghostRotation = Quaternion.Euler(currentPitch, currentYaw, 0f);

        //desired position before collision, including offset
        Vector3 targetPosition = target.position + targetOffset;

        //positional damping to movement only
        //get delta relative to rotation
        Vector3 targetDelta = targetPosition - smoothedTargetPosition;
        Vector3 localTargetDelta = Quaternion.Inverse(ghostRotation) * targetDelta;

        //smooth delta to 0
        float smoothedX = Mathf.SmoothDamp(0, localTargetDelta.x, ref positionVelocity.x, positionDamping.x);
        float smoothedY = Mathf.SmoothDamp(0, localTargetDelta.y, ref positionVelocity.y, positionDamping.y);
        float smoothedZ = Mathf.SmoothDamp(0, localTargetDelta.z, ref positionVelocity.z, positionDamping.z);

        Vector3 smoothedLocalDelta = new Vector3(smoothedX, smoothedY, smoothedZ);
        Vector3 smoothedWorldDelta = ghostRotation * smoothedLocalDelta;
        smoothedTargetPosition += smoothedWorldDelta;

        //desired position for the camera after damping and screen offset
        Vector3 basePosition = smoothedTargetPosition - (ghostRotation * Vector3.forward * currentDistance);
        Vector3 desiredPosition = ApplyScreenSpaceOffset(basePosition);

        if (handleCollision)
        {
            Vector3 direction = desiredPosition - smoothedTargetPosition;
            float distance = direction.magnitude;

            //detect collision distance, but keep the camera at collisionBuffer at minimum
            //no collision = max distance
            float targetCollisionDistance;
            if (Physics.SphereCast(smoothedTargetPosition, cameraRadius, direction.normalized, out RaycastHit hit, distance, collisionLayers))
            {
                float hitDistance = hit.distance - collisionBuffer;
                targetCollisionDistance = Mathf.Max(hitDistance, collisionBuffer);
                colliding = true;
            }
            else
            {
                targetCollisionDistance = distance;
                colliding = false;
            }

            //change smooth time based on zooming in vs out
            bool zoomingIn = targetCollisionDistance < previousTargetDistance - 0.01f;
            bool zoomingOut = targetCollisionDistance > previousTargetDistance + 0.01f;

            if (overrideSmoothTime.HasValue)
                collisionSmoothTime = overrideSmoothTime.Value;
            else if (zoomingIn) 
                collisionSmoothTime = collisionZoomInTime; 
            else if (zoomingOut)
                collisionSmoothTime = collisionZoomOutTime;

            //only move camera on input or if colliding
            if (colliding || hasInput || overrideSmoothTime.HasValue) 
                collisionDistance = Mathf.SmoothDamp(collisionDistance, targetCollisionDistance, ref collisionVelocity, collisionSmoothTime);
            else
                collisionVelocity = 0f;

            previousTargetDistance = targetCollisionDistance;

            //set ghost position for smoothing
            ghostPosition = smoothedTargetPosition + direction.normalized * collisionDistance;
        }
        else
        {
            ghostPosition = desiredPosition;
            collisionDistance = desiredPosition.magnitude;
        }
    }

    private Vector3 ApplyScreenSpaceOffset(Vector3 cameraPosition)
    {
        float xOffset = screenOffset.x;
        float yOffset = screenOffset.y;

        Vector3 right = ghostRotation * Vector3.right;
        Vector3 up = ghostRotation * Vector3.up;
        float fov = cam.fieldOfView * Mathf.Deg2Rad;
        float aspect = cam.aspect;

        float verticalSize = Mathf.Tan(fov / 2f) * currentDistance;
        float horizontalSize = verticalSize * aspect;

        Vector3 offset = (right * (xOffset * horizontalSize)) + (up * (yOffset * verticalSize));

        return cameraPosition + offset;
    }

    private void SmoothCameraToGhost()
    {
        transform.position = ghostPosition;
        transform.rotation = ghostRotation;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void SetRotation(float yaw, float pitch)
    {
        targetYaw = yaw; 
        targetPitch = Mathf.Clamp(pitch, minVerticalAngle, maxVerticalAngle);
    }

    public void SetDistance(float distance)
    {
        targetDistance = Mathf.Clamp(distance, minDistance, currentMaxDistance);
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

    public Vector2 GetScreenOffset() => screenOffset;
    public Vector3 GetTargetOffset() => targetOffset;
    public float GetDefaultDistance() => defaultDistance;
    public float GetMouseXSensitivity() => mouseXSensitivity;
    public float GetMouseYSensitivity() => mouseYSensitivity;
    public void SetScreenOffset(Vector2 offset) => screenOffset = offset;
    public void SetTargetOffset(Vector3 offset) => targetOffset = offset;
    public void SetMouseSensitivity(float xSens, float ySens)
    {
        mouseXSensitivity = xSens;
        mouseYSensitivity = ySens;
    }
    public void SetYAxisLocked(bool locked) => yAxisLocked = locked;
    public void SetDistanceLimit(float max)
    {
        currentMaxDistance = max;
    }

    public void SetCollisionSmoothTimeOverride(float? time)
    {
        overrideSmoothTime = time;
    }

    public void SetPitchSmoothOverride(float? time)
    {
        pitchSmoothOverride = time;
    }

}