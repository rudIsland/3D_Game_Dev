using UnityEngine;


public class EnemyStateMachine : MonoBehaviour
{
    public EnemyState currentState { get; private set; }
    public Enemy enemy;

    public void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    private void Start()
    {
        enemy.SetupStateMachine(this);
    }

    public void Initalize(EnemyState state) //현재 상태 등록
    {
        currentState = state;
        currentState?.Enter();
    }

    public void SwitchState(EnemyState newState)
    {
        if (currentState == newState) return;

        //현재 상태를 나가고 새 상태를 받고나서 시작 실행
        currentState?.Exit();
        currentState = newState;
        currentState?.Enter();
    }

    //현재 상태의 Tick을 실행
    public void Update()
    {
        currentState?.Tick(Time.deltaTime);
    }

}
