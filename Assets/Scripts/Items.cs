// Items.cs
using System.Collections;
using UnityEngine;

public class Items : MonoBehaviour
{
    [Header("Items")]
    [SerializeField] private int healthPots = 2;
    [SerializeField] private Stats playerStats; // Drag the player object here in inspector
    [SerializeField] private float healthToAdd = 15f;

    private const float maxHealth = 100f;
    private const float healDuration = 2f;

    // Keep a reference to the running heal‐over‐time coroutine:
    private Coroutine _healCoroutine;

    public int GetNumPots()
    {
        return healthPots;
    }

    public void SetHealthPots(int num)
    {
        healthPots = num;
    }

    public bool UseHealthPot()
    {
        if (healthPots <= 0) return false;
        healthPots--;

        float currentHealth = playerStats.GetHealth();
        float targetHealth = Mathf.Min(currentHealth + healthToAdd, maxHealth);

        // If there is already a heal coroutine running, stop it first:
        if (_healCoroutine != null)
        {
            StopCoroutine(_healCoroutine);
            _healCoroutine = null;
        }

        _healCoroutine = StartCoroutine(HealOverTime(currentHealth, targetHealth, healDuration));
        return true;
    }

    private IEnumerator HealOverTime(float startHealth, float endHealth, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float newHealth = Mathf.Lerp(startHealth, endHealth, elapsed / duration);
            playerStats.SetHealth(newHealth);
            yield return null;
        }

        // Ensure it ends exactly at endHealth
        playerStats.SetHealth(endHealth);
        _healCoroutine = null;
    }

    // Call this to stop any in‐progress heal (leaving health at whatever point it reached)
    public void CancelHeal()
    {
        if (_healCoroutine != null)
        {
            StopCoroutine(_healCoroutine);
            _healCoroutine = null;
        }
    }
}
