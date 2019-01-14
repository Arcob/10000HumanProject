using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public  class FrameCounter : MonoBehaviour {

    int animationFps = 30;

    public static int frameCount = 0;

    public static int interval = 1;


    void Awake()
    {
        interval = 60 / animationFps;
    }

	// Update is called once per frame
	void Update () {

        frameCount++;
	}
}
