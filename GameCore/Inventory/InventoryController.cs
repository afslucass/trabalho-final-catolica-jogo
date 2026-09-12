
using System.Collections.Generic;
using GameCore.Graphics;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace GameCore.Inventory;

public class InventoryController
{
    private Dictionary<string, Sprite> _sprites;
    private const int RENDER_GAP = 12;

    public InventoryController()
    {
        _sprites = new Dictionary<string, Sprite>();
    }

    public void CollectItem(string name, Sprite sprite)
    {
        Sprite copy = new Sprite(sprite.Region);
        copy.Scale = sprite.Scale;
        _sprites.Add(name, copy);
    }

    public Dictionary<string, Sprite> GetItems()
    {
        return _sprites;
    }


    public void Draw(SpriteBatch spriteBatch, Vector2 position)
    {
        Vector2 _spritePosition = position;
        foreach(var _sprite in _sprites)
        {
            _sprite.Value.Draw(spriteBatch, _spritePosition);
            _spritePosition.X += RENDER_GAP + _sprite.Value.Width;
        }
    }
}