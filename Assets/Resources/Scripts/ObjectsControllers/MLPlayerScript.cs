using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MLPlayerScript : MonoBehaviour
{
    private static MLPlayerScript playerScript;

    // Start is called before the first frame update
    void Start()
    {
        if(playerScript == null)
        {
            DontDestroyOnLoad(gameObject);
            playerScript = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
