using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/***
 * Interface used to define 
 */
public interface BaseAI
{
    /***
     * Performs a transition from attack state to wander state
     */
    void attackWanderTransition();
    /***
     * Performs a transition from wander state to attack state
     */
    void wanderAttackTransistion(GameObject target);
}
