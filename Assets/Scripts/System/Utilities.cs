// ================================================================================================================================
// File:        Utils.cs
// Description:	Contains various useful functions
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

namespace Swaelo.Robotron
{
    public static class Utils
    {
        //Returns the world location where the mouse cursor is current hovering over
        public static Vector3 GetMouseWorldPos()
        {
            //Break out if we cant find the camera
            if(Camera.main == null)
            {
                Debug.Log("cannot find camera");
                return Vector3.zero;
            }

            return Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }
    }
}