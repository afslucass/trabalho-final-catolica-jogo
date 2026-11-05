using GameCore.Inventory;

namespace GameCore;
public enum StoryStage
{
    Intro,

    FalouComVigia,

    /// <summary>The Justice judged the soul (tie) and sent Alma back to the Watcher.</summary>
    Julgada,

    /// <summary>The Watcher sent Alma to the Haunted Train (Stranger puzzle).</summary>
    VagaoAssombrado,

    /// <summary>Alma passed the Stranger and proceeds to the Death room.</summary>
    Final
}

/// <summary>
/// Defines item IDs stored in the inventory and their corresponding atlas icon regions.
/// Centralizes these values to avoid duplication across scenes.
/// </summary>
public static class GameItems
{
    // Mask found on the Haunted Train scarecrow.
    public const string MaskId = "mascara";
    public const string MaskIcon = "item-face";

    // Watcher's necklace icon region.
    public const string NecklaceId = "colar-vigia";
    public const string NecklaceIcon = "item-skirt-1";

    // Item found in the Boiler with the Justice.
    public const string HairId = "cabelo";
    public const string HairIcon = "item-hair-1";
}

public static class GameSession
{
    // Stores the player's global score across scenes.
    public static int Score { get; set; } = 0;

    // Stores the shared inventory of collected items.
    public static InventoryController Inventory { get; set; } = new InventoryController();

    // Tracks story progress, controlling exits and dialog selection.
    public static StoryStage Stage { get; set; } = StoryStage.Intro;

    public static void Reset()
    {
        // Restores all session values to their initial state.
        Score = 0;
        Inventory = new InventoryController();
        Stage = StoryStage.Intro;
    }
}
