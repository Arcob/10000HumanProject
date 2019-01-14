using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogHelper : MonoBehaviour {

    Transform[] ts;
	void Start () {
        ts = GetComponentsInChildren<Transform>();
        Debug.Log(ts.Length);
        foreach(Transform t in ts)
        {
            Debug.Log(t.name);
        }
	}
	
}
