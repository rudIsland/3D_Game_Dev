using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//공격 상태를 갖도록 하는 인터페이스
public interface IAttackState
{
    float GetDamageFromData(int weaponIndex, int hitIndex);
}
