using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class EnemyMoveState : FSMState
{
    private float speed;
    private Transform enemyTransform;
    private Stopwatch watch;
    private long time;
    private Vector3 direction;

    private Time unityTime;

    public EnemyMoveState(Transform enemyTransform, float speed)
    {
        this.speed = speed;
        this.enemyTransform = enemyTransform;
        watch = System.Diagnostics.Stopwatch.StartNew();
    }

    public override void DoBeforeEntering()
    {
        
    }

    public override void DoBeforeLeaving()
    {
        watch.Stop();
    }

    public override void Reason(GameObject player, GameObject npc)
    {
        time = watch.ElapsedMilliseconds / 1000;
        Debug.Log(time);
    }

    public override void Act(GameObject player, GameObject npc)
    {
        if (time > 1.0)
        {
            direction = new Vector3(Random.Range(-1.0f,1.0f), Random.Range(-1.0f,1.0f)
                        , Random.Range(-1.0f,1.0f));
            watch = System.Diagnostics.Stopwatch.StartNew();
            time = 0;
        }
        enemyTransform.Translate(direction*speed*Time.deltaTime);
    }
}
