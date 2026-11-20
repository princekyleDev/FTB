using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class ProximityOutlineGlow : MonoBehaviour
{
    private static readonly int ProximityGlow = Shader.PropertyToID("_ProximityGlow");

    [SerializeField, Min(0f)] private float glowRadius = 4f;
    [SerializeField, Min(0f)] private float maxGlowBoost = 4f;

    private SpriteRenderer spriteRenderer;
    private MaterialPropertyBlock properties;
    private Transform player;
    private float currentBoost;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        properties = new MaterialPropertyBlock();
    }

    private void LateUpdate()
    {
        if (player == null || !player.gameObject.activeInHierarchy)
            player = GameObject.FindWithTag("Player")?.transform;

        float boost = 0f;
        if (player != null)
        {
            float distance = Vector2.Distance(transform.position, player.position);
            boost = maxGlowBoost * Mathf.InverseLerp(glowRadius, 0f, distance);
        }

        SetBoost(boost);
    }

    private void OnDisable()
    {
        SetBoost(0f);
    }

    private void SetBoost(float boost)
    {
        if (boost == currentBoost || spriteRenderer == null)
            return;

        spriteRenderer.GetPropertyBlock(properties);
        properties.SetFloat(ProximityGlow, boost);
        spriteRenderer.SetPropertyBlock(properties);
        currentBoost = boost;
    }
}
