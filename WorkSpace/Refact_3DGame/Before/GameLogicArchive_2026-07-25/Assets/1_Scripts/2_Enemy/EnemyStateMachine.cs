using UnityEngine;


public class EnemyStateMachine : MonoBehaviour
{
    public EnemyState currentState { get; private set; }
    public Enemy enemy;

    // 모든 적이 공통으로 가질 상태들 (추상화)
    public EnemyState deadState;
    public EnemyState idleState;
    public EnemyState attackState;
    public EnemyState hitState;
    public EnemyState moveState;

    public void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    private void Start()
    {
        enemy.SetupStateMachine(this);
    }

    public void InitalizeState(EnemyState state) //현재 상태 등록
    {
        currentState = state;
        currentState?.Enter();
    }

    public void SwitchState(EnemyState newState)    //상태변환
    {
        if (currentState == newState) return;
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
