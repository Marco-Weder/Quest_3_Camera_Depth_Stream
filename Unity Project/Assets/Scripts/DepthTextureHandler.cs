using System;
using System.Collections;
using System.Collections.Generic;
using Meta.XR.EnvironmentDepth;
using Scripts;
//using Unity.XR.Oculus;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.XR;

public class DepthTextureHandler : MonoBehaviour
{
    private static readonly int EnvironmentDepthTexture = Shader.PropertyToID("_EnvironmentDepthTexture");
    [SerializeField] private EnvironmentDepthManager depthManager;
    [SerializeField] private RawImage previewImage;
    
    public enum DepthUnit
    {
        Meters,
        Millimeters
    }

    // local variables
    [FormerlySerializedAs("_depthTexture")] public RenderTexture depthTexture;

    public IEnumerator StartDepthRetrieval()
    {
        if (!EnvironmentDepthManager.IsSupported)
        {
            Debug.LogError("DepthTextureHandler: Environment Depth is not supported!");
            yield break;
        }

        depthManager.enabled = true;

        while (!depthManager.IsDepthAvailable)
        {
            yield return null;
        }

        Debug.Log("DepthTextureHandler: Depth Textures are now available!");

        while (true)
        {
            UpdateDepthTexture();
            yield return null;
        }
    }


    private void UpdateDepthTexture()
    {
        depthTexture = Shader.GetGlobalTexture(EnvironmentDepthTexture) as RenderTexture;
        
        previewImage.texture = depthTexture;
    }
}