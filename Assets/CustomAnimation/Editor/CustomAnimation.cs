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
                PolygonCollider2D collider = myTarget.gameObject.AddComponent<PolygonCollider2D>();
                List<Vector2> points = new List<Vector2>();

                int xOffset = Mathf.RoundToInt(myTarget.animationFrames[i].rect.xMin);
                int yOffset = Mathf.RoundToInt(myTarget.animationFrames[i].rect.yMin);

                for (int y = -1; y < myTarget.animationFrames[i].rect.height + 1; y++)
                {
                    for (int x = -1; x < myTarget.animationFrames[i].rect.width + 1; x++)
                    {
                        int coloredPixels = 0;

                        if (myTarget.animationFrames[i].texture.GetPixel(xOffset + x, yOffset + y).a != 0 ||
                            myTarget.animationFrames[i].texture.GetPixel(xOffset + x + 1, yOffset + y).a != 0 ||
                            myTarget.animationFrames[i].texture.GetPixel(xOffset + x, yOffset + y + 1).a != 0 ||
                            myTarget.animationFrames[i].texture.GetPixel(xOffset + x + 1, yOffset + y + 1).a != 0)
                            coloredPixels++;

                        if (coloredPixels == 1 || coloredPixels == 3)
                            points.Add(new Vector2(x - myTarget.animationFrames[i].pivot.x + 1, y - myTarget.animationFrames[i].pivot.y + 1) / myTarget.animationFrames[i].pixelsPerUnit);
                    }
                }


                List<Vector2> newPoints = new List<Vector2>();
                newPoints.Add(points[0]);
                points.RemoveAt(0);
                points.Add(newPoints[0]);

                int lastCount = 0;

                while (points.Count > 0)
                {
                    if (lastCount == points.Count)
                    {
                        Debug.Log("Error.");
                        throw new System.Exception("Infinite Loop at " + newPoints[newPoints.Count - 1] + ". Points left: " + points.Count);
                    }
                    else
                    {
                        lastCount = points.Count;
                    }

                    points.Sort((v1, v2) => Vector2.Distance(v1, newPoints[newPoints.Count - 1]).CompareTo(Vector2.Distance(v2, newPoints[newPoints.Count - 1])));

                    for (int startIndex = 0; startIndex < points.Count; startIndex++)
                    {
                        if (newPoints[newPoints.Count - 1].x == points[startIndex].x ||
                            newPoints[newPoints.Count - 1].y == points[startIndex].y)
                        {

                            Debug.Log(myTarget.animationFrames[i].texture.GetPixel(xOffset + Mathf.RoundToInt(points[startIndex].x) + 1, yOffset + Mathf.RoundToInt(points[startIndex].y)).a != 0);
                            newPoints.Add(points[startIndex]);
                            points.RemoveAt(startIndex);
                        }
                    }
                }

                collider.points = newPoints.ToArray();
                myTarget.animationColliders[i] = collider;
            }
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(myTarget);
        }

        serializedObject.Update();
    }
}