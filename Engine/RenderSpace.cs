namespace Hefty.Engine;

/// <summary>Selects whether camera transformation is applied while drawing an object.</summary>
public enum RenderSpace
{
    /// <summary>Draw through the active camera.</summary>
    World,
    /// <summary>Draw in viewport coordinates.</summary>
    Screen
}
