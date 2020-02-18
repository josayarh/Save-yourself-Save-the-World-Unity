using System;
using System.Collections;
using System.Collections.Generic;
using FLFlight;
using UnityEngine;
using MLAgents;

public class AgentController : Agent 
{
        [Tooltip("Set this ship to be the player ship. The player ship can always be accessed through the PlayerShip " +
                 "property.")]
        [SerializeField] private bool isPlayer = false;
        [SerializeField] private float fireRate = 0.5f;
        [SerializeField] private Transform gunTipPosition;
        [SerializeField] private Transform gun;
        
        // Keep a static reference for whether or not this is the player ship. It can be used
        // by various gameplay mechanics. Returns the player ship if possible, otherwise null.
        public static Ship PlayerShip { get; private set; }

        public Vector3 Velocity { get { return Physics.Rigidbody.velocity; } }
        public ShipInput Input { get; private set; }
        public ShipPhysics Physics { get; internal set; }
        
        private float timer = 0.0f;
        private PlayerSave playerSave;
        
        void Start()
        {
            playerSave = GetComponent<PlayerSave>();
        }

        private void Awake()
        {
            Input = GetComponent<ShipInput>();
            Physics = GetComponent<ShipPhysics>();
        }

        private void FixedUpdate()
        {
            float fireKey = UnityEngine.Input.GetAxis("Fire1");
            timer += Time.deltaTime;

            if (timer > fireRate && fireKey != 0)
            {
                fire();
            }
            
            // Pass the input to the physics to move the ship.
            Physics.SetPhysicsInput(new Vector3(Input.Strafe, 0.0f, Input.Throttle), 
                new Vector3(Input.Pitch, Input.Yaw, Input.Roll));

            // If this is the player ship, then set the static reference. If more than one ship
            // is set to player, then whatever happens to be the last ship to be updated will be
            // considered the player. Don't let this happen.
//            if (isPlayer)
//                PlayerShip = this;
        }
        
        private void fire()
        {
            GameObject bullet = Pool.Instance.get(PoolableTypes.Bullets, gunTipPosition, playerSave.Id);
            timer = 0.0f;
        }
        
        public void Destroy(Guid killerId = new Guid())
        {
            if(killerId != Guid.Empty)
                playerSave.Destroy(killerId);
            else 
                playerSave.Destroy();
            GameManager.Instance.reloadScene();
            Destroy(gameObject);
        } 
}