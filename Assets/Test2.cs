using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test2 : MonoBehaviour {

    public GameObject go;

	// Use this for initialization
	void Start () {

        Debug.Log("before ins");

        var s = GameObject.Instantiate(go);

        Debug.Log("after ins");
        s.GetComponent<Test>().aaa = 666;

        Debug.Log("after asss");

	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
