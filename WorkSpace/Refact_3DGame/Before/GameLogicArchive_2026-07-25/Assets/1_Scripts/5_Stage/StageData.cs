using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "Scriptable Object Asset/Stage Data")]
public class StageData : ScriptableObject
{
    public string stageName;
    public Enemy monsterPrefab; // 이번 스테이지에 소환할 몬스터 프리팹 리스트
    public int clearKillCount;              // 스테이지 클리어에 필요한 처치 수
}