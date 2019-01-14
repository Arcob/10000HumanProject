using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class FPSPrinter : MonoBehaviour {

    public Text fpsText;
    public float timeToUpdateFPS = 1.0f;
    float timer = 0.0f;

    DateTime lastUpdate;
    float deltaTime;

    float totalTime = 0.0f;
    int totalFrame = 0;

    private void Start()
    {
        lastUpdate = DateTime.Now;
    }

    void Update()
    {

        timer += Time.deltaTime;
        if (Time.frameCount > 10)
        {
            totalTime += Time.deltaTime;
            totalFrame++;
        }
        if (timer > timeToUpdateFPS)
        {
            fpsText.text = "Human Count:" + SettingData.instance.data.humanCount + "\nAverage FPS:" + (totalFrame * 1.0f / totalTime).ToString("0.00") + "\nCurrent FPS:" + (1.0f / Time.deltaTime).ToString("0.00");// + ":" + (1.0f / deltaTime).ToString("0.00");
            timer = 0.0f;
        }
    }
}
