using System;
using UnityEngine;

public abstract class BasePuzzleManager : MonoBehaviour
{
    [Header("Puzzle Base Settings")]
    [SerializeField] protected float itemSpacing;

    public event Action OnPuzzleCleared;

    public bool IsClear { get; protected set; } = false;

    protected virtual void Start()
    {
        InitPuzzle();
    }

    public abstract void InitPuzzle();
    public abstract void CheckClear();
    protected abstract void OnPuzzleClear();

    protected void TriggerClearEvent()
    {
        OnPuzzleCleared?.Invoke();
    }
}