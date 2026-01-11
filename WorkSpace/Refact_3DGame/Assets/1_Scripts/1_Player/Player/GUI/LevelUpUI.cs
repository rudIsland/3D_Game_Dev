using TMPro;
using UnityEngine;

public class LevelUpUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI pointText;

    // 자주 쓰니까 캐싱해두면 코드가 짧아집니다.
    private PlayerStateMachine PlayerPlayer => StageManager.Instance.player;

    private void OnEnable() => UpdatePointUI();

    public void OnClickHP()
    {
        PlayerPlayer.IncreaseMaxHP(); // 여기서 이벤트가 발생함 -> GUI 자동 갱신
        PostClickProcess();
    }

    public void OnClickSTR()
    {
        PlayerPlayer.IncreaseAttack(); // 여기서 이벤트가 발생함 -> GUI 자동 갱신
        PostClickProcess();
    }

    public void OnClickDEF()
    {
        PlayerPlayer.IncreaseDefense(); // 여기서 이벤트가 발생함 -> GUI 자동 갱신
        PostClickProcess();
    }

    // 중복되는 코드 정리
    private void PostClickProcess()
    {
        UpdatePointUI();
        CheckPoints();
    }

    private void UpdatePointUI()
    {
        if (pointText != null && PlayerPlayer != null)
        {
            pointText.text = $"AP: {PlayerPlayer.statPoints}";
        }
    }

    private void CheckPoints()
    {
        if (PlayerPlayer.statPoints <= 0)
        {
            UIManager.Instance.CloseLevelUpPanel();
        }
    }

    public void OnClickClose() => UIManager.Instance.CloseLevelUpPanel();
}