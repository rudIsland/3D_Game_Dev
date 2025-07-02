using UnityEngine;
using System.Collections;

public class MouseLock : MonoBehaviour {

	void Start()
	{
        MouseLockON();
    }

    public void MouseLockON()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void MouseLockOff()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
