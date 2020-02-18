using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackState : FSMState
{
    private Guid id;
    
    private Transform gunTipPosition;
    private Vector3 velocity;
    
    private float max_velocity = 1.5f;
    private float max_force = 1.5f;
    private float mass = 2;
    private float max_speed = 3;
    
    private GameObject laserPrefab;
    private GameObject target;

    private Vector3 steering;
    private float timer = 0.0f;
    
    SeekBehaviour seekBehaviour = null;


    public AttackState(Transform gunTipPosition, Vector3 velocity, GameObject laserPrefab, Guid npcId)
    {
        this.gunTipPosition = gunTipPosition;
        this.velocity = velocity;
        this.laserPrefab = laserPrefab;
        this.id = npcId;
        
        seekBehaviour = new SeekBehaviour();
        
        stateID = StateID.EnemyAttackStateID;
    }

    public override void Reason(GameObject player, GameObject npc)
    {
        if (target == null || !target.activeSelf)
        {
            BaseAI baseAi = npc.GetComponent<BaseAI>();
            baseAi.attackWanderTransition();
        }
        else
        {
            steering = seekBehaviour.getSeekForce(target.transform, npc.transform, velocity);
            velocity = Vector3.ClampMagnitude(velocity + steering, max_speed);
            timer += Time.deltaTime;
        }
    }

    public override void Act(GameObject player, GameObject npc)
    {
        npc.transform.position += velocity*Time.fixedDeltaTime;
        npc.transform.forward = velocity.normalized;

        float angle = Vector3.Angle(npc.transform.forward, target.transform.position - npc.transform.position);

        if (Mathf.Abs(angle) < 90 && timer > 1.0f)
        {
            GameObject bullet = Pool.Instance.get(PoolableTypes.Bullets, gunTipPosition, id);
            
            timer = 0.0f;
        }
    }

    public GameObject Target
    {
        get => target;
        set => target = value;
    }
}
