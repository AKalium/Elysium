using UnityEngine;

public class SpriteChanger : MonoBehaviour
{
    [SerializeField] private Sprite[] sprites;
    private Sprite currentSprite;

    public void ChangeSprite(int spriteNum)
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("SpriteChanger requires a SpriteRenderer on the same GameObject.", this);
            return;
        }
        currentSprite = sprites[spriteNum];
        spriteRenderer.sprite = currentSprite;
    }
}
