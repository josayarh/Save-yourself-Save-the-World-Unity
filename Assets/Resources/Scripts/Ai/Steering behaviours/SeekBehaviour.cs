using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeekBehaviour
{
    private Transform targetTransform;
    private Transform aiTransform;
    
    private float max_velocity;
    private float max_force;
    private float mass;
    private float max_speed;

    public SeekBehaviour(float maxVelocity = 1.5f, float maxForce = 1.5f, float mass = 1, float maxSpeed = 3)
    {
        max_velocity = maxVelocity;
        max_force = maxForce;
        this.mass = mass;
        max_speed = maxSpeed;
    }

    public Vector3 getSeekForce(Transform targetTransform, Transform aiTransform, Vector3 velocity)
    {
        Vector3 dersired_velocity = Vector3.Normalize(targetTransform.position - aiTransform.position) * max_velocity;
        Vector3 steering = dersired_velocity - velocity;
        steering = Vector3.ClampMagnitude(steering, max_force);
        steering = steering / mass;

        return steering;
    }
}
