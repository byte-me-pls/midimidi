using UnityEngine;

public class ObjectRotator : MonoBehaviour
{
    [Header("Dönüş Ayarları")]
    [Tooltip("Objenin hangi eksende döneceğini belirle (X, Y veya Z). 1 yazarsan o eksende döner.")]
    public Vector3 rotateAxis = new Vector3(0, 1, 0); // Varsayılan olarak Y ekseninde (kendi etrafında) döner.

    [Tooltip("Dönüş hızı")]
    public float rotationSpeed = 50f;

    void Update()
    {
        // Time.deltaTime: Her bilgisayarda aynı hızda dönmesini sağlar (kare hızından bağımsız).
        transform.Rotate(rotateAxis * rotationSpeed * Time.deltaTime);
    }
}