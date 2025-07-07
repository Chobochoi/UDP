using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TransformController : MonoBehaviour
{
    [Header("Network")]
    public UDPManager udpManager;

    [Header("Transform Settings")]
    public Transform targetObject;
    public float sendRate = 10f;
    public bool autoSend = true;

    [Header("UI")]
    public TMP_Text statusText;
    //public Button testButton;
    //public Button moveObjectButton;
    public Button sendCurrentButton;
    public TMP_InputField ipInputField;
    public Button connectButton;
    public Toggle autoSendToggle;

    [Header("Debug")]
    public bool enableDebugLogs = true;
    public TMP_Text debugText;

    [Header("Transform Monitoring")]
    public float changeThreshold = 0.001f;  // �� �ΰ��� ���� ����
    public bool forceUpdateEveryFrame = false;  // �� ������ ���� ������Ʈ (�׽�Ʈ��)

    // Transform sync
    private const string SENDER_ID = "SCENE1_CONTROLLER";
    private TransformData lastSentData;
    private TransformData currentTransformData;
    private float sendInterval;
    private float lastSendTime;
    private bool isApplyingReceived = false;

    // Transform monitoring
    private Vector3 lastPosition;
    private Vector3 lastRotation;
    private Vector3 lastScale;
    private bool hasTransformChanged = false;

    private void Start()
    {
        InitializeComponents();
        SetupNetworkEvents();

        // �ʱ� Transform ���� ����
        if (targetObject != null)
        {
            StoreCurrentTransform();
        }

        // �ʱ� ������ ���� 1�� �� �� �� ����
        Invoke(nameof(SendCurrentTransform), 1f);
    }

    private void InitializeComponents()
    {
        sendInterval = 1f / sendRate;

        if (targetObject == null)
        {
            targetObject = transform;
            Debug.LogWarning("Target Object not assigned, using self transform");
        }

        if (udpManager == null)
        {
            udpManager = FindFirstObjectByType<UDPManager>();

            if (udpManager == null)
            {
                GameObject udpGO = new GameObject("UDPManager");
                udpManager = udpGO.AddComponent<UDPManager>();
            }
        }

        // UI ����
        if (ipInputField != null)
        {
            ipInputField.text = udpManager.targetIP;
        }

        // �׽�Ʈ�� ���� ��ư �̺�Ʈ ����
        // �׽�Ʈ �Ϸ��Ͽ� �ּ�ó��
        //if (testButton != null)
        //{
        //    testButton.onClick.AddListener(SendCurrentTransform);
        //}

        //if (moveObjectButton != null)
        //{
        //    moveObjectButton.onClick.AddListener(TestMoveObject);
        //}

        if (sendCurrentButton != null)
        {
            sendCurrentButton.onClick.AddListener(ForceSendCurrent);
        }

        if (connectButton != null)
        {
            connectButton.onClick.AddListener(UpdateTargetIP);
        }

        if (autoSendToggle != null)
        {
            autoSendToggle.isOn = autoSend;
            autoSendToggle.onValueChanged.AddListener(SetAutoSend);
        }

        LogDebug("Components initialized");
    }

    private void SetupNetworkEvents()
    {
        if (udpManager != null)
        {
            udpManager.OnDataReceived += OnDataReceived;
            udpManager.OnNetworkError += OnNetworkError;
            udpManager.OnNetworkConnected += OnNetworkConnected;
            udpManager.OnNetworkDisconnected += OnNetworkDisconnected;
        }
    }

    private void OnDataReceived(string csvData)
    {
        try
        {
            LogDebug($"[RECEIVED CSV] {csvData}");

            TransformMessage message = TransformMessage.FromCSV(csvData);

            if (message != null && message.transformData != null && message.senderType == "SCENE2_UI")
            {
                LogDebug($"[VALID MESSAGE] From Scene2: {message.transformData.ToString()}");
                ApplyTransformToObject(message.transformData);
            }
        }
        catch (System.Exception e)
        {
            LogError($"Error processing received data: {e.Message}");
        }
    }

    private void OnNetworkError(string error)
    {
        UpdateStatus($"Network Error: {error}");
        LogError($"Network error: {error}");
    }

    private void OnNetworkConnected()
    {
        UpdateStatus("Network Connected");
        LogDebug("Network connected");
    }

    private void OnNetworkDisconnected()
    {
        UpdateStatus("Network Disconnected");
        LogDebug("Network disconnected");
    }

    private void Update()
    {
        if (targetObject == null) return;

        // Transform ���� ����
        DetectTransformChanges();

        // �ڵ� ���� ó��
        if (autoSend && !isApplyingReceived)
        {
            if (hasTransformChanged || forceUpdateEveryFrame)
            {
                if (Time.time - lastSendTime >= sendInterval)
                {
                    SendCurrentTransform();
                    lastSendTime = Time.time;
                }
            }
        }

        // ����� ���� ������Ʈ
        UpdateDebugDisplay();
    }

    private void DetectTransformChanges()
    {
        if (targetObject == null) return;

        Vector3 currentPos = targetObject.position;
        Vector3 currentRot = targetObject.eulerAngles;
        Vector3 currentScale = targetObject.localScale;

        // ���� ���� (�� �ΰ��ϰ�)
        bool posChanged = Vector3.Distance(currentPos, lastPosition) > changeThreshold;
        bool rotChanged = Vector3.Distance(currentRot, lastRotation) > changeThreshold;
        bool scaleChanged = Vector3.Distance(currentScale, lastScale) > changeThreshold;

        if (posChanged || rotChanged || scaleChanged)
        {
            hasTransformChanged = true;

            if (enableDebugLogs)
            {
                if (posChanged) LogDebug($"[CHANGE DETECTED] Position: {lastPosition} �� {currentPos}");
                if (rotChanged) LogDebug($"[CHANGE DETECTED] Rotation: {lastRotation} �� {currentRot}");
                if (scaleChanged) LogDebug($"[CHANGE DETECTED] Scale: {lastScale} �� {currentScale}");
            }

            // ���� �� ����
            lastPosition = currentPos;
            lastRotation = currentRot;
            lastScale = currentScale;
        }
        else
        {
            hasTransformChanged = false;
        }
    }

    private void StoreCurrentTransform()
    {
        if (targetObject == null) return;

        lastPosition = targetObject.position;
        lastRotation = targetObject.eulerAngles;
        lastScale = targetObject.localScale;

        currentTransformData = new TransformData(targetObject);
        LogDebug($"[STORED] Current transform: {currentTransformData.ToString()}");
    }

    private void ApplyTransformToObject(TransformData data)
    {
        if (targetObject == null) return;

        isApplyingReceived = true;

        Vector3 oldPos = targetObject.position;
        Vector3 oldRot = targetObject.eulerAngles;
        Vector3 oldScale = targetObject.localScale;

        // �� �� ����
        targetObject.position = data.GetPosition();
        targetObject.eulerAngles = data.GetRotation();
        targetObject.localScale = data.GetScale();

        // ����� ���� ������Ʈ (�ߺ� ���� ����)
        StoreCurrentTransform();

        LogDebug($"[APPLIED CSV] Transform changed:");
        LogDebug($"  Position: {oldPos} �� {targetObject.position}");
        LogDebug($"  Rotation: {oldRot} �� {targetObject.eulerAngles}");
        LogDebug($"  Scale: {oldScale} �� {targetObject.localScale}");

        UpdateStatus($"Applied from Scene2: {data.ToString()}");

        // ª�� ���� �� �÷��� ����
        Invoke(nameof(ResetApplyingFlag), 0.2f);
    }

    private void ResetApplyingFlag()
    {
        isApplyingReceived = false;
        LogDebug("[RESET] Apply flag reset - Auto send enabled again");
    }

    private void SendCurrentTransform()
    {
        if (targetObject == null) return;

        TransformData currentData = new TransformData(targetObject);

        // �����Ͱ� ������ ����Ǿ����� Ȯ��
        if (lastSentData == null || HasDataChanged(currentData, lastSentData))
        {
            LogDebug($"[SENDING] Current transform to Scene2: {currentData.ToString()}");
            SendTransformData(currentData);
            hasTransformChanged = false;  // ���� �� �÷��� ����
        }
        else
        {
            LogDebug("[SKIPPED] No significant changes detected");
        }
    }

    public void ForceSendCurrent()
    {
        if (targetObject == null) return;

        TransformData currentData = new TransformData(targetObject);
        LogDebug("[FORCE SEND] Force sending current transform to Scene2");
        SendTransformData(currentData);
        hasTransformChanged = false;
    }

    private void SendTransformData(TransformData data)
    {
        TransformMessage message = new TransformMessage(data, SENDER_ID);
        string csvData = message.ToCSV();

        if (udpManager != null)
        {
            udpManager.SendData(csvData);
            lastSentData = data;
            UpdateStatus($"Sent CSV to Scene2: {data.ToString()}");
            LogDebug($"[SENT CSV] {csvData}");
        }
        else
        {
            LogError("UDP Manager is null!");
        }
    }

    private bool HasDataChanged(TransformData current, TransformData last)
    {
        return Vector3.Distance(current.GetPosition(), last.GetPosition()) > changeThreshold ||
               Vector3.Distance(current.GetRotation(), last.GetRotation()) > changeThreshold ||
               Vector3.Distance(current.GetScale(), last.GetScale()) > changeThreshold;
    }

    // �׽�Ʈ �޼����
    private void TestMoveObject()
    {
        if (targetObject == null) return;

        Vector3 randomPos = new Vector3(
            UnityEngine.Random.Range(-10f, 10f),
            UnityEngine.Random.Range(-5f, 5f),
            UnityEngine.Random.Range(-10f, 10f)
        );

        targetObject.position = randomPos;
        LogDebug($"[TEST] Moved object to: {randomPos}");

        // �׽�Ʈ �ÿ��� ��� ����
        ForceSendCurrent();
    }

    private void SetAutoSend(bool enabled)
    {
        autoSend = enabled;
        if (sendCurrentButton != null) sendCurrentButton.interactable = !enabled;
        LogDebug($"[SETTING] Auto send: {enabled}");
    }

    private void UpdateTargetIP()
    {
        if (ipInputField != null && udpManager != null)
        {
            udpManager.UpdateTargetIP(ipInputField.text);
            UpdateStatus($"Target IP updated to: {ipInputField.text}");
        }
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private void UpdateDebugDisplay()
    {
        if (debugText != null && udpManager != null)
        {
            string changeStatus = hasTransformChanged ? "CHANGED" : "STABLE";
            debugText.text = $"Auto: {autoSend} | Status: {changeStatus} | {udpManager.GetStatusInfo()}";
        }
    }

    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[SCENE1-CONTROLLER] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[SCENE1-CONTROLLER] {message}");
    }

    private void OnDestroy()
    {
        if (udpManager != null)
        {
            udpManager.OnDataReceived -= OnDataReceived;
            udpManager.OnNetworkError -= OnNetworkError;
            udpManager.OnNetworkConnected -= OnNetworkConnected;
            udpManager.OnNetworkDisconnected -= OnNetworkDisconnected;
        }
    }

    // ���ؽ�Ʈ �޴�
    [ContextMenu("Force Send Current")]
    public void ContextForceSend()
    {
        ForceSendCurrent();
    }

    [ContextMenu("Test Move Object")]
    public void ContextTestMove()
    {
        TestMoveObject();
    }

    [ContextMenu("Enable Force Update")]
    public void EnableForceUpdate()
    {
        forceUpdateEveryFrame = true;
        LogDebug("Force update every frame enabled");
    }

    [ContextMenu("Disable Force Update")]
    public void DisableForceUpdate()
    {
        forceUpdateEveryFrame = false;
        LogDebug("Force update every frame disabled");
    }
}