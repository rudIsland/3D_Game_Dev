using rudIsland.RPG3D.Characters.Enemies.AttackData;
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    // 프리팹과 분리된 NightShadeSword 전투 속성 모음이다.
    [CreateAssetMenu(
        fileName = "NightShadeSwordConfig",
        menuName = "rudIsland/RPG3D/NightShade/Sword Config")]
    public sealed class NightShadeSwordConfig : ScriptableObject
    {
        [Header("공격")]
        [SerializeField] private EnemyAttackData[] attacks =
            new EnemyAttackData[4]; // Light, Combo, Heavy, WideSwing 설정만 보관

        [Header("공통 설정")]
        [SerializeField] private NightShadeSwordLifeSettings life = new(); // 체력, 경직, 사망
        [SerializeField] private NightShadeSwordCombatRangeSettings combatRange = new(); // 타겟, 거리, 방향
        [SerializeField] private NightShadeSwordAttackSelectionSettings attackSelection = new(); // 공격 선택 점수
        [SerializeField] private NightShadeSwordMovementSettings movement = new(); // 이동과 회전
        [SerializeField] private NightShadeSwordRecoverySettings recovery = new(); // 공격 후 회복 행동
        [SerializeField] private NightShadeSwordHitReactionSettings hitReaction = new(); // 피격 밀림

        internal NightShadeSwordSettings CreateRuntimeSettings()
        {
            // ScriptableObject를 직접 넘기지 않고 검증된 런타임 값만 복사한다.
            ValidateSettings();
            return new NightShadeSwordSettings(
                life,
                combatRange,
                attackSelection,
                movement,
                attacks,
                recovery,
                hitReaction);
        }

        private void OnValidate()
        {
            ValidateSettings();
        }

        private void ValidateSettings()
        {
            attacks ??= new EnemyAttackData[4];
            life ??= new NightShadeSwordLifeSettings();
            combatRange ??= new NightShadeSwordCombatRangeSettings();
            attackSelection ??= new NightShadeSwordAttackSelectionSettings();
            movement ??= new NightShadeSwordMovementSettings();
            recovery ??= new NightShadeSwordRecoverySettings();
            hitReaction ??= new NightShadeSwordHitReactionSettings();

            for (int index = 0; index < attacks.Length; index++)
            {
                if (attacks[index] is NightShadeSwordAttackData attack)
                {
                    attack.Validate();
                }
            }

            life.Validate();
            combatRange.Validate();
            attackSelection.Validate();
            movement.Validate();
            recovery.Validate();
            hitReaction.Validate();
        }
    }
}
