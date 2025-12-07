using UnityEngine;
using UnityEngine.SceneManagement; // Sahne yönetimi için gerekli kütüphane

[RequireComponent(typeof(SpriteRenderer))] // Bu scripti ekleyince otomatik SpriteRenderer ister
[RequireComponent(typeof(BoxCollider))]    // Tıklama algılamak için Collider şarttır
public class SpriteSceneButton : MonoBehaviour
{
    [Header("Görsel Ayarları")]
    [Tooltip("Fare üzerindeyken görünecek sprite")]
    public Sprite hoverSprite;
    
    [Tooltip("Fare üzerinde değilken görünecek normal sprite")]
    public Sprite normalSprite;

    [Header("Sahne Ayarları")]
    [Tooltip("Tıklanınca açılacak sahnenin tam adı")]
    public string sceneToLoad;

    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Eğer normalSprite'ı inspector'dan atamadıysan, o anki resmi normal kabul et
        if (normalSprite == null)
        {
            normalSprite = spriteRenderer.sprite;
        }
    }

    // Fare üzerine gelince çalışır
    void OnMouseEnter()
    {
        if (hoverSprite != null)
        {
            spriteRenderer.sprite = hoverSprite;
        }
    }

    // Fare üzerinden gidince çalışır
    void OnMouseExit()
    {
        if (normalSprite != null)
        {
            spriteRenderer.sprite = normalSprite;
        }
    }

    // Tıklama anında çalışır
    void OnMouseDown()
    {
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.Log($"'{sceneToLoad}' sahnesi yükleniyor...");
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogError("Yüklenecek sahne adı boş! Lütfen Inspector'dan sahne adını yazın.");
        }
    }
}