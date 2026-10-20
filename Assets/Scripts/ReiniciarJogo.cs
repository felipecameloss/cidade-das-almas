using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

// Fica no painel de fim de jogo: jogar novamente (botão, R ou Start do controle)
// ou voltar ao menu principal (botão, M ou B do controle).
public class ReiniciarJogo : MonoBehaviour
{
    public string cenaMenu = "MenuPrincipal";

    [Tooltip("Botão selecionado quando o painel aparece (navegação por teclado/controle)")]
    public GameObject botaoPadrao;

    void OnEnable()
    {
        // Espera um pouco para quem estava apertando Espaço/A (dash) não clicar sem querer
        Invoke(nameof(SelecionarBotao), 0.6f);
    }

    void OnDisable()
    {
        CancelInvoke();
    }

    void Update()
    {
        if (!GameController.gameOver) return;

        if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.JoystickButton7))
            Reiniciar();
        else if (Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.JoystickButton1))
            VoltarMenu();
    }

    public void Reiniciar()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void VoltarMenu()
    {
        SceneManager.LoadScene(cenaMenu);
    }

    void SelecionarBotao()
    {
        if (botaoPadrao != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(botaoPadrao);
    }
}
