using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISaveSystem
{
    void Save(PlayerStats stats);
    void Load(Action<SavedData> onLoaded);
    void Reset();
    bool HasSavedData();
}
