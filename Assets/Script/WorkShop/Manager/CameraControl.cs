using UnityEngine;

public class CameraControl : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.5f, 0f);
    
    [Header("Camera Distance")]
    [SerializeField] private float defaultDistance = 5f;
    [SerializeField] private float minDistance = 2f;
    [SerializeField] private float maxDistance = 10f;
    [SerializeField] private float zoomSpeed = 2f;
    
    [Header("Camera Rotation")]
    [SerializeField] private float mouseSensitivity = 3f;
    [SerializeField] private float minVerticalAngle = -30f;
    [SerializeField] private float maxVerticalAngle = 60f;
    
    [Header("Camera Smoothness")]
    [SerializeField] private float rotationSmoothTime = 0.12f;
    [SerializeField] private float positionSmoothTime = 0.2f;
    
    [Header("Collision Detection")]
    [SerializeField] private bool enableCollision = true;
    [SerializeField] private float collisionOffset = 0.3f;
    [SerializeField] private LayerMask collisionLayers;
    
    private float currentDistance;
    private float targetDistance;
    private float currentX = 0f;
    private float currentY = 20f;
    private Vector3 currentVelocity;
    
    private float smoothX = 0f;
    private float smoothY = 20f;
    private Vector2 rotationVelocity;
    
    private Vector3 lastTargetPosition;
    
    private void Start()
    {
        LockCursor();
        
        currentDistance = defaultDistance;
        targetDistance = defaultDistance;
        
        if (target != null)
        {
            Vector3 angles = transform.eulerAngles;
            currentX = angles.y;
            currentY = angles.x;
            smoothX = currentX;
            smoothY = currentY;
            
            lastTargetPosition = target.position + targetOffset;
        }
    }
    
    private void FixedUpdate()
    {
        if (target == null)
        {
            return;
        }
        
        HandleCameraPosition();
    }
    
    private void LateUpdate()
    {
        if (target == null) return;
        
        if (IsGamePaused())
        {
            return;
        }
        
        HandleCursorInput();
        HandleRotation();
        HandleZoom();
    }
    
    private bool IsGamePaused()
    {
        if (GameManager.Instance != null)
        {
            return GameManager.Instance.isGamePaused;
        }
        return false;
    }
    
    private void HandleCursorInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (!IsGamePaused())
            {
                LockCursor();
            }
        }
    }
    
    private void HandleRotation()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        
        currentX += mouseX;
        currentY -= mouseY;
        
        currentY = Mathf.Clamp(currentY, minVerticalAngle, maxVerticalAngle);
        
        smoothX = Mathf.SmoothDampAngle(smoothX, currentX, ref rotationVelocity.x, rotationSmoothTime);
        smoothY = Mathf.SmoothDampAngle(smoothY, currentY, ref rotationVelocity.y, rotationSmoothTime);
    }
    
    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        targetDistance -= scroll * zoomSpeed;
        targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
        
        currentDistance = Mathf.Lerp(currentDistance, targetDistance, Time.deltaTime * 10f);
    }
    
    private void HandleCameraPosition()
    {
        Vector3 targetPosition = target.position + targetOffset;
        targetPosition = Vector3.Lerp(lastTargetPosition, targetPosition, Time.fixedDeltaTime * 60f);
        lastTargetPosition = targetPosition;
        
        Quaternion rotation = Quaternion.Euler(smoothY, smoothX, 0);
        Vector3 direction = rotation * Vector3.back;
        
        Vector3 desiredPosition = targetPosition + direction * currentDistance;
        
        if (enableCollision)
        {
            RaycastHit hit;
            if (Physics.Linecast(targetPosition, desiredPosition, out hit, collisionLayers))
            {
                float adjustedDistance = Vector3.Distance(targetPosition, hit.point) - collisionOffset;
                desiredPosition = targetPosition + direction * Mathf.Max(adjustedDistance, minDistance);
            }
        }
        
        transform.position = Vector3.SmoothDamp(
            transform.position, 
            desiredPosition, 
            ref currentVelocity, 
            positionSmoothTime,
            Mathf.Infinity,
            Time.fixedDeltaTime
        );
        
        transform.LookAt(targetPosition);
    }
    
    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
        {
            lastTargetPosition = target.position + targetOffset;
        }
    }
    
    public void SetDistance(float distance)
    {
        targetDistance = Mathf.Clamp(distance, minDistance, maxDistance);
    }
    
    public void SetSensitivity(float sensitivity)
    {
        mouseSensitivity = sensitivity;
    }
    
    public Vector3 GetCameraDirection()
    {
        return transform.forward;
    }
    
    public float GetCurrentDistance()
    {
        return currentDistance;
    }
}