using UnityEngine;

// Movimento do jogador + dash com stamina. Funciona com teclado e controle de Xbox.
public class PlayerMovement : MonoBehaviour
{
    [Header("Movimento")]
    public float speed = 5f;

    [Header("Dash")]
    public float dashSpeed = 15f;
    public float dashDuration = 0.15f;
    public float dashCost = 35f;
    public float maxStamina = 100f;
    public float staminaRegen = 20f;
    public AudioClip somDash;

    public float Stamina { get; private set; }
    public bool IsDashing => dashTimer > 0f;

    private Rigidbody2D rb;
    private AudioSource audioSource;
    private Vector2 movement;
    private Vector2 dashDirection;
    private float dashTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();
        Stamina = maxStamina;
    }

    void Update()
    {
        if (GameController.gameOver)
        {
            movement = Vector2.zero;
            dashTimer = 0f;
            return;
        }

        // Teclado (WASD/setas) e analógico esquerdo do controle
        Vector2 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        movement = Vector2.ClampMagnitude(input, 1f);

        // Stamina recarrega quando não está dando dash
        if (!IsDashing)
            Stamina = Mathf.Min(maxStamina, Stamina + staminaRegen * Time.deltaTime);

        // Dash: Espaço no teclado ou botão A no controle de Xbox
        bool dashPressed = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.JoystickButton0);
        if (dashPressed && !IsDashing && Stamina >= dashCost && movement.sqrMagnitude > 0.01f)
        {
            Stamina -= dashCost;
            dashTimer = dashDuration;
            dashDirection = movement.normalized;
            if (somDash != null) audioSource.PlayOneShot(somDash);
        }
    }

    void FixedUpdate()
    {
        if (GameController.gameOver) return;

        Vector2 velocity;
        if (IsDashing)
        {
            velocity = dashDirection * dashSpeed;
            dashTimer -= Time.fixedDeltaTime;
        }
        else
        {
            velocity = movement * speed;
        }

        rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (GameController.gameOver) return;

        if (collision.CompareTag("Coletavel"))
        {
            audioSource.Play();
            GameController.Collect();
            Destroy(collision.gameObject);
        }
    }
}
