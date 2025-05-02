using UnityEngine;

namespace OccaSoftware.OrbitCamera.Runtime
{
  public class OrbitCamera : MonoBehaviour
  {
    [Header("Initial Options")]
    [Tooltip("The target transform object that the camera will focus on.")]
    public Transform targetTransform;

    [Tooltip("The initial distance from the camera to the target.")]
    public float startDistance = 2f;

    [Tooltip("The initial polar angle (vertical angle) of the camera.")]
    public float startPolarAngle = Mathf.PI * 0.5f;

    [Tooltip("The initial azimuth angle (horizontal angle) of the camera.")]
    public float startAzimuthAngle;

    [Header("Rotate")]
    [Tooltip("Enable or disable camera rotation.")]
    public bool enableRotate = true;

    [Tooltip("The speed at which the camera rotates around the target.")]
    public float rotateSpeed = 25.0f;

    [Tooltip("The damping factor for camera rotation, higher values result in slower rotation.")]
    public float rotateDampingFactor = 0.35f;

    [Tooltip("The minimum polar angle (vertical angle) allowed for camera rotation.")]
    public float minPolarAngle;

    [Tooltip("The maximum polar angle (vertical angle) allowed for camera rotation.")]
    public float maxPolarAngle = Mathf.PI;

    [Tooltip("The minimum azimuth angle (horizontal angle) allowed for camera rotation.")]
    public float minAzimuthAngle = -Mathf.Infinity;

    [Tooltip("The maximum azimuth angle (horizontal angle) allowed for camera rotation.")]
    public float maxAzimuthAngle = Mathf.Infinity;

    [Header("Pan")]
    [Tooltip("Enable or disable camera panning.")]
    public bool enablePan = true;

    [Tooltip("The speed at which the camera pans across the scene.")]
    public float panSpeed = 25.0f;

    [Tooltip("The damping factor for camera panning, higher values result in slower panning.")]
    public float panDampingFactor = 0.35f;

    [Header("Zoom")]
    [Tooltip("Enable or disable camera zooming.")]
    public bool enableZoom = true;

    [Tooltip("The speed at which the camera zooms in and out.")]
    public float zoomSpeed = 25.0f;

    [Tooltip("The damping factor for camera zooming, higher values result in slower zooming.")]
    public float zoomDampingFactor = 0.35f;

    [Tooltip("The minimum distance allowed for camera zoom.")]
    public float minDistance;

    [Tooltip("The maximum distance allowed for camera zoom.")]
    public float maxDistance = Mathf.Infinity;

    [Header("Auto Rotate")]
    [Tooltip("Enable or disable automatic rotation of the camera around the target.")]
    public bool autoRotate;

    [Tooltip("The speed at which the camera automatically rotates around the target.")]
    public float autoRotateSpeed = 2.0f;

    private float distance;
    private float polarAngle;
    private float azimuthAngle;
    private Vector3 lastMousePosition;
    private Vector3 panVelocity;
    private Vector3 targetDirection;
    private float targetPolarAngle;
    private float targetAzimuthAngle;
    private float cZoomVelocity;
    private float targetDistance;
    private float sinPolarAngle;
    private float cosPolarAngle;
    private float sinAzimuthAngle;
    private float cosAzimuthAngle;
    private float azimuthVelocity;
    private float polarVelocity;
    private Vector3 positionOffset;
    private Vector3 targetPosition;

    private Transform m_Root;

    private Vector3 GetTargetPosition()
    {
      if (targetTransform == null)
        return Vector3.zero;

      return targetTransform.transform.position;
    }

    void Start()
    {
      m_Root = new GameObject("Pivot").transform;
      transform.parent = m_Root;
      transform.localPosition = Vector3.zero;
      transform.localRotation = Quaternion.Euler(0, 180, 0);
      transform.localScale = Vector3.one;

      m_Root.transform.position = GetTargetPosition();
      targetPosition = positionOffset;

      distance = startDistance;
      targetDistance = distance;

      polarAngle = startPolarAngle;
      targetPolarAngle = polarAngle;

      azimuthAngle = startAzimuthAngle;
      targetAzimuthAngle = azimuthAngle;
      SetupAngles();
    }

    private bool GetRotateInput()
    {
      return Input.GetMouseButton(0);
    }

    private bool GetRotateInputDown()
    {
      return Input.GetMouseButtonDown(0);
    }

    private bool GetPanInput()
    {
      return Input.GetMouseButton(1);
    }

    private bool GetPanInputDown()
    {
      return Input.GetMouseButtonDown(1);
    }

    private Vector3 GetCursorPosition()
    {
      return Input.mousePosition;
    }

    private float GetZoomInput()
    {
      return Input.mouseScrollDelta.y;
    }

