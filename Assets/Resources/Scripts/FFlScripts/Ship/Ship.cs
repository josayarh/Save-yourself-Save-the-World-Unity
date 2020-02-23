using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using MLAgents;
using UnityEngine;

namespace FLFlight
{
    /// <summary>
    /// Ties all the primary ship components together.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Ship : Agent,IPoolableObject
    {
        [Tooltip("Set this ship to be the player ship. The player ship can always be accessed through the PlayerShip property.")]
        [SerializeField] private bool isPlayer = false;
        [SerializeField] private float fireRate = 0.5f;
        [SerializeField] private Transform gunTipPosition;
        [SerializeField] private Transform gun;
        [SerializeField] private bool useObs;
        
        // Keep a static reference for whether or not this is the player ship. It can be used
        // by various gameplay mechanics. Returns the player ship if possible, otherwise null.
        public static Ship PlayerShip { get; private set; }

        public Vector3 Velocity { get { return Physics.Rigidbody.velocity; } }
        public ShipInput Input { get; private set; }
        public ShipPhysics Physics { get; internal set; }
        
        private float timer = 0.0f;
        private PlayerSave playerSave;
        private RayPerception3D rayPer;
        private Rigidbody rBody;
        private bool isShooting = false;
        
        protected List<String> frameSteps;

        private void Start()
        {
            if(isPlayer)
                DontDestroyOnLoad(gameObject);
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
            }
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
            if (isPlayer)
            {
                Input = GetComponent<ShipInput>();
                Physics = GetComponent<ShipPhysics>();
            }

            playerSave = GetComponent<PlayerSave>();
            rBody = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if (isPlayer)
            {
                float fireKey = UnityEngine.Input.GetAxis("Fire1");
                timer += Time.deltaTime;
                isShooting = false;

                if (timer > fireRate && fireKey != 0)
                {
                    fire();
                }

                // Pass the input to the physics to move the ship.
                Physics.SetPhysicsInput(new Vector3(Input.Strafe, 0.0f, Input.Throttle),
                    new Vector3(Input.Pitch, Input.Yaw, Input.Roll));

                PlayerShip = this;
            }
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
            
            GameManager.Instance.reloadScene();
            
            if(isPlayer)
                OnRelease();
            
            if(!isPlayer)
                Pool.Instance.release(gameObject.transform.parent.gameObject, 
                    PoolableTypes.Player);
        }

        public override void InitializeAgent()
        {
            rayPer = GetComponent<RayPerception3D>();
        }

        public override float[] Heuristic()
        {
            return new float[] { 0 };
        }

        //Start methods for machine learning 
        public override void CollectObservations()
        {
            if (useObs)
            {
                const float rayDistance = 50f;
                float[] rayAngles = {20f, 90f, 160f, 45f, 135f, 70f, 110f};
                string[] detectableObjects = {"Wall", "Enemy", "PlayerBot", "Building"};
                AddVectorObs(rayPer.Perceive(rayDistance, rayAngles, detectableObjects, 0f, 0f));

                var localVelocity = transform.InverseTransformDirection(Velocity);
                var localRotation = transform.localRotation;
                AddVectorObs(localVelocity.normalized.x);
                AddVectorObs(localVelocity.normalized.y);
                AddVectorObs(localVelocity.normalized.z);
                AddVectorObs(localRotation.normalized.x);
                AddVectorObs(localRotation.normalized.y);
                AddVectorObs(localRotation.normalized.z);
                AddVectorObs(System.Convert.ToInt32(isShooting));
            }
        }

        public override void AgentAction(float[] vectorAction)
        {
            if (!isPlayer && vectorAction.Length > 6)
            {
                rBody.velocity = new Vector3(vectorAction[0],vectorAction[1],vectorAction[2]);
                transform.Rotate(vectorAction[3],vectorAction[4],vectorAction[5]);

                if (vectorAction[6] == 1.0f)
                {
                    fire();
                }
                
                AddReward(0.1f);
            }
        }

        public void addRewardOnKill()
        {
            AddReward(5.0f);
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
        
        
    }
}
