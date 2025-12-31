// ================================================================================================================================
// File:        CameraZoom.cs
// Description:	Allows the player to zoom the camera in and out with the mouse wheel
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

public class CameraZoom : MonoBehaviour
{
    //Speed the camera zooms out with the mouse wheel
    public float ZoomSpeed = 5f;

    //Orthographic projection limits
    public float MinOrtho = 2f;
    public float MaxOrtho = 20f;

    private Camera Cam;

    void Awake() { Cam = GetComponent<Camera>(); }

    void Update()
    {
        //Get input, exit out if none is detected
        float MouseWheelScroll = Input.GetAxis("Mouse ScrollWheel");
        if(MouseWheelScroll == 0f) return;

        //Adjust the orthographic projection to zoom the camera based on that mouse input
        Cam.orthographicSize -= MouseWheelScroll * ZoomSpeed * Time.deltaTime;
        Cam.orthographicSize = Mathf.Clamp(Cam.orthographicSize, MinOrtho, MaxOrtho);
    }
}