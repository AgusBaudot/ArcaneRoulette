using UnityEngine;

public static class CameraUtils
{
    private static Camera _mainCamera;

    public static Camera GetCamera()
    {
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
            
            if (_mainCamera == null)
                Debug.LogError("CRITICAL: Camera.main is null! The scene is missing one.");
        }

        return _mainCamera;
    }
}