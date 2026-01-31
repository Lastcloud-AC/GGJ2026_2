using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QteSpriteDisplay : MonoBehaviour
{
    public Sprite wSprite;
    public Sprite aSprite;
    public Sprite sSprite;
    public Sprite dSprite;
    public Image imagePrefab;
    public float spacingPixels = 0f;
    public float paddingPixels = 12f;
    public Vector2 centerOffsetPixels = Vector2.zero;
    public Vector2 referenceResolution = new Vector2(1920f, 1080f);
    public bool useReferenceResolution = true;

    private readonly List<Image> images = new List<Image>();

    public void ShowSequence(IReadOnlyList<QteController.QteKey> sequence)
    {
        Clear();

        if (sequence == null || sequence.Count == 0 || imagePrefab == null)
        {
            return;
        }

        int count = sequence.Count;
        for (int i = 0; i < count; i++)
        {
            Image image = Instantiate(imagePrefab, transform);
            image.sprite = SpriteForKey(sequence[i]);
            image.enabled = true;
            images.Add(image);
        }

        LayoutCentered(count);
    }

    public void HideIndex(int index)
    {
        if (index < 0 || index >= images.Count)
        {
            return;
        }

        if (images[index] != null)
        {
            images[index].enabled = false;
        }
    }

    public void Clear()
    {
        for (int i = 0; i < images.Count; i++)
        {
            if (images[i] != null)
            {
                Destroy(images[i].gameObject);
            }
        }

        images.Clear();
    }

    private void LayoutCentered(int count)
    {
        Vector2 scale = Vector2.one;
        RectTransform container = transform as RectTransform;
        if (useReferenceResolution && container != null)
        {
            Vector2 size = container.rect.size;
            if (referenceResolution.x > 0f && referenceResolution.y > 0f)
            {
                scale = new Vector2(size.x / referenceResolution.x, size.y / referenceResolution.y);
            }
        }

        float spacing = GetSpacingPixels();
        float offset = (count - 1) * 0.5f * spacing;
        Vector2 centerOffset = new Vector2(centerOffsetPixels.x * scale.x, centerOffsetPixels.y * scale.y);
        for (int i = 0; i < images.Count; i++)
        {
            RectTransform rt = images[i].rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2((i * spacing - offset) * scale.x, 0f) + centerOffset;
        }
    }

    private float GetSpacingPixels()
    {
        if (spacingPixels > 0f)
        {
            return spacingPixels;
        }

        if (imagePrefab != null)
        {
            RectTransform rt = imagePrefab.rectTransform;
            float width = rt.rect.width;
            if (width > 0f)
            {
                return width + paddingPixels;
            }

            if (imagePrefab.sprite != null)
            {
                return imagePrefab.sprite.rect.width + paddingPixels;
            }
        }

        return 100f + paddingPixels;
    }

    private Sprite SpriteForKey(QteController.QteKey key)
    {
        switch (key)
        {
            case QteController.QteKey.W:
                return wSprite;
            case QteController.QteKey.A:
                return aSprite;
            case QteController.QteKey.S:
                return sSprite;
            default:
                return dSprite;
        }
    }
}
