using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum NpcType
{
    PlayerBot,
    Enemy
}

public class NPCDetector
{
    private Dictionary<NpcType, List<GameObject>> NpcDictionary = new Dictionary<NpcType, List<GameObject>>();
    
    public NPCDetector()
    {
        Pool pool = Pool.Instance;
        
        NpcDictionary.Add(NpcType.Enemy, pool.EnemyList);
        NpcDictionary.Add(NpcType.PlayerBot, pool.PlayerBotList);
    }

    public GameObject getNpcInRange(NpcType npcType, Vector3 position, float range)
    {
        List<GameObject> npcList;
        if (NpcDictionary.TryGetValue(npcType, out npcList))
        {
            foreach (GameObject npc in npcList)
            {
                if (Vector3.Distance(npc.transform.position, position) < range)
                    return npc;
            }
        }

        return null;
    }
}
