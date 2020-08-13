using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace FLFlight
{
    /// <summary>
    /// Ties all the primary ship components together.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Ship : Agent,IPoolableObject,BaseAI
    {
        [Tooltip("Set this ship to be the player ship. The player ship can always be accessed through the PlayerShip property.")]
        [SerializeField] private bool isPlayer = false;
        [SerializeField] private float fireRate = 0.5f;
        [SerializeField] private Transform gunTipPosition;
        [SerializeField] private Transform gun;
        [SerializeField] private bool useObs;
        [SerializeField] private bool isFSMdriven;
        [SerializeField] private float detectRange = 300;
        [SerializeField] private float attackRange = 20;
        
        // Keep a static reference for whether or not this is the player ship. It can be used
        // by various gameplay mechanics. Returns the player ship if possible, otherwise null.
        public static Ship PlayerShip { get; private set; }

        public Vector3 Velocity { get { return Physics.Rigidbody.velocity; } }
        public ShipInput Input { get; private set; }
        public ShipPhysics Physics { get; internal set; }
        
        private float timer = 0.0f;
        private PlayerSave playerSave;
        private RayPerceptionSensorComponent3D rayPer;
        private Rigidbody rBody;
        private bool isShooting = false;
        
        private FSMSystem fsm;
        private AttackState attackState;
        
        protected List<String> frameSteps;

        private Vector3 prevPoint;
        private Vector3 prevForward;
        private NPCDetector npcDetector;
        private GameObject target;

        private float dotProduct;

        private Vector3 startPostion;

        private void Start()
        {
            if (isPlayer)
            {
                PlayerShip = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                startPostion = transform.position;
            }
            
            npcDetector = new NPCDetector();
        }
        

        public void OnPoolCreation()
        {
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            
            if(isPlayer)
                DontDestroyOnLoad(gameObject);
        }

        public void creationSetup()
        {
            if (isPlayer)
            {
                if(playerSave)
                    playerSave.Id = Guid.NewGuid();
                if (isFSMdriven)
                {
                    makeFSM();
                }
            }
        }
        
        private void makeFSM()
        {
            fsm = new FSMSystem();
            PlayerSave ps = gameObject.GetComponent<PlayerSave>();
        
            FollowState followState = new FollowState(GameManager.Instance.Player, 
                GameManager.Instance.Player.GetComponent<Ship>(), Velocity.magnitude,
                NpcType.Enemy, detectRange);
            followState.AddTransition(Transition.Follow_Attack, StateID.EnemyAttackStateID);
        
            attackState = new AttackState(gunTipPosition,Vector3.forward*Velocity.magnitude, 
                Resources.Load("Prefabs/shot_prefab") as GameObject, ps.Id);
            attackState.AddTransition(Transition.Attack_Follow, StateID.BotFollowStateID);
        
            fsm.AddState(followState);
            fsm.AddState(attackState);
        }

        public void OnRelease()
        {
            timer = 0.0f;
            isShooting = false;
            
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            
            transform.parent.transform.position = Vector3.zero;
            transform.parent.transform.rotation = Quaternion.identity;
            transform.parent.transform.localScale = Vector3.one;
            
            playerSave.Id = Guid.Empty;
            playerSave.FrameSaveList.Clear();
        }

        private void Awake()
        {
            Input = GetComponent<ShipInput>();
            Physics = GetComponent<ShipPhysics>();

            playerSave = GetComponent<PlayerSave>();
            rBody = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            timer += Time.deltaTime;
        }

        public void setAIMovement(Vector3 velocity)
        {
            Physics.SetPhysicsInput(new Vector3(0.0f,0.0f
                    ,(velocity * Time.fixedDeltaTime).normalized.magnitude),
                Vector3.zero);
        }
        
        private void fire()
        {
            GameObject bullet = Pool.Instance.get(PoolableTypes.Bullets, gunTipPosition, playerSave.Id);
            timer = 0.0f;
            isShooting = false;
        }
        
        public void Destroy(Guid killerId = new Guid())
        {
            if(killerId != Guid.Empty)
                playerSave.Destroy(killerId);
            else 
                playerSave.Destroy();
            
            if(Pool.Instance.EnemyList.Count != 0)
                AddReward(-1f/EnemyManager.Instance.MAXEnemies);
            
            
            if (isPlayer)
            {
                EndEpisode();
                GameManager.Instance.notifyAgentsAlldone();
                OnRelease();
                GameManager.Instance.reloadScene();
            }
            
            if(!isPlayer)
                Pool.Instance.release(gameObject, 
                    PoolableTypes.Player);
        }

        void getProbableTarget()
        {
            float radius = 100000.0f;
            RaycastHit[] raycastHit;
            int layerMask = 1 << 9;
            GameObject mostProbableEnemy;

            raycastHit= UnityEngine.Physics.SphereCastAll(gameObject.transform.position, radius,
                gameObject.transform.forward, 0, layerMask);

            
            if (raycastHit.Length > 0)
            {
                for (int i = 0; i < raycastHit.Length; ++i)
                {
                    GameObject rayhit = raycastHit[i].transform.gameObject;
                    
                    Vector3 targetToPlayer = (rayhit.transform.position 
                                              - transform.position).normalized;
                    float tempDotProduct = Vector3.Dot(transform.forward.normalized, 
                        targetToPlayer);

                    if (tempDotProduct < dotProduct)
                    {
                        target = rayhit;
                        dotProduct = tempDotProduct;
                    }
                }
            }
        }

        //Start methods for machine learning 


        public override void CollectObservations(VectorSensor sensor)
        {
            if (useObs)
            {
                if(target==null && !isPlayer)
                    getProbableTarget();
                
                if (target != null)
                {
                    Vector3 targetToPlayer = (target.transform.position
                                              - transform.position);

                    sensor.AddObservation(targetToPlayer.magnitude);
                    sensor.AddObservation(dotProduct);
                    sensor.AddObservation(target.transform.position);
                }
                else
                {
                    sensor.AddObservation(1000000);
                    sensor.AddObservation(1000000);
                    sensor.AddObservation(Vector3.one);
                }

                sensor.AddObservation(Pool.Instance.EnemyList.Count);
                sensor.AddObservation(isShooting);
            }
        }

        public override void OnActionReceived(float[] vectorAction)
        {
            if (vectorAction.Length > 5)
            {

                Physics.SetPhysicsInput(
                    new Vector3(vectorAction[1], 0.0f, vectorAction[0]),
                    new Vector3(vectorAction[2], vectorAction[3], 
                        vectorAction[4]));

                
                if (timer > fireRate && vectorAction[5] >= 0.5)
                {
                    fire();
                    isShooting = false;
                }
            }
        }

        public override void Heuristic(float[] actionsOut)
        {
            if(isPlayer){
                if (isFSMdriven)
                {
                    if (target == null || !target.activeSelf){
                        target = npcDetector.getNpcInRange(NpcType.Enemy, 
                            transform.position, detectRange);
                    }
        
                    actionsOut[0] = 1;
                    actionsOut[1] = 0;

                    if (target != null)
                    {
                        Vector3 localGotoPos = transform.InverseTransformVector
                            (target.transform.position - transform.position).normalized;

                        actionsOut[2] = Mathf.Clamp(-localGotoPos.y * 
                                                    Input.PitchSensitivity, -1f, 1f);
                        actionsOut[3] = Mathf.Clamp(localGotoPos.x * Input.YawSensitivity,
                                -1f, 1f);

                        if (timer > fireRate && Vector3.Distance(target.transform.position,
                            transform.position) > attackRange)
                        {
                            actionsOut[5] = 1;
                            isShooting = true;
                            AddReward(0.001f);
                        }
                        else
                            actionsOut[5] = 0;

                    }
                }
                else
                {
                    actionsOut[0] = Input.Throttle;
                    actionsOut[1] = Input.Strafe;

                    actionsOut[2] = Input.Pitch;
                    actionsOut[3] = Input.Yaw;
                    actionsOut[4] = Input.Roll;

                    float fireKey = UnityEngine.Input.GetAxis("Fire1");
                    
                    if (timer > fireRate && fireKey != 0)
                    {
                        actionsOut[5] = 1;
                        isShooting = true;
                    }
                    else
                    {
                        actionsOut[5] = 0;
                    }
                    
                    getProbableTarget();
                }

            }
        }

        public override void OnEpisodeBegin()
        {
            if (!isPlayer)
            {
                transform.position = startPostion;
                transform.rotation = Quaternion.identity;
            }
        }

        public void addRewardOnKill()
        {
            AddReward(1f/EnemyManager.Instance.MAXEnemies);
        }

        public bool IsPlayer => isPlayer;

        public List<string> FrameSteps
        {
            get => frameSteps;
            set
            { 
                frameSteps = value;
                LoadFrame(value[0]);
            }
        }
        
        public void LoadFrame(string binarySave)
        {
            byte[] byteArray = Convert.FromBase64String(binarySave);
            MemoryStream mf = new MemoryStream(byteArray);
            BinaryFormatter bf = new BinaryFormatter();
            PlayerBaseFrameData data = (PlayerBaseFrameData)bf.Deserialize(mf);

            transform.position = VectorArrayConverter.arrayToVector3(data.position);
            transform.rotation = Quaternion.Euler(VectorArrayConverter.arrayToVector3(data.rotation));
        
            playerSave.Id = new Guid(data.id);
        }

        public void attackWanderTransition()
        {
            fsm.PerformTransition(Transition.Attack_Follow);
        }

        public void wanderAttackTransistion(GameObject target)
        {
            attackState.Target = target;
            fsm.PerformTransition(Transition.Follow_Attack);
        }
    }
}