    void Update()
    {
      if (autoRotate)
      {
        AutoRotate();
      }

      Vector3 cursorPosition = GetCursorPosition();

      if (GetRotateInputDown() || GetPanInputDown())
      {
        lastMousePosition = cursorPosition;
      }

      bool rotateInput = GetRotateInput();
      bool panInput = GetPanInput();
      float zoomInput = GetZoomInput();

      RotateCamera(rotateInput, cursorPosition);
      ZoomCamera(zoomInput);
      PanCamera(panInput, cursorPosition);
    }

    private void LateUpdate()
    {
      UpdateCameraPosition();
    }

    private Vector2 NormalizedScreenPosition(Vector3 position)
    {
      return new Vector2(position.x / Screen.width, position.y / Screen.height);
    }

    void RotateCamera(bool inputting, Vector3 currentMousePosition)
    {
      if (!enableRotate)
        return;

      if (inputting)
      {
        Vector2 delta =
          NormalizedScreenPosition(currentMousePosition)
          - NormalizedScreenPosition(lastMousePosition);

        delta *= 100;

        targetAzimuthAngle -= delta.x * rotateSpeed * Time.deltaTime;
        targetPolarAngle += delta.y * 0.5f * rotateSpeed * Time.deltaTime;

        lastMousePosition = currentMousePosition;
      }

      azimuthAngle = SmoothDamp(
        azimuthAngle,
        targetAzimuthAngle,
        ref azimuthVelocity,
        rotateDampingFactor,
        Mathf.Infinity,
        Time.deltaTime
      );

      polarAngle = SmoothDamp(
        polarAngle,
        targetPolarAngle,
        ref polarVelocity,
        rotateDampingFactor,
        Mathf.Infinity,
        Time.deltaTime
      );

      targetPolarAngle = Mathf.Clamp(targetPolarAngle, minPolarAngle, maxPolarAngle);
      polarAngle = Mathf.Clamp(polarAngle, minPolarAngle, maxPolarAngle);
      targetAzimuthAngle = Mathf.Clamp(targetAzimuthAngle, minAzimuthAngle, maxAzimuthAngle);
      azimuthAngle = Mathf.Clamp(azimuthAngle, minAzimuthAngle, maxAzimuthAngle);
      SetupAngles();
    }

    private void SetupAngles()
    {
      sinPolarAngle = Mathf.Sin(polarAngle);
      cosPolarAngle = Mathf.Cos(polarAngle);
      sinAzimuthAngle = Mathf.Sin(azimuthAngle);
      cosAzimuthAngle = Mathf.Cos(azimuthAngle);
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

    /// <summary>
    /// Check if the mouse is within the bounds of the game window.
    /// </summary>
    /// <returns>True if mouse is inside game window bounds.</returns>
    private bool IsMouseOverGameWindow()
    {
      return Input.mousePosition.x > 0
        && Input.mousePosition.y > 0
        && Input.mousePosition.x < Screen.width
        && Input.mousePosition.y < Screen.height;
    }

    void ZoomCamera(float zoomInput)
    {
      if (!enableZoom)
        return;

      float delta = zoomInput;
      if (!IsMouseOverGameWindow())
        delta = 0;

      if (Mathf.Abs(delta) > 0)
      {
        delta = Mathf.Sign(delta);
        targetDistance -= delta * zoomSpeed * Time.deltaTime;
      }

      distance = SmoothDamp(
        distance,
        targetDistance,
        ref cZoomVelocity,
        zoomDampingFactor,
        Mathf.Infinity,
        Time.deltaTime
      );

      targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
      distance = Mathf.Clamp(distance, minDistance, maxDistance);
    }

    void PanCamera(bool inputting, Vector3 currentMousePosition)
    {
      if (!enablePan)
        return;

      if (inputting)
      {
        Vector2 delta =
          NormalizedScreenPosition(currentMousePosition)
          - NormalizedScreenPosition(lastMousePosition);

        delta *= 100;

        Vector3 move = new Vector3(
          -delta.x * panSpeed * Time.deltaTime,
          -delta.y * panSpeed * Time.deltaTime,
          0
        );

        targetPosition += transform.TransformDirection(move);

        lastMousePosition = currentMousePosition;
      }

      positionOffset = Vector3.SmoothDamp(
        positionOffset,
        targetPosition,
        ref panVelocity,
        panDampingFactor
      );
    }

    void AutoRotate()
    {
      targetAzimuthAngle += autoRotateSpeed * Time.deltaTime;
      azimuthAngle += autoRotateSpeed * Time.deltaTime;
    }

    public void EnableAutoRotate(bool value) => autoRotate = value;

    void UpdateCameraPosition()
    {
      targetDirection = new Vector3(
        sinPolarAngle * cosAzimuthAngle,
        cosPolarAngle,
        sinPolarAngle * sinAzimuthAngle
      );

      m_Root.transform.position = GetTargetPosition() + positionOffset;
      transform.localPosition = targetDirection * distance;
      transform.LookAt(m_Root.transform.position);
    }
  }
}
