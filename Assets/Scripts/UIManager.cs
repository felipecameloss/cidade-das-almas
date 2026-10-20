using UnityEngine;
using UnityEngine.UI;
using TMPro;

// HUD (almas, tempo, vida, stamina) e painel de fim de jogo.
public class UIManager : MonoBehaviour
{
    [Header("Painel de fim de jogo")]
    public GameObject endGamePanel;
    public TMP_Text resultText;

    [Header("HUD")]
    public TMP_Text scoreText;
    public TMP_Text timeText;
    public Slider barraVida;
    public Slider barraStamina;

    [Header("Jogador")]
    public PlayerHealth playerHealth;
    public PlayerMovement playerMovement;

    [Header("Áudio")]
    public AudioSource musica;
    public AudioClip somVitoria;
    public AudioClip somDerrota;

    [Header("Ritual")]
    public float startTime = 30f;
    public float tempoCritico = 5f;

    private int total;
    private Color corTempoOriginal;

    void Start()
    {
        total = GameObject.FindGameObjectsWithTag("Coletavel").Length;
        GameController.Init(total, startTime);
        endGamePanel.SetActive(false);

        barraVida.maxValue = playerHealth.maxHealth;
        barraStamina.maxValue = playerMovement.maxStamina;
        corTempoOriginal = timeText.color;
    }

    void Update()
    {
        GameController.Tick(Time.deltaTime);

        float tempoRestante = Mathf.Max(0, GameController.timeLeft);
        scoreText.text = "Almas: " + GameController.score + "/" + total;
        timeText.text = "Ritual: " + tempoRestante.ToString("0.0") + "s";
        timeText.color = tempoRestante <= tempoCritico ? Color.red : corTempoOriginal;

        barraVida.value = playerHealth.Health;
        barraStamina.value = playerMovement.Stamina;

        if (GameController.gameOver && !endGamePanel.activeSelf)
        {
            endGamePanel.SetActive(true);
            musica.Stop();

            string tempo = "<size=50%>Tempo: " + GameController.elapsedTime.ToString("0.0") + "s</size>";

            if (GameController.Won)
            {
                resultText.text = "Vitória\n" + tempo;
                AudioSource.PlayClipAtPoint(somVitoria, Camera.main.transform.position);
            }
            else
            {
                resultText.text = "Derrota\n" + tempo;
                AudioSource.PlayClipAtPoint(somDerrota, Camera.main.transform.position);
            }
        }
    }
}
