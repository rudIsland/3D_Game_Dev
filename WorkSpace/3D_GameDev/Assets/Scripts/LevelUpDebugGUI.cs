using UnityEngine;

public class LevelUpDebugGUI : MonoBehaviour
{
    public LevelUpComponent levelUpComponent;

    private void OnGUI()
    {
        if (levelUpComponent == null) return;

        GUI.BeginGroup(new Rect(10, 10, 220, 300), "레벨업 디버그", GUI.skin.window);

        if (GUI.Button(new Rect(10, 30, 200, 40), "공격 +1"))
        {
            levelUpComponent.UpAttack();
        }
        if (GUI.Button(new Rect(10, 80, 200, 40), "방어 +1"))
        {
            levelUpComponent.UpDef();
        }
        if (GUI.Button(new Rect(10, 130, 200, 40), "HP +1"))
        {
            levelUpComponent.UpHP();
        }

        if (GUI.Button(new Rect(10, 180, 200, 40), "공격 -1"))
        {
            levelUpComponent.DownAttack();
        }
        if (GUI.Button(new Rect(10, 230, 200, 40), "방어 -1"))
        {
            levelUpComponent.DownDef();
        }
        if (GUI.Button(new Rect(10, 280, 200, 40), "HP -1"))
        {
            levelUpComponent.DownHP();
        }

        GUI.EndGroup();
    }
}
