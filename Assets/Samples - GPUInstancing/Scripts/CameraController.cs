using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FDUClusterAppToolKits;
public class CameraController : MonoBehaviour {

    public GameObject mainCamera;

    Vector3 initPos;

    Quaternion initqua;

	void Start () {

        initPos = mainCamera.transform.position;
        initqua = mainCamera.transform.rotation;

	}

    float horizontal, vertical;
    float mouse_horizontal, mouse_vertical;
    float moveSpeed = 30.0f,rotateSpeed = 10.0f;
    void Update () {


        if (FduClusterInputMgr.GetKeyDown(KeyCode.C))
        {
            mainCamera.transform.position = initPos;
            mainCamera.transform.rotation = initqua;
        }
        
        {
            horizontal = FduClusterInputMgr.GetAxis("Horizontal");
            vertical = FduClusterInputMgr.GetAxis("Vertical");
            /*
            Vector3 forward = otherCamera.transform.forward, right;
            forward.y = 0.0f;
            forward.Normalize();
            right = Vector3.Cross(forward, Vector3.up);*/

            // fzy modify:
            mainCamera.transform.Translate((Vector3.forward * vertical + Vector3.right * horizontal) * FduClusterTimeMgr.deltaTime * moveSpeed);
            
            // fzy modify:
            mouse_vertical = FduClusterInputMgr.GetAxis("Mouse X");
            mouse_horizontal = FduClusterInputMgr.GetAxis("Mouse Y");
            mainCamera.transform.localEulerAngles += new Vector3(-mouse_horizontal, mouse_vertical, 0.0f) * FduClusterTimeMgr.deltaTime * rotateSpeed;
        }
	}
}
