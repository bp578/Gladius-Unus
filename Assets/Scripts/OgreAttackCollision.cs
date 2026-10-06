// ClubCollision.cs
using System.Collections.Generic;
using UnityEngine;

public class ClubCollision : MonoBehaviour
{
    [Header("Damage Settings")]
    public float damageAmount = 15f;
    
    [Header("Hit Limiting")]
    public float hitCooldown = 1f;
    
    [Header("Target Settings")]
    public string playerTag = "Player";
    
    private Dictionary<Stats, float> _lastHitTimes = new Dictionary<Stats, float>();
	[Header("Owner (drag the enemy’s root here)")]
    [SerializeField] private MonoBehaviour ownerAI;   // OgreAI **or** KnightAI
	
    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log($"[ClubCollision] TriggerEnter with: {other.name}");
        PlayerController pc = other.GetComponent<PlayerController>();
		
		if (ownerAI != null)
        {
            if (ownerAI is OgreAI  ogre   && ogre.IsDead())   return;
            if (ownerAI is KnightAI knight && knight.IsDead()) return;
        }
        
		if (!other.CompareTag(playerTag))
        {
            //Debug.Log($"[ClubCollision] {other.name} is not tagged '{playerTag}' — ignoring");
            return;
        }
        
        Stats playerStats = other.GetComponent<Stats>();
        if (playerStats == null)
        {
            //Debug.LogWarning($"[ClubCollision] No Stats component on {other.name}");
            return;
        }
        
        // Check cooldown
        if (_lastHitTimes.ContainsKey(playerStats))
        {
            float timeSinceLastHit = Time.time - _lastHitTimes[playerStats];
            if (timeSinceLastHit < hitCooldown)
            {
                //Debug.Log($"[ClubCollision] Hit cooldown active for {other.name} — skipping");
                return;
            }
        }
        
        if (pc != null)
        {
            // Trigger the stagger state (this will handle animation and disabling actions)
			/*  i‑frames: ignore any hit while the player is rolling  */
			if (pc.IsRolling())
			{
				return;
			}
			
            pc.TriggerStagger();
        }
        else
        {
            Debug.LogWarning($"[ClubCollision] No PlayerController component on {other.name}");
        }
        
        // Apply damage
        float before = playerStats.GetHealth();
        playerStats.TakeHealthDamage(damageAmount);
        float after = playerStats.GetHealth();
        //Debug.Log($"[ClubCollision] Club collision: dealt {damageAmount} damage to {other.name}. Health: {before} → {after}");
        
        // Record hit time
        _lastHitTimes[playerStats] = Time.time;
    }
}