using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Monta o projeto do zero: tags, sprites, sons, cena da fase, HUD e configurações de build.
// Menu: Cidade das Almas > Construir Cena  (pode ser rodado de novo; a cena é recriada).
public static class CidadeDasAlmasBuilder
{
    const string PastaArte = "Assets/Art";
    const string PastaSons = "Assets/Audio";
    const string CaminhoCena = "Assets/Scenes/CidadeDasAlmas.unity";
    const string CaminhoMenu = "Assets/Scenes/MenuPrincipal.unity";
    const string FonteTMP = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

    static Sprite quadrado, circulo, brilho, anel;
    static Sprite spriteJogador, spriteTropa, spriteGuardiao, spriteAlma, spritePoca;
    static TMP_FontAsset fonte;

    [MenuItem("Cidade das Almas/Importar TMP Essentials")]
    public static void ImportarTMP()
    {
        if (File.Exists(FonteTMP)) return;
        var info = UnityEditor.PackageManager.PackageInfo.FindForPackageName("com.unity.ugui");
        string pacote = Path.Combine(info.resolvedPath, "Package Resources", "TMP Essential Resources.unitypackage");
        AssetDatabase.ImportPackage(pacote, false);
        AssetDatabase.Refresh();
    }

    [MenuItem("Cidade das Almas/Construir Cena")]
    public static void Construir()
    {
        ImportarTMP();
        fonte = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FonteTMP);

        GarantirTag("Coletavel");
        CriarSprites();
        CriarSons();

        PlayerSettings.productName = "Cidade das Almas";
        EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;

        var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CriarCamera();
        CriarMapa();
        GameObject player = CriarJogador(new Vector2(-13f, -6.5f));
        CriarAlmas();
        CriarInimigos();
        CriarUI(player);

        Directory.CreateDirectory(Path.GetDirectoryName(CaminhoCena));
        EditorSceneManager.SaveScene(cena, CaminhoCena);

        var menu = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CriarMenu();
        EditorSceneManager.SaveScene(menu, CaminhoMenu);

