using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Hefty.Engine.Input;

public enum MouseButton
{
    Left,
    Middle,
    Right,
    XButton1,
    XButton2
}

public interface IInputBinding
{
    bool IsDown(KeyboardState keyboard, MouseState mouse);
}

public readonly record struct KeyboardBinding(Keys Key) : IInputBinding
{
    public bool IsDown(KeyboardState keyboard, MouseState mouse) => keyboard.IsKeyDown(Key);
}

public readonly record struct MouseBinding(MouseButton Button) : IInputBinding
{
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
public sealed class InputManager : Component
{
    private sealed class ActionState
    {
        public readonly List<IInputBinding> Bindings = [];
        public bool Previous;
        public bool Current;
    }

    private static InputManager instance;
    private readonly Dictionary<string, ActionState> actions = new(StringComparer.Ordinal);
    private KeyboardState previousKeyboard;
    private KeyboardState currentKeyboard;
    private MouseState previousMouse;
    private MouseState currentMouse;

    public static InputManager Instance() => instance ??= new InputManager();

    public Point MousePosition => currentMouse.Position;

    public bool IsMouseButtonPressed(MouseButton button) =>
        IsMouseButtonDown(currentMouse, button) && !IsMouseButtonDown(previousMouse, button);

    public bool IsMouseButtonReleased(MouseButton button) =>
        !IsMouseButtonDown(currentMouse, button) && IsMouseButtonDown(previousMouse, button);

    public bool IsMouseButtonDown(MouseButton button) => IsMouseButtonDown(currentMouse, button);

    private InputManager()
    {
        currentKeyboard = previousKeyboard = Keyboard.GetState();
        currentMouse = previousMouse = Mouse.GetState();
    }

    public override void Update(GameTime gameTime)
    {
        previousKeyboard = currentKeyboard;
        previousMouse = currentMouse;
        currentKeyboard = Keyboard.GetState();
        currentMouse = Mouse.GetState();

        foreach (ActionState action in actions.Values)
        {
            action.Previous = action.Current;
            action.Current = IsAnyBindingDown(action.Bindings);
        }
    }

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

    public bool Unbind(string actionName, IInputBinding binding)
    {
        ArgumentNullException.ThrowIfNull(binding);
        ActionState action = GetAction(actionName);
        return action is not null && action.Bindings.Remove(binding);
    }

    public bool RemoveAction(string actionName) => actions.Remove(ValidateActionName(actionName));

    public void ClearActions() => actions.Clear();

    public bool IsPressed(string actionName)
    {
        ActionState action = GetAction(actionName);
        return action is not null && action.Current && !action.Previous;
    }

    public bool IsHeld(string actionName) => GetAction(actionName)?.Current ?? false;

    public bool IsReleased(string actionName)
    {
        ActionState action = GetAction(actionName);
        return action is not null && !action.Current && action.Previous;
    }

    public bool IsKeyPressed(Keys key) => currentKeyboard.IsKeyDown(key) && previousKeyboard.IsKeyUp(key);
    public bool IsKeyReleased(Keys key) => currentKeyboard.IsKeyUp(key) && previousKeyboard.IsKeyDown(key);
    public bool IsKeyDown(Keys key) => currentKeyboard.IsKeyDown(key);
    public bool IsKeyUp(Keys key) => currentKeyboard.IsKeyUp(key);

    private bool IsAnyBindingDown(List<IInputBinding> bindings)
    {
        foreach (IInputBinding binding in bindings)
            if (binding.IsDown(currentKeyboard, currentMouse))
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
        if (!actions.TryGetValue(actionName, out ActionState action))
            actions.Add(actionName, action = new ActionState());
        return action;
    }

    private ActionState GetAction(string actionName)
    {
        actions.TryGetValue(ValidateActionName(actionName), out ActionState action);
        return action;
    }

    private static string ValidateActionName(string actionName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionName);
        return actionName;
    }
}
