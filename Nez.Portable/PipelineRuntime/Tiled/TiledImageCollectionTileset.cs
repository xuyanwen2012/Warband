using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Nez.Textures;

namespace Nez.Tiled
{
    public class TiledImageCollectionTileset : TiledTileset
    {
        public TiledImageCollectionTileset(Texture2D texture, int firstId)
            : base(texture, firstId)
        {
        }

        public void setTileTextureRegion(int tileId, Rectangle sourceRect)
        {
            _regions[tileId] = new Subtexture(texture, sourceRect);
        }
    }
}