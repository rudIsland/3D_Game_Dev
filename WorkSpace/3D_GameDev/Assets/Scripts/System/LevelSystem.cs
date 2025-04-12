using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LevelSystem
{
    public int level = 0;
    public float maxExp = 0;
    public float currentExp = 0;

    public float[] ExpArray = new float[50];
    public bool isLevelUp => currentExp > maxExp;

    public void GetExpFromMonster(int baseExp, int monsterLevel, int playerLevel)
    {
        int levelDiff = monsterLevel - playerLevel;

        // �÷��̾�� 3���� ���� ���ʹ� 0.5��
        float multiplier = Mathf.Clamp(1f + levelDiff * 0.1f, 0.5f, 2f);

        // ����ġ ȹ��
        getExp(baseExp * multiplier);
    }

    public void getExp(float upValue)
    {
        currentExp += upValue;
    }


    public void LevelUp()
    {
        level += 1;
    }

}
