using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;

    [Header("Regeneration")]
    [SerializeField] private float regenDelay = 3f;
    [SerializeField] private float regenPerSecond = 5f;

    private float lastDamageTime = -999f;

    public float CurrentHealth { get; private set; }

    public float MaxHealth => maxHealth;

    public float RegenDelay => regenDelay;

    public float RegenPerSecond => regenPerSecond;

    public bool IsDead => CurrentHealth <= 0f;

    // True selama Player baru saja diserang (regenerasi tertahan).
    public bool RecentlyAttacked =>
        Time.time - lastDamageTime < regenDelay;

    public bool IsRegenerating { get; private set; }

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    private void Update()
    {
        IsRegenerating = false;

        if (IsDead || RecentlyAttacked || CurrentHealth >= maxHealth)
            return;

        CurrentHealth = Mathf.Min(
            maxHealth,
            CurrentHealth + regenPerSecond * Time.deltaTime
        );

        IsRegenerating = true;
    }

    public void TakeDamage(float damage)
    {
        if (IsDead)
            return;

        CurrentHealth -= damage;

        CurrentHealth = Mathf.Clamp(
            CurrentHealth,
            0f,
            maxHealth
        );

        lastDamageTime = Time.time;
        IsRegenerating = false;

        Debug.Log(
            "Player HP: " + CurrentHealth
        );

        if (CurrentHealth <= 0f)
        {
            Debug.Log("Player Dead");
        }
    }
}
