using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Stage1 : MonoBehaviour
{
    // Start is called before the first frame update

    private void OnEnable()
    {
        SceneManager.LoadScene("HUD", LoadSceneMode.Additive);
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
