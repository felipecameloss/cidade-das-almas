using UnityEngine;

// Guardião: fica parado no seu posto e persegue o jogador quando ele chega perto.
// Se o jogador se afastar, volta para o posto. Por ser um espectro, atravessa paredes.
public class EnemyChase : MonoBehaviour
{
    public float speed = 3f;
    public float raioDeteccao = 4f;

    private Transform player;
    private Rigidbody2D rb;
    private Vector2 posicaoInicial;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        posicaoInicial = rb.position;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void FixedUpdate()
    {
        if (GameController.gameOver || player == null) return;

        float distancia = Vector2.Distance(rb.position, player.position);
        Vector2 alvo = distancia <= raioDeteccao ? (Vector2)player.position : posicaoInicial;

        rb.MovePosition(Vector2.MoveTowards(rb.position, alvo, speed * Time.fixedDeltaTime));
    }

    // Mostra o raio de detecção na Scene quando o inimigo está selecionado
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, raioDeteccao);
    }
}
