using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

// Menu principal: Jogar, Como jogar e Sair. Funciona com mouse, teclado e controle de Xbox.
public class MainMenu : MonoBehaviour
{
    public string cenaJogo = "CidadeDasAlmas";

    public GameObject painelPrincipal;
    public GameObject painelComoJogar;

    [Tooltip("Botões selecionados ao abrir cada painel (navegação por teclado/controle)")]
    public GameObject primeiroBotao;
    public GameObject botaoVoltar;
    public GameObject botaoSair;

    void Start()
    {
        // No navegador não dá para fechar o jogo, então o botão Sair some
        if (Application.platform == RuntimePlatform.WebGLPlayer && botaoSair != null)
            botaoSair.SetActive(false);

        MostrarPrincipal();
    }

    void Update()
    {
        // Esc ou B do controle fecham o "Como jogar"
        if (painelComoJogar.activeSelf && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton1)))
            MostrarPrincipal();
    }

    public void Jogar()
    {
        SceneManager.LoadScene(cenaJogo);
    }

    public void ComoJogar()
    {
        painelPrincipal.SetActive(false);
        painelComoJogar.SetActive(true);
        Selecionar(botaoVoltar);
    }

    public void MostrarPrincipal()
    {
        painelComoJogar.SetActive(false);
        painelPrincipal.SetActive(true);
        Selecionar(primeiroBotao);
    }

    public void Sair()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void Selecionar(GameObject botao)
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(botao);
    }
}
