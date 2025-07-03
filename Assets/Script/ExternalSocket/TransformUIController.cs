using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TransformUIController : MonoBehaviour
{
    [Header("UI References")]
    public TransformUIReceiver receiver;

    [Header("Position UI")]
    public Slider positionXSlider;
    public Slider positionYSlider;
    public Slider positionZSlider;
    public TMP_Text positionXText;
    public TMP_Text positionYText;
    public TMP_Text positionZText;

    [Header("Rotation UI")]
    public Slider rotationXSlider;
    public Slider rotationYSlider;
    public Slider rotationZSlider;
    public TMP_Text rotationXText;
    public TMP_Text rotationYText;
    public TMP_Text rotationZText;

    [Header("Scale UI")]
    public Slider scaleXSlider;
    public Slider scaleYSlider;
    public Slider scaleZSlider;
    public TMP_Text scaleXText;
    public TMP_Text scaleYText;
    public TMP_Text scaleZText;

    [Header("Control Buttons")]
    public Button sendButton;
    public Button resetButton;
    public Button testValuesButton;  // 테스트 값 설정 버튼
    public Toggle realtimeToggle;

    [Header("Settings")]
    public float positionRange = 20f;
    public float rotationRange = 360f;
    public float scaleMin = 0.1f;
    public float scaleMax = 5f;
    public bool realtimeSync = true;

    [Header("Debug")]
    public bool enableDebugLogs = true;
    public TMP_Text debugText;

    private bool isUpdatingFromNetwork = false;
    private int uiUpdateCount = 0;
    private int sentMessageCount = 0;

    private void Start()
    {
        SetupSliders();
        SetupButtons();
        SetupToggles();

        LogDebug("UI Controller initialized");
    }

    private void SetupSliders()
    {
        // 슬라이더 범위 설정
        SetSliderRange(positionXSlider, -positionRange, positionRange);
        SetSliderRange(positionYSlider, -positionRange, positionRange);
        SetSliderRange(positionZSlider, -positionRange, positionRange);

        SetSliderRange(rotationXSlider, 0, rotationRange);
        SetSliderRange(rotationYSlider, 0, rotationRange);
        SetSliderRange(rotationZSlider, 0, rotationRange);

        SetSliderRange(scaleXSlider, scaleMin, scaleMax);
        SetSliderRange(scaleYSlider, scaleMin, scaleMax);
        SetSliderRange(scaleZSlider, scaleMin, scaleMax);

        // 슬라이더 이벤트 연결
        positionXSlider.onValueChanged.AddListener(value => OnSliderChanged("PosX", value));
        positionYSlider.onValueChanged.AddListener(value => OnSliderChanged("PosY", value));
        positionZSlider.onValueChanged.AddListener(value => OnSliderChanged("PosZ", value));

        rotationXSlider.onValueChanged.AddListener(value => OnSliderChanged("RotX", value));
        rotationYSlider.onValueChanged.AddListener(value => OnSliderChanged("RotY", value));
        rotationZSlider.onValueChanged.AddListener(value => OnSliderChanged("RotZ", value));

        scaleXSlider.onValueChanged.AddListener(value => OnSliderChanged("ScaleX", value));
        scaleYSlider.onValueChanged.AddListener(value => OnSliderChanged("ScaleY", value));
        scaleZSlider.onValueChanged.AddListener(value => OnSliderChanged("ScaleZ", value));

        LogDebug("Sliders setup completed");
    }

    private void SetupButtons()
    {
        if (sendButton != null)
            sendButton.onClick.AddListener(SendCurrentData);

        if (resetButton != null)
            resetButton.onClick.AddListener(ResetToDefault);

        if (testValuesButton != null)
            testValuesButton.onClick.AddListener(SetTestValues);

        LogDebug("Buttons setup completed");
    }

    private void SetupToggles()
    {
        if (realtimeToggle != null)
        {
            realtimeToggle.isOn = realtimeSync;
            realtimeToggle.onValueChanged.AddListener(SetRealtimeSync);
        }

        LogDebug("Toggles setup completed");
    }

    private void OnSliderChanged(string sliderName, float value)
    {
        if (isUpdatingFromNetwork)
        {
            LogDebug($"[SLIDER] {sliderName} changed to {value:F2} (from network - ignored)");
            return;
        }

        LogDebug($"[SLIDER] {sliderName} changed to {value:F2} (user input)");

        // 텍스트 업데이트
        UpdateTextForSlider(sliderName, value);

        // 실시간 동기화가 활성화되어 있으면 즉시 전송
        if (realtimeSync)
        {
            LogDebug($"[REALTIME] Sending due to slider change: {sliderName}");
            SendCurrentData();
        }
    }

    private void UpdateTextForSlider(string sliderName, float value)
    {
        switch (sliderName)
        {
            case "PosX": if (positionXText != null) positionXText.text = $"{value:F2}"; break;
            case "PosY": if (positionYText != null) positionYText.text = $"{value:F2}"; break;
            case "PosZ": if (positionZText != null) positionZText.text = $"{value:F2}"; break;
            case "RotX": if (rotationXText != null) rotationXText.text = $"{value:F2}"; break;
            case "RotY": if (rotationYText != null) rotationYText.text = $"{value:F2}"; break;
            case "RotZ": if (rotationZText != null) rotationZText.text = $"{value:F2}"; break;
            case "ScaleX": if (scaleXText != null) scaleXText.text = $"{value:F2}"; break;
            case "ScaleY": if (scaleYText != null) scaleYText.text = $"{value:F2}"; break;
            case "ScaleZ": if (scaleZText != null) scaleZText.text = $"{value:F2}"; break;
        }
    }

    private void SendCurrentData()
    {
        if (receiver == null)
        {
            LogError("Receiver is null!");
            return;
        }

        if (isUpdatingFromNetwork)
        {
            LogDebug("[SEND] Skipping send - updating from network");
            return;
        }

        TransformData currentData = GetCurrentUIData();
        sentMessageCount++;

        LogDebug($"[SEND #{sentMessageCount}] Sending current UI data: {currentData.ToString()}");
        receiver.SendUIData(currentData);

        UpdateDebugDisplay();
    }

    private TransformData GetCurrentUIData()
    {
        TransformData data = new TransformData();

        data.posX = positionXSlider.value;
        data.posY = positionYSlider.value;
        data.posZ = positionZSlider.value;

        data.rotX = rotationXSlider.value;
        data.rotY = rotationYSlider.value;
        data.rotZ = rotationZSlider.value;

        data.scaleX = scaleXSlider.value;
        data.scaleY = scaleYSlider.value;
        data.scaleZ = scaleZSlider.value;

        return data;
    }

    public void UpdateUIFromNetwork(TransformData data)
    {
        uiUpdateCount++;

        LogDebug($"[UPDATE #{uiUpdateCount}] UpdateUIFromNetwork called with: {data.ToString()}");

        isUpdatingFromNetwork = true;

        try
        {
            // Position 업데이트
            UpdateSliderAndText(positionXSlider, positionXText, data.posX);
            UpdateSliderAndText(positionYSlider, positionYText, data.posY);
            UpdateSliderAndText(positionZSlider, positionZText, data.posZ);

            // Rotation 업데이트
            UpdateSliderAndText(rotationXSlider, rotationXText, data.rotX);
            UpdateSliderAndText(rotationYSlider, rotationYText, data.rotY);
            UpdateSliderAndText(rotationZSlider, rotationZText, data.rotZ);

            // Scale 업데이트
            UpdateSliderAndText(scaleXSlider, scaleXText, data.scaleX);
            UpdateSliderAndText(scaleYSlider, scaleYText, data.scaleY);
            UpdateSliderAndText(scaleZSlider, scaleZText, data.scaleZ);

            LogDebug($"[UPDATE] All UI elements updated successfully");
            UpdateDebugDisplay();
        }
        catch (System.Exception e)
        {
            LogError($"Error updating UI: {e.Message}");
        }
        finally
        {
            isUpdatingFromNetwork = false;
        }
    }

    private void UpdateSliderAndText(Slider slider, TMP_Text text, float value)
    {
        if (slider != null)
        {
            slider.value = value;
        }
        if (text != null)
        {
            text.text = $"{value:F2}";
        }
    }

    private void ResetToDefault()
    {
        TransformData defaultData = new TransformData();
        LogDebug("[RESET] Resetting to default values");

        UpdateUIFromNetwork(defaultData);
        SendCurrentData();
    }

    private void SetTestValues()
    {
        LogDebug("[TEST] Setting test values");

        // 테스트용 값 설정
        positionXSlider.value = 5f;
        positionYSlider.value = 2f;
        positionZSlider.value = -3f;

        rotationXSlider.value = 45f;
        rotationYSlider.value = 90f;
        rotationZSlider.value = 0f;

        scaleXSlider.value = 2f;
        scaleYSlider.value = 1.5f;
        scaleZSlider.value = 1f;

        LogDebug("[TEST] Test values set - sending to Scene1");
        SendCurrentData();
    }

    private void SetSliderRange(Slider slider, float min, float max)
    {
        if (slider != null)
        {
            slider.minValue = min;
            slider.maxValue = max;
        }
    }

    public void SetReceiver(TransformUIReceiver receiver)
    {
        this.receiver = receiver;
        LogDebug($"[SETUP] Receiver set: {(receiver != null ? "OK" : "NULL")}");
    }

    public void SetRealtimeSync(bool enable)
    {
        realtimeSync = enable;
        LogDebug($"[SETTING] Realtime sync: {enable}");
    }

    private void UpdateDebugDisplay()
    {
        if (debugText != null)
        {
            debugText.text = $"UI Updates: {uiUpdateCount} | Sent: {sentMessageCount} | Realtime: {realtimeSync}";
        }
    }

    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[SCENE2-UI] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[SCENE2-UI] {message}");
    }
}