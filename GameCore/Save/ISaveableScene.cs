using Microsoft.Xna.Framework;

namespace GameCore.Save;

public interface ISaveableScene
{
    /// <summary>Identificador único da cena no arquivo de save (ex.: "recepcao").</summary>
    string SaveId { get; }

    /// <summary>Posição do jogador nesta cena (o que fizer sentido para ela).</summary>
    Vector2 SavePlayerPosition { get; }
}