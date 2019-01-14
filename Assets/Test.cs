using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour {

    public int aaa = 0;

    byte viewStateFlags = 0;

    public bool IsImmediatelyDeserialize
    {
        get
        {
            return (viewStateFlags & 0x04) != 0;
        }
        set
        {
            if (value)
                viewStateFlags |= 0x04;
            else
                viewStateFlags &= 0xfb;
        }
    }
    public bool IsObservingCreation
    {
        get
        {
            return (viewStateFlags & 0x01) != 0;
        }
        internal set
        {
            if (value)
                viewStateFlags |= 0x01;
            else
                viewStateFlags &= 0xfe;
        }
    }

    public bool IsObservingDestruction
    {
        get
        {
            return (viewStateFlags & 0x02) != 0;
        }
        set
        {
            if (value)
                viewStateFlags |= 0x02;
            else
                viewStateFlags &= 0xfd;
        }
    }

    public bool IsObservingActiveState
    {
        get
        {
            return (viewStateFlags & 0x08) != 0;
        }
        set
        {
            if (value)
                viewStateFlags |= 0x08;
            else
                viewStateFlags &= 0xf7;
        }
    }
    void Awake()
    {
        Debug.Log("I Awake");
    }
	// Use this for initialization
    void Start()
    {
        Debug.Log("I Start");
	}

    void show()
    {
        string s = string.Format("crea {0}, des {1} , ime {2},active{3}", IsObservingCreation, IsObservingDestruction, IsImmediatelyDeserialize, IsObservingActiveState);
        Debug.Log(s);
    }
	// Update is called once per frame
	void Update () {

        Debug.Log(aaa);

	}
}
