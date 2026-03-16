using UnityEngine;

public class ZeldaCameraController : MonoBehaviour
{
    public static ZeldaCameraController Instance { get; private set; }

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
    [SerializeField] private float rotationSmoothTime = 0f;

    [Header("Position Smoothing")]
    [SerializeField] private Vector3 positionDamping;
    private Vector3 originalPositionDamping;

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

    private bool isFrozen;

    public bool IsColliding() => colliding;
    public float GetCollisionDistance() => collisionDistance;
    public float GetCameraRange01() => Mathf.Clamp01(collisionDistance / defaultDistance);
    public Vector3 TargetOffset => targetOffset;
    public Vector3 TargetPos => target.position;

    //z offset (framing push along camera forward axis)
    private float zOffset = 0f;

    //smoothing
    private Vector3 positionVelocity;
    private Vector3 rotationVelocity;
    private Vector3 smoothedTargetPosition;
    private float? overrideSmoothTime = null;
    private float? pitchSmoothOverride = null;

    //input
    private bool hasInput = false;
    private bool yAxisLocked = false;
    private bool xAxisLocked = false;
    private Camera cam;

    //up-down pitching
    private float originalMinVerticalAngle;
    private float originalMaxVerticalAngle;

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        cam = GetComponent<Camera>();
        if (cam == null)
            cam = Camera.main;

        currentDistance = defaultDistance;
        targetDistance = defaultDistance;
        collisionDistance = defaultDistance;
        previousTargetDistance = defaultDistance;
        collisionSmoothTime = collisionZoomInTime;
        originalPositionDamping = positionDamping;

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

