using UnityEngine;

public class CustomAnimation : MonoBehaviour
{
    public PolygonCollider2D[] animationColliders;
    public Sprite[] animationFrames;
    public float secondsPerFrame = 1;

    SpriteRenderer spriteRenderer;
    int maxFrameIndex = 0;
    int lastFrameIndex = 0;
    int currentFrameIndex = 0;

    void Start()
    {
        if (animationFrames.Length == 0)
            Error("There are no sprites attached to this script's \"Sprite\" array! (CustomAnimation will be disabled.)");
        else if (animationColliders.Length == 0)
            Error("There are no colliders added to this script's \"PolygonCollider2D\" array! (CustomAnimation will be disabled.)");
        else if (animationFrames.Length < animationColliders.Length)
            Error("There are more animation sprites than colliders! (CustomAnimation will be disabled.)");
        else if (animationFrames.Length > animationColliders.Length)
            Error("There are more colliders than animation sprites! (CustomAnimation will be disabled.)");
        else
        {
            if ((spriteRenderer = gameObject.GetComponent<SpriteRenderer>()) == null)
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

            maxFrameIndex = animationFrames.Length - 1;

            foreach (PolygonCollider2D collider in animationColliders)
                collider.enabled = false;

            animationColliders[0].enabled = true;
            spriteRenderer.sprite = animationFrames[0];

            InvokeRepeating("Animate", 0, secondsPerFrame);
        }
    }
    void Error(string message)
    {
        Debug.LogError("(ID: " + transform.GetInstanceID() + ") " + transform.name + ": " + message);
        enabled = false;
    }

    void Animate()
    {
        lastFrameIndex = currentFrameIndex;
        currentFrameIndex++;
        if (currentFrameIndex > maxFrameIndex)
            currentFrameIndex = 0;

        spriteRenderer.sprite = animationFrames[currentFrameIndex];
        animationColliders[lastFrameIndex].enabled = false;
        animationColliders[currentFrameIndex].enabled = true;
    }
}