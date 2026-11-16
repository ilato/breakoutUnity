using UnityEngine;

/// <summary>
/// Pedacinhos que voam quando um tijolo quebra (efeito visual simples, sem Particle System).
/// </summary>
public class Debris : MonoBehaviour
{
    Vector2 velocity;
    float life, maxLife, spin;
    Vector3 startScale;
    SpriteRenderer sr;
    Color color;

    public static void Burst(Sprite sprite, Material material, Vector3 position, Color color, int count)
    {
        if (sprite == null) return;
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Debris");
            go.transform.position = position + (Vector3)(Random.insideUnitCircle * 0.25f);
            float size = Random.Range(0.08f, 0.18f);
            go.transform.localScale = new Vector3(size, size, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            if (material != null) sr.sharedMaterial = material;
            sr.color = color;
            sr.sortingOrder = 5;

            var d = go.AddComponent<Debris>();
            d.sr = sr;
            d.color = color;
            d.startScale = go.transform.localScale;
            d.velocity = Random.insideUnitCircle.normalized * Random.Range(2f, 5f) + Vector2.up * 1.5f;
            d.maxLife = d.life = Random.Range(0.35f, 0.6f);
            d.spin = Random.Range(-540f, 540f);
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;
        velocity.y -= 12f * dt;
        transform.position += (Vector3)(velocity * dt);
        transform.Rotate(0f, 0f, spin * dt);

        life -= dt;
        float k = Mathf.Clamp01(life / maxLife);
        Color c = color;
        c.a = k;
        sr.color = c;
        transform.localScale = startScale * (0.5f + 0.5f * k);

        if (life <= 0f) Destroy(gameObject);
    }
}
