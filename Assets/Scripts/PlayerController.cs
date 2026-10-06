using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Animator animator;
    public AnimationClip attackAnimation;
    public AnimationClip heavyAttackAnimation; // New field for heavy attack animation
	public AnimationClip healAnimation;

	public AnimationClip attack2Animation;      // assign “Attack2” clip in Inspector
	public AnimationClip attack3Animation;      // assign “Attack3” clip in Inspector


	public bool isStaggered = false;
	public AnimationClip staggerAnimation; // Assign the stagger animation clip in Inspector

	[Header("Combo Attack Settings")]
	[Tooltip("Time (in seconds) after Attack1 starts when we can transition into Attack2.")]
	public float attack2Time = 0.5f;

	[Tooltip("Time (in seconds) after Attack2 starts when we can transition into Attack3.")]
	public float attack3Time = 0.5f;

	// Runtime combo‐tracking:
	private int attackQueue = 0;    // how many extra clicks are buffered

	[Header("Healing Effects")]
	[Tooltip("Particle system that plays during healing")]
	public ParticleSystem healingParticleSystem;

	[Tooltip("Should the particle effect follow the player during healing")]
	public bool particleFollowsPlayer = true;

	[Header("Healing Sound Effects")]
	[Tooltip("Sound that plays during healing")]
	public AudioClip healingSound;

	[Tooltip("Volume of healing sound")]
	[Range(0f, 1f)]
	public float healingSoundVolume = 0.6f;

	[Tooltip("Random pitch variation for healing sound")]
	[Range(0f, 0.3f)]
	public float healingSoundPitchVariation = 0.1f;

	[Tooltip("Should the healing sound loop during the entire heal duration")]
	public bool loopHealingSound = true;

    public float moveSpeed = 5f;
    public float sprintMultiplier = 1.8f; // Speed multiplier when sprinting
    public float rotationSpeed = 10f;
    public Transform cameraTransform;
    public GameObject weapon;
    
    // Reference to the camera script for lock-on status
    private ThirdPersonCamera cameraScript;
    
    // Add reference to the skeleton transform
    public Transform skeletonTransform;
    public Vector3 desiredSkeletonPosition = new Vector3(0, -1, 0);
    public bool isAttacking = false;
	public bool isHeavyAttacking = false;
    public bool isHealing = false;
	public bool gameOver = false;
	
    // Gravity settings
    [Header("Physics Settings")]
    [Tooltip("Gravity force applied to the character")]
    public float gravity = -9.81f;
    
    [Tooltip("Maximum fall speed")]
    public float maxFallSpeed = -20f;
    
    // Heavy attack forward movement parameters
    [Header("Heavy Attack Settings")]
    [Tooltip("Speed at which the character moves forward during heavy attack")]
    public float heavyAttackForwardSpeed = 5f;
    
	
    [Tooltip("How long the character moves forward during heavy attack (in seconds)")]
    public float heavyAttackMovementDuration = 1.0f;
    
    [Tooltip("Whether to continue movement for the entire animation or just for the specified duration")]
    public bool useFullAnimationForMovement = false;
    
    // Stamina System Settings
    [Header("Stamina System")]
    [Tooltip("Stamina cost per second while sprinting")]
    [Range(1f, 50f)]
    public float sprintStaminaCost = 10f;
    
    [Tooltip("Stamina cost for normal attack")]
    [Range(1f, 50f)]
    public float normalAttackStaminaCost = 15f;
    
    [Tooltip("Stamina cost for heavy attack")]
    [Range(1f, 100f)]
    public float heavyAttackStaminaCost = 30f;
    
    [Tooltip("Stamina regeneration per second when not using stamina")]
    [Range(1f, 50f)]
    public float staminaRegenRate = 15f;
    
    [Tooltip("Delay before stamina starts regenerating after use (in seconds)")]
    [Range(0f, 5f)]
    public float staminaRegenDelay = 1.0f;
    
    [Tooltip("Minimum stamina required to start sprinting")]
    [Range(1f, 50f)]
    public float minimumSprintStamina = 10f;
    
    [Tooltip("Enable debug logging for stamina system")]
    public bool debugStamina = false;
    
    // Footstep System Settings
    [Header("Footstep System")]
    [Tooltip("Base distance traveled before playing a footstep")]
    public float footstepDistance = 2.0f;
    
    [Tooltip("Volume of footstep sounds")]
    [Range(0f, 1f)]
    public float footstepVolume = 0.5f;
    
    [Tooltip("Random pitch variation for footstep sounds")]
    [Range(0f, 0.3f)]
    public float footstepPitchVariation = 0.1f;
    
    [Tooltip("Enable debug logging for footstep system")]
    public bool debugFootsteps = false;
    
    // Attack Sound Settings
    [Header("Attack Sound Effects")]
    [Tooltip("Sound that plays during a normal attack")]
    public AudioClip normalAttackSound;
    
    [Tooltip("Delay before playing the normal attack sound (in seconds)")]
    [Range(0f, 1f)]
    public float normalAttackSoundDelay = 0.1f;
    
    [Tooltip("Sound that plays during a heavy attack")]
    public AudioClip heavyAttackSound;
    
    [Tooltip("Delay before playing the heavy attack sound (in seconds)")]
    [Range(0f, 1f)]
    public float heavyAttackSoundDelay = 0.2f;
    
    [Tooltip("Volume of attack sounds")]
    [Range(0f, 1f)]
    public float attackSoundVolume = 0.7f;
    
    [Tooltip("Random pitch variation for attack sounds")]
    [Range(0f, 0.3f)]
    public float attackPitchVariation = 0.1f;
    
	[Header("Equipment References")]
    [Tooltip("Drag your sword GameObject here")]
    [SerializeField] private GameObject swordObject;

    [Tooltip("Drag your shield GameObject here")]
    [SerializeField] private GameObject shieldObject;
	
	[Header("Stagger Sound Effects")]
	[Tooltip("Sound that plays when the player gets staggered")]
	public AudioClip staggerSound;

	[Tooltip("Volume of stagger sound")]
	[Range(0f, 1f)]
	public float staggerSoundVolume = 0.8f;

	[Tooltip("Random pitch variation for stagger sound")]
	[Range(0f, 0.3f)]
	public float staggerSoundPitchVariation = 0.1f;
	
	[Tooltip("How fast the character turns toward rollDir (deg / sec)")]
	public float rollTurnSpeed = 720f;   // ≈½ turn in 0.25 s


	/* ─── Dodge Roll Settings ─────────────────────────────────────────────── */
	[Header("Dodge Roll Settings")]
	[Tooltip("Key the player presses to roll")]
	public KeyCode rollKey = KeyCode.Space;

	[Tooltip("Stamina cost per roll")]
	public float rollStaminaCost = 20f;

	[Tooltip("Must have at least this stamina to roll")]
	public float minimumRollStamina = 20f;

	[Tooltip("Roll speed (units / sec)")]
	public float rollSpeed = 8f;

	[Tooltip("How long input is locked out while rolling (sec)")]
	public float rollLockTime = 0.8f;

	/* runtime state */
	private bool     isRolling = false;   // invulnerability flag / input lock
	private Vector3  rollDir;             // cached world‑space roll direction
	
    // Player Actions
    private bool canMove = true;
    public bool canAttack = true;
    private Vector3 moveDirection = Vector3.zero;
    private CharacterController characterController;
    private bool isSprinting = false;
    private bool wasMovingLastFrame = false; // Track if we were moving last frame
    private bool isInHeavyAttack = false; // New flag to track if in heavy attack
    
    // Add buffer to prevent false idle triggers during key transitions
    private float idleBuffer = 0f;
    private bool isInIdleBuffer = false; // Track if we're currently in buffer period
    private const float IDLE_BUFFER_TIME = 0.1f; // 100ms buffer
    
    // Physics variables
    private Vector3 velocity;
    private bool isGrounded;
    
    // Audio sources
    private AudioSource footstepAudioSource;
    private AudioSource attackAudioSource;
    private AudioClip[] footstepClips;
    private float distanceTraveled = 0f;
    private Vector3 lastPosition;
    
    // Stamina system variables
    private Stats playerStats;
    private float lastStaminaUseTime = 0f;
    private bool isUsingStamina = false;
	private Items playerItems;
	private Coroutine _playerHealCoroutine;
	
	[SerializeField] private SwordCollision swordCollision;
	
    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
		gameOver = false;
        // Get the Stats component
        playerStats = GetComponent<Stats>();
        if (playerStats == null)
        {
            Debug.LogError("Stats component not found on PlayerController! Stamina system will not work.");
        }
		
		playerItems = GetComponent<Items>();
		if (playerItems == null)
		{
			Debug.LogError("Items component not found on PlayerController! Healing will not work.");
		}
        
        // If camera transform is not assigned, try to find the main camera
        if (cameraTransform == null)
        {
            if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
            else
            {
                Debug.LogWarning("No main camera found. Please assign a camera transform.");
            }
        }
		
        if (healingParticleSystem == null)
		{
			Debug.LogWarning("Healing particle system not assigned. Healing will work but without visual effects.");
		}
		else
		{
			// Make sure the particle system starts stopped
			healingParticleSystem.Stop();
		}

		if (healingSound == null)
		{
			Debug.LogWarning("Healing sound is not assigned. Healing will work but without sound effects.");
		}
		
        // Find the skeleton transform if not assigned
        if (skeletonTransform == null)
        {
            skeletonTransform = transform.Find("Skeleton");
            if (skeletonTransform == null)
            {
                Debug.LogWarning("Skeleton transform not found. Please assign it manually.");
            }
        }
        
        // Set the initial position
        if (skeletonTransform != null)
        {
            skeletonTransform.localPosition = desiredSkeletonPosition;
        }
        
        // Initialize footstep system
        InitializeFootsteps();
        
        // Initialize attack sound system
        InitializeAttackSounds();
        
        // Store initial position for distance tracking
        lastPosition = transform.position;
        
        // Find and store reference to the camera script
        if (cameraTransform != null)
        {
            cameraScript = cameraTransform.GetComponent<ThirdPersonCamera>();
            if (cameraScript == null)
            {
                Debug.LogWarning("ThirdPersonCamera script not found on camera. Directional animations when locked on will not work.");
            }
        }
    }
    
    // Initialize the footstep system
    private void InitializeFootsteps()
    {
        // Add an AudioSource component if one doesn't exist
        footstepAudioSource = GetComponent<AudioSource>();
        if (footstepAudioSource == null)
        {
            footstepAudioSource = gameObject.AddComponent<AudioSource>();
            footstepAudioSource.playOnAwake = false;
            footstepAudioSource.spatialBlend = 1.0f; // 3D sound
            footstepAudioSource.volume = footstepVolume;
        }
        
        // Load all footstep sounds from Resources folder
        footstepClips = Resources.LoadAll<AudioClip>("Footsteps");
        
        if (footstepClips.Length == 0)
        {
            Debug.LogWarning("No footstep audio clips found in Assets/Resources/Footsteps. Please add some footstep sounds.");
            // Try to find any audio clips in Resources as fallback
            AudioClip[] allClips = Resources.LoadAll<AudioClip>("");
            if (allClips.Length > 0)
            {
                Debug.Log($"Found {allClips.Length} audio clips in Resources folder. Consider moving footstep sounds to Resources/Footsteps/");
            }
        }
        else
        {
            Debug.Log($"Loaded {footstepClips.Length} footstep sounds.");
            if (debugFootsteps)
            {
                for (int i = 0; i < footstepClips.Length; i++)
                {
                    Debug.Log($"Footstep clip {i}: {footstepClips[i].name}");
                }
            }
        }
    }
    
    // Initialize the attack sound system
	private void InitializeAttackSounds()
	{
		// Add a separate AudioSource component for attack sounds
		attackAudioSource = gameObject.AddComponent<AudioSource>();
		attackAudioSource.playOnAwake = false;
		attackAudioSource.spatialBlend = 1.0f; // 3D sound
		attackAudioSource.volume = attackSoundVolume;
		
		// Check if attack sounds are assigned
		if (normalAttackSound == null)
		{
			Debug.LogWarning("Normal attack sound is not assigned. Please add a sound clip in the inspector.");
		}
		
		if (heavyAttackSound == null)
		{
			Debug.LogWarning("Heavy attack sound is not assigned. Please add a sound clip in the inspector.");
		}
		
		// Add this check for stagger sound
		if (staggerSound == null)
		{
			Debug.LogWarning("Stagger sound is not assigned. Please add a sound clip in the inspector.");
		}
		
		// Add this check for healing sound
		if (healingSound == null)
		{
			Debug.LogWarning("Healing sound is not assigned. Please add a sound clip in the inspector.");
		}
	}
    
    // Update is called once per frame
    void Update()
    {
		if(playerStats.GetHealth() <= 0)
		{
			gameOver = true;
			Debug.Log("GAME OVER");
		}
		/* ── Dodge‑Roll state ───────────────────────────── */
		if (isRolling)
		{
			HandleGravity();                         // still apply gravity
			characterController.Move(velocity * Time.deltaTime);
			return;                                  // swallow every other input
		}
		if (isStaggered)
		{
			// Apply only gravity when staggered
			HandleGravity();
			characterController.Move(velocity * Time.deltaTime);
			return; // Skip all other input processing
		}
		
		if (Input.GetKeyDown(rollKey) && CanStartRoll())
		{
			StartCoroutine(DodgeRoll());
			return;                                  // nothing else this frame
		}
        // Handle stamina regeneration
        HandleStaminaRegeneration();
        
        // Handle grounding and gravity
        HandleGravity();
        
        // Check for WASD keys for movement input detection
        bool isMovementKeyPressed = IsMovementKeyPressed();
        
        // Update sprint state - only allow sprinting if movement keys are pressed, player can move, and has enough stamina
        bool shiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool wantsToSprint = shiftPressed && isMovementKeyPressed && canMove;
        
        // Check if player can start/continue sprinting based on stamina
        bool canSprint = CanSprint();
        isSprinting = wantsToSprint && canSprint;
        
        // If sprinting, consume stamina
        if (isSprinting)
        {
            ConsumeStamina(sprintStaminaCost * Time.deltaTime);
        }
		
        if (Input.GetKeyDown(KeyCode.Q) && canMove && !isAttacking && !isHeavyAttacking && !isHealing)
		{
			// Check if player has health pots available
			if (playerItems != null && playerItems.GetNumPots() > 0)
			{
				_playerHealCoroutine = StartCoroutine(Heal());
			}
			else
			{
				Debug.Log("No health potions available!");
			}
			return; // Skip the rest of the Update function when healing
		}
		
        // Handle footsteps based on actual movement
        if (isGrounded && footstepClips.Length > 0)
        {
            HandleFootsteps();
        }
        
        // Attack input check
        if (Input.GetMouseButtonDown(0))
		{
			if (!isSprinting)
			{
				if (isAttacking)
				{
					// We are already mid‐Attack1 (or Attack2). Buffer one more click:
					if (attackQueue < 2)
						attackQueue++;
						Debug.Log("attackQueue: " + attackQueue);
				}
				else if (canAttack && canMove)
				{
					// Start the very first Attack:
					if (HasEnoughStamina(normalAttackStaminaCost))
						StartCoroutine(Attack());
					else if (debugStamina)
						Debug.Log("Not enough stamina for normal attack!");
				}
			}
			else
			{
				// your existing heavy‐attack branch (unchanged)
				bool isLockedOn = cameraScript != null && cameraScript.GetLockedOnStatus();
				bool isMovingBackwards = Input.GetKey(KeyCode.S);
				if (isLockedOn && isMovingBackwards)
				{
					if (HasEnoughStamina(normalAttackStaminaCost))
						StartCoroutine(Attack());
					else if (debugStamina)
						Debug.Log("Not enough stamina for normal attack!");
				}
				else
				{
					if (HasEnoughStamina(heavyAttackStaminaCost))
						StartCoroutine(HeavyAttack());
					else if (debugStamina)
						Debug.Log("Not enough stamina for heavy attack!");
				}
			}
			return;
		}

        
        // Handle forward movement during heavy attack
        if (isInHeavyAttack)
        {
            // Continue moving forward in current facing direction
            Vector3 forwardMovement = transform.forward * heavyAttackForwardSpeed * Time.deltaTime;
            characterController.Move(forwardMovement + velocity * Time.deltaTime);
        }
        // Only process normal movement if player can move and is not in heavy attack
        else if (canMove)
        {
            // Get explicit WASD key input
            float horizontalInput = 0f;
            float verticalInput = 0f;
            
            // Check for WASD keys
            if (Input.GetKey(KeyCode.W))
                verticalInput += 1f;
            if (Input.GetKey(KeyCode.S))
                verticalInput -= 1f;
            if (Input.GetKey(KeyCode.A))
                horizontalInput -= 1f;
            if (Input.GetKey(KeyCode.D))
                horizontalInput += 1f;
            
            // Only calculate movement if there's input
            bool isMoving = horizontalInput != 0 || verticalInput != 0;
            
            if (isMoving)
            {
                // Reset idle buffer since we're moving
                idleBuffer = 0f;
                isInIdleBuffer = false;
                
                // Create a normalized direction vector based on input
                Vector3 inputDirection = new Vector3(horizontalInput, 0, verticalInput).normalized;
                
                // Calculate movement direction relative to camera
                Vector3 cameraForward = cameraTransform.forward;
                Vector3 cameraRight = cameraTransform.right;
                
                // Flatten camera direction vectors to ignore camera's Y rotation
                cameraForward.y = 0;
                cameraRight.y = 0;
                cameraForward.Normalize();
                cameraRight.Normalize();
                
                // Calculate final move direction based on camera orientation
                moveDirection = (cameraForward * inputDirection.z + cameraRight * inputDirection.x).normalized;
                
                // Handle character rotation based on camera lock-on status
                HandleCharacterRotation(moveDirection);
                
                // Apply movement using CharacterController with sprint multiplier if sprinting
                float currentSpeed = isSprinting ? moveSpeed * sprintMultiplier : moveSpeed;
                Vector3 horizontalMovement = moveDirection * currentSpeed * Time.deltaTime;
                characterController.Move(horizontalMovement + velocity * Time.deltaTime);
                
                // Handle animation transitions elegantly
                HandleMovementAnimations(isMoving, isSprinting, wasMovingLastFrame, inputDirection);
            }
            else
            {
                // Apply only gravity when not moving horizontally
                characterController.Move(velocity * Time.deltaTime);
                
                // Handle idle buffer logic
                if (wasMovingLastFrame)
                {
                    // Just stopped moving, start the idle buffer
                    isInIdleBuffer = true;
                    idleBuffer = 0f;
                }
                
                if (isInIdleBuffer)
                {
                    idleBuffer += Time.deltaTime;
                    
                    // Only trigger idle after buffer time has passed
                    if (idleBuffer >= IDLE_BUFFER_TIME)
                    {
                        // Time to go idle
                        ResetAllMovementTriggers();
                        animator.SetTrigger("Idle");
                        isInIdleBuffer = false;
                        idleBuffer = 0f;
                    }
                    // If still within buffer time, don't change animation
                }
            }
            
            // Update tracking variable for next frame
            wasMovingLastFrame = isMoving;
        }
        else
        {
            // Apply only gravity when movement is disabled
            characterController.Move(velocity * Time.deltaTime);
        }
        
        // Maintain skeleton position - this is the key addition to fix your issue
        if (skeletonTransform != null && skeletonTransform.localPosition != desiredSkeletonPosition)
        {
            skeletonTransform.localPosition = desiredSkeletonPosition;
        }
		
    }
    
    // Stamina system methods
    private bool HasEnoughStamina(float requiredStamina)
    {
        if (playerStats == null) return true; // If no stats component, allow actions
        return playerStats.GetStamina() >= requiredStamina;
    }
    
    private bool CanSprint()
    {
        if (playerStats == null) return true; // If no stats component, allow sprinting
        
        // If already sprinting, allow until stamina is nearly depleted
        if (isSprinting)
        {
            return playerStats.GetStamina() > 0.1f; // Small buffer to prevent negative stamina
        }
        else
        {
            // If not sprinting, require minimum stamina to start
            return playerStats.GetStamina() >= minimumSprintStamina;
        }
    }
    
    private void ConsumeStamina(float amount)
    {
        if (playerStats == null) return;
        
        playerStats.TakeStaminaDamage(amount);
        lastStaminaUseTime = Time.time;
        isUsingStamina = true;
        
        if (debugStamina)
        {
            Debug.Log($"Consumed {amount:F1} stamina. Current: {playerStats.GetStamina():F1}");
        }
        
        // If sprinting and stamina runs too low, stop sprinting
        if (isSprinting && playerStats.GetStamina() <= 0.1f)
        {
            isSprinting = false;
            if (debugStamina) Debug.Log("Stopped sprinting due to depleted stamina");
        }
    }
    
	private void HandleStaminaRegeneration()
	{
		if (playerStats == null) return;
		
		// Check if we should start regenerating stamina
		bool shouldRegen = Time.time - lastStaminaUseTime >= staminaRegenDelay;
		
		if (shouldRegen && !isUsingStamina)
		{
			float regenAmount = staminaRegenRate * Time.deltaTime;
			float currentStamina = playerStats.GetStamina();
			float maxStamina = playerStats.GetMaxStamina(); // Get the maximum stamina value
			
			// Calculate new stamina value and clamp it to not exceed maximum
			float newStamina = Mathf.Min(currentStamina + regenAmount, maxStamina);
			playerStats.SetStamina(newStamina);
			
			if (debugStamina && regenAmount > 0)
			{
				Debug.Log($"Regenerated {newStamina - currentStamina:F1} stamina. Current: {newStamina:F1}/{maxStamina:F1}");
			}
		}
		
		// Reset stamina use flag if not currently using stamina
		// ADD isStaggered to this condition
		if (!isSprinting && !isAttacking && !isHeavyAttacking && !isHealing && !isStaggered && !isRolling)
		{
			isUsingStamina = false;
		}
	}
    
    // Handle gravity and grounding
    private void HandleGravity()
    {
        isGrounded = characterController.isGrounded;
        
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small negative value to keep grounded
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
            // Clamp fall speed
            if (velocity.y < maxFallSpeed)
            {
                velocity.y = maxFallSpeed;
            }
        }
    }
    
    // Handle footstep sound effects based on movement distance
    private void HandleFootsteps()
    {
        // Calculate distance traveled since last frame
        Vector3 currentPosition = transform.position;
        float distanceThisFrame = Vector3.Distance(currentPosition, lastPosition);
        
        // Only count horizontal movement for footsteps (ignore vertical changes from gravity/jumping)
        Vector3 horizontalMovement = new Vector3(currentPosition.x - lastPosition.x, 0, currentPosition.z - lastPosition.z);
        float horizontalDistance = horizontalMovement.magnitude;
        
        // Add to total distance traveled
        distanceTraveled += horizontalDistance;
        
        // Calculate current speed for dynamic footstep timing
        float currentSpeed = 0f;
        
        if (isInHeavyAttack)
        {
            currentSpeed = heavyAttackForwardSpeed;
        }
        else if (moveDirection != Vector3.zero)
        {
            currentSpeed = isSprinting ? moveSpeed * sprintMultiplier : moveSpeed;
        }
        
        // Scale footstep distance with speed (faster movement = more spread out footsteps)
        float dynamicFootstepDistance = footstepDistance * Mathf.Lerp(1.0f, 1.5f, currentSpeed / (moveSpeed * sprintMultiplier));
        
        // Check if we've traveled far enough for a footstep and we're actually moving
        if (distanceTraveled >= dynamicFootstepDistance && currentSpeed > 0.1f && horizontalDistance > 0.01f)
        {
            if (debugFootsteps)
            {
                Debug.Log($"Playing footstep - Distance: {distanceTraveled:F2}, Speed: {currentSpeed:F2}, Grounded: {isGrounded}");
            }
            
            // Play a random footstep sound
            PlayRandomFootstep();
            
            // Reset distance counter (keep remainder for smoother timing)
            distanceTraveled = distanceTraveled % dynamicFootstepDistance;
        }
        
        // Update last position for next frame
        lastPosition = currentPosition;
    }
    
    // Play a random footstep sound from available clips
    private void PlayRandomFootstep()
    {
        if (footstepClips.Length > 0 && footstepAudioSource != null)
        {
            // Select a random footstep clip
            int randomIndex = UnityEngine.Random.Range(0, footstepClips.Length);
            AudioClip randomFootstep = footstepClips[randomIndex];
            
            // Apply random pitch variation
            footstepAudioSource.pitch = 1.0f + UnityEngine.Random.Range(-footstepPitchVariation, footstepPitchVariation);
            
            // Play the selected footstep sound
            footstepAudioSource.PlayOneShot(randomFootstep, footstepVolume);
            
            if (debugFootsteps)
            {
                Debug.Log($"Played footstep: {randomFootstep.name}");
            }
        }
        else if (debugFootsteps)
        {
            Debug.LogWarning("Cannot play footstep - no clips or audio source missing");
        }
    }
    
    // Play attack sound with delay
    private IEnumerator PlayAttackSoundWithDelay(AudioClip soundClip, float delay)
    {
        if (soundClip != null && attackAudioSource != null)
        {
            // Wait for the specified delay
            yield return new WaitForSeconds(delay);
            
            // Apply random pitch variation
            attackAudioSource.pitch = 1.0f + UnityEngine.Random.Range(-attackPitchVariation, attackPitchVariation);
            
            // Play the attack sound
            attackAudioSource.PlayOneShot(soundClip, attackSoundVolume);
        }
    }
    
    // Handle character rotation based on camera lock-on status
    private void HandleCharacterRotation(Vector3 moveDirection)
    {
        // Check if camera is locked onto an enemy
        bool isLockedOn = cameraScript != null && cameraScript.GetLockedOnStatus();
        
        if (isLockedOn)
        {
            // When locked on, face the same direction as the camera (toward the enemy)
            Vector3 cameraForward = cameraTransform.forward;
            // Flatten the camera direction to prevent character from tilting up/down
            cameraForward.y = 0;
            cameraForward.Normalize();
            
            if (cameraForward != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(cameraForward);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
        else
        {
            // Normal behavior: face movement direction
            if (moveDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }
    
    // Handles animation transitions between idle, walk, and run states
    private void HandleMovementAnimations(bool isMoving, bool isSprinting, bool wasMoving, Vector3 inputDirection)
    {
        if (!isMoving)
            return; // This method only handles moving animations
        
        // Check if camera is locked onto an enemy
        bool isLockedOn = cameraScript != null && cameraScript.GetLockedOnStatus();
        
        if (isLockedOn)
        {
            // Use directional animations when locked onto enemy
            HandleLockedOnMovementAnimations(isSprinting, inputDirection);
        }
        else
        {
            // Use normal forward-facing animations
            HandleNormalMovementAnimations(isSprinting);
        }
    }
    
    // Handle normal movement animations (forward-facing)
    private void HandleNormalMovementAnimations(bool isSprinting)
    {
        if (isSprinting)
        {
            // Only change animation if we weren't already running
            if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Run"))
            {
                ResetAllMovementTriggers();
                animator.SetTrigger("Run");
            }
        }
        else // Walking
        {
            // Only change animation if we weren't already walking
            if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Walk"))
            {
                ResetAllMovementTriggers();
                animator.SetTrigger("Walk");
            }
        }
    }
    
    // Handle directional movement animations when locked onto enemy
    private void HandleLockedOnMovementAnimations(bool isSprinting, Vector3 inputDirection)
    {
        // Determine the primary movement direction based on input
        string currentTrigger = GetDirectionalTrigger(inputDirection, isSprinting);
        string currentStateName = GetAnimationStateName(currentTrigger);
        
        // Get current animation state info
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        
        // Only change animation if we're not already playing this animation
        if (!stateInfo.IsName(currentStateName))
        {
            // Check if we're transitioning between similar movement types to avoid idle
            if (ShouldSkipIdleTransition(stateInfo, currentTrigger))
            {
                // Direct transition without resetting all triggers
                animator.SetTrigger(currentTrigger);
            }
            else
            {
                // Full reset for major transitions
                ResetAllMovementTriggers();
                animator.SetTrigger(currentTrigger);
            }
        }
    }
    
    // Check if we should skip idle transition for smoother movement
    private bool ShouldSkipIdleTransition(AnimatorStateInfo currentState, string newTrigger)
    {
        // Get current state name
        string currentStateName = "";
        
        // Check what animation is currently playing
        if (currentState.IsName("WalkLeft") || currentState.IsName("RunLeft"))
            currentStateName = "Left";
        else if (currentState.IsName("WalkRight") || currentState.IsName("RunRight"))
            currentStateName = "Right";
        else if (currentState.IsName("WalkBack") || currentState.IsName("RunBack"))
            currentStateName = "Back";
        else if (currentState.IsName("Walk") || currentState.IsName("Run"))
            currentStateName = "Forward";
        
        // Get new direction
        string newDirection = "";
        if (newTrigger.Contains("Left"))
            newDirection = "Left";
        else if (newTrigger.Contains("Right"))
            newDirection = "Right";
        else if (newTrigger.Contains("Back"))
            newDirection = "Back";
        else if (newTrigger == "Walk" || newTrigger == "Run")
            newDirection = "Forward";
        
        // Skip idle for directional transitions (left to right, etc.)
        return !string.IsNullOrEmpty(currentStateName) && !string.IsNullOrEmpty(newDirection) && 
               currentStateName != newDirection;
    }
    
    // Get the appropriate directional trigger based on input direction and sprint state
    private string GetDirectionalTrigger(Vector3 inputDirection, bool isSprinting)
    {
        // Determine primary direction based on strongest input component
        float absX = Mathf.Abs(inputDirection.x);
        float absZ = Mathf.Abs(inputDirection.z);
        
        string direction;
        
        if (absZ > absX)
        {
            // Forward/Backward movement is stronger
            if (inputDirection.z > 0)
            {
                // Forward movement - use regular Walk/Run
                return isSprinting ? "Run" : "Walk";
            }
            else
            {
                // Backward movement
                direction = "Back";
            }
        }
        else
        {
            // Left/Right movement is stronger
            if (inputDirection.x > 0)
            {
                direction = "Right";
            }
            else
            {
                direction = "Left";
            }
        }
        
        // Combine with sprint state
        return isSprinting ? ("Run" + direction) : ("Walk" + direction);
    }
    
    // Get the animation state name from trigger name
    private string GetAnimationStateName(string triggerName)
    {
        // Animation state names typically match trigger names
        return triggerName;
    }
    
    // Reset all movement-related animation triggers
    private void ResetAllMovementTriggers()
    {
        animator.ResetTrigger("Idle");
        animator.ResetTrigger("Walk");
        animator.ResetTrigger("Run");
        animator.ResetTrigger("WalkLeft");
        animator.ResetTrigger("WalkRight");
        animator.ResetTrigger("WalkBack");
        animator.ResetTrigger("RunLeft");
        animator.ResetTrigger("RunRight");
        animator.ResetTrigger("RunBack");
        animator.ResetTrigger("Attack");
        animator.ResetTrigger("AttackRun");
		animator.ResetTrigger("Heal");
    }
    
    // This ensures the position is maintained even after animations or physics updates
    void LateUpdate()
    {
        // Make sure skeleton position is maintained at all times
        if (skeletonTransform != null)
        {
            skeletonTransform.localPosition = desiredSkeletonPosition;
        }
    }
    
    // Helper method to check if any movement keys are currently pressed
    private bool IsMovementKeyPressed()
    {
        return Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || 
               Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D);
    }
    
    // Helper method to determine the appropriate animation state after an attack
    private void SetPostActionAnimation()
    {
        // Check for movement input immediately and set appropriate animation
        StartCoroutine(SetPostActionAnimationDirect());
    }

    // Coroutine to handle direct post-action animation without going to idle
    private IEnumerator SetPostActionAnimationDirect()
    {
        // Very small buffer to ensure input sampling is stable
        yield return new WaitForSeconds(0.02f);
        
        // Check if any movement keys are pressed
        bool movementPressed = IsMovementKeyPressed();
        
        // Update sprint state - only allow sprinting if movement keys are pressed
        bool shiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool isSprinting = shiftPressed && movementPressed && CanSprint(); // Added stamina check
        
        if (movementPressed)
        {
            // Get explicit WASD key input (same logic as main Update method)
            float horizontalInput = 0f;
            float verticalInput = 0f;
            
            // Check for WASD keys
            if (Input.GetKey(KeyCode.W))
                verticalInput += 1f;
            if (Input.GetKey(KeyCode.S))
                verticalInput -= 1f;
            if (Input.GetKey(KeyCode.A))
                horizontalInput -= 1f;
            if (Input.GetKey(KeyCode.D))
                horizontalInput += 1f;
            
            // Create a normalized direction vector based on input
            Vector3 inputDirection = new Vector3(horizontalInput, 0, verticalInput).normalized;
            
            // Check if camera is locked onto an enemy (same logic as main movement system)
            bool isLockedOn = cameraScript != null && cameraScript.GetLockedOnStatus();
            
            // Only reset movement triggers, not the attack trigger that just finished
            ResetMovementTriggersOnly();
            
            if (isLockedOn)
            {
                // Use directional animations when locked onto enemy
                string directionalTrigger = GetDirectionalTrigger(inputDirection, isSprinting);
                animator.SetTrigger(directionalTrigger);
            }
            else
            {
                // Use normal forward-facing animations
                if (isSprinting)
                {
                    animator.SetTrigger("Run");
                }
                else
                {
                    animator.SetTrigger("Walk");
                }
            }
            
            // Update the tracking variable for consistent behavior and prevent idle buffer system from interfering
            wasMovingLastFrame = true;
            isInIdleBuffer = false;
            idleBuffer = 0f;
        }
        else
        {
            // No movement keys pressed, set to idle
            ResetMovementTriggersOnly();
            animator.SetTrigger("Idle");
            wasMovingLastFrame = false;
        }
    }
	
    IEnumerator Heal()
	{
		Debug.Log("Healing");
		isHealing = true;
		isUsingStamina = true; // Prevent stamina regen during healing
		if (swordObject != null)   swordObject.SetActive(false);
		if (shieldObject != null)  shieldObject.SetActive(false);
		
		// Stop movement immediately
		moveDirection = Vector3.zero;
		
		// Disable movement and attack during animation
		DisableAllActions();
		
		// Reset all animation triggers for a clean transition to heal
		ResetAllMovementTriggers();
		
		// Play heal animation
		animator.SetTrigger("Heal");
		
		// Start healing effects
		StartHealingParticles();
		PlayHealingSound();
		
		// Use the health potion (this also heals the player inside the Items script)
		bool healSuccess = playerItems.UseHealthPot();
		
		if (healSuccess)
		{
			Debug.Log($"Used health potion. Remaining: {playerItems.GetNumPots()}");
		}
		
		// Wait for animation to complete (use a default time if healAnimation is not assigned)
		yield return new WaitForSeconds(2.0f);
		
		// Stop healing effects
		StopHealingParticles();
		StopHealingSound();
		
		// Make sure we reset the heal trigger when done
		animator.ResetTrigger("Heal");
		isHealing = false;
		isUsingStamina = false;
		
		if (swordObject != null)   swordObject.SetActive(true);
		if (shieldObject != null)  shieldObject.SetActive(true);
		
		// Re-enable movement and attack
		EnableAllActions();
		
		// Check for movement input and set appropriate animation
		SetPostActionAnimation();
		
		// Ensure skeleton position is correct after healing
		if (skeletonTransform != null)
		{
			skeletonTransform.localPosition = desiredSkeletonPosition;
		}
	}
	
	public void CancelPlayerHeal(bool isBeingStaggered = false)
	{
		// 1) Stop the smooth heal in Items
		if (playerItems != null)
		{
			playerItems.CancelHeal();
		}

		// 2) Stop the Heal() coroutine in this script
		if (_playerHealCoroutine != null)
		{
			StopCoroutine(_playerHealCoroutine);
			_playerHealCoroutine = null;
		}

		// 3) Stop healing effects when cancelled
		StopHealingParticles();
		StopHealingSound();

		// 4) Reset animation/flags
		animator.ResetTrigger("Heal");
		isHealing = false;
		isUsingStamina = false;
		
		if (swordObject != null)   swordObject.SetActive(true);
		if (shieldObject != null)  shieldObject.SetActive(true);
		
		// Only enable actions and set post-action animation if NOT being staggered
		if (!isBeingStaggered)
		{
			EnableAllActions();
			SetPostActionAnimation();
		}

		// Fix skeleton position
		if (skeletonTransform != null)
		{
			skeletonTransform.localPosition = desiredSkeletonPosition;
		}
	}

	
    // Reset only movement-related triggers, not attack triggers
    private void ResetMovementTriggersOnly()
    {
        animator.ResetTrigger("Idle");
        animator.ResetTrigger("Walk");
        animator.ResetTrigger("Run");
        animator.ResetTrigger("WalkLeft");
        animator.ResetTrigger("WalkRight");
        animator.ResetTrigger("WalkBack");
        animator.ResetTrigger("RunLeft");
        animator.ResetTrigger("RunRight");
        animator.ResetTrigger("RunBack");
        // Don't reset Attack and AttackRun triggers here
    }
    
    IEnumerator Attack()
	{
		Debug.Log("Attacking");
		isAttacking = true;
		
		// Consume stamina for the first attack (Attack1)
		ConsumeStamina(normalAttackStaminaCost);
		
		moveDirection = Vector3.zero;
		DisableAllActions();
		ResetAllMovementTriggers();
		animator.SetTrigger("Attack");
		StartCoroutine(PlayAttackSoundWithDelay(normalAttackSound, normalAttackSoundDelay));

		float timer = 0f;
		float attack1Length = (attackAnimation != null ? attackAnimation.length : 0f);
		float attack2Length = (attack2Animation   != null ? attack2Animation.length   : 0f);
		float attack3Length = (attack3Animation   != null ? attack3Animation.length   : 0f);

		// —— WAIT until we hit the Attack2 window —— 
		while (timer < attack2Time)
		{
			timer += Time.deltaTime;
			yield return null;
		}

		// Check if we have a buffered click and enough stamina for Attack2
		if (attackQueue > 0 && attack2Animation != null && HasEnoughStamina(normalAttackStaminaCost))
		{
			// Consume stamina for Attack2
			ConsumeStamina(normalAttackStaminaCost);
			Debug.Log("Combo → Attack2");
			animator.ResetTrigger("Attack");
			animator.SetTrigger("Attack2");
			swordCollision.ClearHitList();
			StartCoroutine(PlayAttackSoundWithDelay(normalAttackSound, normalAttackSoundDelay));
			attackQueue--;
			timer = 0f;

			// —— WAIT until we hit the Attack3 window —— 
			while (timer < attack3Time)
			{
				timer += Time.deltaTime;
				yield return null;
			}

			// Check if we have another buffered click and enough stamina for Attack3
			if (attackQueue > 0 && attack3Animation != null && HasEnoughStamina(normalAttackStaminaCost))
			{
				// Consume stamina for Attack3
				ConsumeStamina(normalAttackStaminaCost);
				Debug.Log("Combo → Attack3");
				animator.ResetTrigger("Attack2");
				animator.SetTrigger("Attack3");
				swordCollision.ClearHitList();
				StartCoroutine(PlayAttackSoundWithDelay(normalAttackSound, normalAttackSoundDelay));
				attackQueue--;
				// Fully play Attack3 before returning:
				yield return new WaitForSeconds(attack3Length);
			}
			else
			{
				// No Attack3 queued (or not enough stamina) → finish Attack2's remaining time
				yield return new WaitForSeconds(1f);
			}
		}
		else
		{
			// No Attack2 queued (or not enough stamina) → finish Attack1's remaining time
			yield return new WaitForSeconds(attack1Length - attack2Time);
		}

		// Reset all triggers and clear state
		animator.ResetTrigger("Attack");
		animator.ResetTrigger("Attack2");
		animator.ResetTrigger("Attack3");
		isAttacking = false;
		attackQueue = 0;
		EnableAllActions();
		SetPostActionAnimation();

		if (skeletonTransform != null)
			skeletonTransform.localPosition = desiredSkeletonPosition;
	}


    
    // Modified HeavyAttack coroutine to allow forward movement during animation
    IEnumerator HeavyAttack()
    {
        Debug.Log("Heavy Attacking");
        isHeavyAttacking = true;
        
        // Consume stamina for heavy attack
        ConsumeStamina(heavyAttackStaminaCost);
        
        // Disable player input control but don't stop movement
        DisableAllActions();
        isInHeavyAttack = true; // Set the heavy attack flag
        
        // Reset all animation triggers for a clean transition to heavy attack
        ResetAllMovementTriggers();
        
        // Play heavy attack animation
        animator.SetTrigger("AttackRun");
        
        // Start the sound coroutine with delay
        StartCoroutine(PlayAttackSoundWithDelay(heavyAttackSound, heavyAttackSoundDelay));
        
        // Get the total animation length
        float animationLength = heavyAttackAnimation != null ? heavyAttackAnimation.length : attackAnimation.length;
        
        // Handle forward movement for specified duration or throughout the animation
        if (useFullAnimationForMovement)
        {
            // Wait for the full animation to complete (movement happens in Update)
            yield return new WaitForSeconds(animationLength);
        }
        else
        {
            // Move forward only for the specified duration
            float movementTime = Mathf.Min(heavyAttackMovementDuration, animationLength);
            yield return new WaitForSeconds(movementTime);
            
            // Stop the forward movement but continue the animation
            isInHeavyAttack = false;
            
            // Wait for the rest of the animation to complete
            yield return new WaitForSeconds(animationLength - movementTime);
        }
        
        // Make sure we reset the heavy attack trigger when done
        animator.ResetTrigger("AttackRun");
        
        // End heavy attack state (in case we're using full animation time)
        isInHeavyAttack = false;
        isHeavyAttacking = false;
        // Re-enable movement and attack
        EnableAllActions();
        
        // Check for movement input and set appropriate animation
        SetPostActionAnimation();
        
        // Ensure skeleton position is correct after attack
        if (skeletonTransform != null)
        {
            skeletonTransform.localPosition = desiredSkeletonPosition;
        }
    }

    private void DisableAllActions()
    {
        canMove = false;
        canAttack = false;
    }

    private void EnableAllActions()
    {
        canMove = true;
        canAttack = true;
    }
    
    public bool IsSprinting()
    {
        return isSprinting;
    }
	public bool IsAttacking()
    {
        return isAttacking;
    }
	public bool IsHeavyAttacking()
    {
        return isHeavyAttacking;
    }
    
	// Public method to get current stamina percentage (useful for UI)
	public float GetStaminaPercentage()
	{
		if (playerStats == null) return 1f;
		
		float currentStamina = playerStats.GetStamina();
		float maxStamina = playerStats.GetMaxStamina();
		
		// Prevent division by zero
		if (maxStamina <= 0) return 1f;
		
		return currentStamina / maxStamina;
	}
	
	public void TriggerStagger()
	{
		if (!isStaggered) // Prevent multiple staggers
		{
			StartCoroutine(Stagger());
		}
	}

	private IEnumerator Stagger()
	{
		Debug.Log("Player staggered!");
		isStaggered = true;
		isUsingStamina = true; // Stop stamina regeneration
		
		// Cancel any ongoing actions FIRST - this stops healing sound
		if (_playerHealCoroutine != null)
		{
			CancelPlayerHeal(true); // Pass true to prevent re-enabling actions
		}
		
		// NOW play stagger sound after healing sound is stopped
		PlayStaggerSound();
		
		// Reset attack state and buffer
		isAttacking = false;
		isHeavyAttacking = false;
		attackQueue = 0;  // Reset the attack buffer
		
		// Stop all movement
		moveDirection = Vector3.zero;
		velocity = Vector3.zero;
		isInHeavyAttack = false;
		isSprinting = false;
		
		// Reset idle buffer
		idleBuffer = 0f;
		isInIdleBuffer = false;
		
		// Disable all player actions
		DisableAllActions();
		
		// Reset all animation triggers for clean transition
		ResetAllMovementTriggers();
		animator.ResetTrigger("Attack");
		animator.ResetTrigger("Attack2");
		animator.ResetTrigger("Attack3");
		animator.ResetTrigger("AttackRun");
		animator.ResetTrigger("Heal");
		
		// Play stagger animation
		animator.SetTrigger("Stagger1");
		
		// Wait for stagger animation to complete
		float staggerDuration = staggerAnimation != null ? staggerAnimation.length : 1.0f;
		yield return new WaitForSeconds(staggerDuration);
		
		// Reset stagger trigger
		animator.ResetTrigger("Stagger1");
		
		// Re-enable all actions
		isStaggered = false;
		isUsingStamina = false;
		lastStaminaUseTime = Time.time;
		EnableAllActions();
		
		// Set appropriate post-stagger animation
		SetPostActionAnimation();
		
		// Ensure skeleton position is correct
		if (skeletonTransform != null)
		{
			skeletonTransform.localPosition = desiredSkeletonPosition;
		}
	}
	
	private void PlayStaggerSound()
	{
		if (staggerSound != null && attackAudioSource != null)
		{
			// Apply random pitch variation
			float originalPitch = attackAudioSource.pitch;
			attackAudioSource.pitch = 1.0f + UnityEngine.Random.Range(-staggerSoundPitchVariation, staggerSoundPitchVariation);
			
			// Play the stagger sound
			attackAudioSource.PlayOneShot(staggerSound, staggerSoundVolume);
			
			// Restore original pitch for other sounds
			attackAudioSource.pitch = originalPitch;
		}
		else
		{
			Debug.LogWarning("Cannot play stagger sound - sound clip or audio source missing");
		}
	}
	
	private void StartHealingParticles()
	{
		if (healingParticleSystem != null)
		{
			// Position the particle system if it should follow player
			if (particleFollowsPlayer)
			{
				healingParticleSystem.transform.position = transform.position;
				healingParticleSystem.transform.SetParent(transform);
			}
			
			// Start the particle effect
			healingParticleSystem.Play();
			Debug.Log("Started healing particle effect");
		}
	}
	
	private void StopHealingParticles()
	{
		if (healingParticleSystem != null)
		{
			// Stop emitting new particles but let existing ones finish
			healingParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
			
			// Unparent if it was following player
			if (particleFollowsPlayer && healingParticleSystem.transform.parent == transform)
			{
				healingParticleSystem.transform.SetParent(null);
			}
			
			Debug.Log("Stopped healing particle effect");
		}
	}
	
	private void StopHealingSound()
	{
		if (attackAudioSource != null)
		{
			if (loopHealingSound && attackAudioSource.isPlaying && attackAudioSource.clip == healingSound)
			{
				attackAudioSource.Stop();
				attackAudioSource.loop = false;
				attackAudioSource.clip = null;
				Debug.Log("Stopped healing sound effect");
			}
		}
	}
	
	private void PlayHealingSound()
	{
		if (healingSound != null && attackAudioSource != null)
		{
			// Apply random pitch variation
			attackAudioSource.pitch = 1.0f + UnityEngine.Random.Range(-healingSoundPitchVariation, healingSoundPitchVariation);
			
			if (loopHealingSound)
			{
				// For looping sound during entire heal duration
				attackAudioSource.clip = healingSound;
				attackAudioSource.volume = healingSoundVolume;
				attackAudioSource.loop = true;
				attackAudioSource.Play();
			}
			else
			{
				// For one-shot sound at start of healing
				attackAudioSource.PlayOneShot(healingSound, healingSoundVolume);
			}
			
			Debug.Log("Started healing sound effect");
		}
		else
		{
			Debug.LogWarning("Cannot play healing sound - sound clip or audio source missing");
		}
	}
	public bool getGameOverState()
	{
		return gameOver;
	}
	private bool CanStartRoll()
	{
		return !isRolling &&
			   !isAttacking && !isHeavyAttacking &&
			   !isHealing   && !isStaggered &&
			   playerStats.GetStamina() >= Mathf.Max(rollStaminaCost, minimumRollStamina);
	}
	
	private IEnumerator DodgeRoll()
	{
		/* ────────── SET‑UP ─────────────────────────────────────── */
		isRolling = true;
		ConsumeStamina(rollStaminaCost);

		DisableAllActions();            // block movement / attack inputs
		ResetAllMovementTriggers();     // clear latent Walk / Run triggers
		animator.SetTrigger("Roll");

		/* ────────── PICK DIRECTION ─────────────────────────────── */
		Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f,
									Input.GetAxisRaw("Vertical")).normalized;

		if (input.sqrMagnitude > 0.01f)
		{
			Vector3 camF = cameraTransform.forward; camF.y = 0f;
			Vector3 camR = cameraTransform.right;   camR.y = 0f;
			rollDir = (camF * input.z + camR * input.x).normalized;
		}
		else
		{
			rollDir = transform.forward;           // default: straight ahead
		}

		/* ────────── ORIENT character toward rollDir ───────────── */
		Quaternion targetRot = Quaternion.LookRotation(rollDir, Vector3.up);

		/* ────────── MOVE for the whole lock‑time ──────────────── */
		float elapsed = 0f;
		while (elapsed < rollLockTime)
		{
			/* smooth turn toward rollDir (rollTurnSpeed is deg / sec) */
			transform.rotation = Quaternion.RotateTowards(
				transform.rotation,
				targetRot,
				rollTurnSpeed * Time.deltaTime);

			/* translate */
			characterController.Move(rollDir * rollSpeed * Time.deltaTime);

			/* gravity */
			HandleGravity();
			characterController.Move(velocity * Time.deltaTime);

			elapsed += Time.deltaTime;
			yield return null;
		}

		/* ────────── CLEAN‑UP ───────────────────────────────────── */
		animator.ResetTrigger("Roll");      // ready for next roll
		isRolling = false;
		EnableAllActions();
		SetPostActionAnimation();           // Idle / Walk / Run as appropriate
	}
	
	public bool IsRolling() => isRolling;
	
}