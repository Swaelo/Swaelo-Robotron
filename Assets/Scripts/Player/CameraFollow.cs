// ================================================================================================================================
// File:        CameraFollow.cs
// Description:	Has the camera follow the player around the game, if the player is around the 40$
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Tooltip("The target that this camera will follow around the game world.")]
    public Transform FollowTarget;

    [Tooltip("How quickly the camera catches up to its target.")]
    public float FollowSpeed = 6f;

    [Tooltip("Percentage of screen width/height considered the dead zone (0-1)")]
    [Range(0f, 0.9f)]
    public float DeadZonePercent = 0.4f;

    private Camera Cam; //Camera component on this game object
    private Vector3 Velocity; //Cameras movement velocity as it moves to follow the target

    //Assign references
    void Awake() { Cam = GetComponent<Camera>(); }

    void LateUpdate()
    {
        //Exit out if we have no target to follow
        if(!FollowTarget) return;

        //Convert target world position into viewport space (0-1)
        Vector3 ViewPortPos = Cam.WorldToViewportPoint(FollowTarget.position);

        //Dead zone bounds
        float HalfDead = DeadZonePercent * 0.5f;
        float MinX = 0.5f - HalfDead;
        float MaxX = 0.5f + HalfDead;
        float MinY = 0.5f - HalfDead;
        float MaxY = 0.5f + HalfDead;

        //Compute how far outside the allowable viewing area our target has reached
        Vector3 Offset = Vector3.zero;

        //Adjust offset based on targets distance outside of x bounds
        if(ViewPortPos.x < MinX)
            Offset.x = ViewPortPos.x - MinX;
        else if (ViewPortPos.x > MaxX)
            Offset.x = ViewPortPos.x - MaxX;
        //Then the same for y bounds
        if(ViewPortPos.y < MinY)
            Offset.y = ViewPortPos.y - MinY;
        else if(ViewPortPos.y > MaxY)
            Offset.y = ViewPortPos.y - MaxY;

        //If the target is still in the middle of the screen we don't need to do anything
        if(Offset == Vector3.zero) return; 

        //Figure out the ideal location for the camera to be located to keep the target in the middle of the screen
        Vector3 WorldOffset = Cam.ViewportToWorldPoint(ViewPortPos - Offset) - Cam.ViewportToWorldPoint(ViewPortPos);
        Vector3 TargetPos = transform.position - WorldOffset;

        //Smoothly move towards that target location
        transform.position = Vector3.SmoothDamp(
            transform.position,
            TargetPos,
            ref Velocity,
            1f / FollowSpeed);
    }
}