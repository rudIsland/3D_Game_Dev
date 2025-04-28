using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Stage2 : MonoBehaviour
{

    void Start()
    {
        GameManager.Instance.ResetEnemyCount();
    }

}
