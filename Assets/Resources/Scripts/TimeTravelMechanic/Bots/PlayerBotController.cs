using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using FLFlight;
using UnityEngine;

public class PlayerBotController : Bot, IPoolableObject, BaseAI
{
    private float speed;
    private float rotateSpeedH;
    private float rotateSpeedV;
    private float fireRate;
    private float turnSpeed;
    private float accelerationFactor;
    
    [SerializeField] private Transform gunTipPosition;
    [SerializeField] private float detectRange;
    [SerializeField] private Material underAiMaterial;
    [SerializeField] private Material replayMaterial;
    
    private Rigidbody rigidBody;
    private GameObject laser;

    private FSMSystem fsm;
    bool isAiOn = false;
    
    private float timer = 0.0f;

    private AttackState attackState;
    private Guid killerGUID = new Guid();

    private int frameCountOnLoad;
    
    // Start is called before the first frame update
    void Start()
    {
        rigidBody = GetComponent<Rigidbody>();
        laser = Resources.Load("Prefabs/shot_prefab") as GameObject;
    }

    public void OnPoolCreation()
    {
        makeFSM();
        changeChildMaterial(gameObject.transform.GetChild(0).gameObject, replayMaterial);
    }
    
    private void makeFSM()
    {
        fsm = new FSMSystem();
        
        FollowState followState = new FollowState(GameManager.Instance.Player, 
            GameManager.Instance.Player.GetComponent<Ship>(), speed, NpcType.Enemy);
        followState.AddTransition(Transition.Follow_Attack, StateID.EnemyAttackStateID);
        
        attackState = new AttackState(gunTipPosition,Vector3.forward*speed, 
            Resources.Load("Prefabs/shot_prefab") as GameObject, id);
        attackState.AddTransition(Transition.Attack_Follow, StateID.BotFollowStateID);
        
        fsm.AddState(followState);
        fsm.AddState(attackState);
    }

    public override void FixedUpdate()
    {
        uint frameNumber = GameObjectStateManager.Instance.FrameNumber;
        
        if (frameCountOnLoad <= frameNumber)
        {
            if(!isAiOn)
                changeChildMaterial(gameObject.transform.GetChild(0).gameObject, underAiMaterial);
            
            isAiOn = true;
            fsm.CurrentState.Reason(GameManager.Instance.Player, gameObject);
            fsm.CurrentState.Act(GameManager.Instance.Player, gameObject);
            frameSteps.Add(SaveDiffFrame());
            if(killerGUID != Guid.Empty)
                Destroy(killerGUID);
        }
        else
        {
            if (frameNumber == 0)
            {
                LoadFrame(frameSteps[(int)frameNumber]);
            }
            else
            {
                LoadDiffFrame(frameSteps[(int)frameNumber]);
            }
        }
    }
    
    public void wanderAttackTransistion(GameObject target)
    {
        attackState.Target = target;
        fsm.PerformTransition(Transition.Follow_Attack);
    }

    public void attackWanderTransition()
    {
        fsm.PerformTransition(Transition.Attack_Follow);
    }
    
    public string SaveDiffFrame()
    {
        BinaryFormatter bf = new BinaryFormatter();
        MemoryStream ms = new MemoryStream();
        
        PlayerDiffFrameData frameData = new PlayerDiffFrameData();

        frameData.position = VectorArrayConverter.vector3ToArray(transform.position);
        frameData.rotation = VectorArrayConverter.vector3ToArray(transform.rotation.eulerAngles);

        if (killerGUID != Guid.Empty)
        {
            frameData.killerGUID = new Byte[killerGUID.ToByteArray().Length];
            killerGUID.ToByteArray().CopyTo(frameData.killerGUID,0);
        }
        
        bf.Serialize(ms,frameData);

        return Convert.ToBase64String(ms.ToArray());
    }
    

    public override void LoadFrame(string binarySave)
    {
        byte[] byteArray = Convert.FromBase64String(binarySave);
        MemoryStream mf = new MemoryStream(byteArray);
        BinaryFormatter bf = new BinaryFormatter();
        PlayerBaseFrameData data = (PlayerBaseFrameData)bf.Deserialize(mf);
        
        id = new Guid(data.id);

        transform.position = VectorArrayConverter.arrayToVector3(data.position);
        transform.rotation = Quaternion.Euler(VectorArrayConverter.arrayToVector3(data.rotation));
    }

    public override void LoadDiffFrame(string binarySave)
    {
        byte[] byteArray = Convert.FromBase64String(binarySave);
        MemoryStream mf = new MemoryStream(byteArray);
        BinaryFormatter bf = new BinaryFormatter();
        PlayerDiffFrameData data = (PlayerDiffFrameData)bf.Deserialize(mf);

        transform.position = VectorArrayConverter.arrayToVector3(data.position);
        transform.rotation = Quaternion.Euler(VectorArrayConverter.arrayToVector3(data.rotation));

        if (data.killerGUID != null)
        {
            killerGUID = new Guid(data.killerGUID);
            if (GameObjectStateManager.Instance.doesParentExist(killerGUID))
            {
                Destroy(killerGUID);
            }
        }
    }
    
    private void changeChildMaterial(GameObject parentGameObject,Material materialToApply)
    {
        GameObject child;
        for (int c=0; c < parentGameObject.transform.childCount; c++)
        {
            child = parentGameObject.transform.GetChild(c).gameObject;
            child.GetComponent<Renderer>().material = materialToApply;
        }
    }

    public void OnRelease()
    {
        isAiOn = false;
        if (id != Guid.Empty && frameSteps.Count <= GameObjectStateManager.Instance.FrameNumber)
        {
            GameObjectStateManager.Instance.addDynamicObject(id, GetType(), frameSteps, 0);
            frameSteps = new List<string>();
        }
    }

    public void Destroy(Guid killerGuid = new Guid())
    {
        if (GameObjectStateManager.Instance.doesParentExist(killerGuid))
        {
            killerGUID = killerGuid;
            Pool.Instance.release(gameObject, PoolableTypes.PlayerBot);
        }
    }

    public List<string> FrameSteps
    {
        set
        {
            frameSteps = value;
            LoadFrame(frameSteps[0]);
            frameCountOnLoad = value.Count;
        }
    }
}
