using UnityEngine;

// Tropa que marcha entre pontos pré-definidos, em loop.
// Os pontos podem ser objetos vazios filhos do inimigo: as posições são guardadas no início.
public class EnemyPatrol : MonoBehaviour
{
    public Transform[] pontos;
    public float speed = 2f;

    private Vector2[] posicoes;
    private int indice;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        posicoes = new Vector2[pontos.Length];
        for (int i = 0; i < pontos.Length; i++)
            posicoes[i] = pontos[i].position;
    }

    void FixedUpdate()
    {
        if (GameController.gameOver || posicoes.Length == 0) return;

        Vector2 alvo = posicoes[indice];
        Vector2 novaPosicao = Vector2.MoveTowards(rb.position, alvo, speed * Time.fixedDeltaTime);
        rb.MovePosition(novaPosicao);

        if (Vector2.Distance(novaPosicao, alvo) < 0.05f)
            indice = (indice + 1) % posicoes.Length;
    }
}
