using UnityEngine;

// Causa dano ao jogador enquanto ele estiver encostando neste objeto.
// Use em todos os inimigos e também em armadilhas paradas (ex.: poça de corrupção).
public class EnemyDamage : MonoBehaviour
{
    public float dano = 25f;

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player") && other.TryGetComponent(out PlayerHealth vida))
            vida.TakeDamage(dano);
    }
}
