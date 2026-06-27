using UnityEngine;
using UnityEngine.UI;

public static class UGUISetup
{
    public static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

    public static void ConfigureCanvasScaler(CanvasScaler scaler)
    {
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }

    public static Canvas EnsureOverlayCanvas(GameObject host, int sortingOrder)
    {
        var canvas = host.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = host.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            host.AddComponent<GraphicRaycaster>();
        }

        canvas.sortingOrder = sortingOrder;

        var scaler = host.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = host.AddComponent<CanvasScaler>();

        ConfigureCanvasScaler(scaler);
        return canvas;
    }
}
