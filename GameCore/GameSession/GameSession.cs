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
}

/// <summary>
/// Estado do jogo que precisa sobreviver às trocas de cena (e ao salvamento).
/// Regra: toda informação que o jogo precisa lembrar mora aqui e é só dado simples
/// (números, textos, ids), nunca Sprite ou textura.
/// </summary>
public static class GameSession
{
    // Armazena a pontuação global do jogador entre todas as telas
    public static int Score { get; set; } = 0;

    // Armazena o inventário global com os itens coletados
    public static InventoryController Inventory { get; set; } = new InventoryController();

    // Em que ponto da história o jogador está (libera portas e muda os diálogos).
    public static StoryStage Stage { get; set; } = StoryStage.Intro;

    /// <summary>
    /// Zera tudo para começar um novo jogo. Chame ao escolher "Novo jogo",
    /// porque estes dados são estáticos e sobrevivem a uma volta ao título.
    /// </summary>
    public static void Reset()
    {
        Score = 0;
        Inventory = new InventoryController();
        Stage = StoryStage.Intro;
    }
    public static SaveData CreateSaveData(string sceneId, Vector2 playerPosition)
    {
        SaveData data = new()
        {
            SceneId = sceneId,
            PlayerX = playerPosition.X,
            PlayerY = playerPosition.Y,
            Score = Score,
            Stage = Stage
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

        // Mantém a mesma instância do inventário (cenas podem estar guardando a referência).
        Inventory.Clear();
        foreach (var item in data.Items)
        {
            Inventory.CollectItem(item.Key, item.Value);
        }
    }
}