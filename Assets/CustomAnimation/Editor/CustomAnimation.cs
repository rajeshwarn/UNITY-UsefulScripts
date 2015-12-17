using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

[CustomEditor(typeof(CustomAnimation))]
public class LevelScriptEditor : Editor
{
    bool debugMode = false;
    float minTransparency = 0.1f;
    public override void OnInspectorGUI()
    {
        // Read data from object to editor.
        serializedObject.Update();

        #region UI Items

        EditorGUILayout.LabelField("Animation", EditorStyles.boldLabel); // Label to show that animation settings begins below.
        
        // Add a button which, if enabled, shows the array of colliders.
        debugMode = EditorGUILayout.Toggle("Debug Mode", debugMode);
        if (debugMode) EditorGUILayout.PropertyField(serializedObject.FindProperty("animationColliders"), true);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("animationFrames"), true); // Array to put sprites in.
        EditorGUILayout.PropertyField(serializedObject.FindProperty("secondsPerFrame"), true); // Field to set how long each frame will be displayed while animating.

        EditorGUILayout.LabelField("Collider Generation", EditorStyles.boldLabel); // Label to show that collider generation settings begins below.
        minTransparency = EditorGUILayout.Slider("Minimum Transparency", minTransparency, 0, 1); // Slider to set how low transparency has to be before counting as transparent (0 - 1).

        if (GUILayout.Button("Generate/Update PolygonCollider2D for each frame.")) // Button to generate colliders.
        {
            // And if it's pressed, well, generate the colliders.
            generateColliders((CustomAnimation)target);
        }
        #endregion

