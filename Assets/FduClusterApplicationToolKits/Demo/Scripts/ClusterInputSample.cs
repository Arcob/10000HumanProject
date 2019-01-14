using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FDUClusterAppToolKits;
public class ClusterInputSample : MonoBehaviour {

    public float ratio = 0.1f;

	void Update () {


        transform.position += Vector3.right * ratio * FduClusterInputMgr.GetAxis("Horizontal");

        transform.position += Vector3.up * ratio * FduClusterInputMgr.GetAxis("Vertical");


        if (FduClusterInputMgr.GetMouseButton(0))
        {
            transform.position = Camera.main.ScreenPointToRay(FduClusterInputMgr.scaledMousePosition).GetPoint(10.0f);
            //Debug.Log(FduClusterInputMgr.mousePosition);
        }
	}
}
