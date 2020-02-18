using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WanderBehaviour
{
    private static float CIRCLE_RADIUS = 1.5f;
    private static float CIRCLE_DISTANCE = 3;
    
    private static float MaxForce = 1.5f;

    public Vector3 getSteeringForce(Vector3 velocity)
    {
        Vector3 steering = Vector3.ClampMagnitude(applyDisplacement(velocity), MaxForce);

        return steering;
    }
    
    private Vector3 calcCircle(Vector3 velocity)
    {
        Vector3 circlecenter = velocity;
        
        circlecenter.Normalize();
        circlecenter *= CIRCLE_DISTANCE;
        
        return circlecenter;
    }

    private Vector3 applyDisplacement(Vector3 velocity)
    {
        Vector3 circleCenter = calcCircle(velocity);
        Vector3 randomPoint = Random.insideUnitCircle;
        
        randomPoint.Normalize();
        randomPoint *= CIRCLE_RADIUS;

        return circleCenter + Quaternion.LookRotation(velocity) * randomPoint;
    }
}
