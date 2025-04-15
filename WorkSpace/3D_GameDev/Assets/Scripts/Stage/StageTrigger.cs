using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageTrigger : MonoBehaviour
{
    [SerializeField] private string stageSceneName;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            StageManager.Instance.MoveToStage(stageSceneName);
            Debug.Log("¿Ãµø");
        }
    }

}
