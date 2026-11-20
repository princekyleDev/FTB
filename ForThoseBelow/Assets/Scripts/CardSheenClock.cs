using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public class CardSheenClock : MonoBehaviour
{
    private static readonly int StartTime = Shader.PropertyToID("_SheenStartTime");
    private static readonly int UnscaledTime = Shader.PropertyToID("_CardSheenUnscaledTime");
    private static readonly int CardUVRect = Shader.PropertyToID("_CardUVRect");

    private Image image;
    private Material originalMaterial;
    private Material animatedMaterial;

    private void OnEnable()
    {
        image = GetComponent<Image>();
        originalMaterial = image.material;
        if (!originalMaterial.HasProperty(StartTime))
            return;

        animatedMaterial = new Material(originalMaterial);
        if (image.overrideSprite != null)
        {
            Vector4 uv = DataUtility.GetOuterUV(image.overrideSprite);
            animatedMaterial.SetVector(CardUVRect, new Vector4(uv.x, uv.y, uv.z - uv.x, uv.w - uv.y));
        }

        animatedMaterial.SetFloat(StartTime, Time.unscaledTime);
        Shader.SetGlobalFloat(UnscaledTime, Time.unscaledTime);
        image.material = animatedMaterial;
    }

    private void Update()
    {
        if (animatedMaterial != null)
            Shader.SetGlobalFloat(UnscaledTime, Time.unscaledTime);
    }

    private void OnDisable()
    {
        if (animatedMaterial == null)
            return;

        if (image != null && image.material == animatedMaterial)
            image.material = originalMaterial;

        Destroy(animatedMaterial);
        animatedMaterial = null;
    }
}
