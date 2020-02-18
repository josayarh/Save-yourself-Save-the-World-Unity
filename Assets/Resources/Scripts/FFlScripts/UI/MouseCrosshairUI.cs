using UnityEngine;
using UnityEngine.UI;

namespace FLFlight.UI
{
    /// <summary>
    /// Updates the position of this GameObject to reflect the position of the mouse
    /// when the player ship is using mouse input. Otherwise, it just hides it.
    /// </summary>
    public class MouseCrosshairUI : MonoBehaviour
    {
        private Image crosshair;
        [SerializeField] private float speedX = 2;
        [SerializeField] private float speedY = 2;

        private void Awake()
        {
            crosshair = GetComponent<Image>();
        }

        private void Update()
        {
            if (crosshair != null && (Ship.PlayerShip != null || GameManager.Instance.Player != null))
            {
                float xOffset = UnityEngine.Input.GetAxis("Mouse X");
                float yOffset = UnityEngine.Input.GetAxis("Mouse Y");
                
                transform.position = new Vector3(transform.position.x + xOffset * speedX * Time.deltaTime,
                    transform.position.y + yOffset * speedY * Time.deltaTime, transform.position.z);
                
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }
}
