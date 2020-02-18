using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class BulletBotController : Bot, IPoolableObject
{
    private float speed;
    private Vector3 direction;
    
    public void OnPoolCreation()
    {
    }

    public override void FixedUpdate()
    {
        transform.position += direction * speed * Time.fixedDeltaTime;
    }

    public override void LoadFrame(string binarySave)
    {
        byte[] byteArray = Convert.FromBase64String(binarySave);
        MemoryStream mf = new MemoryStream(byteArray);
        BinaryFormatter bf = new BinaryFormatter();
        BulletBaseFrameSave data = (BulletBaseFrameSave)bf.Deserialize(mf);
        
        id = new Guid(data.id);

        transform.position = VectorArrayConverter.arrayToVector3(data.position);
        transform.rotation = Quaternion.Euler(VectorArrayConverter.arrayToVector3(data.rotation));
        direction = VectorArrayConverter.arrayToVector3(data.direction);

        speed = data.speed;
    }
    
    public override void LoadDiffFrame(string binarySave)
    {
        byte[] byteArray = Convert.FromBase64String(binarySave);
        MemoryStream mf = new MemoryStream(byteArray);
        BinaryFormatter bf = new BinaryFormatter();
        BulletDiffFrameData data = (BulletDiffFrameData)bf.Deserialize(mf);

        transform.position = VectorArrayConverter.arrayToVector3(data.position);
        transform.rotation = Quaternion.Euler(VectorArrayConverter.arrayToVector3(data.rotation));
    }
    
    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Enemy"))
        {
            Debug.Log("Bot Bullet touched enemy ");
            EnemyController ectrl = other.gameObject.GetComponent<EnemyController>();
            ectrl.Destroy();
        }
        else if (other.gameObject.CompareTag("Player"))
        {
            Debug.Log("Bot Bullet touched Player ");
            PlayerBotController pBCtrl = other.gameObject.GetComponent<PlayerBotController>();

            if (pBCtrl)
            {
                pBCtrl.Destroy(id);
            }
            else
            {
                PlayerController pc = other.gameObject.GetComponent<PlayerController>();
                if (pc)
                {
                    pc.Destroy(id);
                }
                else
                {
                    Debug.Log("Bullet Bot Controller : Invalid Player tag type was touched");
                }
            }
        }

        Destroy();
    }

    public void OnRelease()
    {
        
    }

    public void Destroy()
    {
        Pool.Instance.release(gameObject, PoolableTypes.BulletBot);
    }
}
