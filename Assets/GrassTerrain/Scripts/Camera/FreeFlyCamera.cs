using UnityEngine;
using UnityEngine.InputSystem;

namespace OccaSoftware.FreeFlyCamera.Runtime
{
  [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
  public class FreeFlyCamera : MonoBehaviour
  {
    [Tooltip("Select your Main Camera. (Note: Your camera should in a separate GameObject.)")]
    public Transform cam;

    [Header("Movement Settings")]
    public float accelerationRate = 50f;
    public float decelerationRate = 10f;
    public float maxVelocity = 20f;
    public VerticalMode verticalMovementMode = VerticalMode.Global;

    public enum VerticalMode
    {
      Local,
      Global
    };

    [Header("Mouse Look Settings")]
    public float mouseSensitivity = 0.1f;
    public float mouseLookSmoothing = 0.35f;
    public bool hideCursor = true;
    private bool hideCursor_cached;
    public bool lockCursor = true;
    private bool lockCursor_cached;

    [Tooltip("The minimum polar angle (vertical angle) allowed for camera rotation.")]
    public float minPolarAngle;

    [Tooltip("The maximum polar angle (vertical angle) allowed for camera rotation.")]
    public float maxPolarAngle = Mathf.PI;

    [Tooltip("The minimum azimuth angle (horizontal angle) allowed for camera rotation.")]
    public float minAzimuthAngle = -Mathf.Infinity;

    [Tooltip("The maximum azimuth angle (horizontal angle) allowed for camera rotation.")]
    public float maxAzimuthAngle = Mathf.Infinity;

    [Header("Collision Detection")]
    public Rigidbody body;
    public bool enableCollisionDetection = true;
    public SphereCollider sphereCollider;

    [Header("Input Setup")]
    public InputActionReference moveAction;
    public InputActionReference lookAction;
    public InputActionReference moveUpAction;
    public InputActionReference moveDownAction;

    private Vector2 movementInput;
    private Vector2 mouseInput;
    private float moveUpInput;
    private float moveDownInput;
    private Vector3 velocity;

    private float polarAngle;
    private float azimuthAngle;
    private Vector3 targetDirection;
    private float targetPolarAngle;
    private float targetAzimuthAngle;
    private float sinPolarAngle;
    private float cosPolarAngle;
    private float sinAzimuthAngle;
    private float cosAzimuthAngle;
    private float azimuthVelocity;
    private float polarVelocity;

    private void Start()
    {
      // 初期のカメラの向きを設定
      polarAngle = Mathf.PI / 2; // 90度（水平）
      azimuthAngle = Mathf.PI / 2;         // 前方向
      targetPolarAngle = polarAngle;
      targetAzimuthAngle = azimuthAngle;
    }

    private void Reset()
    {
      body = GetComponent<Rigidbody>();
      body.interpolation = RigidbodyInterpolation.Interpolate;
      body.mass = 50;
      body.useGravity = false;
      body.freezeRotation = true;

      sphereCollider = GetComponent<SphereCollider>();

      cam = Camera.main.transform;
    }

    private void OnEnable()
    {
      moveAction.action.Enable();
      lookAction.action.Enable();
      moveUpAction.action.Enable();
      moveDownAction.action.Enable();
      SetCursorMode();
    }

    private void OnValidate()
    {
      UpdateCursorMode();
    }

    /// <summary>
    /// Call this method after updating lockCursor or hideCursor from script.
    /// </summary>
    public void UpdateCursorMode()
    {
      if (lockCursor != lockCursor_cached)
      {
        Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
        lockCursor_cached = lockCursor;
      }

      if (hideCursor != hideCursor_cached)
      {
        Cursor.visible = hideCursor ? false : true;
        hideCursor_cached = hideCursor;
      }
    }

    private void SetCursorMode()
    {
      lockCursor_cached = lockCursor;
      hideCursor_cached = hideCursor;
      if (lockCursor)
      {
        Cursor.lockState = CursorLockMode.Locked;
      }
      if (hideCursor)
      {
        Cursor.visible = false;
      }
    }

    private void Update()
    {
      HandleInput();
      HandleCameraMovement();
    }

    private void FixedUpdate()
    {
      SetCollisionOption();
      HandleRigidbodyMovement();
    }

    private void LateUpdate()
    {
      HandleMouseLook();
    }

    private void HandleInput()
    {
      movementInput = moveAction.action.ReadValue<Vector2>();
      mouseInput = lookAction.action.ReadValue<Vector2>();
      moveUpInput = moveUpAction.action.ReadValue<float>();
      moveDownInput = moveDownAction.action.ReadValue<float>();
    }

    private void HandleCameraMovement()
    {
      cam.position = transform.position;
    }

    private void SetCollisionOption()
    {
      sphereCollider.enabled = enableCollisionDetection;
    }

    private void HandleRigidbodyMovement()
    {
      velocity = body.linearVelocity;

      // Apply deceleration
      velocity = Vector3.MoveTowards(velocity, Vector3.zero, decelerationRate * Time.deltaTime);
      if (velocity.magnitude < 0.01f)
        velocity = Vector3.zero;

      float verticalInput = moveUpInput - moveDownInput;
      Vector3 verticalMovementDirection =
        verticalMovementMode == VerticalMode.Global ? Vector3.up : cam.up;

      // Apply acceleration
      Vector3 moveDirection =
        cam.forward * movementInput.y
        + cam.right * movementInput.x
        + verticalMovementDirection * verticalInput;

      velocity += moveDirection.normalized * accelerationRate * Time.deltaTime;

      velocity = Vector3.ClampMagnitude(velocity, maxVelocity);
      body.linearVelocity = velocity;
    }

    void HandleMouseLook()
    {
      Vector2 mouseDelta = mouseInput * mouseSensitivity * 0.01f;
      targetAzimuthAngle -= mouseDelta.x;
      targetPolarAngle -= mouseDelta.y;

      azimuthAngle = SmoothDamp(
        azimuthAngle,
        targetAzimuthAngle,
        ref azimuthVelocity,
        mouseLookSmoothing,
        Mathf.Infinity,
        Time.deltaTime
      );

      polarAngle = SmoothDamp(
        polarAngle,
        targetPolarAngle,
        ref polarVelocity,
        mouseLookSmoothing,
        Mathf.Infinity,
        Time.deltaTime
      );

      targetPolarAngle = Mathf.Clamp(targetPolarAngle, minPolarAngle, maxPolarAngle);
      polarAngle = Mathf.Clamp(polarAngle, minPolarAngle, maxPolarAngle);
      targetAzimuthAngle = Mathf.Clamp(targetAzimuthAngle, minAzimuthAngle, maxAzimuthAngle);
      azimuthAngle = Mathf.Clamp(azimuthAngle, minAzimuthAngle, maxAzimuthAngle);

      sinPolarAngle = Mathf.Sin(polarAngle);
      cosPolarAngle = Mathf.Cos(polarAngle);
      sinAzimuthAngle = Mathf.Sin(azimuthAngle);
      cosAzimuthAngle = Mathf.Cos(azimuthAngle);

      targetDirection = new Vector3(
        sinPolarAngle * cosAzimuthAngle,
        cosPolarAngle,
        sinPolarAngle * sinAzimuthAngle
      );

      cam.LookAt(cam.position + targetDirection);
    }

    private void OnDrawGizmos()
    {
      if (sphereCollider)
      {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, sphereCollider.radius);
      }
    }

    /// <summary>
    /// This method is pulled from Unity's Mathf.SmoothDamp implementation.
    /// However, Unity's system has a bug.
    /// It only functions for positive changes.
    /// The bug originates in the overshoot prevention logic.
    /// We pulled out the source of the bug.
    /// </summary>
    private float SmoothDamp(
      float current,
      float target,
      ref float currentVelocity,
      float smoothTime,
      float maxSpeed,
      float deltaTime
    )
    {
      if (deltaTime <= 0)
        return current;

      smoothTime = Mathf.Max(0.0001F, smoothTime);
      float omega = 2f / smoothTime;

      float x = omega * deltaTime;
      float exp = 1f / (1f + x + (0.48f * x * x) + (0.235f * x * x * x));
      float change = current - target;

      float maxChange = maxSpeed * smoothTime;
      change = Mathf.Clamp(change, -maxChange, maxChange);
      target = current - change;

      float temp = (currentVelocity + (omega * change)) * deltaTime;
      currentVelocity = (currentVelocity - (omega * temp)) * exp;
      return target + ((change + temp) * exp);
    }
  }
}
