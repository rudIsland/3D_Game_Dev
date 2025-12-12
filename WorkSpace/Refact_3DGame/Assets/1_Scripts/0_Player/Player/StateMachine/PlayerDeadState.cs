using System.Collections;
using UnityEngine;
using System;
using Unity.VisualScripting;

public class PlayerDeadState : PlayerBaseState
{

    private float elapsed = 0f;

    public Action onDead;

    public PlayerDeadState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        Debug.Log("DeadState진입");
        stateMachine.animator.applyRootMotion = true;
        stateMachine.animator.SetTrigger(stateMachine._animIDDead);
        ChangeDeadMtl();

    }

    public override void Exit()
    {
        Debug.Log("DeadState나가기");

    }

    private void ChangeDeadMtl()
    {
        foreach (var skin in stateMachine.playerSkin)
        {
            // 현재 SkinnedMeshRenderer가 사용하는 머터리얼 개수만큼 deadMtl을 복제해서 배열로 만듦
            Material[] deadMaterials = new Material[skin.materials.Length];
            for (int i = 0; i < deadMaterials.Length; i++)
            {
                deadMaterials[i] = stateMachine.deadMtl;
            }

            skin.materials = deadMaterials;
        }
    }

    public override void Tick(float deltaTime)
    {
        float duration = 5f; // 몇 초 동안 서서히 사라질지


        if (elapsed >= duration)
            return;

        elapsed += deltaTime;
        float dissolve = Mathf.Clamp01(elapsed / duration);

        foreach (var skin in stateMachine.playerSkin)
        {
            foreach (var mat in skin.materials)
            {
                if (mat.HasProperty("_NoiseAmount"))
                {
                    mat.SetFloat("_NoiseAmount", dissolve);
                   // UIManager.Instance.OnPlayerDeath(dissolve);
                }
            }
        }
    }

    public void Dead()
    {

    }
}
