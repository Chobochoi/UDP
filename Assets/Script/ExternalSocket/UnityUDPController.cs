using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class UnityUDPController : MonoBehaviour
{
    [Header("Connection Settings")]
    public string targetIP = "192.168.156.146";
    public int targetPort = 12223;

    [Header("Manual Control UI")]
    public Slider posXSlider;
    public Slider posYSlider;
    public Slider posZSlider;
    public Slider rotXSlider;
    public Slider rotYSlider;
    public Slider rotZSlider;

    [Header("Input Fields")]
    public TMP_InputField posXInput;
    public TMP_InputField posYInput;
    public TMP_InputField posZInput;
    public TMP_InputField rotXInput;
    public TMP_InputField rotYInput;
    public TMP_InputField rotZInput;

    [Header("Control Buttons")]
    public Button sendOnceButton;
    public Button startRealtimeButton;
    public Button stopRealtimeButton;
    public Button resetButton;

    //[Header("Pattern Buttons")]
    //public Button circleMotionButton;
    //public Button squareMotionButton;
    //public Button figureEightButton;
    //public Button randomMotionButton;
    //public Button stopPatternButton;

    [Header("Status UI")]
    public TMP_Text statusText;
    public TMP_Text currentValuesText;
    public Toggle realtimeToggle;

    [Header("Motion Settings")]
    public float motionSpeed = 1f;
    public float motionRadius = 5f;

    private UDPClientSender udpSender;
    private bool isRealtimeSending = false;
    //private bool isPatternRunning = false;
    //private Coroutine currentPatternCoroutine;

    // 현재 Transform 값들
    private Vector3 currentPosition = Vector3.zero;
    private Vector3 currentRotation = Vector3.zero;

    void Start()
    {
        InitializeUDP();
        InitializeUI();
        UpdateCurrentValues();
    }

    void InitializeUDP()
    {
        udpSender = gameObject.AddComponent<UDPClientSender>();
        udpSender.Initialize(targetIP, targetPort);
    }

    void InitializeUI()
    {
        // 슬라이더 범위 설정
        SetSliderRange(posXSlider, -100f, 100f);
        SetSliderRange(posYSlider, -100f, 100f);
        SetSliderRange(posZSlider, -100f, 100f);
        SetSliderRange(rotXSlider, 0f, 360f);
        SetSliderRange(rotYSlider, 0f, 360f);
        SetSliderRange(rotZSlider, 0f, 360f);

        // 슬라이더 이벤트 연결
        posXSlider.onValueChanged.AddListener(OnSliderChanged);
        posYSlider.onValueChanged.AddListener(OnSliderChanged);
        posZSlider.onValueChanged.AddListener(OnSliderChanged);
        rotXSlider.onValueChanged.AddListener(OnSliderChanged);
        rotYSlider.onValueChanged.AddListener(OnSliderChanged);
        rotZSlider.onValueChanged.AddListener(OnSliderChanged);

        // 입력 필드 이벤트 연결
        posXInput.onEndEdit.AddListener(OnInputChanged);
        posYInput.onEndEdit.AddListener(OnInputChanged);
        posZInput.onEndEdit.AddListener(OnInputChanged);
        rotXInput.onEndEdit.AddListener(OnInputChanged);
        rotYInput.onEndEdit.AddListener(OnInputChanged);
        rotZInput.onEndEdit.AddListener(OnInputChanged);

        // 버튼 이벤트 연결
        sendOnceButton.onClick.AddListener(SendOnce);
        startRealtimeButton.onClick.AddListener(StartRealtimeSending);
        stopRealtimeButton.onClick.AddListener(StopRealtimeSending);
        resetButton.onClick.AddListener(ResetValues);

        // 패턴 버튼 이벤트 연결
        //circleMotionButton.onClick.AddListener(() => StartPattern(PatternType.Circle));
        //squareMotionButton.onClick.AddListener(() => StartPattern(PatternType.Square));
        //figureEightButton.onClick.AddListener(() => StartPattern(PatternType.FigureEight));
        //randomMotionButton.onClick.AddListener(() => StartPattern(PatternType.Random));
        //stopPatternButton.onClick.AddListener(StopPattern);

        UpdateButtonStates();
    }

    void SetSliderRange(Slider slider, float min, float max)
    {
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = 0f;
    }

    void OnSliderChanged(float value)
    {
        UpdateCurrentValuesFromSliders();
        if (realtimeToggle.isOn && isRealtimeSending)
        {
            SendCurrentValues();
        }
    }

    void OnInputChanged(string value)
    {
        UpdateSlidersFromInputs();
        UpdateCurrentValuesFromSliders();
    }

    void UpdateCurrentValuesFromSliders()
    {
        currentPosition = new Vector3(posXSlider.value, posYSlider.value, posZSlider.value);
        currentRotation = new Vector3(rotXSlider.value, rotYSlider.value, rotZSlider.value);
        UpdateCurrentValues();
    }

    void UpdateSlidersFromInputs()
    {
        if (float.TryParse(posXInput.text, out float posX)) posXSlider.value = posX;
        if (float.TryParse(posYInput.text, out float posY)) posYSlider.value = posY;
        if (float.TryParse(posZInput.text, out float posZ)) posZSlider.value = posZ;
        if (float.TryParse(rotXInput.text, out float rotX)) rotXSlider.value = rotX;
        if (float.TryParse(rotYInput.text, out float rotY)) rotYSlider.value = rotY;
        if (float.TryParse(rotZInput.text, out float rotZ)) rotZSlider.value = rotZ;
    }

    void UpdateInputsFromSliders()
    {
        posXInput.text = posXSlider.value.ToString("F2");
        posYInput.text = posYSlider.value.ToString("F2");
        posZInput.text = posZSlider.value.ToString("F2");
        rotXInput.text = rotXSlider.value.ToString("F1");
        rotYInput.text = rotYSlider.value.ToString("F1");
        rotZInput.text = rotZSlider.value.ToString("F1");
    }

    void UpdateCurrentValues()
    {
        currentValuesText.text = $"Position: ({currentPosition.x:F2}, {currentPosition.y:F2}, {currentPosition.z:F2})\n" +
                                $"Rotation: ({currentRotation.x:F1}°, {currentRotation.y:F1}°, {currentRotation.z:F1}°)";
        UpdateInputsFromSliders();
    }

    void SendOnce()
    {
        SendCurrentValues();
        statusText.text = "Sent once!";
    }

    void SendCurrentValues()
    {
        udpSender.SendTransform(currentPosition, currentRotation);
    }

    void StartRealtimeSending()
    {
        isRealtimeSending = true;
        StartCoroutine(RealtimeSendingCoroutine());
        UpdateButtonStates();
        statusText.text = "Realtime sending started";
    }

    void StopRealtimeSending()
    {
        isRealtimeSending = false;
        UpdateButtonStates();
        statusText.text = "Realtime sending stopped";
    }

    IEnumerator RealtimeSendingCoroutine()
    {
        while (isRealtimeSending)
        {
            SendCurrentValues();
            yield return new WaitForSeconds(0.1f); // 10Hz
        }
    }

    void ResetValues()
    {
        posXSlider.value = 0f;
        posYSlider.value = 0f;
        posZSlider.value = 0f;
        rotXSlider.value = 0f;
        rotYSlider.value = 0f;
        rotZSlider.value = 0f;

        UpdateCurrentValuesFromSliders();
        SendCurrentValues();
        statusText.text = "Reset to origin";
    }

    // 패턴 모션들
    enum PatternType { Circle, Square, FigureEight, Random }

    //void StartPattern(PatternType pattern)
    //{
    //    StopPattern();
    //    isPatternRunning = true;

    //    switch (pattern)
    //    {
    //        case PatternType.Circle:
    //            currentPatternCoroutine = StartCoroutine(CircleMotion());
    //            statusText.text = "Circle motion started";
    //            break;
    //        case PatternType.Square:
    //            currentPatternCoroutine = StartCoroutine(SquareMotion());
    //            statusText.text = "Square motion started";
    //            break;
    //        case PatternType.FigureEight:
    //            currentPatternCoroutine = StartCoroutine(FigureEightMotion());
    //            statusText.text = "Figure-8 motion started";
    //            break;
    //        case PatternType.Random:
    //            currentPatternCoroutine = StartCoroutine(RandomMotion());
    //            statusText.text = "Random motion started";
    //            break;
    //    }

    //    UpdateButtonStates();
    //}

    //void StopPattern()
    //{
    //    if (currentPatternCoroutine != null)
    //    {
    //        StopCoroutine(currentPatternCoroutine);
    //        currentPatternCoroutine = null;
    //    }
    //    isPatternRunning = false;
    //    UpdateButtonStates();
    //    statusText.text = "Pattern stopped";
    //}

    //IEnumerator CircleMotion()
    //{
    //    float angle = 0f;
    //    while (isPatternRunning)
    //    {
    //        float x = Mathf.Cos(angle) * motionRadius;
    //        float z = Mathf.Sin(angle) * motionRadius;
    //        float y = Mathf.Sin(angle * 2) * 2f; // 위아래 움직임

    //        Vector3 pos = new Vector3(x, y, z);
    //        Vector3 rot = new Vector3(0, angle * Mathf.Rad2Deg, 0);

    //        udpSender.SendTransform(pos, rot);
    //        UpdateSliders(pos, rot);

    //        angle += motionSpeed * Time.deltaTime;
    //        if (angle > 2 * Mathf.PI) angle -= 2 * Mathf.PI;

    //        yield return null;
    //    }
    //}

    //IEnumerator SquareMotion()
    //{
    //    Vector3[] corners = {
    //        new Vector3(-motionRadius, 0, -motionRadius),
    //        new Vector3(motionRadius, 0, -motionRadius),
    //        new Vector3(motionRadius, 0, motionRadius),
    //        new Vector3(-motionRadius, 0, motionRadius)
    //    };

    //    int currentCorner = 0;
    //    float t = 0f;

    //    while (isPatternRunning)
    //    {
    //        Vector3 start = corners[currentCorner];
    //        Vector3 end = corners[(currentCorner + 1) % 4];

    //        Vector3 pos = Vector3.Lerp(start, end, t);
    //        Vector3 rot = new Vector3(0, currentCorner * 90f, 0);

    //        udpSender.SendTransform(pos, rot);
    //        UpdateSliders(pos, rot);

    //        t += motionSpeed * Time.deltaTime;
    //        if (t >= 1f)
    //        {
    //            t = 0f;
    //            currentCorner = (currentCorner + 1) % 4;
    //        }

    //        yield return null;
    //    }
    //}

    //IEnumerator FigureEightMotion()
    //{
    //    float t = 0f;
    //    while (isPatternRunning)
    //    {
    //        float x = motionRadius * Mathf.Sin(t);
    //        float z = motionRadius * Mathf.Sin(t) * Mathf.Cos(t);
    //        float y = Mathf.Sin(t * 2) * 1f;

    //        Vector3 pos = new Vector3(x, y, z);
    //        Vector3 rot = new Vector3(0, t * Mathf.Rad2Deg, 0);

    //        udpSender.SendTransform(pos, rot);
    //        UpdateSliders(pos, rot);

    //        t += motionSpeed * Time.deltaTime;
    //        if (t > 2 * Mathf.PI) t -= 2 * Mathf.PI;

    //        yield return null;
    //    }
    //}

    //IEnumerator RandomMotion()
    //{
    //    Vector3 targetPos = Vector3.zero;
    //    Vector3 targetRot = Vector3.zero;
    //    Vector3 currentPos = Vector3.zero;
    //    Vector3 currentRot = Vector3.zero;

    //    while (isPatternRunning)
    //    {
    //        // 5초마다 새로운 랜덤 타겟 생성
    //        if (Time.time % 5f < Time.deltaTime)
    //        {
    //            targetPos = new Vector3(
    //                Random.Range(-motionRadius, motionRadius),
    //                Random.Range(-2f, 2f),
    //                Random.Range(-motionRadius, motionRadius)
    //            );
    //            targetRot = new Vector3(
    //                Random.Range(0f, 360f),
    //                Random.Range(0f, 360f),
    //                Random.Range(0f, 360f)
    //            );
    //        }

    //        // 부드럽게 타겟으로 이동
    //        currentPos = Vector3.Lerp(currentPos, targetPos, motionSpeed * Time.deltaTime);
    //        currentRot = Vector3.Lerp(currentRot, targetRot, motionSpeed * Time.deltaTime);

    //        udpSender.SendTransform(currentPos, currentRot);
    //        UpdateSliders(currentPos, currentRot);

    //        yield return null;
    //    }
    //}

    void UpdateSliders(Vector3 pos, Vector3 rot)
    {
        posXSlider.value = pos.x;
        posYSlider.value = pos.y;
        posZSlider.value = pos.z;
        rotXSlider.value = rot.x;
        rotYSlider.value = rot.y;
        rotZSlider.value = rot.z;

        UpdateCurrentValuesFromSliders();
    }

    void UpdateButtonStates()
    {
        startRealtimeButton.interactable = !isRealtimeSending; //&& !isPatternRunning;
        stopRealtimeButton.interactable = isRealtimeSending;

        bool canStartPattern = !isRealtimeSending; //&& //!isPatternRunning;
        //circleMotionButton.interactable = canStartPattern;
        //squareMotionButton.interactable = canStartPattern;
        //figureEightButton.interactable = canStartPattern;
        //randomMotionButton.interactable = canStartPattern;
        //stopPatternButton.interactable = isPatternRunning;
    }

    void OnDestroy()
    {
        if (udpSender != null) udpSender.Close();
    }
}