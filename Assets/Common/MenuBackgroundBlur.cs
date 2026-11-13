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
        RenderTexture downsampled = null;
        RenderTexture temporary = null;
        try
        {
            float radiusPixels = Mathf.Pow((float)Screen.width * Screen.height, 0.2f);
            float step = radiusPixels / 4f;
            // Prefilter in 2x steps so the Gaussian samples stay at most one texel apart.
            // Widely spaced samples at full resolution leave repeated, grid-like edges.
            var input = source;
            while (step * input.width / source.width > 1f && input.width > 1 && input.height > 1)
            {
                var smaller = GetBlurTexture(Mathf.Max(1, input.width / 2), Mathf.Max(1, input.height / 2), source.format);
                try { Graphics.Blit(input, smaller, this.material, 1); }
                catch { RenderTexture.ReleaseTemporary(smaller); throw; }
                if (downsampled != null) { RenderTexture.ReleaseTemporary(downsampled); }
                downsampled = smaller;
                input = smaller;
            }
            temporary = GetBlurTexture(input.width, input.height, source.format);
            this.material.SetVector("_BlurDirection", new Vector4(step / source.width, 0f, 0f, 0f));
            Graphics.Blit(input, temporary, this.material, 0);
            this.material.SetVector("_BlurDirection", new Vector4(0f, step / source.height, 0f, 0f));
            if (panels)
            {
                if (this.panelTexture == null || this.panelTexture.width != input.width ||
                    this.panelTexture.height != input.height || this.panelTexture.format != source.format)
                {
                    this.ReleasePanelTexture();
                    this.panelTexture = new RenderTexture(input.width, input.height, 0, source.format) {
                        name = "UI panel background blur", filterMode = FilterMode.Bilinear,
                        wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave
                    };
                    this.panelTexture.Create();
                }
                Graphics.Blit(temporary, this.panelTexture, this.material, 0);
                Shader.SetGlobalTexture("_PanelBlurTexture", this.panelTexture);
                Graphics.Blit(fullScreen ? this.panelTexture : source, destination);
            }
            else
            {
                this.ReleasePanelTexture();
                // Finish at the reduced resolution before bilinear upsampling to the screen.
                if (downsampled != null)
                {
                    Graphics.Blit(temporary, downsampled, this.material, 0);
                    Graphics.Blit(downsampled, destination);
                }
                else { Graphics.Blit(temporary, destination, this.material, 0); }
            }
        }
        finally
        {
            if (temporary != null) { RenderTexture.ReleaseTemporary(temporary); }
            if (downsampled != null) { RenderTexture.ReleaseTemporary(downsampled); }
        }
    }

    static RenderTexture GetBlurTexture(int width, int height, RenderTextureFormat format)
    {
        var texture = RenderTexture.GetTemporary(width, height, 0, format);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        return texture;
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
