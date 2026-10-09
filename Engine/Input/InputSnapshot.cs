using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Hefty.Engine.Input;

/// <summary>One immutable sample of all devices. Unfocused samples are treated as neutral.</summary>
public readonly record struct InputSnapshot(KeyboardState Keyboard, MouseState Mouse,
    GamePadState One = default, GamePadState Two = default, GamePadState Three = default,
    GamePadState Four = default, bool IsFocused = true)
{
    public GamePadState GamePad(PlayerIndex player) => player switch
    {
        PlayerIndex.One => One, PlayerIndex.Two => Two, PlayerIndex.Three => Three,
        PlayerIndex.Four => Four, _ => throw new ArgumentOutOfRangeException(nameof(player))
    };
}

/// <summary>Supplies exactly one snapshot per InputManager update; implementations may replay or synthesize devices.</summary>
public interface IInputSource { InputSnapshot Sample(); }

public sealed class DeviceInputSource(Func<bool>? isFocused = null) : IInputSource
{
    public InputSnapshot Sample() => new(Keyboard.GetState(), Mouse.GetState(),
        GamePad.GetState(PlayerIndex.One, GamePadDeadZone.None),
        GamePad.GetState(PlayerIndex.Two, GamePadDeadZone.None),
        GamePad.GetState(PlayerIndex.Three, GamePadDeadZone.None),
        GamePad.GetState(PlayerIndex.Four, GamePadDeadZone.None), isFocused?.Invoke() ?? true);
}

public enum GamePadAxis { LeftX, LeftY, RightX, RightY, LeftTrigger, RightTrigger }

public readonly record struct GamePadButtonBinding(Buttons Button, PlayerIndex Player = PlayerIndex.One) : IInputBinding
{
    public bool IsDown(KeyboardState keyboard, MouseState mouse) => false;
    public bool IsDown(InputSnapshot snapshot) => snapshot.GamePad(Player).IsConnected && snapshot.GamePad(Player).IsButtonDown(Button);
}

/// <summary>Digital threshold on a raw axis; negative thresholds select the negative direction.</summary>
public readonly record struct GamePadAxisBinding(GamePadAxis Axis, float Threshold = 0.5f,
    PlayerIndex Player = PlayerIndex.One) : IInputBinding
{
    public bool IsDown(KeyboardState keyboard, MouseState mouse) => false;
    public bool IsDown(InputSnapshot snapshot)
    {
        if (!float.IsFinite(Threshold) || Threshold == 0 || MathF.Abs(Threshold) > 1)
            throw new ArgumentOutOfRangeException(nameof(Threshold));
        float value = InputManager.ReadAxis(snapshot.GamePad(Player), Axis);
        return Threshold > 0 ? value >= Threshold : value <= Threshold;
    }
}

/// <summary>Rescaled deadzones. Inputs clamp to the unit interval/disc; outputs retain direction.</summary>
public static class InputDeadzone
{
    public static float Apply(float value, float deadzone = 0.2f)
    {
        Validate(deadzone);
        if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
        return MathF.Sign(value) * Math.Clamp((MathF.Abs(value) - deadzone) / (1 - deadzone), 0, 1);
    }

    public static Vector2 ApplyRadial(Vector2 value, float deadzone = 0.2f)
    {
        Validate(deadzone);
        if (!float.IsFinite(value.X) || !float.IsFinite(value.Y)) throw new ArgumentOutOfRangeException(nameof(value));
        float length = value.Length();
        return length <= deadzone ? Vector2.Zero : value / length * Apply(length, deadzone);
    }

    private static void Validate(float value)
    {
        if (!float.IsFinite(value) || value < 0 || value >= 1) throw new ArgumentOutOfRangeException(nameof(value));
    }
}
