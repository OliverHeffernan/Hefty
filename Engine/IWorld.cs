namespace Hefty.Engine;

/// <summary>Defines a scene whose objects and bindings share one lifetime.</summary>
public interface IWorld
{
    /// <summary>Loads resources, bindings, and objects through the new active context.</summary>
    void Load(WorldContext context);
    /// <summary>Releases resources created by the world. All objects are destroyed immediately afterward.</summary>
    void Unload(WorldContext context) { }
}
