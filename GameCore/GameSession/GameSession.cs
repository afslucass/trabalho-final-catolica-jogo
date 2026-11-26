using GameCore.Inventory;
using GameCore.Save;
using Microsoft.Xna.Framework;

namespace GameCore;

public enum StoryStage
{
    /// <summary>Acabou de chegar ao Limbo e ainda não falou com o Vigia.</summary>
    Intro,

    /// <summary>Falou com o Vigia, que mandou ir à Caldeira.</summary>
    FalouComVigia,

    /// <summary>A Justiça pesou a alma (empate) e mandou voltar ao Vigia.</summary>
    Julgada,

    /// <summary>O Vigia levou Alma ao vagão assombrado (puzzle do Estranho).</summary>
    VagaoAssombrado,

    /// <summary>Passou pelo Estranho e segue para a sala da Morte.</summary>
    Final
}

/// <summary>
/// Ids dos itens (o que é salvo no inventário) e os nomes das regiões dos
/// ícones no atlas. Ficam num lugar só para as cenas não repetirem os textos.
/// </summary>
public static class GameItems
{
    // Máscara: fica no espantalho do vagão assombrado.
    public const string MaskId = "mascara";
    public const string MaskIcon = "item-face";

    // Colar do Vigia. Por enquanto só o item-skirt-1 está em uso.
    public const string NecklaceId = "colar-vigia";
    public const string NecklaceIcon = "item-skirt-1";

    // Item que fica na Caldeira, com a Justiça. Provisório: ainda falta
    // confirmar qual dos dois desenhos de cabelo é.
    public const string HairId = "cabelo";
    public const string HairIcon = "item-hair-1";

    /// <summary>
    /// O disfarce são 3 itens: 1 = máscara (vagão assombrado), 2 = colar (recepção)
    /// e 3 = cabelo (Caldeira). Com os três o Estranho deixa o jogador passar.
    /// </summary>
    public static bool HasFullDisguise =>
        GameSession.Inventory.Has(MaskId) &&
        GameSession.Inventory.Has(NecklaceId) &&
        GameSession.Inventory.Has(HairId);
}

/// <summary>
/// Estado do jogo que precisa sobreviver às trocas de cena (e ao salvamento).
/// Regra: toda informação que o jogo precisa lembrar mora aqui e é só dado simples
/// (números, textos, ids), nunca Sprite ou textura.
/// </summary>
public static class GameSession
{
    public static bool IntroSeen { get; set; } = false;
    // Armazena a pontuação global do jogador entre todas as telas
    public static int Score { get; set; } = 0;
    public static bool IntroCutscenePlayed { get; set; } = false;

    // Armazena o inventário global com os itens coletados
    public static InventoryController Inventory { get; set; } = new InventoryController();

    // Em que ponto da história o jogador está (libera portas e muda os diálogos).
    public static StoryStage Stage { get; set; } = StoryStage.Intro;

    // Quantas vezes o jogador tentou sair do vagão assombrado sem a máscara
    // (cabeça da boneca). Cada vez aumenta a velocidade do Raposo na sala.
    public static int HauntedRetries { get; set; } = 0;

    // true depois que o jogador falou com o Estranho. Só então os itens do disfarce
    // podem ser coletados.
    public static bool HauntedPuzzleStarted { get; set; } = false;

    // Primeiro checkpoint: o momento em que o Vigia coloca Alma no vagão assombrado.
    // O game over volta para ele.
    public static SaveData Checkpoint { get; private set; }

    /// <summary>
    /// Grava o checkpoint (na memória e no arquivo de save). O puzzle volta ao zero
    /// nele: sem conversa com o Estranho, sem voltas e sem itens novos.
    /// </summary>
    public static void SaveCheckpoint(float playerX)
    {
        if (Stage < StoryStage.VagaoAssombrado)
            Stage = StoryStage.VagaoAssombrado;

        SaveData data = CreateSaveData("recepcao", new Vector2(playerX, 0f));
        data.HauntedRetries = 0;
        data.HauntedPuzzleStarted = false;

        Checkpoint = data;
        SaveManager.Save(data);
    }

    /// <summary>Cria o checkpoint só se ainda não existir um.</summary>
    public static void EnsureCheckpoint(float playerX)
    {
        if (Checkpoint == null)
            SaveCheckpoint(playerX);
    }

    /// <summary>Volta o estado do jogo para o checkpoint (usado no game over).</summary>
    public static void RestoreCheckpoint()
    {
        if (Checkpoint != null)
        {
            LoadFrom(Checkpoint);
            return;
        }

        // Sem checkpoint gravado: volta ao ponto em que o vagão assombrado é liberado.
        Inventory.Clear();
        Stage = StoryStage.VagaoAssombrado;
        HauntedRetries = 0;
        HauntedPuzzleStarted = false;
    }

    /// <summary>
    /// Zera tudo para começar um novo jogo. Chame ao escolher "Novo jogo",
    /// porque estes dados são estáticos e sobrevivem a uma volta ao título.
    /// </summary>
    public static void Reset()
    {
        Score = 0;
        Inventory = new InventoryController();
        Stage = StoryStage.Intro;
        HauntedRetries = 0;
        HauntedPuzzleStarted = false;
        Checkpoint = null;
        IntroCutscenePlayed = false;
        IntroSeen = false;
    }
    public static SaveData CreateSaveData(string sceneId, Vector2 playerPosition)
    {
        
        SaveData data = new()
        {
            SceneId = sceneId,
            PlayerX = playerPosition.X,
            PlayerY = playerPosition.Y,
            Score = Score,
            Stage = Stage,
            HauntedRetries = HauntedRetries,
            HauntedPuzzleStarted = HauntedPuzzleStarted,
            IntroCutscenePlayed = IntroCutscenePlayed,
            IntroSeen = IntroSeen
        };

        foreach (var item in Inventory.Items)
        {
            data.Items[item.Key] = item.Value;
        }
    
        return data;
    }
    public static void LoadFrom(SaveData data)
    {
        Score = data.Score;
        Stage = data.Stage;
        HauntedRetries = data.HauntedRetries;
        HauntedPuzzleStarted = data.HauntedPuzzleStarted;
        IntroCutscenePlayed = data.IntroCutscenePlayed;
        IntroSeen = data.IntroSeen;

        // Mantém a mesma instância do inventário (cenas podem estar guardando a referência).
        Inventory.Clear();
        foreach (var item in data.Items)
        {
            Inventory.CollectItem(item.Key, item.Value);
        }
    }
}