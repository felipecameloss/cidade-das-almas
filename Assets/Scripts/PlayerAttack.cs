using System.Collections.Generic;
using UnityEngine;

// Ataque do jogador: uma onda de energia em volta dele que acerta todos os inimigos no raio.
// J, clique esquerdo ou botão X do controle de Xbox. Tem tempo de recarga.
public class PlayerAttack : MonoBehaviour
{
    public float dano = 1f;
    public float raio = 1.6f;
    public float recarga = 0.5f;
    public AudioClip somAtaque;

    [Tooltip("Sprite filho que aparece como a onda do ataque")]
    public SpriteRenderer efeito;

    public bool Pronto => recargaTimer <= 0f;

    private const float duracaoEfeito = 0.2f;
    private float recargaTimer;
    private float efeitoTimer;
    private Color corEfeito;
    private AudioSource audioSource;
    private readonly List<Collider2D> acertos = new List<Collider2D>();

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (efeito != null)
        {
            corEfeito = efeito.color;
            efeito.enabled = false;
        }
    }

    void Update()
    {
        recargaTimer -= Time.deltaTime;
        AtualizarEfeito();

        if (GameController.gameOver) return;

        bool ataquePressionado = Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0)
            || Input.GetKeyDown(KeyCode.JoystickButton2);
        if (ataquePressionado && Pronto)
            Atacar();
    }

    void Atacar()
    {
        recargaTimer = recarga;
        efeitoTimer = duracaoEfeito;
        if (somAtaque != null) audioSource.PlayOneShot(somAtaque);

        var filtro = new ContactFilter2D { useTriggers = true };
        Physics2D.OverlapCircle(transform.position, raio, filtro, acertos);
        foreach (var alvo in acertos)
            if (alvo.TryGetComponent(out EnemyHealth inimigo))
                inimigo.TakeDamage(dano);
    }

    // A onda cresce até o raio do ataque e some
    void AtualizarEfeito()
    {
        if (efeito == null) return;

        efeito.enabled = efeitoTimer > 0f;
        if (efeitoTimer <= 0f) return;

        efeitoTimer -= Time.deltaTime;
        float progresso = 1f - efeitoTimer / duracaoEfeito;
        float diametro = Mathf.Lerp(0.5f, raio * 2f, progresso) / transform.localScale.x;
        efeito.transform.localScale = Vector3.one * diametro;
        efeito.color = new Color(corEfeito.r, corEfeito.g, corEfeito.b, corEfeito.a * (1f - progresso));
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, raio);
    }
}
