using UnityEngine;

public class SpriteChanger : MonoBehaviour
{
    [SerializeField] private Sprite[] sprites;
    private Sprite currentSprite;
    private static readonly Sprite[] stoneConnections = new Sprite[16];

    public void ChangeSprite(int spriteNum)
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("SpriteChanger requires a SpriteRenderer on the same GameObject.", this);
            return;
        }
        bool stoneBridge = GetComponent<GridPlaceable>()?.IsStoneBridge == true;
        // Stone infrastructure belongs behind buildings, including when an
        // existing wooden bridge is upgraded in place.
        if (stoneBridge) spriteRenderer.sortingOrder = -1;
        currentSprite = stoneBridge
            ? StoneConnectionSprite(spriteNum) : sprites[spriteNum];
        spriteRenderer.sprite = currentSprite;
    }

    public static Sprite StoneConnectionSprite(int mask)
    {
        mask &= 15;
        if (stoneConnections[mask] != null) return stoneConnections[mask];
        const int size = 32;
        var paving = Resources.Load<Texture2D>("StoneBridgePaving");
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "StoneBridgeTile_" + mask,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            Color32 color = new Color32(0, 0, 0, 0);
            if (StoneDeckContains(x, y, mask))
            {
                var stone = paving != null ? (Color32)paving.GetPixel(x * paving.width / size, y * paving.height / size)
                    : new Color32(145, 140, 133, 255);
                // Restrained paving texture: mortar must not compete with the rails.
                color = new Color32((byte)((stone.r + 145 * 2) / 3),
                    (byte)((stone.g + 141 * 2) / 3), (byte)((stone.b + 133 * 2) / 3), 255);
                bool edge = !StoneDeckContains(x - 1, y, mask) || !StoneDeckContains(x + 1, y, mask) ||
                    !StoneDeckContains(x, y - 1, mask) || !StoneDeckContains(x, y + 1, mask);
                if (edge) color = new Color32(112, 110, 104, 255);
            }
            else
            {
                // Thin raised handrails OUTSIDE the walking surface. The open
                // pixel between posts and rail reads as a balustrade, not a frame
                // painted on the deck. Periodic posts align across tile boundaries.
                for (int distance = 1; distance <= 3; distance++)
                {
                    bool vertical = StoneDeckContains(x - distance, y, mask) || StoneDeckContains(x + distance, y, mask);
                    bool horizontal = StoneDeckContains(x, y - distance, mask) || StoneDeckContains(x, y + distance, mask);
                    if (!vertical && !horizontal) continue;
                    int along = vertical ? y : x;
                    bool post = along % 8 == 3 || along % 8 == 4;
                    if (distance == 3)
                        color = post ? new Color32(200, 195, 182, 255) : new Color32(174, 169, 157, 255);
                    else if (distance == 2)
                        color = post ? new Color32(163, 158, 146, 255) : new Color32(95, 93, 88, 255);
                    else if (post)
                        color = new Color32(117, 114, 106, 255);
                    break;
                }
                bool nearSide = StoneDeckContains(x - 3, y, mask) || StoneDeckContains(x + 3, y, mask) ||
                    StoneDeckContains(x, y - 3, mask) || StoneDeckContains(x, y + 3, mask);
                bool nearCorner = StoneDeckContains(x - 3, y - 3, mask) || StoneDeckContains(x + 3, y - 3, mask) ||
                    StoneDeckContains(x - 3, y + 3, mask) || StoneDeckContains(x + 3, y + 3, mask);
                if (!nearSide && nearCorner)
                    color = new Color32(185, 179, 165, 255);
            }
            pixels[y * size + x] = color;
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size,
            0, SpriteMeshType.FullRect);
        sprite.name = "StoneBridge_Connections_" + mask;
        stoneConnections[mask] = sprite;
        return sprite;
    }

    // Continue beyond connected edges when testing the parapet boundary. This
    // prevents end caps or seams between adjacent tiles, including at junctions.
    private static bool StoneDeckContains(int x, int y, int mask)
    {
        bool across = x >= 6 && x < 26;
        bool along = y >= 6 && y < 26;
        return (across && along) || (along && ((x >= 26 && (mask & 1) != 0) || (x < 6 && (mask & 2) != 0))) ||
            (across && ((y >= 26 && (mask & 4) != 0) || (y < 6 && (mask & 8) != 0)));
    }

    public void UseSpritesFrom(SpriteChanger source)
    {
        sprites = source.sprites;
    }

}
