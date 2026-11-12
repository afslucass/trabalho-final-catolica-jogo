using System;
using System.Collections.Generic;
using GameCore.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameCore.Inventory;

public class InventoryController
{
    // Atlas file containing the inventory item icons.
    private const string ITEMS_ATLAS_FILE = "images/characters-atlas-definition.xml";

    // Maximum icon size in pixels, preserving the original aspect ratio.
    private const float ICON_SIZE = 64f;

    // Horizontal spacing between inventory icons.
    private const int RENDER_GAP = 12;

    // Shared icon atlas, loaded only once across inventory instances.
    private static TextureAtlas s_itemsAtlas;

    // Maps each item ID to its icon region in the atlas.
    private readonly Dictionary<string, string> _items = new();

    // Caches created sprites to avoid recreating them every frame.

    public IReadOnlyDictionary<string, string> Items => _items;

    private readonly Dictionary<string, Sprite> _icons = new();

    /// <summary>Gets the number of collected items.</summary>
    public int Count => _items.Count;

    /// <summary>Gets collected item IDs in collection order.</summary>
    public IReadOnlyCollection<string> ItemIds => _items.Keys;

    /// <summary>Checks whether an item has been collected.</summary>
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
        // Loads the shared atlas when it is first needed.
        s_itemsAtlas ??= TextureAtlas.FromFile(Core.Content, ITEMS_ATLAS_FILE);

        Vector2 iconPosition = position;

        foreach (var item in _items)
        {
            if (!_icons.TryGetValue(item.Key, out Sprite icon))
            {
                icon = s_itemsAtlas.CreateSprite(item.Value);

                // Scales the icon to fit within the maximum size.
                float fit = ICON_SIZE / MathF.Max(icon.Width, icon.Height);
                icon.Scale = new Vector2(fit, fit);

                _icons[item.Key] = icon;
            }

            // Draws the icon and advances to the next position.
            icon.Draw(spriteBatch, iconPosition);
            iconPosition.X += RENDER_GAP + icon.Width;
        }
    }
    public void Clear()
    {
        _items.Clear();
        _icons.Clear();
    }
}
