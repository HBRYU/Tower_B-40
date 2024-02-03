using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEngine.U2D;
using UnityEngine.Rendering;
 
public class SmoothPixelPerfectCameraMovement : MonoBehaviour
{
    public int ppu;
    private Vector2 viewportScale;
    private RenderTexture tempRenderTexture;
    public Camera myCamera;
    private int width, height;
 
    private void Start()
    {
        // myCamera = Camera.main;
        SetWidthAndHeight();
 
        RenderPipelineManager.beginFrameRendering += FrameRenderingStart;
        RenderPipelineManager.endFrameRendering += FrameRenderingEnd;
    }
 
    private void OnDestroy()
    {
        RenderPipelineManager.beginFrameRendering -= FrameRenderingStart;
        RenderPipelineManager.endFrameRendering -= FrameRenderingEnd;
    }
 
    private void FrameRenderingStart(ScriptableRenderContext context, Camera[] cameras)
    {
        if(width != Screen.width || height != Screen.height)
            SetWidthAndHeight();
 
        tempRenderTexture = RenderTexture.GetTemporary(width, height, 16);
       
        //you only need to change texture format if you want to use HDR colors e.q. with Glow Post Processing
        if(SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGB111110Float))
        {
            tempRenderTexture.format = RenderTextureFormat.RGB111110Float;
        } else { Debug.Log("RenderTexture format not supported"); }
 
        myCamera.targetTexture = tempRenderTexture;
        Graphics.SetRenderTarget(tempRenderTexture); //dont think this is actually needed
    }
 
    private void FrameRenderingEnd(ScriptableRenderContext context, Camera[] cameras)
    {
        myCamera.targetTexture = null; //blit directly into render buffer
        Vector2 screenWorldBounds = new Vector2(myCamera.orthographicSize * 2 * Screen.width / Screen.height, myCamera.orthographicSize * 2);
        Vector2 direction = new Vector2(Mathf.Round(transform.position.x * ppu) / ppu - transform.position.x, Mathf.Round(transform.position.y * ppu) / ppu - transform.position.y);
        Graphics.Blit(tempRenderTexture, null as RenderTexture, Vector2.one, -(direction / screenWorldBounds));
 
        RenderTexture.ReleaseTemporary(tempRenderTexture);
    }
 
    private void SetWidthAndHeight()
    {
        width = Screen.width;
        height = Screen.height;
    }
}