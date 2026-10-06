using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SwordCollision : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag your Player (with PlayerController) here")]
    [SerializeField] private PlayerController playerController;
    
    [Tooltip("Tag used to identify enemies")]
    [SerializeField] private string enemyTag = "Enemy";
    
    [Header("Damage Settings")]
    [Tooltip("Damage dealt by a normal attack")]
    [SerializeField] private float damageAmount = 10f;
    
    [Tooltip("Damage dealt by a heavy attack")]
    [SerializeField] private float heavyDamageAmount = 20f;
    
    [Header("Overlap Delay Settings")]
    [Tooltip("Delay before damaging enemies that are already overlapping when attack starts")]
    [SerializeField] private float overlapDamageDelay = 0.15f;
    
    [Tooltip("Enable debug logging for overlap delays")]
    [SerializeField] private bool debugOverlapDelay = false;
    
    [Header("Hit Sound Effects")]
    [Tooltip("Sound that plays when hitting an enemy with a normal attack")]
    [SerializeField] private AudioClip normalHitSound;
    
    [Tooltip("Sound that plays when hitting an enemy with a heavy attack")]
    [SerializeField] private AudioClip heavyHitSound;
    
    [Tooltip("Volume of hit sounds")]
    [Range(0f, 1f)]
    [SerializeField] private float hitSoundVolume = 0.7f;
    
    [Tooltip("Random pitch variation for hit sounds")]
    [Range(0f, 0.3f)]
    [SerializeField] private float hitPitchVariation = 0.1f;
    
    [Tooltip("Enable debug logging for hit sounds")]
    [SerializeField] private bool debugHitSounds = false;
    
    // Track which enemies we've already hit in this swing
    private HashSet<Stats> _hitThisAttack = new HashSet<Stats>();
    private HashSet<Stats> _pendingDelayedHits = new HashSet<Stats>();
    private bool _wasAttacking = false;
    private bool _wasHeavyAttacking = false;
    
    // Audio source for hit sounds
    private AudioSource hitAudioSource;
    
    // Collider component for overlap checks
    private Collider swordCollider;
    
    // Coroutine reference for overlap delay
    private Coroutine overlapDelayCoroutine = null;
    
    // Track if we need to check overlaps on next attack
    private bool _shouldCheckOverlapsNextAttack = false;
    
    private void Start()
    {
        InitializeHitSounds();
        
        // Get the collider component
        swordCollider = GetComponent<Collider>();
        if (swordCollider == null)
        {
            Debug.LogError("[SwordCollision] No Collider component found on sword!");
        }
    }
    
    // Initialize the hit sound system
    private void InitializeHitSounds()
    {
        // Add an AudioSource component if one doesn't exist
        hitAudioSource = GetComponent<AudioSource>();
        if (hitAudioSource == null)
        {
            hitAudioSource = gameObject.AddComponent<AudioSource>();
            hitAudioSource.playOnAwake = false;
            hitAudioSource.spatialBlend = 1.0f; // 3D sound
            hitAudioSource.volume = hitSoundVolume;
        }
        
        // Check if hit sounds are assigned
        if (normalHitSound == null)
        {
            Debug.LogWarning("[SwordCollision] Normal hit sound is not assigned. Please add a sound clip in the inspector.");
        }
        
        if (heavyHitSound == null)
        {
            Debug.LogWarning("[SwordCollision] Heavy hit sound is not assigned. Please add a sound clip in the inspector.");
        }
        
        if (debugHitSounds)
        {
            Debug.Log("[SwordCollision] Hit sound system initialized successfully.");
        }
    }
    
    // Play hit sound based on attack type
    private void PlayHitSound(bool isHeavyAttack)
    {
        if (hitAudioSource == null) return;
        
        AudioClip soundToPlay = isHeavyAttack ? heavyHitSound : normalHitSound;
        
        if (soundToPlay != null)
        {
            // Apply random pitch variation
            hitAudioSource.pitch = 1.0f + Random.Range(-hitPitchVariation, hitPitchVariation);
            
            // Play the hit sound
            hitAudioSource.PlayOneShot(soundToPlay, hitSoundVolume);
            
            if (debugHitSounds)
            {
                string attackType = isHeavyAttack ? "Heavy" : "Normal";
                Debug.Log($"[SwordCollision] Played {attackType} hit sound: {soundToPlay.name}");
            }
        }
        else
        {
            if (debugHitSounds)
            {
                string attackType = isHeavyAttack ? "Heavy" : "Normal";
                Debug.LogWarning($"[SwordCollision] {attackType} hit sound is not assigned!");
            }
        }
    }
    
    private void Update()
    {
        if (playerController == null) return;
        
        bool isAttacking = playerController.IsAttacking();
        bool isHeavyAttacking = playerController.IsHeavyAttacking();
        
        // If a new normal or heavy attack just started
        if ((isAttacking && !_wasAttacking) ||
            (isHeavyAttacking && !_wasHeavyAttacking))
        {
            // Stop any pending delayed hits from previous attack
            if (overlapDelayCoroutine != null)
            {
                StopCoroutine(overlapDelayCoroutine);
                overlapDelayCoroutine = null;
            }
            
            _hitThisAttack.Clear();
            _pendingDelayedHits.Clear();
            Debug.Log("[SwordCollision] New attack started, cleared hit list");
            
            // Check for enemies already overlapping when attack starts
            CheckOverlappingEnemiesWithDelay(isHeavyAttacking);
            _shouldCheckOverlapsNextAttack = false;
        }
        // Check if we need to apply overlaps after ClearHitList was called
        else if (_shouldCheckOverlapsNextAttack && (isAttacking || isHeavyAttacking))
        {
            CheckOverlappingEnemiesWithDelay(isHeavyAttacking);
            _shouldCheckOverlapsNextAttack = false;
        }
        // If attack ended, clean up
        else if (!isAttacking && !isHeavyAttacking && (_wasAttacking || _wasHeavyAttacking))
        {
            if (overlapDelayCoroutine != null)
            {
                StopCoroutine(overlapDelayCoroutine);
                overlapDelayCoroutine = null;
            }
            _pendingDelayedHits.Clear();
        }
        
        _wasAttacking = isAttacking;
        _wasHeavyAttacking = isHeavyAttacking;
    }
    
    // Check for enemies that are already overlapping with the sword and apply delay
    private void CheckOverlappingEnemiesWithDelay(bool isHeavyAttack)
    {
        if (swordCollider == null) return;
        
        // Get all overlapping colliders
        Collider[] overlaps = Physics.OverlapBox(
            swordCollider.bounds.center,
            swordCollider.bounds.extents,
            transform.rotation
        );
        
        List<Stats> overlappingEnemies = new List<Stats>();
        
        foreach (Collider other in overlaps)
        {
            // Skip self
            if (other == swordCollider) continue;
            
            // Only process enemies
            if (!other.CompareTag(enemyTag)) continue;
            
            Stats enemyStats = other.GetComponent<Stats>();
            if (enemyStats != null && !_hitThisAttack.Contains(enemyStats))
            {
                overlappingEnemies.Add(enemyStats);
                _pendingDelayedHits.Add(enemyStats);
                
                if (debugOverlapDelay)
                {
                    Debug.Log($"[SwordCollision] Enemy {other.name} is overlapping at attack start. Will damage after {overlapDamageDelay}s delay.");
                }
            }
        }
        
        // Start coroutine to apply delayed damage if we found overlapping enemies
        if (overlappingEnemies.Count > 0)
        {
            overlapDelayCoroutine = StartCoroutine(ApplyDelayedDamage(overlappingEnemies, isHeavyAttack));
        }
    }
    
    // Coroutine to apply damage after delay
    private IEnumerator ApplyDelayedDamage(List<Stats> enemies, bool isHeavyAttack)
    {
        yield return new WaitForSeconds(overlapDamageDelay);
        
        // Check if still attacking (attack might have ended during delay)
        bool stillAttacking = playerController.IsAttacking() || playerController.IsHeavyAttacking();
        if (!stillAttacking)
        {
            if (debugOverlapDelay)
            {
                Debug.Log("[SwordCollision] Attack ended during overlap delay. Cancelling delayed damage.");
            }
            yield break;
        }
        
        // Apply damage to all overlapping enemies
        foreach (Stats enemyStats in enemies)
        {
            // Check if enemy is still valid and we haven't hit them yet
            if (enemyStats != null && !_hitThisAttack.Contains(enemyStats) && _pendingDelayedHits.Contains(enemyStats))
            {
                ApplyDamageToEnemy(enemyStats, isHeavyAttack);
                _pendingDelayedHits.Remove(enemyStats);
                
                if (debugOverlapDelay)
                {
                    Debug.Log($"[SwordCollision] Applied delayed damage to {enemyStats.name}");
                }
            }
        }
        
        overlapDelayCoroutine = null;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log($"[SwordCollision] TriggerEnter with: {other.name}");
        if (playerController == null)
        {
            //Debug.LogError("[SwordCollision] PlayerController reference is missing!");
            return;
        }
        
        bool isAttacking = playerController.IsAttacking();
        bool isHeavyAttacking = playerController.IsHeavyAttacking();
        
        // Only proceed if either attack is active
        if (!isAttacking && !isHeavyAttacking)
        {
            //Debug.Log($"[SwordCollision] Not attacking — ignoring collision with {other.name}");
            return;
        }
        
        ProcessHit(other, isHeavyAttacking);
    }
    
    // Also handle OnTriggerStay for continuous checks
    private void OnTriggerStay(Collider other)
    {
        if (playerController == null) return;
        
        bool isAttacking = playerController.IsAttacking();
        bool isHeavyAttacking = playerController.IsHeavyAttacking();
        
        // Only proceed if either attack is active
        if (!isAttacking && !isHeavyAttacking) return;
        
        ProcessHit(other, isHeavyAttacking);
    }
    
    // Process a potential hit on an enemy
    private void ProcessHit(Collider other, bool isHeavyAttack)
    {
        // Only hit gameObjects tagged as enemies
        if (!other.CompareTag(enemyTag))
        {
            //Debug.Log($"[SwordCollision] {other.name} is not tagged '{enemyTag}' — ignoring");
            return;
        }
        
        // Fetch the shared Stats component on the enemy
        Stats enemyStats = other.GetComponent<Stats>();
        if (enemyStats == null)
        {
            //Debug.LogWarning($"[SwordCollision] No Stats component on {other.name}");
            return;
        }
        
        // Skip if we've already hit this enemy in this swing
        if (_hitThisAttack.Contains(enemyStats))
        {
            //Debug.Log($"[SwordCollision] Already hit {other.name} this attack — skipping");
            return;
        }
        
        // Skip if this enemy is pending a delayed hit (was overlapping at attack start)
        if (_pendingDelayedHits.Contains(enemyStats))
        {
            if (debugOverlapDelay)
            {
                Debug.Log($"[SwordCollision] {other.name} is pending delayed damage — skipping immediate hit");
            }
            return;
        }
        
        // Apply damage immediately since this enemy entered the trigger after attack started
        ApplyDamageToEnemy(enemyStats, isHeavyAttack);
    }
    
    // Apply damage to an enemy
    private void ApplyDamageToEnemy(Stats enemyStats, bool isHeavyAttack)
    {
        // Determine damage amount
        float appliedDamage = isHeavyAttack 
                                ? heavyDamageAmount 
                                : damageAmount;
        string attackType = isHeavyAttack 
                                ? "Heavy" 
                                : "Normal";
        
        // Apply damage
        float before = enemyStats.GetHealth();
        enemyStats.TakeHealthDamage(appliedDamage);
        float after = enemyStats.GetHealth();
        
        // Play hit sound when damage is successfully applied
        PlayHitSound(isHeavyAttack);
        
        //Debug.Log($"[SwordCollision] {attackType} attack: dealt {appliedDamage} damage to {enemyStats.name}. Health: {before} → {after}");
        
        // Mark this enemy as hit for this attack swing
        _hitThisAttack.Add(enemyStats);
    }
    
    public void ClearHitList()
    {
        _hitThisAttack.Clear();
        _pendingDelayedHits.Clear();
        
        if (overlapDelayCoroutine != null)
        {
            StopCoroutine(overlapDelayCoroutine);
            overlapDelayCoroutine = null;
        }
        
        // Mark that we should check for overlaps on the next Update if still attacking
        _shouldCheckOverlapsNextAttack = true;
        
        if (debugOverlapDelay)
        {
            Debug.Log("[SwordCollision] ClearHitList called - will check overlaps next frame if still attacking");
        }
    }
    
    private void OnDisable()
    {
        // Clean up when the sword is disabled
        if (overlapDelayCoroutine != null)
        {
            StopCoroutine(overlapDelayCoroutine);
            overlapDelayCoroutine = null;
        }
    }
}