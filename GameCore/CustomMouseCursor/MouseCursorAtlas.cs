using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GameCore.CustomMouseCursor;

/// <summary>
/// Represents a rectangular region within a texture.
/// </summary>
public class MouseCursorAtlas
{
    private Dictionary<string, Texture2D> _cursortexture;
    private Dictionary<string, MouseCursor> _cursors;

    public MouseCursorAtlas()
    {
        _cursortexture = new Dictionary<string, Texture2D>();    
        _cursors = new Dictionary<string, MouseCursor>();    
    }

    private void AddCursor(ContentManager content, string name, string texturePath)
    {
        Texture2D _texture = content.Load<Texture2D>(texturePath);
        MouseCursor _cursor = MouseCursor.FromTexture2D(_texture, 0, 0);
        _cursortexture.Add(name, _texture);
        _cursors.Add(name, _cursor);
    }

    public static MouseCursorAtlas FromFile(ContentManager content, string fileName)
    {
        MouseCursorAtlas atlas = new MouseCursorAtlas();

        string filePath = Path.Combine(content.RootDirectory, fileName);

        using (Stream stream = TitleContainer.OpenStream(filePath))
        {
            using (XmlReader reader = XmlReader.Create(stream))
            {
                XDocument doc = XDocument.Load(reader);
                XElement root = doc.Root;

                var textures = root.Elements("Texture");

                if (textures != null)
                {
                    foreach (var texture in textures)
                    {
                        string name = texture.Attribute("name")?.Value;
                        string texturePath = texture?.Value;
                        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(texturePath))
                        {
                            atlas.AddCursor(content, name, texturePath);
                        }
                    }
                }

                return atlas;
            }
        }
    }

    public void SetCursor(string name)
    {
        Mouse.SetCursor(_cursors[name]);
    }
}