        originalMinVerticalAngle = minVerticalAngle;
        originalMaxVerticalAngle = maxVerticalAngle;
    }

    private void OnEnable()
    {
        if (target != null)
        {
            smoothedTargetPosition = target.position + targetOffset;
            positionVelocity = Vector3.zero;
        }
    }

    private void LateUpdate()
    {
        if (Time.timeScale == 0f || Time.deltaTime <= float.Epsilon || target == null)
            return;

        if (!isFrozen)
            HandleInput();

        UpdateGhostTransform();

        if (!isFrozen)
            SmoothCameraToGhost();
    }

    private void HandleInput()
    {
        float mouseX = PlayerActions.Instance.LookInput.x;
        float mouseY = PlayerActions.Instance.LookInput.y;

        if (PlayerActions.Instance.CurrentDevice.Equals(PlayerActions.InputType.MouseKeyboard))
        {
            mouseX *= mouseXSensitivity;
            mouseY *= mouseYSensitivity;
        }
        else if (PlayerActions.Instance.CurrentDevice.Equals(PlayerActions.InputType.Controller))
        {
            mouseX *= mouseXSensitivity * controllerXSensitivityMultiplier;
            mouseY *= mouseYSensitivity * controllerYSensitivityMultiplier;
        }

        hasInput = Mathf.Abs(mouseX) > 0.001f || Mathf.Abs(mouseY) > 0.001f || Mathf.Abs(PlayerActions.Instance.MoveInput.sqrMagnitude) > 0.001f;

        if (!xAxisLocked)
        {
            targetYaw += mouseX;
        }

        if (!yAxisLocked)
        {
            targetPitch -= mouseY;
            targetPitch = Mathf.Clamp(targetPitch, minVerticalAngle, maxVerticalAngle);
        }
    }

    private void UpdateGhostTransform()
    {

        //rotation
        if (rotationSmoothTime > 0.001f)
        {
            currentYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref rotationVelocity.y, rotationSmoothTime);
        }
        else
        {
            //pausing was causing infinite division, so in a circumstance where the value is 0, this is a safety net
            currentYaw = targetYaw;
            rotationVelocity.y = 0f;
        }

        float smoothTime = pitchSmoothOverride.HasValue ? pitchSmoothOverride.Value : rotationSmoothTime;
        if (smoothTime > 0.001f)
        {
            currentPitch = Mathf.SmoothDampAngle(currentPitch, targetPitch, ref rotationVelocity.x, smoothTime);
        }
        else
        {
            currentPitch = targetPitch;
            rotationVelocity.x = 0f;
        }

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
            //base direction used for zoom detection and smoothing — no zOffset so it stays stable
            Vector3 direction = desiredPosition - smoothedTargetPosition;
            float distance = direction.magnitude;

            //extended position including zOffset — this is where the camera actually ends up
            Vector3 extendedDesiredPosition = desiredPosition - ghostRotation * Vector3.forward * zOffset;
            Vector3 extendedDirection = extendedDesiredPosition - smoothedTargetPosition;
            float extendedDistance = extendedDirection.magnitude;

            //cast over the full extended distance so geometry is never missed
            float targetCollisionDistance;
            if (Physics.SphereCast(smoothedTargetPosition, cameraRadius, extendedDirection.normalized, out RaycastHit hit, extendedDistance, collisionLayers))
            {
                float hitDistance = hit.distance - collisionBuffer;
                targetCollisionDistance = Mathf.Max(hitDistance, collisionBuffer);
                colliding = true;
            }
            else
            {
                targetCollisionDistance = extendedDistance;
                colliding = false;
            }

            //zoom detection uses base distance so zOffset lerping doesn't trigger stutter
            float baseTargetCollisionDistance = Mathf.Min(targetCollisionDistance, distance);
            bool zoomingIn = baseTargetCollisionDistance < previousTargetDistance - 0.01f;
            bool zoomingOut = baseTargetCollisionDistance > previousTargetDistance + 0.01f;

            if (overrideSmoothTime.HasValue)
                collisionSmoothTime = overrideSmoothTime.Value;
            else if (zoomingIn)
                collisionSmoothTime = collisionZoomInTime;
            else if (zoomingOut)
                collisionSmoothTime = collisionZoomOutTime;

            /*//only move camera on input or if colliding
            if (colliding || hasInput || overrideSmoothTime.HasValue)
                collisionDistance = Mathf.SmoothDamp(collisionDistance, targetCollisionDistance, ref collisionVelocity, collisionSmoothTime);
            else
                collisionVelocity = 0f;*/

            collisionDistance = Mathf.SmoothDamp(collisionDistance, targetCollisionDistance, ref collisionVelocity, collisionSmoothTime);

            previousTargetDistance = baseTargetCollisionDistance;
            ghostPosition = smoothedTargetPosition + extendedDirection.normalized * collisionDistance;
        }
        else
        {
            ghostPosition = desiredPosition - ghostRotation * Vector3.forward * zOffset;
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

    public void SnapDistance(float distance)
    {
        currentDistance = distance;
        targetDistance = distance;
        collisionDistance = distance;
        collisionVelocity = 0f;
        previousTargetDistance = distance;
    }

    public Vector3 GetGhostPosition() => ghostPosition;
    public Quaternion GetGhostRotation() => ghostRotation;
    public float GetCurrentDistance() => currentDistance;
    public float GetCurrentYaw() => currentYaw;
    public float GetCurrentPitch() => currentPitch;
    public float GetRawLookInputY() => PlayerActions.Instance.LookInput.y;
    public float GetTargetPitch() => targetPitch;
    public void SetVerticalClamp(float? min, float? max)
    {
        minVerticalAngle = min ?? originalMinVerticalAngle;
        maxVerticalAngle = max ?? originalMaxVerticalAngle;
    }
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

    public void SetBaseXSensitivity(float value)
    {
        mouseXSensitivity = value;
    }

    public void SetBaseYSensitivity(float value)
    {
        mouseYSensitivity = value;
    }
    public void SetZOffset(float offset) => zOffset = offset;
    public void SetYAxisLocked(bool locked) => yAxisLocked = locked;
    public void SetXAxisLocked(bool locked) => xAxisLocked = locked;
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

    public void SetPositionDamping(Vector3? newDamping)
    {
        if (newDamping.HasValue) positionDamping = newDamping.Value;
        else positionDamping = originalPositionDamping;
    }

    public void SetFrozen(bool frozen) => isFrozen = frozen;
    public void SnapSmoothedPosition()
    {
        smoothedTargetPosition = target.position + targetOffset;
        positionVelocity = Vector3.zero;
    }
    public void UpdateGhostTransformPublic() => UpdateGhostTransform();
}