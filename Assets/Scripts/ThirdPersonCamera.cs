using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;                // The player object to follow
    public Vector3 targetOffset = new Vector3(0, 1.5f, 0); // Offset from target's position (e.g., to focus on player's head)
    
    [Header("Distance Settings")]
    public float cameraDistance = 5.0f;     // Fixed distance from target
    
    [Header("Orbit Settings")]
    public float orbitSpeed = 3.0f;         // Mouse orbit sensitivity
    private float currentX = 0.0f;          // Current X rotation
    private float currentY = 0.0f;          // Current Y rotation
    public float yMinLimit = -20.0f;        // Minimum vertical angle
    public float yMaxLimit = 80.0f;         // Maximum vertical angle
    
    [Header("Smoothing")]
    public float smoothTime = 0.1f;         // How quickly the camera moves to target position
    private Vector3 velocity = Vector3.zero;
    
    [Header("Sprint FOV Settings")]
    public float sprintFOVIncrease = 10f;   // Amount to increase FOV when sprinting
    public float fovChangeSpeed = 4f;       // Speed of FOV transition
    private float defaultFOV;               // Original camera FOV
    private float targetFOV;                // Target FOV based on sprint state
    private Camera cameraComponent;         // Reference to the camera component
    
    [Header("Mouse Settings")]
    public bool lockCursor = true;          // Whether to lock the cursor
    public KeyCode unlockCursorKey = KeyCode.Escape; // Key to unlock the cursor temporarily
    
    [Header("Lock-On Settings")]
    public float lockOnRotationSpeed = 8f;  // Speed of camera rotation when locking on
    public GameObject lockOnIndicator;      // UI object to show/hide and lock onto
    
    // Lock-on state
    private bool isLockedOn = false;
    private float lockOnTargetX = 0f;       // Target rotation when locked on
    private float lockOnTargetY = 0f;       // Target rotation when locked on
    
    // Camera collision settings
    [Header("Collision Settings")]
    public bool enableCollision = true;     // Enable camera collision
    public float collisionRadius = 0.2f;    // Radius of collision detection
    public LayerMask collisionLayers = -1;  // Layers to check for collision
    
	[Header("Lock‑On Range")]
	[Tooltip("Maximum distance (in metres) at which a target can be locked on.")]
	public float maxLockOnDistance = 50f;
	
    // Reference to the player controller to check sprint state
    private PlayerController playerController;
    
    void Start()
    {
        // Set initial rotation angles based on camera's initial position
        Vector3 angles = transform.eulerAngles;
        currentX = angles.y;
        currentY = angles.x;
        
        // Initialize cursor lock
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        // Check if target is assigned
        if (target == null)
        {
            Debug.LogError("ThirdPersonCamera: No target assigned. Please assign a player transform.");
        }
        else
        {
            // Try to get the PlayerController from the target
            playerController = target.GetComponent<PlayerController>();
            if (playerController == null)
            {
                Debug.LogWarning("PlayerController not found on target. Sprint FOV adjustments will not work.");
            }
        }
        
        // Get the camera component and store the default FOV
        cameraComponent = GetComponent<Camera>();
        if (cameraComponent != null)
        {
            defaultFOV = cameraComponent.fieldOfView;
            targetFOV = defaultFOV;
        }
        else
        {
            Debug.LogError("No Camera component found on this GameObject. FOV adjustments will not work.");
        }
        
        // Initialize lock-on indicator as disabled
        if (lockOnIndicator != null)
        {
            lockOnIndicator.SetActive(false);
        }
    }
    
    void LateUpdate()
    {
		
		if (isLockedOn && lockOnIndicator == null)
		{
			isLockedOn      = false;
			lockOnIndicator = null;   // <— add
		}
		
		if (isLockedOn && lockOnIndicator != null && Vector3.Distance(target.position, lockOnIndicator.transform.position) > maxLockOnDistance)
		{
			ToggleLockOn();        // cleanly turn it off
			return;                // skip the rest this frame
		}
		
		if (target == null)
            return;
            
        // Handle lock-on toggle with middle mouse button
        if (Input.GetMouseButtonDown(2)) // Middle mouse button
        {
            ToggleLockOn();
        }
        
        // Handle camera rotation
        if (isLockedOn && lockOnIndicator != null)
        {
            // Lock-on mode: smoothly rotate to look at indicator
            UpdateLockOnRotation();
        }
        else
        {
            // Normal mode: handle mouse input for orbit (only if cursor is locked)
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                currentX += Input.GetAxis("Mouse X") * orbitSpeed;
                currentY -= Input.GetAxis("Mouse Y") * orbitSpeed;
                
                // Clamp vertical rotation
                currentY = Mathf.Clamp(currentY, yMinLimit, yMaxLimit);
            }
        }
        
        // Calculate desired camera position
        Vector3 targetPosition = target.position + targetOffset;
        
        // Convert the camera's orbit angles to a rotation
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
        
        // Calculate the camera position based on the target position, rotation and distance
        Vector3 desiredPosition = targetPosition - (rotation * Vector3.forward * cameraDistance);
        
        // Handle camera collision
        if (enableCollision)
        {
            RaycastHit hit;
            Vector3 direction = desiredPosition - targetPosition;
            if (Physics.SphereCast(targetPosition, collisionRadius, direction.normalized, out hit, 
                                  direction.magnitude, collisionLayers))
            {
                // If camera would hit something, adjust the distance
                float adjustedDistance = hit.distance * 0.8f; // 80% of hit distance for some padding
                desiredPosition = targetPosition + direction.normalized * adjustedDistance;
            }
        }
        
        // Smoothly move the camera to the desired position
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
        
        // Always look at the target (player)
        transform.LookAt(targetPosition);
        
        // Update FOV based on player's sprint state
        UpdateCameraFOV();
    }
    
    // Toggle lock-on functionality
	private void ToggleLockOn()
	{
		// ───────── LOCK‑ON ON ─────────
		if (!isLockedOn)
		{
			// Always search for the nearest valid indicator
			GameObject nearest = FindNearestLockTarget();
			if (nearest == null)
			{
				Debug.Log("No lock targets in range");
				return;                        // nothing to lock onto
			}

			lockOnIndicator = nearest;
			lockOnIndicator.SetActive(true);

			isLockedOn = true;
			CalculateLockOnRotation();

			Debug.Log("Lock‑on enabled – looking at indicator");
		}
		// ───────── LOCK‑ON OFF ────────
		else
		{
			if (lockOnIndicator != null)
				lockOnIndicator.SetActive(false);

			lockOnIndicator = null;            // <— clear reference
			isLockedOn      = false;

			Debug.Log("Lock‑on disabled");
		}
	}
    
    // Find the nearest inactive "Lock" object within camera view
    private GameObject FindNearestLockTarget()
	{
		GameObject best   = null;
		float      bestSq = float.MaxValue;

		// ❶  Gather every potential enemy you want to lock onto
		GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
		if (enemies.Length == 0) return null;

		foreach (GameObject enemy in enemies)
		{
			// Skip if enemy is too far away
			if (Vector3.Distance(target.position, enemy.transform.position) > maxLockOnDistance)
				continue;

			// ❷  Find the indicator somewhere under that enemy
			Transform indicator = FindIndicatorRecursive(enemy.transform);
			if (indicator == null) continue;

			// ❸  Is it in front of (and inside) the camera view?
			Vector3 vp = cameraComponent.WorldToViewportPoint(indicator.position);
			if (vp.z < 0f || vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f)
				continue;

			// ❹  Pick the one closest to screen centre (0.5, 0.5)
			Vector2 offset   = new Vector2(vp.x - 0.5f, vp.y - 0.5f);
			float    distSq  = offset.sqrMagnitude;
			if (distSq < bestSq)
			{
				best   = indicator.gameObject;
				bestSq = distSq;
			}
		}
		return best;
	}
    
    // Calculate the rotation needed to look at the lock-on indicator
    private void CalculateLockOnRotation()
    {
        if (lockOnIndicator == null) return;
        
        Vector3 playerPosition = target.position + targetOffset;
        Vector3 indicatorPosition = lockOnIndicator.transform.position;
        
        // Calculate the direction from player to indicator
        Vector3 directionToIndicator = indicatorPosition - playerPosition;
        
        // Convert to rotation angles
        lockOnTargetY = -Mathf.Atan2(directionToIndicator.y, 
                                    Mathf.Sqrt(directionToIndicator.x * directionToIndicator.x + directionToIndicator.z * directionToIndicator.z)) * Mathf.Rad2Deg;
        lockOnTargetX = Mathf.Atan2(directionToIndicator.x, directionToIndicator.z) * Mathf.Rad2Deg;
        
        // Clamp vertical rotation
        lockOnTargetY = Mathf.Clamp(lockOnTargetY, yMinLimit, yMaxLimit);
    }
    
    // Update camera rotation when locked on
    private void UpdateLockOnRotation()
    {
        if (lockOnIndicator == null) return;
        
        // Recalculate target rotation each frame to track if the indicator moves
        CalculateLockOnRotation();
        
        // Smoothly rotate towards the target rotation
        currentX = Mathf.LerpAngle(currentX, lockOnTargetX, lockOnRotationSpeed * Time.deltaTime);
        currentY = Mathf.Lerp(currentY, lockOnTargetY, lockOnRotationSpeed * Time.deltaTime);
    }
    
    // Update the camera FOV based on the player's sprint state
    private void UpdateCameraFOV()
    {
        if (cameraComponent == null || playerController == null)
            return;
            
        // Check if player is sprinting - accessing a public field from PlayerController
        bool isSprinting = playerController.IsSprinting();
        
        // Set target FOV based on sprint state
        targetFOV = isSprinting ? defaultFOV + sprintFOVIncrease : defaultFOV;
        
        // Smoothly transition to target FOV
        if (Mathf.Abs(cameraComponent.fieldOfView - targetFOV) > 0.1f)
        {
            cameraComponent.fieldOfView = Mathf.Lerp(cameraComponent.fieldOfView, targetFOV, fovChangeSpeed * Time.deltaTime);
        }
    }
    
    // Public method to check lock-on status
    public bool GetLockedOnStatus()
    {
        return isLockedOn;
    }
    
    // Public method to get the lock-on indicator
    public GameObject GetLockOnIndicator()
    {
        return lockOnIndicator;
    }
    
    // Helper method to visualize collision sphere in the editor
    void OnDrawGizmosSelected()
    {
        if (target != null && enableCollision)
        {
            Gizmos.color = Color.red;
            Vector3 targetPosition = target.position + targetOffset;
            Gizmos.DrawWireSphere(targetPosition, collisionRadius);
        }
        
        // Draw line to lock-on indicator when locked on
        if (isLockedOn && lockOnIndicator != null && target != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(target.position + targetOffset, lockOnIndicator.transform.position);
        }
    }
	
	private Transform FindIndicatorRecursive(Transform root)
	{
		foreach (Transform child in root)
		{
			if (child.name == "LockOnCircle" || child.CompareTag("Lock"))
				return child;

			Transform sub = FindIndicatorRecursive(child);
			if (sub != null) return sub;
		}
		return null;
	}
}