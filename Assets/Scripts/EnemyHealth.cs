using UnityEngine;

// Vida de um inimigo. Ao ser atingido pisca em vermelho; ao chegar a zero é destruído.
public class EnemyHealth : MonoBehaviour
{
    public float vida = 1f;
    public AudioClip somMorte;
    public Color corDano = new Color(1f, 0.3f, 0.3f);

    private const float duracaoPiscar = 0.12f;
    private float piscarTimer;
    private SpriteRenderer sr;
    private Color corOriginal;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        corOriginal = sr.color;
    }

    void Update()
    {
        if (piscarTimer <= 0f) return;
        piscarTimer -= Time.deltaTime;
        sr.color = piscarTimer > 0f ? corDano : corOriginal;
    }

    public void TakeDamage(float amount)
    {
        vida -= amount;
        piscarTimer = duracaoPiscar;

        if (vida <= 0f)
        {
            if (somMorte != null)
                AudioSource.PlayClipAtPoint(somMorte, Camera.main.transform.position);
            Destroy(gameObject);
        }
    }
}
