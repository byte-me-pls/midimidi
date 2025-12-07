using UnityEngine;

public class BubbleController : MonoBehaviour
{
    private Transform target;
    private float shakeAmount;
    private float shakeSpeed;
    private Vector3 offset; // Hesaplanan son ofset

    // YENİ: extraHeight parametresi eklendi
    public void Initialize(Transform targetTransform, float shake, float speed, float extraHeight)
    {
        target = targetTransform;
        shakeAmount = shake;
        shakeSpeed = speed;

        // Hedefin Renderer'ı varsa boyunu hesaba kat, yoksa direkt verdiğin yüksekliği kullan
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null)
        {
            // Karakterin boyunun yarısı + Senin JumpEffect'te verdiğin ofset
            offset = new Vector3(0, renderer.bounds.extents.y + extraHeight, 0);
        }
        else
        {
            offset = new Vector3(0, extraHeight, 0);
        }
    }

    private void Update()
    {
        // Hedef yok olduysa balon da yok olsun
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        // Titreme efekti
        float shakeX = Mathf.Sin(Time.time * shakeSpeed) * shakeAmount;
        float shakeY = Mathf.Cos(Time.time * shakeSpeed * 1.5f) * shakeAmount * 0.5f;
        Vector3 shake = new Vector3(shakeX, shakeY, 0);

        // Hedefin üstünde takip et
        transform.position = target.position + offset + shake;

        // Kameraya bak (Billboard effect)
        if (Camera.main != null)
        {
            transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward,
                Camera.main.transform.rotation * Vector3.up);
        }
    }
}