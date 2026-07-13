using System;
using UnityEngine;

public abstract class BasePuzzleManager : MonoBehaviour
{
    [Header("Puzzle Base Settings")]
    [SerializeField] protected float itemSpacing = 1.2f;

    public event Action OnPuzzleClearedEvent;

    public bool IsClear { get; protected set; } = false;

    protected virtual void Start()
    {
        InitializePuzzle();
    }

    public abstract void InitializePuzzle();
    public abstract void CheckClearCondition();
    protected abstract void OnPuzzleClear();

    protected void TriggerClearEvent()
    {
        OnPuzzleClearedEvent?.Invoke();
    }
}