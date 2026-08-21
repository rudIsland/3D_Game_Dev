using rudIsland.RPG3D.Characters.Combat.AttackData;
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.AttackData
{
    // 적 종류와 관계없이 공격 하나의 변하지 않는 설정만 보관한다.
    public abstract class EnemyAttackData : ScriptableObject
    {
        [Header("피해 단계")]
        [SerializeField] private AttackDamage[] hitDamages =
            { new AttackDamage() };

        [Header("공격 종료 후 대기")]
        [SerializeField, Min(0f)] private float postAttackDelay = 1f;

        [Header("공격 선택 점수")]
        [SerializeField] private EnemyAttackUtilitySettings utility = new();

        public int HitCount => hitDamages != null ? hitDamages.Length : 0;
        public float PostAttackDelay => postAttackDelay;
        public EnemyAttackUtilitySettings Utility => utility;

        public AttackDamage GetHitDamage(int hitIndex)
        {
            if (hitDamages == null ||
                hitIndex < 0 ||
                hitIndex >= hitDamages.Length)
            {
                return null;
            }

            return hitDamages[hitIndex];
        }

        protected void ValidateAttackData(int minimumHitCount)
        {
            int safeHitCount = Mathf.Max(1, minimumHitCount);
            if (hitDamages == null)
            {
                hitDamages = new AttackDamage[safeHitCount];
            }
            else if (hitDamages.Length < safeHitCount)
            {
                System.Array.Resize(ref hitDamages, safeHitCount);
            }

            for (int index = 0; index < hitDamages.Length; index++)
            {
                hitDamages[index] ??= new AttackDamage();
            }

            postAttackDelay = Mathf.Max(0f, postAttackDelay);
            utility ??= new EnemyAttackUtilitySettings();
            utility.Validate();
        }
    }
}
