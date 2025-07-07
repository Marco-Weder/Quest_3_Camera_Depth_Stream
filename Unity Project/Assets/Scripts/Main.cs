using System.Collections;
using Meta.XR.EnvironmentDepth;
using Scripts;
using UnityEngine;
using UnityEngine.UI;

public class Main : MonoBehaviour
{
    [SerializeField] private PermissionsHandler permissionsHandler;
    [SerializeField] private PassthroughCameraHandler passthroughCameraHandler;
    [SerializeField] private DepthTextureHandler depthTextureHandler;
    [SerializeField] private EnvironmentDepthManager depthManager;
    [SerializeField] private NetworkStream networkStream;

    [SerializeField] private RawImage previewImage;
    
    IEnumerator Start()
    {
        yield return StartCoroutine(permissionsHandler.AskForPermissions());

        if (!permissionsHandler.ArePermissionsGranted())
        {
            Debug.LogError("Main: not all permissions have been granted by user!!!");
            yield break;
        }
        
        yield return StartCoroutine(passthroughCameraHandler.InitializeWebCamTexture());
        previewImage.texture = passthroughCameraHandler.WebCamTexture;
        Debug.Log("Main: Camera ready to be used!");
        
        StartCoroutine(depthTextureHandler.StartDepthRetrieval());
        Debug.Log("Main: DepthTexture ready to be used!");

        networkStream.InitialTCPHandshake();
        yield return StartCoroutine(networkStream.StartRGBDStream());
    }

 
}
