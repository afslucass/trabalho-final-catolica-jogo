using GameCore.Inventory;

namespace GameCore;

public static class GameSession
{
    // Armazena a pontuação global do jogador entre todas as telas
    public static int Score { get; set; } = 0;

    // Armazena o inventário global com os itens coletados
    public static InventoryController Inventory { get; set; } = new InventoryController();

    public static void Reset()
    {
        Score = 0;
        Inventory = new InventoryController();
    }
}