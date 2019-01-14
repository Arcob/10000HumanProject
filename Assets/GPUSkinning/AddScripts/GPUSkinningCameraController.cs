using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GPUSkinningCameraController : MonoBehaviour {
    Camera mainCamera;
    private void Start()
    {
        mainCamera = Camera.main;
        mainCamera.opaqueSortMode = UnityEngine.Rendering.OpaqueSortMode.NoDistanceSort;
        Debug.Log(mainCamera.opaqueSortMode);
    }
}
