using System;

[Serializable]
struct BulletBaseFrameSave
{
    public byte[] id;
    public float speed;
    public float[] position;
    public float[] rotation;
    public float[] direction;
    public byte[] shipOriginId;
}

[Serializable]
struct BulletDiffFrameData
{
    public float[] position;
    public float[] rotation;
}