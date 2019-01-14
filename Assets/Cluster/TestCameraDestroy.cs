using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCameraDestroy : MonoBehaviour {

    [SerializeField]
    GameObject manager;

    void Awake()
    {
        if (SettingData.instance.data.isUsingMultiScreen)
        {
            manager.SetActive(true);
        }

    }

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
