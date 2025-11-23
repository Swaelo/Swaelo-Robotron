using UnityEngine;

public class SpriteColorCycle : MonoBehaviour
{
    public float ColorSpeed = 1f; //How fast the colors cycle through

    private SpriteRenderer SR;
    private float Hue;

    void Awake()
    {
        SR = GetComponent<SpriteRenderer>();
        Hue = 0f;
    }

    void Update()
    {
        //Increment hue (wraps automatically with Mathf.Repeat)
        Hue = Mathf.Repeat(Hue + ColorSpeed * Time.deltaTime, 1f);

        //Convert hue to RGB
        Color RGB = Color.HSVToRGB(Hue, 1f, 1f);
        SR.color = RGB;
    }
}
