using UnityEngine;
using UnityEngine.UI;

namespace FLFlight.UI
{
    public class BoresightCrosshair : MonoBehaviour
    {
        public Transform ship;
        public Transform gunTipPosition;
        public float boresightDistance = 1000f;
        public Image crosshairimage;

        void Update()
        {
            if (ship != null)
            {
                RaycastHit hit;
                Ray ray = new Ray(gunTipPosition.position, ship.forward);
                Vector3 boresightPos = new Vector3();
                

                if (Physics.Raycast(ray, out hit, boresightDistance, LayerMask.GetMask("ReticuleRaycast")))
                {
                    crosshairimage.enabled = true;
                    boresightPos = hit.point;
                }
                else
                {
                    crosshairimage.enabled = false;
                } 
                    
                Vector3 screenPos = Camera.main.WorldToScreenPoint(boresightPos);
                screenPos.z = 0f;

                transform.position = screenPos;
            }
        }
    }
}
