using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.Undead
{
    [DisallowMultipleComponent]
    // Unity 생명주기를 Undead Warrior 상태머신에 전달한다.
    public sealed class UndeadWarriorController : MonoBehaviour
    {
        private UndeadWarriorStateMachine stateMachine; // 현재 행동 상태

        private void Awake()
        {
            stateMachine = new UndeadWarriorStateMachine();
        }

        private void OnEnable()
        {
            stateMachine?.Enable();
        }

        private void Update()
        {
            stateMachine?.Update(Time.deltaTime);
        }

        private void OnDisable()
        {
            stateMachine?.Disable();
        }
    }
}
