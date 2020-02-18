using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

public class WanderState : FSMState
{
    private static float TIME_TILL_DIRECTION_CHANGE = 2;
    private static float CIRCLE_RADIUS = 2;
    private static float CIRCLE_DISTANCE = 6;
    private static float ANGLE_CHANGE = 2;
    private static float MAX_SEE_AHEAD = 10;
    private static float MAX_AVOID_FORCE = 5;
    private static float MAX_DISTANCE_TILL_RETURN_TO_START = 50;
    private static float DISTANCE_TILL_STOP_RETURN = 15;
    
    private Vector3 velocity;
    private Vector3 startPosition;
    private Transform gunTipPosition;
    private Vector3 wanderForce = Vector3.zero;
    private bool goBackToStart = false;
    
    private NpcType typeToDetect;
    private float detectrange;

    private float MaxForce = 0.2f;
    private float MaxSpeed = 15;
    private float TimeSinceLastUpdate;
    private Quaternion randomAngle = Quaternion.identity;
    
    private Vector3 steering = Vector3.zero;
    private NPCDetector npcDetector;
    

    public WanderState(GameObject npc, Vector3 velocity, NpcType typeToDetect, float detectrange,
        Transform gunTipPosition)
    {
        this.velocity = npc.transform.forward * MaxSpeed;
        this.startPosition = npc.transform.position;
        this.detectrange = detectrange;
        this.typeToDetect = typeToDetect;
        this.gunTipPosition = gunTipPosition;
        
        npcDetector = new NPCDetector();
        randomAngle = Random.rotation;
                
        stateID = StateID.EnemyWanderStateID;
    }

    private Vector3 getWander(Transform npcTransform)
    {
        Vector3 circleCenter = new Vector3();
        circleCenter = velocity;
        circleCenter.Normalize();
        circleCenter *= CIRCLE_DISTANCE;
            
        Vector3 displacement = npcTransform.forward;
        displacement *= CIRCLE_RADIUS;
        displacement = randomAngle * displacement;

        randomAngle.eulerAngles += new Vector3(
            (float)(Random.value * ANGLE_CHANGE - ANGLE_CHANGE * .5),
            (float)(Random.value * ANGLE_CHANGE - ANGLE_CHANGE * .5),
            (float)(Random.value * ANGLE_CHANGE - ANGLE_CHANGE * .5));

        return circleCenter + displacement;
    }

    private Vector3 calculateAvoidance(RaycastHit obstacle, Vector3 ahead)
    {
        Vector3 obstaclePosition = obstacle.collider.transform.position;
        Vector3 avoidance = new Vector3(0,0,0);
        
        avoidance.x = obstacle.point.x - obstaclePosition.x;
        avoidance.y = obstacle.point.y - obstaclePosition.y;
        avoidance.z = obstacle.point.z - obstaclePosition.z;
                
        avoidance.Normalize();
        avoidance *= MAX_AVOID_FORCE;

        return avoidance;
    }

    private Vector3 getAvoidance(GameObject npc)
    {
        Vector3 avoidance = new Vector3(0,0,0);
        float dynamaicLength = velocity.magnitude;
        
        RaycastHit hitAhead1;

        Vector3 ahead1 = npc.transform.forward;

        if (Physics.Raycast(gunTipPosition.position, ahead1, out hitAhead1, dynamaicLength))
        {
            avoidance = getFlee(npc.transform.position, hitAhead1.point);
            goBackToStart = true;
        }

        return avoidance;
    }

    private Vector3 getFlee(Vector3 npcPosition, Vector3 targetPosition)
    {
        Vector3 desiredVelocity = npcPosition - targetPosition;
        desiredVelocity = desiredVelocity.normalized * MaxSpeed;

        return desiredVelocity - velocity;
    }

    private Vector3 getSeek(Vector3 npcPosition, Vector3 targetPosition)
    {
        Vector3 desiredVelocity = targetPosition - npcPosition;
        desiredVelocity = desiredVelocity.normalized * MaxSpeed;

        return desiredVelocity - velocity;
    }
    
    public override void Reason(GameObject player, GameObject npc)
    {
        GameObject target = npcDetector.getNpcInRange(typeToDetect, npc.transform.position, detectrange);

        if (target != null)
        {
            BaseAI baseAi = npc.GetComponent<BaseAI>();
            baseAi.wanderAttackTransistion(target);
        }
        else
        {
            if (Vector3.Distance(startPosition, npc.transform.position) 
                >= MAX_DISTANCE_TILL_RETURN_TO_START)
            {
                goBackToStart = true;
            }

            if (goBackToStart)
            {
                wanderForce = getSeek(npc.transform.position, startPosition);
                if (Vector3.Distance(startPosition, npc.transform.position) <
                    DISTANCE_TILL_STOP_RETURN)
                    goBackToStart = false;
            }
            else
            {
                wanderForce = getWander(npc.transform);
            }
            
            wanderForce += getAvoidance(npc);
        }
    }

    public override void Act(GameObject player, GameObject npc)
    {
        steering = wanderForce;
        steering = Vector3.ClampMagnitude(steering, MaxForce);
        steering /= npc.GetComponent<Rigidbody>().mass;

        velocity = Vector3.ClampMagnitude(velocity + steering, MaxSpeed);
        
        npc.transform.LookAt(npc.transform.position + velocity);
        npc.transform.position += velocity * Time.fixedDeltaTime;

    }
}
