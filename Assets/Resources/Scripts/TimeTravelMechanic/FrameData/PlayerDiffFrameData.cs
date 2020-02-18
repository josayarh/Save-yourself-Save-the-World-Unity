using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
struct PlayerDiffFrameData
{
    public float[] position;
    public float[] rotation;
    public byte[] killerGUID;
}
