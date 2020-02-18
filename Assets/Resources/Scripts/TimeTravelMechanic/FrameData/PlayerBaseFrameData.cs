using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
struct PlayerBaseFrameData
{
    public byte[] id;
    
    public float[] position;
    public float[] rotation;

    public byte[] killerId;
}