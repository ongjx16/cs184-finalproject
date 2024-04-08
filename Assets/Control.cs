using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Control : MonoBehaviour
{
    public float speed = 10.0f;
    public float sensitivity = 5.0f;
    private bool isWaitingForSecondTap = false; 
    private float timeOfLastTap = 0;
    private bool fly = false;
    public float doubleTapThreshold = 0.5f;
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Camera control is activated!");
    }

    // Update is called once per frame
    void Update()
    {
        //Left and right movement of camera depending on where the camera is facing using WASD or arrow keys
        transform.position += transform.forward * Input.GetAxis("Vertical") * speed * Time.deltaTime;
        transform.position += transform.right * Input.GetAxis("Horizontal") * speed * Time.deltaTime;

        //Allows for desecent in world y axis regardless of where the camera is facing
        if(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            transform.position -= Vector3.up * speed * Time.deltaTime;
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
           transform.position += Vector3.up * speed * Time.deltaTime;
        }


        // Camera Panning if left mousebutton is clicked down
        if(Input.GetMouseButton(0)){
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            transform.eulerAngles += new Vector3(-mouseY * sensitivity, mouseX * sensitivity, 0);
        }
    }
}