        // Save data from editor to object.
        serializedObject.ApplyModifiedProperties();
    }

    void generateColliders(CustomAnimation customAnimation)
    {
        // Destroy all already existing colliders from the object to prevent duplicates.
        foreach (PolygonCollider2D pc in customAnimation.gameObject.GetComponents<PolygonCollider2D>())
            DestroyImmediate(pc);

        // Create a new array and replace the current array of colliders, rendering it empty and at the right size. 
        customAnimation.animationColliders = new PolygonCollider2D[customAnimation.animationFrames.Length];

        // Generate a collider for each frame and add it to the array of colliders.
        for (int i = 0; i < customAnimation.animationFrames.Length; i++)
        {
            // Create a new collider.
            PolygonCollider2D collider = customAnimation.gameObject.AddComponent<PolygonCollider2D>();

            // Create a list of all corners (corners). This will be used to create a collider around the object.
            List<Vector2> corners = new List<Vector2>();

            // Find the borders of the current sprite and assign them to offsets. (Else if the sprite is on a spritesheet, the entire spritesheet would be read)
            int xOffset = Mathf.RoundToInt(customAnimation.animationFrames[i].rect.xMin);
            int yOffset = Mathf.RoundToInt(customAnimation.animationFrames[i].rect.yMin);

            // Find all the corners by searching horizontally on each row.
            for (int y = -1; y < customAnimation.animationFrames[i].rect.height + 1; y++)
            {
                for (int x = -1; x < customAnimation.animationFrames[i].rect.width + 1; x++)
                {
                    // Create a variable that will count the amount of colored pixels surrounding a point.
                    int coloredPixels = 0;

                    // Check if the pixels surrounding a point is colored and if they are, increase the counter. 
                    if (customAnimation.animationFrames[i].texture.GetPixel(xOffset + x, yOffset + y).a != 0) coloredPixels++;
                    if (customAnimation.animationFrames[i].texture.GetPixel(xOffset + x + 1, yOffset + y).a != 0) coloredPixels++;
                    if (customAnimation.animationFrames[i].texture.GetPixel(xOffset + x, yOffset + y + 1).a != 0) coloredPixels++;
                    if (customAnimation.animationFrames[i].texture.GetPixel(xOffset + x + 1, yOffset + y + 1).a != 0) coloredPixels++;

                    // If the number of colored pixels surrounding the point is 1 or 3, then it's a corner.
                    if (coloredPixels == 1 || coloredPixels == 3)
                        corners.Add(new Vector2(x - customAnimation.animationFrames[i].pivot.x + 1, y - customAnimation.animationFrames[i].pivot.y + 1) / customAnimation.animationFrames[i].pixelsPerUnit);
                }
            }

            // Create a new vector that will be holding all corners of the sprite.
            List<Vector2> newCorners = new List<Vector2>();

            // Add the first corner to the list of new corners, making it the start point.
            newCorners.Add(corners[0]);

            // Remove it from the old list and re-add it at the end, making it the final corner.
            corners.RemoveAt(0);
            //corners.Add(newCorners[0]);

            // A variable used to check for infinite recursion. If the next while-loop loops without finding a new valid corner it will continue to do so forever, and that will freeze the editor too.
            int lastCount = 0;

            // While there are corners who are not currently a part of the collider, find the next one.
            while (corners.Count > 0)
            {
                // Protect against infinite recursion.
                if (lastCount == corners.Count)
                {
                    collider.points = newCorners.ToArray();
                    customAnimation.animationColliders[i] = collider;
                    throw new System.Exception("Infinite loop at " + newCorners[newCorners.Count - 1] + ". Corners left: " + corners.Count);
                }
                else
                    lastCount = corners.Count;

                // Sort the corners by distance to the last corner.
                corners.Sort((v1, v2) => Vector2.Distance(v1, newCorners[newCorners.Count - 1]).CompareTo(Vector2.Distance(v2, newCorners[newCorners.Count - 1])));

                // From the first (closest) corner and forward, check if it is a valid next move. If it is, make it the next corner and stop searching.
                for (int startIndex = 0; startIndex < corners.Count; startIndex++)
                {
                    bool isValid = isValidMove(newCorners[newCorners.Count - 1], corners[startIndex], customAnimation.animationFrames[i]);
                    
                    if (((newCorners[newCorners.Count - 1].x == corners[startIndex].x) || (newCorners[newCorners.Count - 1].y == corners[startIndex].y)) && isValid)
                    {
                        Debug.Log("Moved to: " + corners[startIndex]);

                        newCorners.Add(corners[startIndex]);
                        corners.RemoveAt(startIndex);
                        break;
                    }
                }
            }

            // Update the new collider.
            collider.points = newCorners.ToArray();
            customAnimation.animationColliders[i] = collider;
        }
    }
    bool isValidMove(Vector2 fromPosition, Vector2 toPosition, Sprite sprite)
    {
        // Save the offsets of the sprite relative to the imported image. This makes no difference if the sprite is not on a spritesheet.
        int xOffset = Mathf.RoundToInt(sprite.rect.xMin);
        int yOffset = Mathf.RoundToInt(sprite.rect.yMin);
        int width = Mathf.RoundToInt(sprite.rect.width);
        int height = Mathf.RoundToInt(sprite.rect.height);

        // Save the length between the two corners that are being checked.
        int movementLength = Mathf.RoundToInt(Vector2.Distance(fromPosition, toPosition));

        // Save the corners into ints, making them easier to refer to.
        int fromX = Mathf.RoundToInt(fromPosition.x);
        int fromY = Mathf.RoundToInt(fromPosition.y);
        int toX = Mathf.RoundToInt(toPosition.x);
        int toY = Mathf.RoundToInt(toPosition.y);

        // Check which way the next corner is, and if the route to it is valid. If it is, return sucess. If it isn't, return disappointment.
        for (int steps = 0; steps < movementLength; steps++)
        {
            if (fromPosition.y == toPosition.y) // Horizontal Movement
            {
                if (fromPosition.x < toPosition.x) // Right
                {
                    if ((sprite.texture.GetPixel(xOffset + fromX + steps, yOffset + fromY - 1).a < minTransparency) != (sprite.texture.GetPixel(xOffset + fromX + steps, yOffset + fromY).a < minTransparency))
                        return true;
                }
                else if (fromPosition.x > toPosition.x) // Left
                {
                    if ((sprite.texture.GetPixel(xOffset + fromX - steps - 1, yOffset + fromY - 1).a < minTransparency) != (sprite.texture.GetPixel(xOffset + fromX + steps - 1, yOffset + fromY).a < minTransparency))
                        return true;
                }
            }
            else if (fromPosition.x == toPosition.x) //Vertical Movement
            {
                if (fromPosition.y < toPosition.y) // Up
                {
                    if ((sprite.texture.GetPixel(xOffset + fromX - 1, yOffset + fromY + steps).a < minTransparency) != (sprite.texture.GetPixel(xOffset + fromX, yOffset + fromY + steps).a < minTransparency))
                        return true;
                }
                else if (fromPosition.y > toPosition.y) // Down
                {
                    if ((sprite.texture.GetPixel(xOffset + fromX - 1, yOffset + fromY + steps - 2).a < minTransparency) == (sprite.texture.GetPixel(xOffset + fromX, yOffset + fromY + steps - 2).a < minTransparency))
                        return true;
                }
            }
        }

        // The corner has not been identified as a valid next corner.
        Debug.Log("Failed to move to: " + toPosition);
        return false;
    }
}