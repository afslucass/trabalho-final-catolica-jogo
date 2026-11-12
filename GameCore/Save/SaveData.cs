using System;
using System.Collections.Generic;

namespace GameCore.Save;

public class SaveData
{
    // Aumente este número só se mudar o formato de um jeito que quebre saves antigos
    // (por exemplo, renomear ou remover uma propriedade).
    public const int CURRENT_VERSION = 1;

    public int Version { get; set; } = CURRENT_VERSION;

    public DateTime SavedAtUtc { get; set; }

    // Qual cena estava ativa (ver ISaveableScene.SaveId) e onde o jogador estava nela.
    public string SceneId { get; set; } = "";
    public float PlayerX { get; set; }
    public float PlayerY { get; set; }

    public int Score { get; set; }

    // Em que ponto da história o jogador está. É gravado como texto
    // ("FalouComVigia"), então reordenar o enum não corrompe saves antigos.
    public StoryStage Stage { get; set; } = StoryStage.Intro;

    // Itens do inventário: id do item -> nome da região do ícone no atlas.
    public Dictionary<string, string> Items { get; set; } = new();
}