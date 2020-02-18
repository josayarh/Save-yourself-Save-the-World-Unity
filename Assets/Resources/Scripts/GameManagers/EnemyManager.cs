using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [SerializeField] private int maxEnemies = Int32.MaxValue;
    private List<Transform> enemyPositions = new List<Transform>();
    private static EnemyManager instance = null;
    //private List<Tuple<Guid, Transform>> enemySpawnlist;
    
    // Start is called before the first frame update
    void Start()
    {
        if (instance == null)
        {
            DontDestroyOnLoad(gameObject);
            instance = this;

            for (int i = 0; i < transform.childCount && enemyPositions.Count < maxEnemies; ++i)
            {
                Transform childTransform = transform.GetChild(i);
                if (childTransform.childCount > 0)
                {
                    for (int c = 0; c < childTransform.childCount; ++c)
                    {
                        enemyPositions.Add(childTransform.GetChild(c));
                    }
                }
                else
                {
                    enemyPositions.Add(childTransform);
                }
            }
            
            foreach (var transform in enemyPositions)
            {
                GameObject go = Pool.Instance.get(PoolableTypes.Enemy, transform);
                
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public EnemyManager Instance => instance;
}
