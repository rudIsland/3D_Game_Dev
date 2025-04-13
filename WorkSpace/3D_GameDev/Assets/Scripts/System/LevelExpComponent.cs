
using UnityEngine;
using UnityEngine.UI;

public class LevelExpComponent : MonoBehaviour
{
    public LevelSystem levelSys;
    public Slider ExpSlider;

    private void Awake()
    {
        levelSys = UIManager.Instance.stateMachine.levelSys;
        if (levelSys.ExpArray == null)
        {
            Debug.LogError("ExpArray is null!");
            return;
        }
        SetExpArrayAndSlider();
    }

    public void SetExpArrayAndSlider()
    {
        for (int level = 0; level < 50; level++)
        {
            levelSys.ExpArray[level] = Mathf.Round(100f * Mathf.Pow(level + 1, 1.5f));
        }

        ExpSlider.maxValue = levelSys.ExpArray[0];
        ExpSlider.value = levelSys.currentExp;
    }

    public void UpdateExpSlider()
    {
        int nowLevel = levelSys.level;
        ExpSlider.maxValue = levelSys.ExpArray[nowLevel - 1];

        ExpSlider.value = levelSys.currentExp;

        //Check levelUp
        CheckLevelUp(nowLevel);
    }

    private void CheckLevelUp(int nowLevel)
    {
        if (levelSys.ExpArray[nowLevel - 1] < levelSys.currentExp)
        {
            float saveExp = levelSys.ExpArray[nowLevel - 1] - levelSys.currentExp;
            levelSys.LevelUp();
            levelSys.currentExp = saveExp;
            UpdateExpSlider();
            gameObject.SetActive(true);
            UIManager.Instance.playerStatUIComp.UpdateLevelText();
        }
    }

}
