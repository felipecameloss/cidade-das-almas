using UnityEngine;

// Vida do jogador. Depois de tomar dano fica invulnerável por um tempo e pisca.
// Durante o dash o jogador também não toma dano.
public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float invulnerableTime = 1f;
    public AudioClip somDano;

    public float Health { get; private set; }

    private float invulnerableTimer;
    private SpriteRenderer sr;
    private AudioSource audioSource;
    private PlayerMovement movement;

    void Start()
    {
        Health = maxHealth;
        sr = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        movement = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        if (GameController.gameOver)
        {
            sr.enabled = true;
            return;
        }

        if (invulnerableTimer > 0f)
        {
            invulnerableTimer -= Time.deltaTime;
            // Pisca 10 vezes por segundo enquanto está invulnerável
            sr.enabled = Mathf.FloorToInt(invulnerableTimer * 10f) % 2 == 0;
            if (invulnerableTimer <= 0f) sr.enabled = true;
        }
    }

    public void TakeDamage(float amount)
    {
        if (GameController.gameOver || invulnerableTimer > 0f) return;
        if (movement != null && movement.IsDashing) return;

        Health -= amount;
        invulnerableTimer = invulnerableTime;
        if (somDano != null) audioSource.PlayOneShot(somDano);

        if (Health <= 0f)
        {
            Health = 0f;
            GameController.playerDead = true;
        }
    }
}
