using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEditor;
using UnityEngine;

public class PlayerSave : SavableObject
{
    private void Awake()
    {
        id=Guid.Empty;
    }

    private void FixedUpdate()
    {
        SaveDiffFrame();
    }

    public void SaveFrame()
    {
        if(id != Guid.Empty)
            frameSaveList.Add(MakeFrame());
    }
    
    public void SaveDiffFrame(Guid killer = new Guid())
    {
        if (id != Guid.Empty)
        {
            string diffFrame;
            if (killer == Guid.Empty)
                diffFrame = MakeDiffFrame();
            else
                diffFrame = MakeDiffFrame(killer);

            frameSaveList.Add(diffFrame);
        }
    }
    
    public override string MakeFrame()
    {
        BinaryFormatter bf = new BinaryFormatter();
        MemoryStream ms = new MemoryStream();
        
        PlayerBaseFrameData frameData = new PlayerBaseFrameData();

        frameData.id = new Byte[id.ToByteArray().Length];
        id.ToByteArray().CopyTo(frameData.id,0);

        frameData.position = VectorArrayConverter.vector3ToArray(transform.position);
        frameData.rotation = VectorArrayConverter.vector3ToArray(transform.rotation.eulerAngles);
        
        bf.Serialize(ms,frameData);

        return Convert.ToBase64String(ms.ToArray());
    }
    
    public override string MakeDiffFrame()
    {
        BinaryFormatter bf = new BinaryFormatter();
        MemoryStream ms = new MemoryStream();
        
        PlayerDiffFrameData frameData = new PlayerDiffFrameData();

        frameData.position = VectorArrayConverter.vector3ToArray(transform.position);
        frameData.rotation = VectorArrayConverter.vector3ToArray(transform.rotation.eulerAngles);
        
        bf.Serialize(ms,frameData);

        return Convert.ToBase64String(ms.ToArray());
    }
    
    public string MakeDiffFrame(Guid killer)
    {
        BinaryFormatter bf = new BinaryFormatter();
        MemoryStream ms = new MemoryStream();
        
        PlayerDiffFrameData frameData = new PlayerDiffFrameData();

        frameData.position = VectorArrayConverter.vector3ToArray(transform.position);
        frameData.rotation = VectorArrayConverter.vector3ToArray(transform.rotation.eulerAngles);
        frameData.killerGUID = new Byte[killer.ToByteArray().Length];
        killer.ToByteArray().CopyTo(frameData.killerGUID,0);
        
        bf.Serialize(ms,frameData);

        return Convert.ToBase64String(ms.ToArray());
    }

    public void Destroy(Guid killerId = new Guid())
    {
        if (id != Guid.Empty)
        {
            if(killerId != Guid.Empty)
                SaveDiffFrame(killerId);
            else 
                SaveDiffFrame();
            
            GameObjectStateManager.Instance.addDynamicObject(id, 
                GetType(), frameSaveList, 0);
        }
    }

    public Guid Id
    {
        get => id;
        set
        {
            id = value;
            if (id != Guid.Empty)
            {
                GameObjectStateManager.Instance.addInstanciatedObject(id, gameObject);
                SaveFrame();
            }
        }
    }

    public List<string> FrameSaveList
    {
        get => frameSaveList;
        set => frameSaveList = value;
    }
}
