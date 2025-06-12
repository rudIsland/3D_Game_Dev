using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IGamePauseHandler
{
    void PauseGame();
    void ResumeGame();
}
