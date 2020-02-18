using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.Serialization.Formatters.Binary;
using FLFlight;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

using Vector3 = UnityEngine.Vector3;
using Quaternion = UnityEngine.Quaternion;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float fireRate = 1.0f;
    [SerializeField] private Transform gunTipPosition;
    
    private float timer = 0.0f;
    private PlayerSave playerSave;

    private Vector3 velocity;
    
    public ShipInput Input { get; private set; }
    public ShipPhysics Physics { get; internal set; }
    
    // Start is called before the first frame update
    void Start()
    {
        playerSave = GetComponent<PlayerSave>();
        
    }

    private void Awake()
    {
        Input = GetComponent<ShipInput>();
        Physics = GetComponent<ShipPhysics>();
    }


    private void FixedUpdate()
    {
        velocity = new Vector3(Input.Strafe, 0.0f, Input.Throttle);
        Physics.SetPhysicsInput(velocity, 
            new Vector3(Input.Pitch, Input.Yaw, Input.Roll));
        
        float fireKey = UnityEngine.Input.GetAxis("Fire1");
        timer += Time.deltaTime;

        if (timer > fireRate && fireKey != 0)
        {
            Fire();
        }
    }

    private void Fire()
    {
        GameObject bullet = Pool.Instance.get(PoolableTypes.Bullets, gunTipPosition, playerSave.Id);
        BulletController bc = bullet.GetComponent<BulletController>();
        bc.Speed *= 2;
        timer = 0.0f;
    }
    
    public void Destroy(Guid killerId = new Guid())
    {
        if(killerId != Guid.Empty)
            playerSave.Destroy(killerId);
        else 
            playerSave.Destroy();
        GameManager.Instance.reloadScene();
        Destroy(gameObject);
    }

    public Vector3 Velocity => velocity;
}
