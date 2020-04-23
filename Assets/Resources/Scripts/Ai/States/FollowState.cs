using System.Collections;
using System.Collections.Generic;
using FLFlight;
using UnityEngine;

public class FollowState : FSMState
{
    private static float DISTANCE_BEHIND_LEADER = 5;
    private static float ARRIVAL_RADIUS_BEFORE_SLOW_DOWN = 10;
    private static float SEPARATION_RADIUS = 3;
    private static float MAX_SEPARATION = 10;
    
    private GameObject leader;
    private Ship leaderController;
    private float maxVelocity;
    private NpcType typeToDetect;

    private Vector3 velocity;
    private Vector3 followForce;
    private Vector3 steering = Vector3.zero;
    private float MaxForce = 0.2f;
    private NPCDetector npcDetector;
    private float detectrange = 100;

    private bool isMovementset;

    public FollowState(GameObject leader, Ship leaderController, 
        float maxVelocity, NpcType typeToDetect)
    {
        this.leader = leader;
        this.leaderController = leaderController;
        this.maxVelocity = 30;
        this.typeToDetect = typeToDetect;
        
        npcDetector = new NPCDetector();

        stateID = StateID.BotFollowStateID;
    }

    private Vector3 arrive(Vector3 npcPosition, Vector3 targetPosition)
    {
        Vector3 desiredVelocity = targetPosition - npcPosition;
        float distance = desiredVelocity.magnitude;

        if (distance < ARRIVAL_RADIUS_BEFORE_SLOW_DOWN)
        {
            desiredVelocity = maxVelocity * (distance/ARRIVAL_RADIUS_BEFORE_SLOW_DOWN)
                                          * Vector3.Normalize(desiredVelocity);
        }
        else
        {
            desiredVelocity = Vector3.Normalize(desiredVelocity) * maxVelocity;
        }

        return desiredVelocity - velocity;
    }

    private Vector3 followLeader(GameObject npc, Vector3 leaderPosition)
    {
        Vector3 leaderVelocity = leaderController.Velocity;
        Vector3 force = Vector3.zero;

        leaderVelocity *= -1;
        leaderVelocity.Normalize();
        leaderVelocity *= DISTANCE_BEHIND_LEADER;

        force = arrive(npc.transform.position,leaderPosition + leaderVelocity);
        force += separation(npc, Pool.Instance.PlayerBotList);
        return force;
    }

    private Vector3 separation(GameObject npc, List<GameObject> otherVehicleList)
    {
        Vector3 force = Vector3.zero;
        Vector3 npcPosition = npc.transform.position;
        int neighborCount = 0;


        for (int c = 0; c < otherVehicleList.Count; c++)
        {
            GameObject vehicle = otherVehicleList[c];
            Vector3 vehiclePosition = vehicle.transform.position;

            if (vehicle != npc &&
                Vector3.Distance(vehiclePosition, npcPosition) < SEPARATION_RADIUS)
            {
                force.x = vehiclePosition.x - npcPosition.x;
                force.y = vehiclePosition.y - npcPosition.y;
                force.z = vehiclePosition.z - npcPosition.z;

                neighborCount++;
            }
        }

        if (neighborCount > 0)
        {
            force /= neighborCount;
            force *= -1;
        }
        
        force.Normalize();
        force *= MAX_SEPARATION;

        return force;
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
            followForce = followLeader(npc, leader.transform.position);
        }
    }

    public override void Act(GameObject player, GameObject npc)
    {
        steering = followForce;
        steering = Vector3.ClampMagnitude(steering, MaxForce);
        steering /= npc.GetComponent<Rigidbody>().mass;

        velocity = Vector3.ClampMagnitude(velocity + steering, maxVelocity);

        
        npc.transform.LookAt(npc.transform.position + velocity);
        if (npc.tag == "Player")
        {
            if (!isMovementset)
            {
                npc.GetComponent<Ship>().setAIMovement(velocity);
                isMovementset = true;
            }
        }
        else
        {
            npc.transform.position += velocity * Time.fixedDeltaTime;
        }
    }

    public override void DoBeforeLeaving()
    {
        isMovementset = false;
    }
}
