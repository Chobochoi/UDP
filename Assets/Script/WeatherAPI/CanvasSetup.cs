using UnityEngine;
using UnityEngine.UI;

public class CanvasSetup : MonoBehaviour
{
    [Header("Canvas ����")]
    public Canvas weatherCanvas;
    public CanvasScaler weatherCanvasScaler;

    void Start()
    {
        SetupWeatherCanvas();
    }

    void SetupWeatherCanvas()
    {
        if (weatherCanvas == null)
            weatherCanvas = GetComponent<Canvas>();

        if (weatherCanvasScaler == null)
            weatherCanvasScaler = GetComponent<CanvasScaler>();

        if (weatherCanvasScaler != null)
        {
            weatherCanvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            weatherCanvasScaler.referenceResolution = new Vector2(1920, 1080);
            weatherCanvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            weatherCanvasScaler.matchWidthOrHeight = 0.5f;
        }

        Debug.Log($"Canvas ���� �Ϸ� - ���� �ػ�: {Screen.width}x{Screen.height}");
    }
}