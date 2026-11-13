using System.Collections.Generic;
using UnityEngine;

// Use with PropType.Image. The image's sprite masks the blur; its color tints the scene.
public class FrostedImage : BaseImage
{
    static readonly HashSet<FrostedImage> activeImages = new HashSet<FrostedImage>();
    public static bool BlurRequested => activeImages.Count > 0;
    Material blurMaterial;

    public override void Setup()
    {
        base.Setup();
        var shader = Resources.Load<Shader>("UI/FrostedImage");
        if (shader == null || !shader.isSupported) { return; }
        this.blurMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        this.image.material = this.blurMaterial;
        this.Register();
    }

    void OnEnable() => this.Register();

    void LateUpdate()
    {
        // Keep tint strength separate from inherited CanvasGroup opacity.
        if (this.blurMaterial != null) { this.blurMaterial.SetFloat("_TintOpacity", this.image.color.a); }
    }

    void Register()
    {
        if (this.blurMaterial == null || !this.isActiveAndEnabled) { return; }
        var camera = Camera.main;
        if (camera == null) { return; }
        if (camera.GetComponent<MenuBackgroundBlur>() == null)
        {
            camera.gameObject.AddComponent<MenuBackgroundBlur>();
        }
        activeImages.Add(this);
    }

    protected override void OnDisable()
    {
        activeImages.Remove(this);
        base.OnDisable();
    }

    void OnDestroy()
    {
        activeImages.Remove(this);
        if (this.blurMaterial != null) { Destroy(this.blurMaterial); }
    }
}
