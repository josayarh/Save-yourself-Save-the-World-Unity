using System;
using System.Collections;
using System.Collections.Generic;
using FLFlight;
using UnityEngine;

enum DynamicTags
{
    Player,
    Dynamic,
}

public class GameObjectStateManager : MonoBehaviour
{
    private static GameObjectStateManager instance;
    private List<GameObject> dynamicGameObjectlist;
    private uint frameNumber = 0;
    
    private Dictionary<Guid, Tuple<Type ,List<string>>> frameDataDictionary 
        = new Dictionary<Guid, Tuple<Type, List<string>>>();
    private Dictionary<uint, List<Guid>> objectApperanceDictionnary = new Dictionary<uint, List<Guid>>();
    private Dictionary<Guid, GameObject> instanciatedGameobjects = new Dictionary<Guid, GameObject>();
    /***
     * Stores the objects ID whose existance depends on a parent object
     * Key : Object id
     * Value : Parent id 
     */
    private Dictionary<Guid, Guid> parentIds = new Dictionary<Guid, Guid>();
    
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            instance.frameNumber = 0;
        }

        DontDestroyOnLoad(gameObject);
    }

    private void FixedUpdate()
    {
        reloadObjects();
        frameNumber++;
    }

    public void initializeDynamicObjects()
    {
        dynamicGameObjectlist = new List<GameObject>();
        GameObject[] tab = GameObject.FindObjectsOfType<GameObject>();

        if (tab != null)
        {
            DynamicTags tmp;
            foreach (var gameObject in tab)
            {
                bool isDynamic = false;
                foreach (var name in Enum.GetNames(typeof(DynamicTags)))
                {
                    if (gameObject.CompareTag(name))
                        isDynamic = true;
                }
                if(isDynamic)
                    dynamicGameObjectlist.Add(gameObject);
            }
        }
    }

    private void reloadObjects()
    {
        List<Guid> objectList;
        if (objectApperanceDictionnary.TryGetValue(frameNumber, out objectList))
        {
            foreach(Guid id in objectList)
            {
                Tuple<Type, List<string>> gameObjectTuple;
                if (frameDataDictionary.TryGetValue(id, out gameObjectTuple))
                {
                    GameObject go = null;
                    
                    Guid tryGetParentGuid = Guid.Empty;
                    GameObject tryGetGameObject = null;

                    if (parentIds.TryGetValue(id, out tryGetParentGuid))
                    {
                        if (!doesParentExist(id))
                            break;
                    }

                    // if (gameObjectTuple.Item1 == typeof(PlayerSave))
                    // {
                    //     go = Pool.Instance.get(PoolableTypes.PlayerBot);
                    //     Ship ship  = go.GetComponent<Ship>();
                    //     ship.FrameSteps = gameObjectTuple.Item2;
                    // }
                    else if (gameObjectTuple.Item1 == typeof(BulletController))
                    {
                        go = Pool.Instance.get(PoolableTypes.BulletBot);
                        BulletBotController bulletBotController  = go.GetComponent<BulletBotController>();
                        bulletBotController.FrameSteps = gameObjectTuple.Item2;
                    }
                    else if(gameObjectTuple.Item1 == typeof(EnemyController))
                    {
                        go = Pool.Instance.get(PoolableTypes.Enemy);
                        EnemyController enemyController = go.GetComponent<EnemyController>();
                        enemyController.FrameSaveList = gameObjectTuple.Item2;
                    }

                    if (go != null)
                    {
                        addInstanciatedObject(id, go);
                    }
                }
            }
        }
    }

    public static GameObjectStateManager Instance
    {
        get => instance;
    }
    
    public void addInstanciatedObject(Guid objectId, GameObject instanciatedObject)
    {
        GameObject tryGetGameObject = null;
        if (instanciatedGameobjects.TryGetValue(objectId,out tryGetGameObject))
        {
            tryGetGameObject = instanciatedObject;
        }
        else
        {
            instanciatedGameobjects.Add(objectId,instanciatedObject);
        }
    }

    public void addDynamicObject(Guid guid, Type type, List<String> frameSave)
    {
        addDynamicObject(guid, type, frameSave, frameNumber);
    }
    
    public void addDynamicObject(Guid guid, Type type, List<String> frameSave, uint saveFrameNumber)
    {
        Tuple<Type, List<string>> tmp;
        if (frameDataDictionary.TryGetValue(guid, out tmp))
        {
            if(frameSave.Count > frameNumber + 1)
                frameSave.RemoveRange((int)frameNumber-1,(int)(frameSave.Count - frameNumber));
            tmp = new Tuple<Type, List<string>>(tmp.Item1, frameSave);
        }
        else
        {
            Tuple<Type, List<string>> couple = new Tuple<Type, List<string>>(type, frameSave);
            List<Guid> list = null;

            if (!objectApperanceDictionnary.TryGetValue(saveFrameNumber, out list))
            {
                list = new List<Guid>();
                objectApperanceDictionnary.Add(saveFrameNumber, list);
            }

            list.Add(guid);
            frameDataDictionary.Add(guid, couple);
        }
    }

    public bool doesParentExist(Guid childGUID)
    {
        bool existance = false;
        Guid parentGUID;

        if (parentIds.TryGetValue(childGUID, out parentGUID))
        {
            GameObject parentObject;
            if (instanciatedGameobjects.TryGetValue(parentGUID, out parentObject))
            {
                if (parentObject != null && parentObject.activeSelf)
                    existance = true;
            }   
        }

        return existance;
    }

    public GameObject getParent(Guid childGUID)
    {
        GameObject parentObject = null;
        Guid parentGUID;

        if (parentIds.TryGetValue(childGUID, out parentGUID))
        {
            GameObject tempObject = null;
            if (instanciatedGameobjects.TryGetValue(parentGUID, out tempObject))
            {
                if (tempObject != null && tempObject.activeSelf)
                    parentObject = tempObject;
            }   
        }

        return parentObject;
    }

    public uint FrameNumber
    {
        get => frameNumber;
    }
    
    public Dictionary<Guid, GameObject> InstanciatedGameobjects
    {
        get => instanciatedGameobjects;
        set => instanciatedGameobjects = value;
    }

    public Dictionary<Guid, Guid> ParentIds
    {
        get => parentIds;
        set => parentIds = value;
    }
}