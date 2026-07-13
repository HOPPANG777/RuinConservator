using UnityEngine;
using UnityEngine.InputSystem;

public static class PuzzleInputUtil
{
    public static bool TryGetMouseWorldPosition(out Vector2 worldPos)
    {
        worldPos = Vector2.zero;
        if (Mouse.current == null) return false;

        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        if (Camera.main == null) return false;

        worldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        return true;
    }

    public static bool IsLeftButtonTriggered() => Mouse.current?.leftButton.wasPressedThisFrame ?? false;
    public static bool IsLeftButtonReleased() => Mouse.current?.leftButton.wasReleasedThisFrame ?? false;
    public static bool IsRightButtonTriggered() => Mouse.current?.rightButton.wasPressedThisFrame ?? false;
}