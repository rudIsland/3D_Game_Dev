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

    // ������ ������ �������� Ȯ���ϴ� �÷���
    private bool _isDissolveFinished = false;

    private List<Material> _cachedMaterials = new List<Material>();

    public PlayerDeadState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        Debug.Log("DeadState����");
        // [����] UI���� ������ ������ �����϶�� �� ���� ������
        if (UIManager.Instance != null && UIManager.Instance.deadPanel != null)
        {
            UIManager.Instance.deadPanel.StartDeadSequence();
        }

        stateMachine.animator.applyRootMotion = true;
        stateMachine.animator.SetTrigger(stateMachine._animIDDead);

        // 1. [�ٽ�] �÷��̾��� ��� ������/������ ��� ����
        DisablePlayerFunctions();

        // �ʱ�ȭ
        elapsed = 0f;
        _isDissolveFinished = false;
        _cachedMaterials.Clear();

        // 3. Enter���� �� �� ���� ���͸������ ������ ������
        // 1. ���͸��� ��ü �� ĳ���� ���ÿ� ����
        foreach (var skin in stateMachine.playerSkin)
        {
            Material[] deadMaterials = new Material[skin.materials.Length];
            for (int i = 0; i < deadMaterials.Length; i++)
            {
                deadMaterials[i] = stateMachine.deadMtl;
            }

            // ���͸��� �迭 ��ü (���⼭ �ν��Ͻ�ȭ�� 1ȸ �Ͼ)
            skin.materials = deadMaterials;

            // 2. ������ �ν��Ͻ����� ����Ʈ�� �����Ͽ� Tick���� �����
            foreach (var mat in skin.materials)
            {
                _cachedMaterials.Add(mat);
            }
        }

        // ������ �� 0���� Ȯ���ϰ� �ʱ�ȭ
        UpdateDissolveMaterials(0f);
    }

    public override void Exit()
    {
        Debug.Log("DeadState������");
        // Ȥ�� �� ������ ���� ����Ʈ ����
        _cachedMaterials.Clear();
    }

    public override void Tick(float deltaTime)
    {
        // [�ٽ� ����] ������ �̹� �����ٸ� �� �̻� �������� ����
        if (_isDissolveFinished) return;

        elapsed += deltaTime;

        // [�ٽ� ����] �ð��� Duration�� �ʰ��ߴ��� Ȯ��
        if (elapsed >= Duration)
        {
            // 1. ���������� Ȯ���ϰ� 1.0(���� ����)���� ����
            UpdateDissolveMaterials(1.0f);

            // 2. �Ϸ� �÷��� ���� (���� Tick ���� ���� �� �ߺ� ȣ�� ����)
            _isDissolveFinished = true;

            Debug.Log("Dissolve ���� �Ϸ�. UI ȣ��.");

            // 3. [���⼭ ȣ��] ������ ���� ������ �̺�Ʈ ����
            onDead?.Invoke();
        }
        else
        {
            // ���� ���� ���� ó��
            float dissolve = Mathf.Clamp01(elapsed / Duration);
            UpdateDissolveMaterials(dissolve);
        }
    }

    // ���͸��� ������Ʈ ������ ���� �޼���� �и� (Tick �ڵ� ������)
    private void UpdateDissolveMaterials(float dissolveValue)
    {
        // ĳ�̵� ����Ʈ�� ��ȸ
        foreach (var mat in _cachedMaterials)
        {
            // Ȥ�� �� null üũ �߰�
            if (mat == null) continue;

            if (mat.HasProperty("_NoiseAmount"))
            {
                mat.SetFloat("_NoiseAmount", dissolveValue);
            }
        }
    }

    // �÷��̾ '��ü' ���·� ����� �Լ�
    private void DisablePlayerFunctions()
    {
        // A. ĳ���� ��Ʈ�ѷ� ���� (�̵� �� �߷� ���� ����)
        if (stateMachine.TryGetComponent(out CharacterController controller))
        {
            controller.enabled = false;
        }

        // B. �浹ü ���� (��ü�� ���� ���� �ʰ� ��)
        // (CharacterController�� ĸ�� �ݶ��̴� ���ҵ� �ϹǷ� ������ ��������, ���� Collider�� �ִٸ� ���ϴ�)
        if (stateMachine.TryGetComponent(out Collider col))
        {
            col.enabled = false;
        }

        // C. �Է�(Input) �ý��� ���� (���� �� ���� ����)
        // (����Ͻô� Input �ý��ۿ� ���� �ٸ� �� �ֽ��ϴ�. ��: PlayerInput ������Ʈ)
        // �ʿ��ϴٸ� �ּ� �����Ͽ� ���
        
        if (stateMachine.TryGetComponent(out UnityEngine.InputSystem.PlayerInput input))
        {
            input.enabled = false;
        }
        

        // D. ������ٵ� �ִٸ� Ű�׸�ƽ���� ��ȯ (���� �� ���� �ʰ�)
        if (stateMachine.TryGetComponent(out Rigidbody rb))
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
        }
    }

    public void Dead()
    {

    }
}