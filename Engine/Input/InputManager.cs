using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Hefty.Engine.Input;

/// <summary>Identifies a mouse button that can be queried or bound to an action.</summary>
public enum MouseButton
{
    /// <summary>The primary mouse button.</summary>
    Left,
    /// <summary>The wheel or middle mouse button.</summary>
    Middle,
    /// <summary>The secondary mouse button.</summary>
    Right,
    /// <summary>The first auxiliary mouse button.</summary>
    XButton1,
    /// <summary>The second auxiliary mouse button.</summary>
    XButton2
}

/// <summary>Tests whether a device input is currently down.</summary>
public interface IInputBinding
{
    /// <summary>Returns whether this binding is active in the supplied device snapshots.</summary>
    bool IsDown(KeyboardState keyboard, MouseState mouse);
    /// <summary>Snapshot-aware binding. Existing keyboard/mouse implementations remain compatible.</summary>
    bool IsDown(InputSnapshot snapshot) => IsDown(snapshot.Keyboard, snapshot.Mouse);
}

/// <summary>Binds an action to one keyboard key.</summary>
public readonly record struct KeyboardBinding(Keys Key) : IInputBinding
{
    /// <inheritdoc />
    public bool IsDown(KeyboardState keyboard, MouseState mouse) => keyboard.IsKeyDown(Key);
}

/// <summary>Binds an action to one mouse button.</summary>
public readonly record struct MouseBinding(MouseButton Button) : IInputBinding
{
    /// <inheritdoc />
    public bool IsDown(KeyboardState keyboard, MouseState mouse) => Button switch
    {
        MouseButton.Left => mouse.LeftButton == ButtonState.Pressed,
        MouseButton.Middle => mouse.MiddleButton == ButtonState.Pressed,
        MouseButton.Right => mouse.RightButton == ButtonState.Pressed,
        MouseButton.XButton1 => mouse.XButton1 == ButtonState.Pressed,
        MouseButton.XButton2 => mouse.XButton2 == ButtonState.Pressed,
        _ => false
    };
}

/// <summary>Polls device state once per frame and exposes named, rebindable actions.</summary>
public sealed class InputManager
{
    private sealed class ActionState
    {
        public readonly List<IInputBinding> Bindings = [];
        public bool Previous;
        public bool Current;
    }

    private readonly Dictionary<string, ActionState> actions = new(StringComparer.Ordinal);
    private readonly IInputSource source;
    private bool sampled;
    private InputSnapshot previous;
    /// <summary>The latest sampled devices. Never polls hardware when read.</summary>
    public InputSnapshot Snapshot { get; private set; }
    internal bool HostOwned { get; init; }
    private KeyboardState previousKeyboard;
    private KeyboardState currentKeyboard;
    private MouseState previousMouse;
    private MouseState currentMouse;

    /// <summary>Gets the latest mouse position.</summary>
    public Point MousePosition => currentMouse.Position;

    /// <summary>Returns whether a mouse button became down during the current frame.</summary>
    public bool IsMouseButtonPressed(MouseButton button) =>
        IsMouseButtonDown(currentMouse, button) && !IsMouseButtonDown(previousMouse, button);

    /// <summary>Returns whether a mouse button became up during the current frame.</summary>
    public bool IsMouseButtonReleased(MouseButton button) =>
        !IsMouseButtonDown(currentMouse, button) && IsMouseButtonDown(previousMouse, button);

    /// <summary>Returns whether a mouse button is currently down.</summary>
    public bool IsMouseButtonDown(MouseButton button) => IsMouseButtonDown(currentMouse, button);

    /// <summary>Constructs without polling. The first update establishes an edge-free baseline.</summary>
    public InputManager(IInputSource? source = null) => this.source = source ?? new DeviceInputSource();

    /// <summary>Samples once. Hosted managers are sampled by HeftyGame and reject manual updates.</summary>
    public void Update()
    {
        if (HostOwned) throw new InvalidOperationException("The host samples input once per update.");
        Sample();
    }

    internal void Sample()
    {
        previous = Snapshot;
        InputSnapshot next = source.Sample();
        Snapshot = next.IsFocused ? next : new InputSnapshot(default, default, IsFocused: false);
        if (!sampled || !previous.IsFocused || !Snapshot.IsFocused)
            previous = Snapshot;
        else
            previous = previous with
            {
                One = Baseline(previous.One, Snapshot.One), Two = Baseline(previous.Two, Snapshot.Two),
                Three = Baseline(previous.Three, Snapshot.Three), Four = Baseline(previous.Four, Snapshot.Four)
            };
        sampled = true;
        previousKeyboard = previous.Keyboard;
        previousMouse = previous.Mouse;
        currentKeyboard = Snapshot.Keyboard;
        currentMouse = Snapshot.Mouse;

        foreach (ActionState action in actions.Values)
        {
            action.Previous = IsAnyBindingDown(action.Bindings, previous);
            action.Current = IsAnyBindingDown(action.Bindings);
        }
    }

    private static GamePadState Baseline(GamePadState old, GamePadState next) =>
        old.IsConnected != next.IsConnected ? next : old;

