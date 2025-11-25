// ================================================================================================================================
// File:        LevelBorders.cs
// Description:	Allows the size of the level to be changed from the inspector which is then
// generated at run time and placed down
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

public class LevelBorders : MonoBehaviour
{
    [Header("Level Size")]
    public float LevelWidth = 15f;
    public float LevelHeight = 8f;

    [Header("Border Settings")]
    public float BorderThickness = 1f;

    private void Start()
    {
        GenerateBorders();
    }

    //Sets down all the level borders into place
    private void GenerateBorders()
    {
        //Calculate half extents for placement
        float halfWidth = LevelWidth / 2f;
        float halfHeight = LevelHeight / 2f;
        
        CreateBorder("TopBorder",
            new Vector2(0, -halfHeight),
            new Vector2(LevelWidth + BorderThickness, BorderThickness));

        CreateBorder("BottomBorder",
            new Vector2(0, halfHeight),
            new Vector2(LevelWidth + BorderThickness, BorderThickness));

        CreateBorder("LeftBorder",
            new Vector2(-halfWidth, 0),
            new Vector2(BorderThickness, LevelHeight + BorderThickness));

        CreateBorder("RightBorder",
            new Vector2(halfWidth, 0),
            new Vector2(BorderThickness, LevelHeight + BorderThickness));
    }

    private void CreateBorder(string BorderName, Vector2 BorderPos, Vector2 BorderSize)
    {
        //Create the new border and set its location
        GameObject NewBorder = new GameObject(name);
        NewBorder.transform.parent = transform;
        NewBorder.transform.localPosition = BorderPos;

        //Add the 2D box collider component
        BoxCollider2D BorderCollider = NewBorder.AddComponent<BoxCollider2D>();
        BorderCollider.size = BorderSize;

        //Add a sprite component which will make the border visible
        SpriteRenderer BorderRenderer = NewBorder.AddComponent<SpriteRenderer>();
        Texture2D SpriteTexture = new Texture2D(1, 1);

        //Generate a 1x1 white texture and apply that to the sprite
        SpriteTexture.SetPixel(0, 0, Color.white);
        SpriteTexture.Apply();
        BorderRenderer.sprite = Sprite.Create(SpriteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
        BorderRenderer.drawMode = SpriteDrawMode.Sliced;

        //Set the size
        BorderRenderer.size = BorderSize;

        //Add the seizure trigger
        SpriteColorCycle ColorCycle = NewBorder.AddComponent<SpriteColorCycle>();
        ColorCycle.ColorSpeed = .5f;
    }
}