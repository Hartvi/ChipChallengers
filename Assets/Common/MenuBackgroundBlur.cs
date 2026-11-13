using UnityEngine;

[RequireComponent(typeof(Camera))]
public class MenuBackgroundBlur : MonoBehaviour
{
    public MainMenu Menu { get; set; }
    Material material;

    // The scene is blurred before the screen-space overlay canvas draws the menu.
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (this.Menu == null || !this.Menu.isActiveAndEnabled || !this.Menu.isSelected)
        {
            Graphics.Blit(source, destination);
            return;
        }
        if (this.material == null)
        {
            var shader = Resources.Load<Shader>("UI/MenuBackgroundBlur");
            if (shader == null || !shader.isSupported) { Graphics.Blit(source, destination); return; }
            this.material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }
        var temporary = RenderTexture.GetTemporary(source.width, source.height, 0, source.format);
        temporary.filterMode = FilterMode.Bilinear;
        try
        {
            float radiusPixels = Mathf.Pow((float)Screen.width * Screen.height, 0.2f);
            float step = radiusPixels / 4f;
            this.material.SetVector("_BlurDirection", new Vector4(step / source.width, 0f, 0f, 0f));
            Graphics.Blit(source, temporary, this.material);
            this.material.SetVector("_BlurDirection", new Vector4(0f, step / source.height, 0f, 0f));
            Graphics.Blit(temporary, destination, this.material);
        }
        finally { RenderTexture.ReleaseTemporary(temporary); }
    }

    void OnDestroy()
    {
        if (this.material != null) { Destroy(this.material); }
    }
}
