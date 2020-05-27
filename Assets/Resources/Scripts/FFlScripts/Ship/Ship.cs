using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using MLAgents;
using MLAgents.Sensor;
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

        private void Start()
        {
            if (isPlayer)
            {
                PlayerShip = this;
                DontDestroyOnLoad(gameObject);
            }
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
            if (isPlayer)
            {
                if (isFSMdriven)
                {
                    fsm.CurrentState.Reason(GameManager.Instance.Player, gameObject);
                    fsm.CurrentState.Act(GameManager.Instance.Player, gameObject);
                }
                else{
                    float fireKey = UnityEngine.Input.GetAxis("Fire1");
                    timer += Time.deltaTime;
                    isShooting = false;

                    if (timer > fireRate && fireKey != 0)
                    {
                        fire();
                    }

                    // Pass the input to the physics to move the ship.
                    Physics.SetPhysicsInput(
                        new Vector3(Input.Strafe, 0.0f, Input.Throttle),
                        new Vector3(Input.Pitch, Input.Yaw, Input.Roll));


                    PlayerShip = this;

                }
            }
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
            isShooting = true;
        }
        
        public void Destroy(Guid killerId = new Guid())
        {
            if(killerId != Guid.Empty)
                playerSave.Destroy(killerId);
            else 
                playerSave.Destroy();
            
            if (isPlayer)
            {
                Done();
                OnRelease();
                GameManager.Instance.reloadScene();
            }
            
            
            if(!isPlayer)
                Pool.Instance.release(gameObject, 
                    PoolableTypes.Player);
        }

        public override void InitializeAgent()
        {
            //rayPer = GetComponent<RayPerceptionSensorComponent3D>();
        }

        //Start methods for machine learning 
        
        
        public override void CollectObservations()
        {
            if (useObs)
            {
                
                var velocity = transform.position;
                var rotation = transform.rotation;
                
                AddVectorObs(velocity.normalized.x);
                AddVectorObs(velocity.normalized.y);
                AddVectorObs(velocity.normalized.z);
                AddVectorObs(rotation.normalized.x);
                AddVectorObs(rotation.normalized.y);
                AddVectorObs(rotation.normalized.z);
                AddVectorObs(System.Convert.ToInt32(isShooting));
            }
        }

        public override void AgentAction(float[] vectorAction)
        {
            if (!isPlayer && vectorAction.Length > 6)
            {

                Physics.SetPhysicsInput(
                    new Vector3(vectorAction[0], 0.0f, vectorAction[1]),
                    new Vector3(vectorAction[2], vectorAction[3], vectorAction[4]));

                
                if (vectorAction[5] >= 0.80)
                {
                    fire();
                }
                
                AddReward(0.1f);
            }
        }
        
        public override float[] Heuristic()
        {
            var action = new float[7];
            
            if(isPlayer){
                if (isFSMdriven)
                {
                    action[1] = 1;

                    var quat = Quaternion.LookRotation
                        (transform.forward, Vector3.up);

                    action[4] = Mathf.Atan2(2*quat.y*quat.w - 2*quat.x*quat.z,
                        1 - 2*quat.y*quat.y - 2*quat.z*quat.z);
                    action[2] = Mathf.Atan2(2*quat.x*quat.w - 2*quat.y*quat.z,
                        1 - 2*quat.x*quat.x - 2*quat.z*quat.z);
                    action[3] = Mathf.Asin(2*quat.x*quat.y + 2*quat.z*quat.w);

                    action[5] = Convert.ToSingle(isShooting);
                }
                else
                {
                    action[0] = Input.Strafe;
                    action[1] = Input.Throttle;

                    action[2] = Input.Pitch;
                    action[3] = Input.Yaw;
                    action[4] = Input.Roll;

                    action[5] = Convert.ToSingle(isShooting);
                }

            }
            
            return action;
        }

        public void addRewardOnKill()
        {
            AddReward(200.0f);
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
