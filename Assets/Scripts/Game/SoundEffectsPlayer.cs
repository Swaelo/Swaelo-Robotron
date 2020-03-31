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
        public string Name;
        public AudioClip[] SoundClips;
    }
    public Sound[] SoundEffects;

    private void Awake() { Instance = this; }

    //Takes the name of a sound effect, and plays it if it can be found
    public void PlaySound(string SoundName)
    {
        //Find the structure containing the required sound effect
        foreach (Sound Sound in SoundEffects)
        {
            if (Sound.Name == SoundName)
            {
                //Play one of this clips variations at random
                int Variations = Sound.SoundClips.Length;
                int Selection = Random.Range(1, Variations);
                SoundPlayer.PlayOneShot(Sound.SoundClips[Selection-1]);
            }
        }
    }
}
