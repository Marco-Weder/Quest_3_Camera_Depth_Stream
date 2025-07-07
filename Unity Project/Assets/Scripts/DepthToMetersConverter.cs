using UnityEngine;
using Meta.XR.EnvironmentDepth;

public class DepthToMetersConverter
{
    private readonly Material _material;
    private RenderTexture _outputRenderTexture;
    private Texture2D _outputTexture2D;

    public Texture2D OutputTexture2D => _outputTexture2D;

    public DepthToMetersConverter(Shader conversionShader)
    {
        _material = new Material(conversionShader);
    }

    public void Convert(
        RenderTexture sourceTex2DArray,
        int eyeLayer,
        float nearZ,
        float farZ)
    {
        if (sourceTex2DArray == null)
        {
            Debug.LogWarning("DepthToMetersConverter: Source texture is null.");
            return;
        }

        if (_outputRenderTexture == null || _outputRenderTexture.width != sourceTex2DArray.width || _outputRenderTexture.height != sourceTex2DArray.height)
        {
            _outputRenderTexture = new RenderTexture(sourceTex2DArray.width, sourceTex2DArray.height, 0, RenderTextureFormat.RFloat)
            {
                enableRandomWrite = false,
                useMipMap = false,
                dimension = UnityEngine.Rendering.TextureDimension.Tex2D
            };
            _outputRenderTexture.Create();

            _outputTexture2D = new Texture2D(sourceTex2DArray.width, sourceTex2DArray.height, TextureFormat.RFloat, false, true);
        }

        _material.SetTexture("_EnvironmentDepthTexture", sourceTex2DArray);
        _material.SetFloat("_Near", nearZ);
        _material.SetFloat("_Far", farZ);
        _material.SetInt("_Layer", eyeLayer);

        Graphics.Blit(sourceTex2DArray, _outputRenderTexture, _material);

        RenderTexture.active = _outputRenderTexture;
        _outputTexture2D.ReadPixels(new Rect(0, 0, _outputRenderTexture.width, _outputRenderTexture.height), 0, 0);
        _outputTexture2D.Apply();
        RenderTexture.active = null;
    }
}