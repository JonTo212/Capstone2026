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
    [SerializeField] private float closestDistance = 1f;
    [SerializeField] private int stepCount = 100;

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
    [Tooltip("How long (seconds) the desired step eases toward the clear step when collision clears suddenly " +
             "(e.g. exiting a tight room into open air). Set to 0 to disable edge smoothing.")]
    [SerializeField] private float collisionEdgeSmoothTime = 0.4f;

    // how many steps to move per frame, calculated from smooth times
    private float _stepsPerFrameIn;
    private float _stepsPerFrameOut;

    // discrete collision stepping
    private int _currentStep;
    private float[] _stepDistances;
    private bool _hasInput;

    // smoothed desired step — eases the zoom-out target when collision clears abruptly
    private float _smoothedDesiredStep;
    private float _smoothedDesiredStepVelocity;

    //ghost transform -> what the camera tracks
    private Vector3 ghostPosition;
    private Quaternion ghostRotation;

    //rotation
    private float currentYaw = 0f;
    private float currentPitch = 0f;
    private float targetYaw = 0f;
    private float targetPitch = 0f;

    //distance / collision
    private float currentDistance;
    private float collisionDistance;
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
    private float? pitchSmoothOverride = null;

    //input
    private bool yAxisLocked = false;
    private bool xAxisLocked = false;
    private Camera cam;

    //up-down pitching
    private float originalMinVerticalAngle;
    private float originalMaxVerticalAngle;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        cam = GetComponent<Camera>();
        if (cam == null)
            cam = Camera.main;

        BuildStepDistances();
        CalculateStepRates();

        currentDistance = defaultDistance;
        collisionDistance = defaultDistance;
        _currentStep = stepCount - 1;
        _smoothedDesiredStep = stepCount - 1;
        _smoothedDesiredStepVelocity = 0f;

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

        isFrozen = false;
    }

    private void BuildStepDistances()
    {
        stepCount = Mathf.Max(2, stepCount);
        _stepDistances = new float[stepCount];
        for (int i = 0; i < stepCount; i++)
        {
            float t = (float)i / (stepCount - 1);
            _stepDistances[i] = Mathf.Lerp(closestDistance, defaultDistance, t);
        }
    }

    private void CalculateStepRates(float? overrideTime = null)
    {
        float inTime = overrideTime ?? collisionZoomInTime;
        float outTime = overrideTime ?? collisionZoomOutTime;
        _stepsPerFrameIn = (stepCount - 1) / Mathf.Max(inTime / Time.fixedDeltaTime, 1f);
        _stepsPerFrameOut = (stepCount - 1) / Mathf.Max(outTime / Time.fixedDeltaTime, 1f);
    }

    private void OnValidate()
    {
        BuildStepDistances();
        CalculateStepRates();
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

        Vector2 moveInput = PlayerActions.Instance.MoveInput;
        _hasInput = Mathf.Abs(mouseX) > 0.001f || Mathf.Abs(mouseY) > 0.001f || Mathf.Abs(moveInput.x) > 0.001f || Mathf.Abs(moveInput.y) > 0.001f;

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
            //find the highest step whose position is unobstructed
            int rawDesiredStep = FindClearStep();

            //when collision clears suddenly (e.g. exiting a room), ease the desired step so _currentStep doesn't receive a huge jump all at once
            //zoom-in is still instant
            //bool isDrastic = Mathf.Abs(rawDesiredStep - _smoothedDesiredStep) >= collisionEdgeSmoothThreshold * (stepCount - 1);
            //^use this and add collisionEdgeSmoothThreshold to make it work both ways, based on a threshold
            if (rawDesiredStep < _smoothedDesiredStep)
            {
                _smoothedDesiredStep = rawDesiredStep;
                _smoothedDesiredStepVelocity = 0f;
            }
            else if (collisionEdgeSmoothTime > 0.001f)
            {
                _smoothedDesiredStep = Mathf.SmoothDamp(_smoothedDesiredStep, rawDesiredStep, ref _smoothedDesiredStepVelocity, collisionEdgeSmoothTime);
            }
            else
            {
                _smoothedDesiredStep = rawDesiredStep;
                _smoothedDesiredStepVelocity = 0f;
            }

            int desiredStep = Mathf.RoundToInt(_smoothedDesiredStep);

            //zoom in immediately; zoom out only with input
            if (desiredStep < _currentStep)
            {
                float newStep = _currentStep - _stepsPerFrameIn;
                _currentStep = Mathf.Max(Mathf.RoundToInt(newStep), desiredStep);
            }
            else if (desiredStep > _currentStep && _hasInput)
            {
                float newStep = _currentStep + _stepsPerFrameOut;
                _currentStep = Mathf.Min(Mathf.RoundToInt(newStep), desiredStep);
            }

            currentDistance = _stepDistances[_currentStep];
            colliding = _currentStep < stepCount - 1;
            collisionDistance = currentDistance;

            //recompute final position at the stepped distance, preserving zOffset logic from the original
            Vector3 steppedBase = smoothedTargetPosition - (ghostRotation * Vector3.forward * currentDistance);
            Vector3 steppedDesired = ApplyScreenSpaceOffset(steppedBase);
            Vector3 extendedDesiredPosition = steppedDesired - ghostRotation * Vector3.forward * zOffset;
            ghostPosition = extendedDesiredPosition;
        }
        else
        {
            ghostPosition = desiredPosition - ghostRotation * Vector3.forward * zOffset;
            collisionDistance = currentDistance;
        }
    }

    // checks from stepCount-1 downward, returns the highest step that is clear
    private int FindClearStep()
    {
        for (int step = stepCount - 1; step >= 0; step--)
        {
            float distance = _stepDistances[step];

            Vector3 basePosition = smoothedTargetPosition - (ghostRotation * Vector3.forward * distance);
            Vector3 desiredPosition = ApplyScreenSpaceOffset(basePosition);
            Vector3 extendedDesiredPosition = desiredPosition - ghostRotation * Vector3.forward * zOffset;

            Vector3 direction = extendedDesiredPosition - smoothedTargetPosition;
            float castDistance = direction.magnitude;

            //if nothing is hit, this step is valid
            if (!Physics.SphereCast(smoothedTargetPosition, cameraRadius, direction.normalized, out RaycastHit _, castDistance - collisionBuffer, collisionLayers))
            {
                return step;
            }
        }

        //all steps obstructed - stay at closest
        return 0;
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

    public void SetRotation(float yaw, float pitch)
    {
        targetYaw = yaw;
        targetPitch = Mathf.Clamp(pitch, minVerticalAngle, maxVerticalAngle);
    }

    public void SnapDistance(float distance)
    {
        currentDistance = distance;
        collisionDistance = distance;
        _currentStep = stepCount - 1;
    }

    public Vector3 GetGhostPosition() => ghostPosition;
    public Quaternion GetGhostRotation() => ghostRotation;
    public float GetCurrentDistance() => currentDistance;
    public float GetCurrentYaw() => currentYaw;
    public float GetCurrentPitch() => currentPitch;
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

    public void SetZOffset(float offset) => zOffset = offset;
    public void SetYAxisLocked(bool locked) => yAxisLocked = locked;
    public void SetXAxisLocked(bool locked) => xAxisLocked = locked;
    public void SetCollisionSmoothTimeOverride(float? time) => CalculateStepRates(time);
    public void SetPitchSmoothOverride(float? time) => pitchSmoothOverride = time;
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