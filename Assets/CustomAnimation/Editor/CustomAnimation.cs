using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

[CustomEditor(typeof(CustomAnimation))]
public class LevelScriptEditor : Editor
{
    bool debugMode = false;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        CustomAnimation myTarget = (CustomAnimation)target;

        debugMode = EditorGUILayout.Toggle("Debug Mode", debugMode);

        if (debugMode) EditorGUILayout.PropertyField(serializedObject.FindProperty("animationColliders"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("animationFrames"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("secondsPerFrame"), true);

        if (GUILayout.Button("Generate/Update PolygonCollider2D for each frame."))
        {
            foreach (PolygonCollider2D pc in myTarget.gameObject.GetComponents<PolygonCollider2D>())
                Object.DestroyImmediate(pc);

            myTarget.animationColliders = new PolygonCollider2D[myTarget.animationFrames.Length];

            for (int i = 0; i < myTarget.animationFrames.Length; i++)
            {
                PolygonCollider2D newCollider = myTarget.gameObject.AddComponent<PolygonCollider2D>();
                List<Vector2> newPoints = new List<Vector2>();

                int xOffset = Mathf.RoundToInt(myTarget.animationFrames[i].rect.xMin);
                int yOffset = Mathf.RoundToInt(myTarget.animationFrames[i].rect.yMin);

                for (int y = -1; y < myTarget.animationFrames[i].rect.height + 1; y++)
                {
                    for (int x = -1; x < myTarget.animationFrames[i].rect.width + 1; x++)
                    {
                        int coloredPixels = 0;

                        if (myTarget.animationFrames[i].texture.GetPixel(xOffset + x, yOffset + y).a != 0)
                            coloredPixels++;
                        if (myTarget.animationFrames[i].texture.GetPixel(xOffset + x + 1, yOffset + y).a != 0)
                            coloredPixels++;
                        if (myTarget.animationFrames[i].texture.GetPixel(xOffset + x, yOffset + y + 1).a != 0)
                            coloredPixels++;
                        if (myTarget.animationFrames[i].texture.GetPixel(xOffset + x + 1, yOffset + y + 1).a != 0)
                            coloredPixels++;

                        if (coloredPixels == 1 || coloredPixels == 3)
                            newPoints.Add(new Vector2(x - myTarget.animationFrames[i].pivot.x + 1, y - myTarget.animationFrames[i].pivot.y + 1) / myTarget.animationFrames[i].pixelsPerUnit);
                    }
                }

                newCollider.points = newPoints.ToArray();
                myTarget.animationColliders[i] = newCollider;
            }
        }

        if (GUI.changed)
        {
            if (GUI.changed)
            {
                EditorUtility.SetDirty(myTarget);
            }
        }

        serializedObject.Update();
    }
}