using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerDeadState : PlayerBaseState
{
    private float elapsed = 0f;
    private const float Duration = 5.0f;
    public Action onDead;

    // 연출이 완전히 끝났는지 확인하는 플래그
    private bool _isDissolveFinished = false;

    private List<Material> _cachedMaterials = new List<Material>();

    public PlayerDeadState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        Debug.Log("DeadState진입");
        // [변경] UI에게 스스로 연출을 시작하라고 한 번만 명령함
        if (UIManager.Instance != null && UIManager.Instance.deadPanel != null)
        {
            UIManager.Instance.deadPanel.StartDeadSequence();
        }

        stateMachine.animator.applyRootMotion = true;
        stateMachine.animator.SetTrigger(stateMachine._animIDDead);

        // 1. [핵심] 플레이어의 모든 물리적/논리적 기능 끄기
        DisablePlayerFunctions();

        // 초기화
        elapsed = 0f;
        _isDissolveFinished = false;
        _cachedMaterials.Clear();

        // 3. Enter에서 딱 한 번만 머터리얼들을 가져와 저장함
        // 1. 머터리얼 교체 및 캐싱을 동시에 수행
        foreach (var skin in stateMachine.playerSkin)
        {
            Material[] deadMaterials = new Material[skin.materials.Length];
            for (int i = 0; i < deadMaterials.Length; i++)
            {
                deadMaterials[i] = stateMachine.deadMtl;
            }

            // 머터리얼 배열 교체 (여기서 인스턴스화가 1회 일어남)
            skin.materials = deadMaterials;

            // 2. 생성된 인스턴스들을 리스트에 저장하여 Tick에서 사용함
            foreach (var mat in skin.materials)
            {
                _cachedMaterials.Add(mat);
            }
        }

        // 시작할 때 0으로 확실하게 초기화
        UpdateDissolveMaterials(0f);
    }

    public override void Exit()
    {
        Debug.Log("DeadState나가기");
        // 혹시 모를 재사용을 위해 리스트 비우기
        _cachedMaterials.Clear();
    }

    public override void Tick(float deltaTime)
    {
        // [핵심 변경] 연출이 이미 끝났다면 더 이상 실행하지 않음
        if (_isDissolveFinished) return;

        elapsed += deltaTime;

        // [핵심 변경] 시간이 Duration을 초과했는지 확인
        if (elapsed >= Duration)
        {
            // 1. 마지막으로 확실하게 1.0(완전 투명)으로 설정
            UpdateDissolveMaterials(1.0f);

            // 2. 완료 플래그 설정 (이후 Tick 실행 방지 및 중복 호출 방지)
            _isDissolveFinished = true;

            Debug.Log("Dissolve 연출 완료. UI 호출.");

            // 3. [여기서 호출] 연출이 끝난 시점에 이벤트 전파
            onDead?.Invoke();
        }
        else
        {
            // 진행 중일 때의 처리
            float dissolve = Mathf.Clamp01(elapsed / Duration);
            UpdateDissolveMaterials(dissolve);
        }
    }

    // 머터리얼 업데이트 로직을 별도 메서드로 분리 (Tick 코드 정리용)
    private void UpdateDissolveMaterials(float dissolveValue)
    {
        // 캐싱된 리스트만 순회
        foreach (var mat in _cachedMaterials)
        {
            // 혹시 모를 null 체크 추가
            if (mat == null) continue;

            if (mat.HasProperty("_NoiseAmount"))
            {
                mat.SetFloat("_NoiseAmount", dissolveValue);
            }
        }
    }

    // 플레이어를 '시체' 상태로 만드는 함수
    private void DisablePlayerFunctions()
    {
        // A. 캐릭터 컨트롤러 끄기 (이동 및 중력 연산 중지)
        if (stateMachine.TryGetComponent(out CharacterController controller))
        {
            controller.enabled = false;
        }

        // B. 충돌체 끄기 (시체가 길을 막지 않게 함)
        // (CharacterController가 캡슐 콜라이더 역할도 하므로 위에서 꺼지지만, 별도 Collider가 있다면 끕니다)
        if (stateMachine.TryGetComponent(out Collider col))
        {
            col.enabled = false;
        }

        // C. 입력(Input) 시스템 끄기 (죽은 뒤 조작 방지)
        // (사용하시는 Input 시스템에 따라 다를 수 있습니다. 예: PlayerInput 컴포넌트)
        // 필요하다면 주석 해제하여 사용
        
        if (stateMachine.TryGetComponent(out UnityEngine.InputSystem.PlayerInput input))
        {
            input.enabled = false;
        }
        

        // D. 리지드바디가 있다면 키네마틱으로 전환 (물리 힘 받지 않게)
        if (stateMachine.TryGetComponent(out Rigidbody rb))
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
        }
    }

    public void Dead()
    {

    }
}