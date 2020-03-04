// ================================================================================================================================
// File:        ElectrodeAI.cs
// Description:	Electrodes are stationary 
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

public class ElectrodeAI : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        //Kill the player if they collide with the Electrode
        if (collision.transform.CompareTag("Player"))
            Debug.Log("Kill Player!");
    }
}
