using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
public class LightTileComponent : MonoBehaviour
{
    public int Id { get; private set; }
    public bool IsOn { get; private set; }

    private SpriteRenderer spriteRenderer;

    public void Init(int tileId, bool initialState)
    {
        Id = tileId;
        IsOn = initialState;
        spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateVisual();
    }

    public void ToggleState()
    {
        IsOn = !IsOn;
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = IsOn ? Color.yellow : new Color(0.2f, 0.2f, 0.2f);
        }
    }
}