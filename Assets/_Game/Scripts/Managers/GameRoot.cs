using UnityEngine;

public class GameRoot : MonoBehaviour
{
    public static GameRoot Instance { get; private set; }

    [SerializeField] private PlayerMoveController _moveController;
    [SerializeField] private PlayerAttackController _attackController;

    public PlayerMoveController PlayerMoveController => _moveController;
    public PlayerAttackController PlayerAttackController => _attackController;

    private void Awake()
    {
        Instance = this;
        _attackController.Construct(_moveController);
    }
}