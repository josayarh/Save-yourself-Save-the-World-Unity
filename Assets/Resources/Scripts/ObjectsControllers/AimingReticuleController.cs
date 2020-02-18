using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AimingReticuleController : MonoBehaviour
{
    [SerializeField] private float maxSpeed;
    [SerializeField] private float rotateSpeedH;
    [SerializeField] private float rotateSpeedV;
    
    private CursorLockMode previousLockState;
    bool wasCursorVisible;
    
    private void OnEnable()
    {
        previousLockState = Cursor.lockState;
        wasCursorVisible = Cursor.visible;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector2 axisMotion = new Vector2(Input.GetAxis("Mouse X") * rotateSpeedH,
            Input.GetAxis("Mouse Y") * rotateSpeedV);

        if (axisMotion != Vector2.zero)
        {
            transform.position = new Vector3(
                transform.position.x + axisMotion.x, 
                transform.position.y + axisMotion.y,
                transform.position.z);
        }
    }
}
