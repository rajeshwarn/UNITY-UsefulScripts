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

        EditorGUILayout.PropertyField(serializedObject.FindProperty("animationFrames"), true);
        if (debugMode) EditorGUILayout.PropertyField(serializedObject.FindProperty("animationColliders"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("secondsPerFrame"), true);

        if (GUILayout.Button("Generate/Update PolygonCollider2D for each frame."))
        {
            foreach (PolygonCollider2D pc in myTarget.gameObject.GetComponents<PolygonCollider2D>())
                Object.DestroyImmediate(pc);

            for(int i = 0; i < myTarget.animationFrames.Length; i++)
            {
                

                PolygonCollider2D newCollider = myTarget.gameObject.AddComponent<PolygonCollider2D>();
                List<Vector2> newPoints = new List<Vector2>();

                for (int j = 0; j < myTarget.animationFrames[i].texture.height - 1; j++)
                {
                    for (int k = 0; k < myTarget.animationFrames[i].texture.width - 1; k++)
                    {
                        Color totalColor = new Color();
                        foreach (Color color in myTarget.animationFrames[i].texture.GetPixels(j, k, 2, 2))
                            totalColor += color;

                        if (totalColor.a == 0)
                            newPoints.Add(new Vector2(j + 1, k + 1));
                    }
                }

                newCollider.points = newPoints.ToArray();
                myTarget.animationColliders[i] = newCollider;
            }
        }
    }
}