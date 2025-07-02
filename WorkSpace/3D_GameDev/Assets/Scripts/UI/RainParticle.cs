using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RainParticle : MonoBehaviour
{
    void Update()
    {
        Transform playerPosition = GameObject.FindAnyObjectByType<PlayerStateMachine>().transform;
        this.transform.position = new Vector3(playerPosition.position.x, playerPosition.position.y+10, playerPosition.position.z);
    }
}
