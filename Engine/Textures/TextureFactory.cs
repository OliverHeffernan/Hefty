using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Hefty.Engine.Textures;

/// <summary>Creates simple runtime textures without using the MonoGame content pipeline.</summary>
public static class TextureFactory
{
    /// <summary>Creates a one-pixel opaque white texture suitable for tinting and scaling.</summary>
    /// <param name="graphicsDevice">The graphics device that owns the texture resource.</param>
    /// <returns>A new caller-owned texture. The caller must dispose it.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="graphicsDevice"/> is <see langword="null"/>.</exception>
    public static Texture2D CreateBlankTexture(GraphicsDevice graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);

        Texture2D texture = new(graphicsDevice, 1, 1);
        texture.SetData([Color.White]);
        return texture;
    }

    /// <summary>Creates a light-gray and dark-gray checkerboard texture.</summary>
    /// <param name="graphicsDevice">The graphics device that owns the texture resource.</param>
    /// <param name="width">Texture width in pixels.</param>
    /// <param name="height">Texture height in pixels.</param>
    /// <param name="cellSize">Width and height of each checkerboard cell in pixels.</param>
    /// <returns>A new caller-owned texture. The caller must dispose it.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="graphicsDevice"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="width"/>, <paramref name="height"/>, or <paramref name="cellSize"/> is not positive.
    /// </exception>
    /// <exception cref="OverflowException">The requested pixel count exceeds <see cref="int.MaxValue"/>.</exception>
    public static Texture2D CreateCheckerboard(
        GraphicsDevice graphicsDevice,
        int width,
        int height,
        int cellSize)
    {
        return CreateCheckerboard(
            graphicsDevice,
            width,
            height,
            cellSize,
            Color.LightGray,
            Color.DarkGray);
    }

    /// <summary>Creates a checkerboard texture using two alternating colors.</summary>
    /// <param name="graphicsDevice">The graphics device that owns the texture resource.</param>
    /// <param name="width">Texture width in pixels.</param>
    /// <param name="height">Texture height in pixels.</param>
    /// <param name="cellSize">Width and height of each checkerboard cell in pixels.</param>
    /// <param name="firstColor">Color of the top-left cell and alternating cells.</param>
    /// <param name="secondColor">Color of the remaining cells.</param>
    /// <returns>A new caller-owned texture. The caller must dispose it.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="graphicsDevice"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="width"/>, <paramref name="height"/>, or <paramref name="cellSize"/> is not positive.
    /// </exception>
    /// <exception cref="OverflowException">The requested pixel count exceeds <see cref="int.MaxValue"/>.</exception>
    public static Texture2D CreateCheckerboard(
        GraphicsDevice graphicsDevice,
        int width,
        int height,
        int cellSize,
        Color firstColor,
        Color secondColor)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cellSize);

        Color[] pixels = new Color[checked(width * height)];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool useFirstColor = ((x / cellSize) + (y / cellSize)) % 2 == 0;
                pixels[y * width + x] = useFirstColor ? firstColor : secondColor;
            }
        }

        Texture2D texture = new(graphicsDevice, width, height);
        texture.SetData(pixels);
        return texture;
    }
}
