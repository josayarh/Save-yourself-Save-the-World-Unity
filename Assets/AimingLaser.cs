using System.Collections;
using System.Collections.Generic;
using FLFlight;
using UnityEngine;

public class AimingLaser : MonoBehaviour
{
    [SerializeField] private Transform aimingFrom;
    [SerializeField] private LineRenderer laserLineRenderer;
    [SerializeField] private float laserWidth = 0.1f;
    [SerializeField] private float laserLength = 1000f;
    
    // Start is called before the first frame update
    void Start()
    {
        Vector3[] initLaserPositions = new Vector3[ 2 ] { Vector3.zero, Vector3.zero };
        laserLineRenderer.SetPositions( initLaserPositions );
        laserLineRenderer.SetWidth( laserWidth, laserWidth );
    }

    // Update is called once per frame
    void Update()
    {
        if (Ship.PlayerShip != null)
        {
            RaycastHit hit;
            Ray ray = new Ray(aimingFrom.position, Ship.PlayerShip.transform.forward);
            Vector3 endPosition = aimingFrom.position + ( laserLength * Ship.PlayerShip.transform.forward );
            
            if( Physics.Raycast( ray, out hit, laserLength ) ) {
                endPosition = hit.point;
            }
 
            laserLineRenderer.SetPosition( 0, aimingFrom.position );
            laserLineRenderer.SetPosition( 1, endPosition );
            laserLineRenderer.enabled = true;
        }
    }
}
