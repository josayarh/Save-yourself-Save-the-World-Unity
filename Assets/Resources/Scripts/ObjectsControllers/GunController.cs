using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunController : MonoBehaviour
{
    [Header("Rotation Limits")]
    [Tooltip("Turn rate of the turret's base and barrels in degrees per second.")]
    public float turnRate = 30.0f;
    [Tooltip("When true, turret rotates according to left/right traverse limits. When false, turret can rotate freely.")]
    public bool limitTraverse = false;
    [Tooltip("When traverse is limited, how many degrees to the left the turret can turn.")]
    [Range(0.0f, 180.0f)]
    public float leftTraverse = 60.0f;
    [Tooltip("When traverse is limited, how many degrees to the right the turret can turn.")]
    [Range(0.0f, 180.0f)]
    public float rightTraverse = 60.0f;
    [Tooltip("How far up the barrel(s) can rotate.")]
    [Range(0.0f, 90.0f)]
    public float elevation = 60.0f;
    [Tooltip("How far down the barrel(s) can rotate.")]
    [Range(0.0f, 90.0f)]
    public float depression = 5.0f;
    [Tooltip("Reticule to shoot to")] 
    public GameObject fixedReticule;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
