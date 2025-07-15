using UnityEngine;

public class FOVEffect : MonoBehaviour
{
    public Camera vrCamera;
    public Material postProcessMat;

    RenderTexture renderTexture;

    void Start()
    {
        renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
        vrCamera.targetTexture = renderTexture;
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        // 후처리 셰이더 적용
        Graphics.Blit(renderTexture, dest, postProcessMat);
    }
}

