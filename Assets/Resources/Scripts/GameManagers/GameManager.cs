using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using FLFlight;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = System.Object;
using Quaternion = UnityEngine.Quaternion;
using Random = System.Random;
using Vector3 = UnityEngine.Vector3;

public class GameManager : MonoBehaviour
{
    [SerializeField] private float timeLeft;
    [SerializeField] private GameObjectStateManager gameObjectStateManager;
    [SerializeField] private Canvas winScreen;
    [SerializeField] private float minDistanceBetweenShips = 5.0f;
    [SerializeField] private float playerSpawnSphereRadius = 20.0f;
    
    private float ennemiesLeft;
    private TextMeshProUGUI timerText = null;
    private int restartNumber=0;
    private static GameManager gMInstance = null;

    private List<Vector3> occupiedPositions = new List<Vector3>();
    
    private GameObject playerPrefab;
    private GameObject player;

    private static int PLACEMENT_LOOP_MAX = 20;
    
    void Start()
    {
        DontDestroyOnLoad(this);
        
        playerPrefab = Resources.Load("Prefabs/Player") as GameObject;

        if (gMInstance == null)
        {
            gMInstance = this;
            reloadObject();
        }
        else
        {
            // GameObject playerObject = GameObject.Find("Player");
            // if(playerObject)
            //     Destroy(playerObject);
            gMInstance.restartNumber++;
            //From here we are sure that the scene has correctly reloaded 
            gMInstance.reloadObject();
            Destroy(gameObject);
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        ennemiesLeft = Pool.Instance.EnemyList.Count;
        timerText.text = ennemiesLeft.ToString();

        if (ennemiesLeft == 0 && Ship.PlayerShip != null && GameObjectStateManager.Instance.FrameNumber < 5)
        {
            win();
        }
    }

    public void win()
    {
        SceneManager.LoadScene("Resources/Scenes/WinScene");
    }

    public void reloadScene()
    {
        Pool.Instance.recycleAllObjects();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void reloadObject()
    {
        GameObject textMeshPro = GameObject.Find("Time Counter");
        if (textMeshPro)
            timerText = textMeshPro.GetComponent<TextMeshProUGUI>();
        
        if(timerText == null)
            Debug.LogError("Time Counter TextMeshPro or it's UGUI component could not be found");
        
        ennemiesLeft = timeLeft;

        spawnPlayer();

        restartNumber++;
    }

    private void spawnPlayer()
    {
        Vector3 pos = new Vector3();
//        if(player != null) 
//            Destroy(player);
        bool canSpawnunit = false;
        int loopCount = 0;

        while (!canSpawnunit)
        {
            pos = UnityEngine.Random.insideUnitSphere * playerSpawnSphereRadius + transform.position;
            bool isPositionTooClose = false;
            foreach (var formerPosition in occupiedPositions)
            {
                if (Vector3.Distance(formerPosition, pos) < minDistanceBetweenShips)
                {
                    isPositionTooClose = true;
                }
            }

            if (!isPositionTooClose)
                canSpawnunit = true;
            else
            {
                loopCount++;
                if (loopCount >= PLACEMENT_LOOP_MAX)
                {
                    playerSpawnSphereRadius += 5;
                    loopCount = 0;
                }
            }
        }

        GameObject playerObj = GameObject.Find("Player");

        // if (!playerObj || gMInstance.restartNumber > 0)
        // {
        //     player = Pool.Instance.get(PoolableTypes.Player);
        // }
        // else
        // {
        //     player = playerObj;
        // }
        
        
        player = playerObj.transform.Find("PlayerShip").gameObject;
        player.transform.position = pos;
        GameObjectStateManager.Instance.initializeDynamicObjects();

        Ship playerShip = player.GetComponent<Ship>();
        if(playerShip)
            playerShip.creationSetup();
        
        occupiedPositions.Add(pos);
    }

    public GameObject Player => player;
    public static GameManager Instance => gMInstance;
}
