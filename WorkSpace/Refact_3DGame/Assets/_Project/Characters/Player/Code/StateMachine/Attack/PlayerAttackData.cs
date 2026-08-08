using UnityEngine;

namespace rudIsland.RPG3D.Player.States.Attack
{
    // 공격 하나의 변경 가능한 설정을 보관할 ScriptableObject다.
    [CreateAssetMenu(
        fileName = "PlayerAttackData",
        menuName = "rudIsland/RPG3D/Player/Attack Data")]
    public sealed class PlayerAttackData : ScriptableObject
    {
        [Header("공격 식별")]
        [SerializeField, Range(1, 6)] private int attackNumber = 1;

        [Header("콤보 연결")]
        [SerializeField, Range(0f, 1f)] private float nextInputTime = 1f;

        [Header("루트 모션")]
        [SerializeField, Range(0f, 1f)] private float moveScale = 1f;

        public int AttackNumber => attackNumber;
        public float NextInputTime => nextInputTime;
        public float MoveScale => moveScale;

        private void OnValidate()
        {
            attackNumber = Mathf.Clamp(attackNumber, 1, 6);
            nextInputTime = Mathf.Clamp01(nextInputTime);
            moveScale = Mathf.Clamp01(moveScale);
        }
    }
}
