using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float move_speed = 10.0f;
    public float pan_sensitivity = 5.0f;
    public float minHeight = 2.0f; // Minimum height above ground
    private bool isWaitingForSecondTap = false;
    private float timeOfLastTap = 0;
    private bool fly = false;
    public float doubleTapThreshold = 0.5f;
    public Camera controlCamera;
    public float default_fov = 60.0f;
    public float min_fov = 5.0f;
    public float max_fov = 100.0f;
    public float zoom_speed = 1000.0f;
    private Vector3 originalPosition;
    private Quaternion originalRotation;

    void Start()
    {
        Debug.Log("Camera control is activated!");
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        controlCamera = Camera.main;
        if (controlCamera != null)
        {
            controlCamera.fieldOfView = default_fov;
        }
        else
        {
            Debug.Log("Camera not chosen!");
        }
    }

    void Update()
    {
        Vector3 proposedPosition = transform.position + verticalMovement() + horizontalMovement();

        if (Input.GetKey(KeyCode.LeftShift))
        {
            proposedPosition -= worldYMovement();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            handleSpacePress();
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            fly = false;
        }
        if (Input.GetKey(KeyCode.Space) && fly)
        {
            proposedPosition += worldYMovement();
        }

        if (Input.GetMouseButton(0))
        {
            transform.eulerAngles += panCamera();
        }

        handleScroll();

        if (Input.GetKey(KeyCode.Q))
        {
            transform.eulerAngles += new Vector3(0, 0, -0.05f * pan_sensitivity);
        }
        if (Input.GetKey(KeyCode.E))
        {
            transform.eulerAngles += new Vector3(0, 0, 0.05f * pan_sensitivity);
        }
        if (Input.GetKeyDown(KeyCode.F))
        {
            transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, 0);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            resetCameraPosition();
        }

        // Enforce minHeight constraint
        if (proposedPosition.y < minHeight)
        {
            proposedPosition.y = minHeight;
        }

        transform.position = proposedPosition;
    }

    void handleSpacePress()
    {
        if (!isWaitingForSecondTap)
        {
            timeOfLastTap = Time.time;
            isWaitingForSecondTap = true;
        }
        else if (isWaitingForSecondTap && Time.time - timeOfLastTap <= doubleTapThreshold)
        {
            fly = true;
        }
        else
        {
            timeOfLastTap = Time.time;
        }
        if (isWaitingForSecondTap && (Time.time - timeOfLastTap > doubleTapThreshold))
        {
            isWaitingForSecondTap = false;
        }
    }

    void handleScroll()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f)
        {
            controlCamera.fieldOfView = Mathf.Max(controlCamera.fieldOfView - zoom_speed * Time.deltaTime, min_fov);
        }
        else if (scroll < 0f)
        {
            controlCamera.fieldOfView = Mathf.Min(controlCamera.fieldOfView + zoom_speed * Time.deltaTime, max_fov);
        }
    }

    void resetCameraPosition()
    {
        transform.position = originalPosition;
        transform.rotation = originalRotation;
    }

    Vector3 verticalMovement()
    {
        return transform.forward * Input.GetAxis("Vertical") * move_speed * Time.deltaTime;
    }
    Vector3 horizontalMovement()
    {
        return transform.right * Input.GetAxis("Horizontal") * move_speed * Time.deltaTime;
    }
    Vector3 worldYMovement()
    {
        return Vector3.up * move_speed * Time.deltaTime;
    }
    Vector3 panCamera()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        return new Vector3(-mouseY * pan_sensitivity, mouseX * pan_sensitivity, 0);
    }
}