        // O menu é a primeira cena do build
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(CaminhoMenu, true),
            new EditorBuildSettingsScene(CaminhoCena, true),
        };
        AssetDatabase.SaveAssets();
        Debug.Log("Cidade das Almas: cenas construídas em " + CaminhoMenu + " e " + CaminhoCena);
    }

    // ---------------------------------------------------------------- Builds

    // Versão para navegador (itch.io). Gzip com fallback de descompressão funciona em qualquer
    // servidor, sem precisar configurar cabeçalhos HTTP.
    [MenuItem("Cidade das Almas/Build WebGL (itch.io)")]
    public static void BuildWebGL()
    {
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.defaultWebScreenWidth = 960;
        PlayerSettings.defaultWebScreenHeight = 540;
        PlayerSettings.runInBackground = false;

        var opcoes = new BuildPlayerOptions
        {
            scenes = new[] { CaminhoMenu, CaminhoCena },
            locationPathName = "Builds/WebGL",
            target = BuildTarget.WebGL,
        };
        var relatorio = BuildPipeline.BuildPlayer(opcoes);
        Debug.Log("Cidade das Almas: build WebGL " + relatorio.summary.result);
        if (Application.isBatchMode && relatorio.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            EditorApplication.Exit(1);
    }

    // ---------------------------------------------------------------- Tags

    static void GarantirTag(string tag)
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var tags = tagManager.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++)
            if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;

        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        tagManager.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------------------------------------------------------------- Sprites

    static void CriarSprites()
    {
        Directory.CreateDirectory(PastaArte);
        quadrado = CriarSprite("Quadrado", (x, y) => 1f);
        circulo = CriarSprite("Circulo", (x, y) => Mathf.Clamp01((1f - Mathf.Sqrt(x * x + y * y)) * 24f));
        brilho = CriarSprite("Brilho", (x, y) =>
        {
            float d = Mathf.Sqrt(x * x + y * y);
            return Mathf.Clamp01(1f - d) * Mathf.Clamp01(1f - d) + (d < 0.35f ? 0.6f : 0f);
        });
        anel = CriarSprite("Anel", (x, y) =>
        {
            float d = Mathf.Sqrt(x * x + y * y);
            return Mathf.Clamp01(1f - Mathf.Abs(d - 0.85f) * 8f);
        });

        // Personagens desenhados em ArtSource/gerar_sprites.js
        spriteJogador = CarregarSprite("Jogador");
        spriteTropa = CarregarSprite("Tropa");
        spriteGuardiao = CarregarSprite("Guardiao");
        spriteAlma = CarregarSprite("Alma");
        spritePoca = CarregarSprite("Poca");
    }

    static Sprite CarregarSprite(string nome)
    {
        string caminho = $"{PastaArte}/Sprites/{nome}.png";
        AssetDatabase.ImportAsset(caminho);
        var importer = (TextureImporter)AssetImporter.GetAtPath(caminho);
        if (importer == null)
            throw new FileNotFoundException("Sprite não encontrado. Rode: node ArtSource/gerar_sprites.js", caminho);

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 512;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = true; // fica nítido mesmo desenhado bem menor que 512px
        importer.filterMode = FilterMode.Trilinear;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(caminho);
    }

    // alpha(x, y) recebe coordenadas de -1 a 1
    static Sprite CriarSprite(string nome, Func<float, float, float> alpha)
    {
        const int tam = 64;
        string caminho = $"{PastaArte}/{nome}.png";
        var tex = new Texture2D(tam, tam, TextureFormat.RGBA32, false);
        for (int y = 0; y < tam; y++)
            for (int x = 0; x < tam; x++)
            {
                float u = (x + 0.5f) / tam * 2f - 1f;
                float v = (y + 0.5f) / tam * 2f - 1f;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(alpha(u, v))));
            }
        File.WriteAllBytes(caminho, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(caminho);
        var importer = (TextureImporter)AssetImporter.GetAtPath(caminho);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = tam;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(caminho);
    }

    // ---------------------------------------------------------------- Sons (gerados por código)

    const int Taxa = 22050;

    static AudioClip somColeta, somDano, somDash, somVitoria, somDerrota, somAtaque, somMorteInimigo, musica;

    static void CriarSons()
    {
        Directory.CreateDirectory(PastaSons);
        var rnd = new System.Random(7);

        somColeta = CriarSom("Coleta", 0.3f, t =>
        {
            float f = t < 0.1f ? 880f : 1320f;
            return Seno(f, t) * Mathf.Exp(-(t % 0.1f) * 12f) * 0.5f;
        });
        somDano = CriarSom("Dano", 0.3f, t =>
            (Mathf.Sign(Seno(110f - t * 150f, t)) * 0.4f + ((float)rnd.NextDouble() * 2f - 1f) * 0.3f) * Mathf.Exp(-t * 10f));
        float ultimo = 0f;
        somDash = CriarSom("Dash", 0.25f, t =>
        {
            ultimo = Mathf.Lerp(ultimo, (float)rnd.NextDouble() * 2f - 1f, 0.15f + t);
            return ultimo * Mathf.Sin(Mathf.PI * t / 0.25f) * 0.8f;
        });
        somVitoria = CriarSom("Vitoria", 1.2f, t => Notas(t, 0.15f, new[] { 523.25f, 659.25f, 783.99f, 1046.5f }) * 0.4f);
        somDerrota = CriarSom("Derrota", 1.5f, t => Notas(t, 0.3f, new[] { 392f, 311.13f, 261.63f, 196f }) * 0.4f);

        // Ataque: varredura aguda subindo + chiado; morte do inimigo: estouro descendo
        somAtaque = CriarSom("Ataque", 0.2f, t =>
            (Seno(400f + t * 4000f, t) * 0.35f + ((float)rnd.NextDouble() * 2f - 1f) * 0.25f) * Mathf.Exp(-t * 18f));
        somMorteInimigo = CriarSom("MorteInimigo", 0.45f, t =>
            (Seno(600f - t * 1100f, t) * 0.4f + ((float)rnd.NextDouble() * 2f - 1f) * 0.3f * Mathf.Exp(-t * 20f)) * Mathf.Exp(-t * 6f));

        musica = CriarMusica(rnd);
    }

    // Trilha em loop (20s, 96 BPM) em lá menor: Am - F - Dm - E.
    // Camadas: baixo, pad, arpejo, bumbo, chimbal e, na segunda metade, uma melodia.
    static AudioClip CriarMusica(System.Random rnd)
    {
        const float batida = 60f / 96f;
        const float compasso = batida * 4f;
        int[] baixo = { 45, 41, 38, 40 };
        int[][] acordes = { new[] { 57, 60, 64 }, new[] { 53, 57, 60 }, new[] { 57, 62, 65 }, new[] { 56, 59, 64 } };
        int[] padraoArpejo = { 0, 1, 2, 1, 0, 2, 1, 2 };
        int[] melodia = { 69, 72, 71, 69, 65, 69, 72, 69, 65, 62, 65, 69, 68, 71, 68, 64 };

        return CriarSom("Musica", compasso * 8f, t =>
        {
            int c = (int)(t / compasso);
            int acorde = c % 4;
            float tb = t % batida;           // tempo dentro da batida
            int nBatida = (int)(t / batida) % 4;
            float tc = t % (batida / 2f);    // tempo dentro da colcheia
            int nColcheia = (int)(t / (batida / 2f)) % 8;

            float s = 0f;

            // Baixo nos tempos 1 e 3
            float tBaixo = t % (batida * 2f);
            s += Triangulo(Midi(baixo[acorde]), t) * Mathf.Exp(-tBaixo * 2.5f) * 0.28f;

            // Pad suave com os três tons do acorde
            foreach (int n in acordes[acorde])
                s += Seno(Midi(n - 12), t) * 0.05f * (0.7f + 0.3f * Mathf.Sin(t * 1.5f));

            // Arpejo em colcheias
            float fArp = Midi(acordes[acorde][padraoArpejo[nColcheia]] + 12);
            s += (Seno(fArp, t) + 0.25f * Seno(fArp * 2f, t)) * Mathf.Exp(-tc * 9f) * 0.09f;

            // Bumbo nos tempos 1 e 3, chimbal nos contratempos
            if (nBatida % 2 == 0)
                s += Mathf.Sin(2f * Mathf.PI * (45f * tb + 4f * (1f -Mathf.Exp(-tb * 25f)))) * Mathf.Exp(-tb * 12f) * 0.45f;
            float tChimbal = (t + batida / 2f) % batida;
            s += ((float)rnd.NextDouble() * 2f - 1f) * Mathf.Exp(-tChimbal * 60f) * 0.06f;

            // Melodia só na segunda volta da progressão
            if (c >= 4)
            {
                float fMel = Midi(melodia[(c - 4) * 4 + nBatida]);
                float vibrato = 1f + 0.004f * Mathf.Sin(t * 30f);
                s += (Seno(fMel * vibrato, t) + 0.3f * Seno(fMel * 2f, t)) * Mathf.Min(1f, tb * 30f) * Mathf.Exp(-tb * 1.8f) * 0.13f;
            }
            return s;
        });
    }

    static float Midi(int nota) => 440f * Mathf.Pow(2f, (nota - 69) / 12f);

    static float Seno(float freq, float t) => Mathf.Sin(2f * Mathf.PI * freq * t);

    static float Triangulo(float freq, float t) => 1f - 4f * Mathf.Abs(Mathf.Repeat(freq * t + 0.25f, 1f) - 0.5f);

    static float Notas(float t, float dur, float[] freqs)
    {
        int i = Mathf.Min((int)(t / dur), freqs.Length - 1);
        float tn = t - i * dur;
        return (Seno(freqs[i], t) + 0.3f * Seno(freqs[i] * 2f, t)) * Mathf.Exp(-tn * (i == freqs.Length - 1 ? 2.5f : 6f));
    }

    static AudioClip CriarSom(string nome, float duracao, Func<float, float> amostra)
    {
        string caminho = $"{PastaSons}/{nome}.wav";
        int n = (int)(duracao * Taxa);
        using (var w = new BinaryWriter(File.Create(caminho)))
        {
            w.Write("RIFF".ToCharArray()); w.Write(36 + n * 2);
            w.Write("WAVEfmt ".ToCharArray()); w.Write(16); w.Write((short)1); w.Write((short)1);
            w.Write(Taxa); w.Write(Taxa * 2); w.Write((short)2); w.Write((short)16);
            w.Write("data".ToCharArray()); w.Write(n * 2);
            for (int i = 0; i < n; i++)
            {
                float fade = Mathf.Min(1f, (n - i) / (Taxa * 0.01f)); // evita estalo no fim
                w.Write((short)(Mathf.Clamp(amostra((float)i / Taxa) * fade, -1f, 1f) * short.MaxValue));
            }
        }
        AssetDatabase.ImportAsset(caminho);
        return AssetDatabase.LoadAssetAtPath<AudioClip>(caminho);
    }

    // ---------------------------------------------------------------- Mundo

    static void CriarCamera()
    {
        var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        go.tag = "MainCamera";
        go.transform.position = new Vector3(0f, 1f, -10f);
        var cam = go.GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 9.5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.07f, 0.05f, 0.11f);
    }

    static GameObject CriarSpriteObj(string nome, Sprite sprite, Color cor, Vector2 pos, Vector2 escala, int ordem, Transform pai = null)
    {
        var go = new GameObject(nome);
        if (pai != null) go.transform.SetParent(pai);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(escala.x, escala.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = cor;
        sr.sortingOrder = ordem;
        return go;
    }

    static void CriarMapa()
    {
        var mapa = new GameObject("Mapa").transform;
        var corMuro = new Color(0.22f, 0.22f, 0.32f);
        var corPredio = new Color(0.16f, 0.15f, 0.24f);

        // Bordas da arena (interior de x -14.5..14.5, y -7.5..7.5)
        Parede(mapa, "Muro Norte", new Vector2(0f, 8f), new Vector2(31f, 1f), corMuro);
        Parede(mapa, "Muro Sul", new Vector2(0f, -8f), new Vector2(31f, 1f), corMuro);
        Parede(mapa, "Muro Oeste", new Vector2(-15f, 0f), new Vector2(1f, 17f), corMuro);
        Parede(mapa, "Muro Leste", new Vector2(15f, 0f), new Vector2(1f, 17f), corMuro);

        // Prédios da cidade
        Parede(mapa, "Predio 1", new Vector2(-9f, 3f), new Vector2(4f, 3f), corPredio);
        Parede(mapa, "Predio 2", new Vector2(-9f, -3.5f), new Vector2(4f, 2f), corPredio);
        Parede(mapa, "Predio 3", new Vector2(0f, 4.5f), new Vector2(6f, 2f), corPredio);
        Parede(mapa, "Torre Central", new Vector2(0f, -1f), new Vector2(2f, 5f), corPredio);
        Parede(mapa, "Predio 4", new Vector2(8f, 3f), new Vector2(3f, 4f), corPredio);
        Parede(mapa, "Predio 5", new Vector2(8.5f, -4f), new Vector2(5f, 2f), corPredio);

        // Poças de corrupção: armadilhas paradas que causam dano
        foreach (var (pos, tam) in new[] { (new Vector2(3.5f, -3.5f), 1.8f), (new Vector2(-6f, 2.5f), 1.5f) })
        {
            var poca = CriarSpriteObj("Poca de Corrupcao", spritePoca, Color.white, pos, Vector2.one * tam, 1, mapa);
            poca.AddComponent<CircleCollider2D>().isTrigger = true;
            poca.GetComponent<CircleCollider2D>().radius = 0.35f;
            poca.AddComponent<EnemyDamage>().dano = 15f;
        }
    }

    static void Parede(Transform pai, string nome, Vector2 pos, Vector2 tam, Color cor)
    {
        var go = CriarSpriteObj(nome, quadrado, cor, pos, tam, 0, pai);
        go.AddComponent<BoxCollider2D>();
    }

    static GameObject CriarJogador(Vector2 pos)
    {
        var go = CriarSpriteObj("Player", spriteJogador, Color.white, pos, Vector2.one * 0.9f, 4);
        go.tag = "Player";

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep; // mantém OnTriggerStay2D funcionando parado

        go.AddComponent<CircleCollider2D>().radius = 0.48f;

        var audio = go.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.clip = somColeta; // tocado ao coletar uma alma

        var mov = go.AddComponent<PlayerMovement>();
        mov.somDash = somDash;
        var vida = go.AddComponent<PlayerHealth>();
        vida.somDano = somDano;

        var efeito = CriarSpriteObj("Efeito Ataque", anel, new Color(0.6f, 0.95f, 1f, 0.9f), pos, Vector2.one, 5, go.transform);
        var ataque = go.AddComponent<PlayerAttack>();
        ataque.somAtaque = somAtaque;
        ataque.efeito = efeito.GetComponent<SpriteRenderer>();
        return go;
    }

    static void CriarAlmas()
    {
        var pai = new GameObject("Almas").transform;
        Vector2[] posicoes =
        {
            new(-12f, 6.3f), new(-5f, 6.5f), new(5f, 6.5f), new(12.5f, 6.3f),
            new(-12.5f, -0.5f), new(-9f, -1f), new(-5f, 0f), new(4f, 0f), new(12.5f, 0f),
            new(-9f, -6.5f), new(0f, -6.5f), new(12.5f, -6.5f),
        };
        for (int i = 0; i < posicoes.Length; i++)
        {
            var alma = CriarSpriteObj("Alma " + (i + 1), spriteAlma, Color.white, posicoes[i], Vector2.one, 2, pai);
            alma.tag = "Coletavel";
            var col = alma.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.4f;
        }
    }

    static void CriarInimigos()
    {
        var pai = new GameObject("Inimigos").transform;
        Tropa(pai, "Tropa 1", new Vector2(-5f, -6.5f), new Vector2(5f, -6.5f));
        Tropa(pai, "Tropa 2", new Vector2(-4.5f, -4f), new Vector2(-4.5f, 6f));
        Tropa(pai, "Tropa 3", new Vector2(5f, -1.5f), new Vector2(11f, -1.5f), new Vector2(11f, 6f), new Vector2(5f, 6f));

        foreach (var pos in new[] { new Vector2(13f, -3.5f), new Vector2(-12.5f, 3f), new Vector2(0f, 6.6f) })
        {
            var g = Inimigo(pai, "Guardiao", spriteGuardiao, pos, 1.15f);
            g.AddComponent<EnemyChase>();
            g.GetComponent<EnemyHealth>().vida = 2f; // guardiões aguentam dois golpes
        }
    }

    static void Tropa(Transform pai, string nome, params Vector2[] pontos)
    {
        var go = Inimigo(pai, nome, spriteTropa, pontos[0], 0.9f);
        var patrulha = go.AddComponent<EnemyPatrol>();
        patrulha.pontos = new Transform[pontos.Length];
        for (int i = 0; i < pontos.Length; i++)
        {
            var p = new GameObject("Ponto " + (i + 1)).transform;
            p.SetParent(go.transform);
            p.position = pontos[i];
            patrulha.pontos[i] = p;
        }
    }

    static GameObject Inimigo(Transform pai, string nome, Sprite sprite, Vector2 pos, float escala)
    {
        var go = CriarSpriteObj(nome, sprite, Color.white, pos, Vector2.one * escala, 3, pai);
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic; // atravessam paredes
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.45f;
        go.AddComponent<EnemyDamage>();
        go.AddComponent<EnemyHealth>().somMorte = somMorteInimigo;
        return go;
    }

    // ---------------------------------------------------------------- UI

    static Transform CriarCanvas()
    {
        var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.layer = LayerMask.NameToLayer("UI");
        canvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        return canvasGO.transform;
    }

    static AudioSource CriarMusica()
    {
        var fonteMusica = new GameObject("Musica").AddComponent<AudioSource>();
        fonteMusica.clip = musica;
        fonteMusica.loop = true;
        fonteMusica.playOnAwake = true;
        fonteMusica.volume = 0.5f;
        return fonteMusica;
    }

    static void CriarUI(GameObject player)
    {
        var canvas = CriarCanvas();

        // HUD
        var score = Texto(canvas, "Score", "Almas: 0/0", 44, TextAlignmentOptions.TopLeft,
            new Vector2(0, 1), new Vector2(40, -25), new Vector2(500, 70));
        var tempo = Texto(canvas, "Tempo", "Ritual: 30.0s", 52, TextAlignmentOptions.Top,
            new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(600, 80));
        tempo.color = new Color(0.8f, 0.9f, 1f);

        Texto(canvas, "Label Vida", "Vida", 32, TextAlignmentOptions.Right,
            new Vector2(1, 1), new Vector2(-470, -30), new Vector2(160, 40));
        var vida = Barra(canvas, "Barra Vida", new Color(0.9f, 0.25f, 0.3f), new Vector2(-40, -30));
        Texto(canvas, "Label Stamina", "Stamina", 32, TextAlignmentOptions.Right,
            new Vector2(1, 1), new Vector2(-470, -80), new Vector2(160, 40));
        var stamina = Barra(canvas, "Barra Stamina", new Color(0.3f, 0.85f, 0.5f), new Vector2(-40, -80));

        var dica = Texto(canvas, "Controles", "Mover: WASD / setas    Dash: Espaço (A)    Atacar: J / clique (X)", 26,
            TextAlignmentOptions.Bottom, new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(1400, 40));
        dica.color = new Color(1f, 1f, 1f, 0.55f);

        // Painel de fim de jogo
        var painel = new GameObject("EndGamePanel", typeof(RectTransform), typeof(Image));
        painel.transform.SetParent(canvas, false);
        Esticar(painel.GetComponent<RectTransform>());
        painel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

        var resultado = Texto(painel.transform, "Resultado", "Vitória", 140, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), new Vector2(0, 80), new Vector2(1200, 400));

        var reiniciar = painel.AddComponent<ReiniciarJogo>();
        var tamBotao = new Vector2(520, 100);
        reiniciar.botaoPadrao = Botao(painel.transform, "Botao Reiniciar", "Jogar novamente (R)",
            new Vector2(-290, -220), tamBotao, reiniciar.Reiniciar);
        Botao(painel.transform, "Botao Menu", "Menu principal (M)", new Vector2(290, -220), tamBotao, reiniciar.VoltarMenu);

        var fonteMusica = CriarMusica();

        // UIManager
        var ui = new GameObject("UIManager").AddComponent<UIManager>();
        ui.endGamePanel = painel;
        ui.resultText = resultado;
        ui.scoreText = score;
        ui.timeText = tempo;
        ui.barraVida = vida;
        ui.barraStamina = stamina;
        ui.playerHealth = player.GetComponent<PlayerHealth>();
        ui.playerMovement = player.GetComponent<PlayerMovement>();
        ui.musica = fonteMusica;
        ui.somVitoria = somVitoria;
        ui.somDerrota = somDerrota;
    }

    // ---------------------------------------------------------------- Menu principal

    static void CriarMenu()
    {
        CriarCamera();

        // Almas flutuando ao fundo, só de enfeite
        var deco = new GameObject("Decoracao").transform;
        var rnd = new System.Random(3);
        for (int i = 0; i < 28; i++)
        {
            var pos = new Vector2((float)rnd.NextDouble() * 32f - 16f, (float)rnd.NextDouble() * 19f - 8.5f);
            var cor = new Color(1f, 1f, 1f, 0.15f + 0.4f * (float)rnd.NextDouble());
            CriarSpriteObj("Alma", spriteAlma, cor, pos, Vector2.one * (0.5f + (float)rnd.NextDouble()), 0, deco);
        }

        CriarMusica();
        var canvas = CriarCanvas();
        var menu = new GameObject("MainMenu").AddComponent<MainMenu>();
        var centro = new Vector2(0.5f, 0.5f);

        var titulo = Texto(canvas, "Titulo", "Cidade das Almas", 130, TextAlignmentOptions.Center,
            centro, new Vector2(0, 300), new Vector2(1600, 200));
        titulo.color = new Color(0.7f, 0.92f, 1f);
        titulo.fontStyle = FontStyles.Bold;

        // Painel principal
        var principal = new GameObject("Painel Principal", typeof(RectTransform));
        principal.transform.SetParent(canvas, false);
        Esticar(principal.GetComponent<RectTransform>());
        var subtitulo = Texto(principal.transform, "Subtitulo", "Colete as almas antes que o ritual se complete", 38,
            TextAlignmentOptions.Center, centro, new Vector2(0, 185), new Vector2(1400, 60));
        subtitulo.color = new Color(1f, 1f, 1f, 0.65f);

        var tamBotao = new Vector2(480, 100);
        menu.primeiroBotao = Botao(principal.transform, "Botao Jogar", "Jogar", new Vector2(0, 20), tamBotao, menu.Jogar);
        Botao(principal.transform, "Botao Como Jogar", "Como jogar", new Vector2(0, -110), tamBotao, menu.ComoJogar);
        menu.botaoSair = Botao(principal.transform, "Botao Sair", "Sair", new Vector2(0, -240), tamBotao, menu.Sair);

        // Painel "Como jogar"
        var comoJogar = new GameObject("Painel Como Jogar", typeof(RectTransform), typeof(Image));
        comoJogar.transform.SetParent(canvas, false);
        Posicionar(comoJogar.GetComponent<RectTransform>(), centro, new Vector2(0, -90), new Vector2(1200, 580));
        comoJogar.GetComponent<Image>().color = new Color(0.1f, 0.08f, 0.16f, 0.92f);
        Texto(comoJogar.transform, "Instrucoes",
            "<b>Objetivo</b>\n" +
            "Colete todas as almas azuis antes que o tempo do ritual acabe. Cada alma dá +3 segundos.\n\n" +
            "<b>Perigos</b>\n" +
            "Tropas vermelhas patrulham as ruas, guardiões roxos perseguem quem chega perto " +
            "e as poças verdes corroem sua vida.\n\n" +
            "<b>Controles</b>\n" +
            "Mover: WASD / setas / analógico\n" +
            "Dash: Espaço / A  (gasta stamina e deixa invulnerável)\n" +
            "Atacar: J / clique / X",
            32, TextAlignmentOptions.TopLeft, centro, new Vector2(0, 50), new Vector2(1080, 440));
        menu.botaoVoltar = Botao(comoJogar.transform, "Botao Voltar", "Voltar", new Vector2(0, -225), new Vector2(360, 90),
            menu.MostrarPrincipal);

        menu.painelPrincipal = principal;
        menu.painelComoJogar = comoJogar;
    }

    static GameObject Botao(Transform pai, string nome, string texto, Vector2 pos, Vector2 tam, UnityAction acao)
    {
        var go = new GameObject(nome, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(pai, false);
        Posicionar(go.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), pos, tam);

        // Cor de destaque bem visível para quem navega pelo teclado/controle
        var botao = go.GetComponent<Button>();
        var cores = botao.colors;
        cores.normalColor = new Color(0.3f, 0.22f, 0.5f);
        cores.highlightedColor = new Color(0.5f, 0.38f, 0.82f);
        cores.selectedColor = new Color(0.5f, 0.38f, 0.82f);
        cores.pressedColor = new Color(0.68f, 0.58f, 0.98f);
        botao.colors = cores;

        Texto(go.transform, "Texto", texto, 40, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), Vector2.zero, tam);
        UnityEventTools.AddPersistentListener(botao.onClick, acao);
        return go;
    }

    static void Posicionar(RectTransform rt, Vector2 ancora, Vector2 pos, Vector2 tam)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = ancora;
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
    }

    static void Esticar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static TextMeshProUGUI Texto(Transform pai, string nome, string texto, float tamanho, TextAlignmentOptions alinhamento,
        Vector2 ancora, Vector2 pos, Vector2 tam)
    {
        var go = new GameObject(nome, typeof(RectTransform));
        go.transform.SetParent(pai, false);
        Posicionar(go.GetComponent<RectTransform>(), ancora, pos, tam);
        var t = go.AddComponent<TextMeshProUGUI>();
        if (fonte != null) t.font = fonte;
        t.text = texto;
        t.fontSize = tamanho;
        t.alignment = alinhamento;
        t.color = Color.white;
        t.raycastTarget = false;
        return t;
    }

    static Slider Barra(Transform pai, string nome, Color cor, Vector2 pos)
    {
        var res = new DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
        };
        var go = DefaultControls.CreateSlider(res);
        go.name = nome;
        go.transform.SetParent(pai, false);
        Posicionar(go.GetComponent<RectTransform>(), new Vector2(1, 1), pos, new Vector2(420, 32));

        var slider = go.GetComponent<Slider>();
        UnityEngine.Object.DestroyImmediate(slider.handleRect.parent.gameObject);
        slider.handleRect = null;
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;
        slider.navigation = new Navigation { mode = Navigation.Mode.None };
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.value = 100f;

        Esticar((RectTransform)go.transform.Find("Background"));
        go.transform.Find("Background").GetComponent<Image>().color = new Color(0.12f, 0.1f, 0.18f);
        var area = (RectTransform)go.transform.Find("Fill Area");
        Esticar(area);
        area.offsetMin = new Vector2(3, 3);
        area.offsetMax = new Vector2(-3, -3);
        slider.fillRect.sizeDelta = Vector2.zero;
        slider.fillRect.GetComponent<Image>().color = cor;
        return slider;
    }
}