    public bool IsGamePadDown(Buttons button, PlayerIndex player = PlayerIndex.One) =>
        Snapshot.GamePad(player).IsConnected && Snapshot.GamePad(player).IsButtonDown(button);
    public bool IsGamePadPressed(Buttons button, PlayerIndex player = PlayerIndex.One) =>
        IsGamePadDown(button, player) && !previous.GamePad(player).IsButtonDown(button);
    public bool IsGamePadReleased(Buttons button, PlayerIndex player = PlayerIndex.One) =>
        !IsGamePadDown(button, player) && previous.GamePad(player).IsButtonDown(button);
    public float GetAxis(GamePadAxis axis, PlayerIndex player = PlayerIndex.One, float deadzone = 0.2f) =>
        InputDeadzone.Apply(ReadAxis(Snapshot.GamePad(player), axis), deadzone);
    public Vector2 GetStick(bool right = false, PlayerIndex player = PlayerIndex.One, float deadzone = 0.2f) =>
        InputDeadzone.ApplyRadial(!Snapshot.GamePad(player).IsConnected ? Vector2.Zero
            : right ? Snapshot.GamePad(player).ThumbSticks.Right : Snapshot.GamePad(player).ThumbSticks.Left, deadzone);

    internal static float ReadAxis(GamePadState pad, GamePadAxis axis) => !pad.IsConnected ? 0 : axis switch
    {
        GamePadAxis.LeftX => pad.ThumbSticks.Left.X, GamePadAxis.LeftY => pad.ThumbSticks.Left.Y,
        GamePadAxis.RightX => pad.ThumbSticks.Right.X, GamePadAxis.RightY => pad.ThumbSticks.Right.Y,
        GamePadAxis.LeftTrigger => pad.Triggers.Left, GamePadAxis.RightTrigger => pad.Triggers.Right,
        _ => throw new ArgumentOutOfRangeException(nameof(axis))
    };

    /// <summary>Binds an input to an action, creating the action if necessary.</summary>
    /// <remarks>Several bindings may activate one action. Adding the same binding twice has no effect.</remarks>
    public void Bind(string actionName, IInputBinding binding)
    {
        ArgumentNullException.ThrowIfNull(binding);
        ActionState action = GetOrCreateAction(actionName);
        if (action.Bindings.Contains(binding))
            return;

        bool wasHeld = action.Current;
        action.Bindings.Add(binding);
        action.Current = IsAnyBindingDown(action.Bindings);
        if (!wasHeld && action.Current)
            action.Previous = true;
    }

    /// <summary>Removes one binding from an action.</summary>
    /// <returns><see langword="true"/> when the binding was present.</returns>
    public bool Unbind(string actionName, IInputBinding binding)
    {
        ArgumentNullException.ThrowIfNull(binding);
        ActionState? action = GetAction(actionName);
        return action is not null && action.Bindings.Remove(binding);
    }

    /// <summary>Removes an action and all of its bindings.</summary>
    public bool RemoveAction(string actionName) => actions.Remove(ValidateActionName(actionName));

    /// <summary>Removes every action. The engine calls this automatically when a world unloads.</summary>
    public void ClearActions() => actions.Clear();

    /// <summary>Returns whether an action became active during the current frame.</summary>
    public bool IsPressed(string actionName)
    {
        ActionState? action = GetAction(actionName);
        return action is not null && action.Current && !action.Previous;
    }

    /// <summary>Returns whether at least one binding for an action is currently down.</summary>
    public bool IsHeld(string actionName) => GetAction(actionName)?.Current ?? false;

    /// <summary>Returns whether an action became inactive during the current frame.</summary>
    public bool IsReleased(string actionName)
    {
        ActionState? action = GetAction(actionName);
        return action is not null && !action.Current && action.Previous;
    }

    /// <summary>Returns whether a key became down during the current frame.</summary>
    public bool IsKeyPressed(Keys key) => currentKeyboard.IsKeyDown(key) && previousKeyboard.IsKeyUp(key);
    /// <summary>Returns whether a key became up during the current frame.</summary>
    public bool IsKeyReleased(Keys key) => currentKeyboard.IsKeyUp(key) && previousKeyboard.IsKeyDown(key);
    /// <summary>Returns whether a key is currently down.</summary>
    public bool IsKeyDown(Keys key) => currentKeyboard.IsKeyDown(key);
    /// <summary>Returns whether a key is currently up.</summary>
    public bool IsKeyUp(Keys key) => currentKeyboard.IsKeyUp(key);

    private bool IsAnyBindingDown(List<IInputBinding> bindings) => IsAnyBindingDown(bindings, Snapshot);

    private static bool IsAnyBindingDown(List<IInputBinding> bindings, InputSnapshot snapshot)
    {
        if (!snapshot.IsFocused) return false;
        foreach (IInputBinding binding in bindings)
            if (binding.IsDown(snapshot))
                return true;
        return false;
    }

    private static bool IsMouseButtonDown(MouseState state, MouseButton button) => button switch
    {
        MouseButton.Left => state.LeftButton == ButtonState.Pressed,
        MouseButton.Middle => state.MiddleButton == ButtonState.Pressed,
        MouseButton.Right => state.RightButton == ButtonState.Pressed,
        MouseButton.XButton1 => state.XButton1 == ButtonState.Pressed,
        MouseButton.XButton2 => state.XButton2 == ButtonState.Pressed,
        _ => false
    };

    private ActionState GetOrCreateAction(string actionName)
    {
        actionName = ValidateActionName(actionName);
        if (!actions.TryGetValue(actionName, out ActionState? action))
            actions.Add(actionName, action = new ActionState());
        return action;
    }

    private ActionState? GetAction(string actionName)
    {
        actions.TryGetValue(ValidateActionName(actionName), out ActionState? action);
        return action;
    }

    private static string ValidateActionName(string actionName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionName);
        return actionName;
    }
}
