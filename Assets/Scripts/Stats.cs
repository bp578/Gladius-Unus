using UnityEngine;

public class Stats : MonoBehaviour
{
    [Header("Core Stats")]
    [SerializeField] private float health = 100f;
    [SerializeField] private float stamina = 100f;
	[SerializeField] private float maxStamina = 100f;
	[SerializeField] private float maxHealth = 100f;
    /// <summary>
    /// Returns current health.
    /// </summary>
    public float GetHealth()
    {
        return health;
    }

    /// <summary>
    /// Sets health, clamped to [0, ∞).
    /// </summary>
    public void SetHealth(float value)
    {
        health = Mathf.Max(0f, value);
    }

    /// <summary>
    /// Returns current stamina.
    /// </summary>
    public float GetStamina()
    {
        return stamina;
    }

    /// <summary>
    /// Sets stamina, clamped to [0, ∞).
    /// </summary>
    public void SetStamina(float value)
    {
        stamina = Mathf.Max(0f, value);
    }

    /// <summary>
    /// Reduces health by the given amount (always positive).
    /// </summary>
    public void TakeHealthDamage(float amount)
    {
        SetHealth(health - Mathf.Abs(amount));
    }

    /// <summary>
    /// Reduces stamina by the given amount (always positive).
    /// </summary>
    public void TakeStaminaDamage(float amount)
    {
        SetStamina(stamina - Mathf.Abs(amount));
    }
	public float GetMaxStamina()
	{
		return maxStamina;
	}
	public float GetMaxHealth()
	{
		return maxHealth;
	}
}
