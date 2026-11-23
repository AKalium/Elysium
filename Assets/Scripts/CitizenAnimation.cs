
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class CitizenAnimation : MonoBehaviour
{
    [Header("Walking Sprites")]
    [SerializeField] private Sprite[] walkUp;
    [SerializeField] private Sprite[] walkDown;
    [SerializeField] private Sprite[] walkLeft;
    [SerializeField] private Sprite[] walkRight;

    [Header("Animation Settings")]
    [SerializeField] private float frameDuration = 0.2f;

    
    public void SetSprites(
    Sprite[] up,
    Sprite[] down,
    Sprite[] left,
    Sprite[] right)
{
    walkUp = up;
    walkDown = down;
    walkLeft = left;
    walkRight = right;

    currentAnimation = null;
    frameIndex = 0;
    timer = 0f;

    if (spriteRenderer == null)
        spriteRenderer = GetComponent<SpriteRenderer>();

    if (walkDown != null && walkDown.Length > 0)
        spriteRenderer.sprite = walkDown[0];
}
    private SpriteRenderer spriteRenderer;
    private Sprite[] currentAnimation;

    private float timer;
    private int frameIndex;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void SetMovement(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.001f)
        {
            timer = 0f;
            return;
        }

        Sprite[] nextAnimation;

        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            nextAnimation = direction.x > 0 ? walkRight : walkLeft;
        else
            nextAnimation = direction.y > 0 ? walkUp : walkDown;

        if (nextAnimation == null || nextAnimation.Length == 0)
            return;

        if (currentAnimation != nextAnimation)
        {
            currentAnimation = nextAnimation;
            frameIndex = 0;
            timer = 0f;
        }

        timer += Time.deltaTime;

        if (timer >= frameDuration)
        {
            timer = 0f;
            frameIndex = (frameIndex + 1) % currentAnimation.Length;
        }

        spriteRenderer.sprite = currentAnimation[frameIndex];
    }
}
