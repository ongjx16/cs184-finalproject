using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Control : MonoBehaviour
{
    public float move_speed = 10.0f;
    public float pan_sensitivity = 5.0f;
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
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Camera control is activated!");
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        controlCamera = Camera.main;
        if(controlCamera!=null){
            controlCamera.fieldOfView = default_fov;
        }
        else{
            Debug.Log("Camera not chosen!");
        }
    }

    // Update is called once per frame
    void Update()
    {
        //Left and right movement of camera depending on where the camera is facing using WASD or arrow keys
        transform.position += verticalMovement();
        transform.position += horizontalMovement();

        //Allows for desecent in world y axis regardless of where the camera is facing
        if(Input.GetKey(KeyCode.LeftShift))
        {
            transform.position -= worldYMovement();
        }

        //Doubletap spacebar to fly vertically upward
        if(Input.GetKeyDown(KeyCode.Space)){
            if(!isWaitingForSecondTap){
                timeOfLastTap = Time.time;
                isWaitingForSecondTap = true;
            }
            else if(isWaitingForSecondTap && Time.time - timeOfLastTap<=doubleTapThreshold){
                fly = true;
            }
            else{
                timeOfLastTap = Time.time;
            }
        }
        if (isWaitingForSecondTap && (Time.time - timeOfLastTap > doubleTapThreshold))
        {
            isWaitingForSecondTap = false;
        }

        if(Input.GetKeyUp(KeyCode.Space)){
            fly = false;
        }
        if(Input.GetKey(KeyCode.Space) && fly){
           transform.position += worldYMovement();
        }

        // Camera Panning if left mousebutton is clicked down
        if(Input.GetMouseButton(0)){
            transform.eulerAngles += panCamera();
        }

        //Change FOV using scroll wheel
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if(scroll>0f){
            controlCamera.fieldOfView = Mathf.Max(controlCamera.fieldOfView - zoom_speed * Time.deltaTime, min_fov);
        }
        else if(scroll<0f){
            controlCamera.fieldOfView = Mathf.Min(controlCamera.fieldOfView + zoom_speed * Time.deltaTime, max_fov);
        }

        //Change Roll of camera
        if(Input.GetKey(KeyCode.Q)){
            transform.eulerAngles += new Vector3(0,0,-0.05f*pan_sensitivity);
        }
        if(Input.GetKey(KeyCode.E)){
            transform.eulerAngles += new Vector3(0,0,0.05f*pan_sensitivity);
        }
        if(Input.GetKeyDown(KeyCode.F)){
            transform.eulerAngles = new Vector3(transform.eulerAngles.x,transform.eulerAngles.y,0);
        }

        //Reset to original camera position
        if(Input.GetKeyDown(KeyCode.R)){
            transform.position = originalPosition;
            transform.rotation = originalRotation;
        }
    }
    Vector3 verticalMovement(){
        return transform.forward * Input.GetAxis("Vertical") * move_speed * Time.deltaTime;
    }
    Vector3 horizontalMovement(){
        return transform.right * Input.GetAxis("Horizontal") * move_speed * Time.deltaTime;
    }
    Vector3 worldYMovement(){
        return Vector3.up * move_speed * Time.deltaTime;
    }
    Vector3 panCamera(){
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        return new Vector3(-mouseY * pan_sensitivity, mouseX * pan_sensitivity, 0);
    }
}
