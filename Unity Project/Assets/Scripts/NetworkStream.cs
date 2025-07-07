using UnityEngine;
using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Scripts;
using UnityEngine.UI;

public class NetworkStream : MonoBehaviour
{
    [SerializeField] private PassthroughCameraHandler passthroughCameraHandler;
    [SerializeField] private DepthTextureHandler depthTextureHandler;
    [SerializeField] private RawImage sentDepthImage;

    private WebCamTexture webcamTexture;
    private Texture2D webcamTexture2D;
    private UdpClient udpClient;

    public string pcIP = "127.0.0.1";  // for adb reverse
    public int handshakePort = 8888;
    public int serverPort = 9999;

    public void InitialTCPHandshake()
    {
        using var client = new TcpClient();
        client.Connect(pcIP, handshakePort);

        var stream = client.GetStream();
        byte[] myIp = Encoding.ASCII.GetBytes(GetLocalIPAddress());
        stream.Write(myIp, 0, myIp.Length);

        byte[] buf = new byte[256];
        int n = stream.Read(buf, 0, buf.Length);
        pcIP = Encoding.ASCII.GetString(buf, 0, n).Trim();
        Debug.Log($"[Net] PC-IP received from server → {pcIP}");

        if (udpClient == null)
            udpClient = new UdpClient();
    }

    public IEnumerator StartRGBDStream()
    {
        webcamTexture = passthroughCameraHandler.WebCamTexture;
        webcamTexture2D = new Texture2D(webcamTexture.width, webcamTexture.height, TextureFormat.RGB24, false);

        var wait = new WaitForEndOfFrame();

        while (true)
        {
            yield return wait;

            if (!webcamTexture.didUpdateThisFrame || depthTextureHandler.depthTexture == null)
                continue;

            webcamTexture2D.SetPixels32(webcamTexture.GetPixels32());
            webcamTexture2D.Apply();

            byte[] depthBytes = ExtractDepthRaw(depthTextureHandler.depthTexture, 0);
            if (depthBytes.Length == 0)
            {
                Debug.LogWarning("[Depth] Skipped sending empty depth frame.");
                continue;
            }

            SendRGBDTexture(webcamTexture2D, depthBytes,
                depthTextureHandler.depthTexture.width,
                depthTextureHandler.depthTexture.height);
        }
    }

    private byte[] ExtractDepthRaw(RenderTexture srcArrayRT, int slice = 0)
    {
        if (srcArrayRT == null)
        {
            Debug.LogError("[Depth] RenderTexture is null!");
            return Array.Empty<byte>();
        }

        RenderTexture tmpRT = RenderTexture.GetTemporary(
            srcArrayRT.width, srcArrayRT.height, 0,
            RenderTextureFormat.R16, RenderTextureReadWrite.Linear);

        try
        {
            Graphics.CopyTexture(srcArrayRT, slice, 0, tmpRT, 0, 0);

            Texture2D cpuTex = new Texture2D(tmpRT.width, tmpRT.height,
                TextureFormat.R16, false, true);

            Graphics.CopyTexture(tmpRT, cpuTex);
            cpuTex.Apply(false, false);

            byte[] rawBytes = cpuTex.GetRawTextureData();
            Destroy(cpuTex);
            return rawBytes;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Depth] Copy failed: {e.Message}");
            return Array.Empty<byte>();
        }
        finally
        {
            RenderTexture.ReleaseTemporary(tmpRT);
        }
    }

    private void SendRGBDTexture(Texture2D rgbTex, byte[] depthBytes, int depthW, int depthH)
    {
        byte[] rgbBytes = rgbTex.EncodeToJPG(50);
        byte[] depthCompressed = CompressGZip(depthBytes);

        string header = DateTime.UtcNow.ToString("o") + ";" +
                        rgbTex.width + ";" + rgbTex.height + ";" + rgbBytes.Length + ";" +
                        depthW + ";" + depthH + ";" + depthCompressed.Length + ";" +
                        "gzip;";

        byte[] headerBytes = Encoding.ASCII.GetBytes(header);

        byte[] packet = new byte[headerBytes.Length + rgbBytes.Length + depthCompressed.Length];
        Buffer.BlockCopy(headerBytes, 0, packet, 0, headerBytes.Length);
        Buffer.BlockCopy(rgbBytes, 0, packet, headerBytes.Length, rgbBytes.Length);
        Buffer.BlockCopy(depthCompressed, 0, packet, headerBytes.Length + rgbBytes.Length, depthCompressed.Length);

        udpClient.Send(packet, packet.Length, pcIP, serverPort);
        Debug.Log($"[Net] Sent packet — size: {packet.Length} bytes");
    }

    private static byte[] CompressGZip(byte[] data)
    {
        using var outMs = new System.IO.MemoryStream();
        using (var gz = new System.IO.Compression.GZipStream(outMs, System.IO.Compression.CompressionLevel.Fastest))
        {
            gz.Write(data, 0, data.Length);
        }
        return outMs.ToArray();
    }

    private static string GetLocalIPAddress()
    {
        foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                return ip.ToString();
        }
        return "127.0.0.1";
    }

    private void OnApplicationQuit()
    {
        webcamTexture?.Stop();
        udpClient?.Close();
    }
}
