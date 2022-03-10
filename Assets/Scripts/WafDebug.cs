using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WafDebug : MonoBehaviour
{
    public FrameInterpolation interpol;
    public bool interpoling = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        interpoling = FrameInterpolation.enableMotionInterpolation;
    }
}
