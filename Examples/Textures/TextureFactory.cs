using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Hefty.Examples.Textures;

public static class TextureFactory
{
    public static Texture2D CreateBlankTexture(GraphicsDevice graphicsDevice)
    {
        Texture2D texture = new(graphicsDevice, 1, 1);
        texture.SetData([Color.White]);

        return texture;
    }

    public static Texture2D CreateCheckeredTexture(
        GraphicsDevice graphicsDevice,
        int width,
        int height,
        int checkSize)
    {
        return CreateCheckeredTexture(
            graphicsDevice,
            width,
            height,
            checkSize,
            Color.LightGray,
            Color.DarkGray
        );
    }

    public static Texture2D CreateCheckeredTexture(
        GraphicsDevice graphicsDevice,
        int width,
        int height,
        int checkSize,
        Color firstColor,
        Color secondColor)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width, nameof(width));
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height, nameof(height));
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(checkSize, nameof(checkSize));

        Color[] pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool useFirstColor = ((x / checkSize) + (y / checkSize)) % 2 == 0;
                pixels[y * width + x] = useFirstColor ? firstColor : secondColor;
            }
        }

        Texture2D texture = new(graphicsDevice, width, height);
        texture.SetData(pixels);
        return texture;
    }
}
