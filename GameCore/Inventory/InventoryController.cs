using System.Collections.Generic;
using GameCore.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameCore.Inventory;

public class InventoryController
{
    private const string ITEMS_ATLAS_FILE = "images/atlas-definition.xml";
    private static readonly Vector2 ICON_SCALE = new(4.0f, 4.0f);
    private const int RENDER_GAP = 12;
    private static TextureAtlas s_itemsAtlas;
    private readonly Dictionary<string, string> _items = new();
    private readonly Dictionary<string, Sprite> _icons = new();
    public int Count => _items.Count;

    /// <summary>Ids de todos os itens coletados, na ordem em que foram coletados.</summary>
    public IReadOnlyCollection<string> ItemIds => _items.Keys;
    /// <summary>Indica se o item já foi coletado.</summary>
    public bool Has(string id) => _items.ContainsKey(id);
    public bool CollectItem(string id, string iconRegionName)
    {
        if (_items.ContainsKey(id))
        {
            return false;
        }

        _items.Add(id, iconRegionName);
        return true;
    }

    public void Draw(SpriteBatch spriteBatch, Vector2 position)
    {
        s_itemsAtlas ??= TextureAtlas.FromFile(Core.Content, ITEMS_ATLAS_FILE);

        Vector2 iconPosition = position;

        foreach (var item in _items)
        {
            if (!_icons.TryGetValue(item.Key, out Sprite icon))
            {
                icon = s_itemsAtlas.CreateSprite(item.Value);
                icon.Scale = ICON_SCALE;
                _icons[item.Key] = icon;
            }

            icon.Draw(spriteBatch, iconPosition);
            iconPosition.X += RENDER_GAP + icon.Width;
        }
    }
}