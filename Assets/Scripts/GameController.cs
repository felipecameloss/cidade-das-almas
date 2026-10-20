// Estado global da partida: almas restantes, pontuação, tempo e morte do jogador.
public static class GameController
{
    private static int collectableCount;
    public static int score;
    public static float timeLeft;
    public static float elapsedTime;
    public static bool playerDead;

    public static bool Won => collectableCount <= 0;
    public static bool gameOver => Won || timeLeft <= 0 || playerDead;

    public static void Init(int totalCollectables, float startTime)
    {
        collectableCount = totalCollectables;
        score = 0;
        timeLeft = startTime;
        elapsedTime = 0;
        playerDead = false;
    }

    public static void Collect()
    {
        collectableCount--;
        score++;
        timeLeft += 3f; // cada alma alimenta o ritual com +3 segundos
    }

    public static void Tick(float dt)
    {
        if (gameOver) return;
        timeLeft -= dt;
        elapsedTime += dt;
    }
}
