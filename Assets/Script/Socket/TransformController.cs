using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TransformController : MonoBehaviour
{
    [Header("Network Settings")]
    public string targetIP = "10.244.88.65";
    public int sendPort = 12222;
    public int receivePort = 12223;

    [Header("Transform Settings")]
    public Transform targetObject;
    public float sendRate = 10f;  // �� ���� �����ϵ��� ����
    public bool autoSend = true;

    [Header("UI")]
    public TMP_Text statusText;
    public Button testButton;
    public Button moveObjectButton;  // ������Ʈ �̵� �׽�Ʈ ��ư
    public Button sendCurrentButton; // ���� ���� ���� ��ư
    public TMP_InputField ipInputField;
    public Button connectButton;
    public Toggle autoSendToggle;

    [Header("Debug")]
    public bool enableDebugLogs = true;
    public TMP_Text debugText;

    // Network
    private UdpClient udpClient;
    private IPEndPoint sendEndPoint;
    private Thread receiveThread;
    private bool isReceiving = true;

    // Transform sync
    private const string SENDER_ID = "SCENE1_CONTROLLER";
    private TransformMessage lastReceivedMessage;
    private bool hasNewMessage = false;
    private bool isApplyingReceived = false;

    // Auto send
    private TransformData lastSentData;
    private float sendInterval;
    private float lastSendTime;
    private int sentMessageCount = 0;
    private int receivedMessageCount = 0;

    private void Start()
    {
        InitializeComponents();
        InitializeNetwork();

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

        if (ipInputField != null)
        {
            ipInputField.text = targetIP;
        }

        // ��ư �̺�Ʈ ����
        if (testButton != null)
        {
            testButton.onClick.AddListener(SendCurrentTransform);
        }

        if (moveObjectButton != null)
        {
            moveObjectButton.onClick.AddListener(TestMoveObject);
        }

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

    private void InitializeNetwork()
    {
        try
        {
            sendEndPoint = new IPEndPoint(IPAddress.Parse(targetIP), sendPort);
            udpClient = new UdpClient(receivePort);

            StartReceiveThread();
            UpdateStatus("Network initialized - Ready for sync");

            LogDebug($"Network initialized - Target: {targetIP}:{sendPort}, Listen: {receivePort}");
        }
        catch (Exception e)
        {
            UpdateStatus($"Network Error: {e.Message}");
            LogError($"Network initialization failed: {e.Message}");
        }
    }

    private void StartReceiveThread()
    {
        receiveThread = new Thread(ReceiveData);
        receiveThread.IsBackground = true;
        receiveThread.Start();

        LogDebug("Receive thread started - Waiting for data from Scene2...");
    }

    private void ReceiveData()
    {
        while (isReceiving)
        {
            try
            {
                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udpClient.Receive(ref remoteEndPoint);
                string jsonData = Encoding.UTF8.GetString(data);

                ProcessReceivedData(jsonData);
            }
            catch (Exception e)
            {
                if (isReceiving)
                {
                    LogError($"Receive failed: {e.Message}");
                }
            }
        }
    }

    private void ProcessReceivedData(string jsonData)
    {
        try
        {
            receivedMessageCount++;
            LogDebug($"[RECEIVED #{receivedMessageCount}] JSON: {jsonData}");

            TransformMessage message = TransformMessage.FromJson(jsonData);

            if (message == null)
            {
                LogError("Message is null after parsing");
                return;
            }

            if (message.transformData == null)
            {
                LogError("Transform data is null");
                return;
            }

            // Scene2���� �� �޽����� ó��
            if (message.senderType == "SCENE2_UI")
            {
                lock (this)
                {
                    lastReceivedMessage = message;
                    hasNewMessage = true;
                }

                LogDebug($"[VALID MESSAGE] From Scene2: {message.transformData.ToString()}");
            }
            else
            {
                LogDebug($"[IGNORED] Message from: {message.senderType}");
            }
        }
        catch (Exception e)
        {
            LogError($"JSON Parse Error: {e.Message}");
            LogError($"Raw JSON: {jsonData}");
        }
    }

    private void Update()
    {
        // ���� �����͸� Target Object�� ����
        ProcessReceivedMessages();

        // �ڵ� ���� (������Ʈ ���� ����)
        if (autoSend && !isApplyingReceived && Time.time - lastSendTime >= sendInterval)
        {
            CheckAndSendTransform();
            lastSendTime = Time.time;
        }

        // ����� ���� ������Ʈ
        UpdateDebugDisplay();
    }

    private void ProcessReceivedMessages()
    {
        if (hasNewMessage && lastReceivedMessage != null)
        {
            lock (this)
            {
                LogDebug("[APPLYING] Received data to target object...");
                ApplyTransformToObject(lastReceivedMessage.transformData);
                hasNewMessage = false;
            }
        }
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

        LogDebug($"[APPLIED] Transform changed:");
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

    private void CheckAndSendTransform()
    {
        if (targetObject == null || isApplyingReceived) return;

        TransformData currentData = new TransformData(targetObject);

        // �����Ͱ� ����� ��쿡�� ����
        if (lastSentData == null || HasDataChanged(currentData, lastSentData))
        {
            LogDebug("[AUTO SEND] Object transform changed - Sending to Scene2");
            SendTransformData(currentData);
        }
    }

    private void SendCurrentTransform()
    {
        if (targetObject == null) return;

        TransformData currentData = new TransformData(targetObject);
        LogDebug("[MANUAL SEND] Sending current transform to Scene2");
        SendTransformData(currentData);
    }

    private void ForceSendCurrent()
    {
        if (targetObject == null) return;

        TransformData currentData = new TransformData(targetObject);
        LogDebug("[FORCE SEND] Force sending current transform to Scene2");
        SendTransformData(currentData);
    }

    private void SendTransformData(TransformData data)
    {
        TransformMessage message = new TransformMessage(data, SENDER_ID);
        SendMessage(message);
        lastSentData = data;
    }

    private bool HasDataChanged(TransformData current, TransformData last)
    {
        float threshold = 0.01f;
        bool changed = Vector3.Distance(current.GetPosition(), last.GetPosition()) > threshold ||
                      Vector3.Distance(current.GetRotation(), last.GetRotation()) > threshold ||
                      Vector3.Distance(current.GetScale(), last.GetScale()) > threshold;

        if (changed)
        {
            LogDebug($"[CHANGE DETECTED] Transform changed beyond threshold ({threshold})");
        }

        return changed;
    }

    private void SendMessage(TransformMessage message)
    {
        try
        {
            string jsonData = message.ToJson();
            sentMessageCount++;

            LogDebug($"[SENDING #{sentMessageCount}] To Scene2 ({targetIP}:{sendPort}):");
            LogDebug($"[SENDING JSON] {jsonData}");

            byte[] bytes = Encoding.UTF8.GetBytes(jsonData);
            udpClient.Send(bytes, bytes.Length, sendEndPoint);

            LogDebug($"[SENT] {bytes.Length} bytes successfully sent");
            UpdateStatus($"Sent to Scene2: {message.transformData.ToString()}");
        }
        catch (Exception e)
        {
            LogError($"Send failed: {e.Message}");
        }
    }

    // �׽�Ʈ �޼����
    private void TestMoveObject()
    {
        if (targetObject == null) return;

        // ���� ��ġ�� �̵�
        Vector3 randomPos = new Vector3(
            UnityEngine.Random.Range(-10f, 10f),
            UnityEngine.Random.Range(-5f, 5f),
            UnityEngine.Random.Range(-10f, 10f)
        );

        targetObject.position = randomPos;
        LogDebug($"[TEST] Moved object to: {randomPos}");

        // ��� ����
        ForceSendCurrent();
    }

    private void SetAutoSend(bool enabled)
    {
        autoSend = enabled;
        LogDebug($"[SETTING] Auto send: {enabled}");
    }

    private void UpdateTargetIP()
    {
        if (ipInputField != null)
        {
            targetIP = ipInputField.text;
            sendEndPoint = new IPEndPoint(IPAddress.Parse(targetIP), sendPort);
            UpdateStatus($"Target IP updated to: {targetIP}");
            LogDebug($"Target IP updated to: {targetIP}");
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
        if (debugText != null)
        {
            debugText.text = $"Sent: {sentMessageCount} | Received: {receivedMessageCount} | Auto: {autoSend}";
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

    private void OnApplicationQuit()
    {
        isReceiving = false;
        receiveThread?.Join(1000);
        udpClient?.Close();

        LogDebug("Application quit - Network closed");
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

    [ContextMenu("Print Network Status")]
    public void PrintNetworkStatus()
    {
        LogDebug($"=== SCENE1 NETWORK STATUS ===");
        LogDebug($"Target: {targetIP}:{sendPort}");
        LogDebug($"Listen: {receivePort}");
        LogDebug($"Auto Send: {autoSend}");
        LogDebug($"Send Rate: {sendRate}");
        LogDebug($"Sent Messages: {sentMessageCount}");
        LogDebug($"Received Messages: {receivedMessageCount}");
        LogDebug($"Target Object: {(targetObject != null ? targetObject.name : "NULL")}");
        LogDebug($"==============================");
    }
}