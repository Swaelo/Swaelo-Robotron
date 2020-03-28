// ================================================================================================================================
// File:        ProgSpriteTrailFollower.cs
// Description:	Controls the movement of the Progs trail sprites so they follow behind it
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

public class ProgSpriteTrailFollower : MonoBehaviour
{
    public GameObject FollowTarget; //The object this sprite is meant to follow around
    private float IdealDistance = 0.225f;    //Amount of distance we want to keep between the target
    private float MaxDistance = 0.375f; //Maximum distance we want to keep between the sprite and its follow target

    private void Update()
    {
        //Move toward the follow target if it gets too far away
        float TargetDistance = Vector3.Distance(transform.position, FollowTarget.transform.position);
        if(TargetDistance >= MaxDistance)
        {
            //Move straight towards the follow target until were the ideal distance away
            Vector3 TargetDirection = Vector3.Normalize(FollowTarget.transform.position - transform.position);
            Vector3 IdealPosition = FollowTarget.transform.position - TargetDirection * IdealDistance;
            transform.position = IdealPosition;
        }
    }
}
