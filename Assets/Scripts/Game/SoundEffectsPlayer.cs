// ================================================================================================================================
// File:        SoundEffectsPlayer.cs
// Description:	Provides any easy way to play soundeffects from anywhere in the code
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

public class SoundEffectsPlayer : MonoBehaviour
{
    public static SoundEffectsPlayer Instance;  //Singleton instance
    public AudioSource SoundPlayer; //Used to play the sounds from
    [System.Serializable]   //List all the available sound effects that can be played
    public struct Sound
    {
        public string Name; //Name used to access this sound through the PlaySound function
        public AudioClip SoundClip;  //Set of clips used for this sound, adding multiple allows for having variants which are randomly selected from
    }
    public Sound[] SoundEffects;

    private void Awake() { Instance = this; }

    //Takes the name of a sound effect, and plays it if it can be found
    public void PlaySound(string SoundName, float VolumeScale = 1.0f)
    {
        //Make sure the provided volume scale isnt outside the proper value range
        VolumeScale = Mathf.Clamp(VolumeScale, 0f, 1f);
        VolumeScale *= 0.35f;

        //Loop through all the available sounds
        for(int i = 0; i < SoundEffects.Length; i++)
        {
            //Access each sound from the library as we iterate through the list
            Sound CurrentSound = SoundEffects[i];

            //Check if this is the sound were looking for
            if(CurrentSound.Name == SoundName)
            {
                //Exit out now that we found the target sound and tried to play it
                SoundPlayer.PlayOneShot(CurrentSound.SoundClip, VolumeScale);
                return;
            }
        }

        //Print an error message if the target sound effect couldnt be found
        Debug.Log("Couldnt find sound effect: " + SoundName);
    }
}
