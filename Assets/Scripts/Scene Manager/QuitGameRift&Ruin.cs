using UnityEngine;

public class QuitGameRift_Ruin : MonoBehaviour
{
    public bool QuitOnAwake;
    void Awake()
    {
        if (QuitOnAwake) Application.Quit();
    }
    public void QuitApp()
    {
        Application.Quit();
    }
}
