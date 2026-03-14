using UnityEngine;

public class WindowedBoot : MonoBehaviour
{
    void Awake()
    {
        // Screen.SetResolution(1280, 720, false);
        
        // Screen.fullScreenMode = FullScreenMode.Windowed;
        Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
    }
}