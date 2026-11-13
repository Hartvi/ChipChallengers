using UnityEngine;

[RequireComponent(typeof(Camera))]
public class MenuBackgroundBlur : MonoBehaviour
{
    public MainMenu Menu { get; set; }
    Material material;
    RenderTexture panelTexture;

    // Share one scene blur across overlay panels, with optional full-screen output for the main menu.
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        bool fullScreen = this.Menu != null && this.Menu.isActiveAndEnabled && this.Menu.isSelected;
        bool panels = FrostedImage.BlurRequested;
        if (!fullScreen && !panels)
        {
            this.ReleasePanelTexture();
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
        temporary.wrapMode = TextureWrapMode.Clamp;
        try
        {
            float radiusPixels = Mathf.Pow((float)Screen.width * Screen.height, 0.2f);
            float step = radiusPixels / 4f;
            this.material.SetVector("_BlurDirection", new Vector4(step / source.width, 0f, 0f, 0f));
            Graphics.Blit(source, temporary, this.material);
            this.material.SetVector("_BlurDirection", new Vector4(0f, step / source.height, 0f, 0f));
            if (panels)
            {
                if (this.panelTexture == null || this.panelTexture.width != source.width ||
                    this.panelTexture.height != source.height || this.panelTexture.format != source.format)
                {
                    this.ReleasePanelTexture();
                    this.panelTexture = new RenderTexture(source.width, source.height, 0, source.format) {
                        name = "UI panel background blur", filterMode = FilterMode.Bilinear,
                        wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave
                    };
                    this.panelTexture.Create();
                }
                Graphics.Blit(temporary, this.panelTexture, this.material);
                Shader.SetGlobalTexture("_PanelBlurTexture", this.panelTexture);
                Graphics.Blit(fullScreen ? this.panelTexture : source, destination);
            }
            else
            {
                this.ReleasePanelTexture();
                Graphics.Blit(temporary, destination, this.material);
            }
        }
        finally { RenderTexture.ReleaseTemporary(temporary); }
    }

    void OnDestroy()
    {
        this.ReleasePanelTexture();
        if (this.material != null) { Destroy(this.material); }
    }

    void OnDisable() => this.ReleasePanelTexture();

    void ReleasePanelTexture()
    {
        if (this.panelTexture == null) { return; }
        Shader.SetGlobalTexture("_PanelBlurTexture", Texture2D.blackTexture);
        this.panelTexture.Release();
        Destroy(this.panelTexture);
        this.panelTexture = null;
    }
}
