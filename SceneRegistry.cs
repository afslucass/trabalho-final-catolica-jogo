using GameCore;
using GameCore.Save;
using GameCore.Scenes;
using TestGame.Scenes;

namespace TestGame;

/// <summary>
/// Liga o id de uma cena gravado no save (SaveId) à cena de verdade.
/// </summary>
public static class SceneRegistry
{
    /// <summary>
    /// Cria a cena que estava ativa quando o jogo foi salvo.
    /// Quem criar uma sala nova adiciona um "case" aqui.
    /// </summary>
    public static Scene Create(SaveData data)
    {
        switch (data.SceneId)
        {
            case "recepcao":
                return new Scene_recepcao(RecepcaoSpawn.Start, data.PlayerX);

            case "caldeirao":
                return new Scene_caldeirao();
        
            default:
                // Cena desconhecida (por exemplo, um save de uma sala que ainda não
                // existe): volta para a recepção em vez de travar.
                return new Scene_recepcao();
        }
    }

    /// <summary>
    /// Carrega o save e abre a cena correspondente.
    /// Retorna false se não há save válido (o jogo continua onde estava).
    /// </summary>
    public static bool ContinueGame()
    {
        if (!SaveManager.TryLoad(out SaveData data))
        {
            return false;
        }

        GameSession.LoadFrom(data);
        Core.ChangeScene(Create(data));
        return true;
    }
}