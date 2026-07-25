using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))] // Image 컴포넌트가 필수임을 명시
public class DeadPanelController : MonoBehaviour, IPointerClickHandler
{
    private Image _image;
    [SerializeField] private float fadeDuration = 2.0f; // 자체 연출 시간
    [SerializeField] private PlayerStatPreset defaultPlayerPreset; // 초기화용 프리셋

    private void Awake()
    {
        _image = GetComponent<Image>();
    }

    private void Start()
    {
        ActiveDeadPanel(false);
    }

    // [핵심] 클릭 시 LobbyUI의 NewGame과 동일한 로직 수행
    public void OnPointerClick(PointerEventData eventData)
{
    if (_image.color.a >= 1f)
    {


        // [수정] SaveSystem의 Reset 함수를 사용하여 파일 삭제와 데이터 수치 리셋을 한 번에 처리
        SaveSystem.Reset(DataManager.Instance.PlayerData, defaultPlayerPreset);

        // 현재 씬에 있는 플레이어 컴포넌트들에게 수치 반영 지시
        PlayerStateMachine player = FindObjectOfType<PlayerStateMachine>();
        if (player != null)
        {
            player.Initialize_Stats();
        }

        // 초기화된 상태를 다시 파일로 저장
        SaveSystem.Save(DataManager.Instance.PlayerData);

        // 패널 끄고 씬 이동
        ActiveDeadPanel(false);
        Time.timeScale = 1f;
        StageManager.Instance.MoveToNextStage("0_Scene_Inital");
    }
}

    // [핵심] 연출을 스스로 수행하는 함수
    public void StartDeadSequence()
    {
        ActiveDeadPanel(true);
        transform.SetAsLastSibling(); // 최상단으로 이동
        StopAllCoroutines();
        StartCoroutine(FadeInRoutine());
    }

    private IEnumerator FadeInRoutine()
    {
        float elapsed = 0f;
        Color c = _image.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Clamp01(elapsed / fadeDuration);
            _image.color = c;
            yield return null;
        }

        c.a = 1f;
        _image.color = c;

        // 클릭 유도를 위한 커서 활성화
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void ActiveDeadPanel(bool onOff)
    {
        gameObject.SetActive(onOff);
        if (!onOff)
        {
            Color c = _image.color;
            c.a = 0;
            _image.color = c;
        }
    }
}