using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TransformUIReceiver : MonoBehaviour
{
    [Header("Network Settings")]
    public string targetIP = "10.244.88.252";   // Scene1 IP
    public int sendPort = 12223;                // Scene1로 전송할 포트
    public int receivePort = 12222;             // Scene1에서 받을 포트

    [Header("UI Controller")]
    public TransformUIController uiController;

    [Header("Status UI")]
    public TMP_Text statusText;
    public Button connectButton;
    public TMP_InputField ipInputField;

    [Header("Debug Settings")]
    public bool enableDebugLogs = true;
    public TMP_Text debugLogText;  // 실시간 디버그 로그 표시용

    // Network
    private UdpClient udpClient;
    private IPEndPoint sendEndPoint;
    private Thread receiveThread;
    private bool isReceiving = true;

    // Message handling
    private const string SENDER_ID = "SCENE2_UI";
    private TransformMessage lastReceivedMessage;
    private bool hasNewMessage = false;

    // Debug
    private int receivedMessageCount = 0;
    private int validMessageCount = 0;
    private int errorMessageCount = 0;

    private void Start()
    {
        InitializeComponents();
        InitializeNetwork();
    }

    private void InitializeComponents()
    {
        if (uiController != null)
        {
            uiController.SetReceiver(this);
        }

        if (ipInputField != null)
        {
            ipInputField.text = targetIP;
        }

        if (connectButton != null)
        {
            connectButton.onClick.AddListener(UpdateTargetIP);
        }

        LogDebug("UI Components initialized");
    }

    private void InitializeNetwork()
    {
        try
        {
            sendEndPoint = new IPEndPoint(IPAddress.Parse(targetIP), sendPort);
            udpClient = new UdpClient(receivePort);

            StartReceiveThread();
            UpdateStatus("Network initialized - Ready to control");

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

        LogDebug("Receive thread started - Waiting for data from Scene1...");
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

                LogDebug($"Raw data received from {remoteEndPoint} - Size: {data.Length} bytes");
                ProcessReceivedData(jsonData, remoteEndPoint);
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

    private void ProcessReceivedData(string jsonData, IPEndPoint senderEndPoint)
    {
        receivedMessageCount++;

        try
        {
            LogDebug($"[MESSAGE #{receivedMessageCount}] Processing data from {senderEndPoint}:");
            LogDebug($"[JSON RAW] {jsonData}");

            TransformMessage message = TransformMessage.FromJson(jsonData);

            if (message == null)
            {
                errorMessageCount++;
                LogError("[PARSE ERROR] Message is null after JSON parsing");
                return;
            }

            if (message.transformData == null)
            {
                errorMessageCount++;
                LogError("[PARSE ERROR] Transform data is null in message");
                return;
            }

            LogDebug($"[PARSED] Sender: {message.senderType}, Timestamp: {message.timestamp}");
            LogDebug($"[TRANSFORM DATA] {message.transformData.ToString()}");

            // Scene1에서 온 메시지만 처리
            if (message.senderType == "SCENE1_CONTROLLER")
            {
                validMessageCount++;

                lock (this)
                {
                    lastReceivedMessage = message;
                    hasNewMessage = true;
                }

                LogDebug($"[VALID MESSAGE] From Scene1 - Total valid: {validMessageCount}");
                LogDebug($"[POSITION] X:{message.transformData.posX:F2}, Y:{message.transformData.posY:F2}, Z:{message.transformData.posZ:F2}");
                LogDebug($"[ROTATION] X:{message.transformData.rotX:F2}, Y:{message.transformData.rotY:F2}, Z:{message.transformData.rotZ:F2}");
                LogDebug($"[SCALE] X:{message.transformData.scaleX:F2}, Y:{message.transformData.scaleY:F2}, Z:{message.transformData.scaleZ:F2}");
            }
            else
            {
                LogDebug($"[IGNORED] Message from: {message.senderType} (not Scene1)");
            }
        }
        catch (Exception e)
        {
            errorMessageCount++;
            LogError($"[JSON PARSE ERROR] {e.Message}");
            LogError($"[RAW JSON] {jsonData}");
            LogError($"[SENDER] {senderEndPoint}");
        }
    }

    private void Update()
    {
        // 받은 데이터를 UI에 반영
        if (hasNewMessage && lastReceivedMessage != null)
        {
            lock (this)
            {
                LogDebug("[UI UPDATE] Applying received data to UI...");

                if (uiController != null)
                {
                    LogDebug("[UI UPDATE] Calling uiController.UpdateUIFromNetwork()");
                    uiController.UpdateUIFromNetwork(lastReceivedMessage.transformData);
                    LogDebug("[UI UPDATE] UI successfully updated");
                }
                else
                {
                    LogError("[UI UPDATE] uiController is null!");
                }

                string statusMsg = $"Applied: {lastReceivedMessage.transformData.ToString()}";
                UpdateStatus(statusMsg);
                UpdateDebugDisplay();

                hasNewMessage = false;
                LogDebug("[UI UPDATE] Update cycle completed");
            }
        }
    }

    // TransformUIController에서 호출하는 메서드
    public void SendUIData(TransformData data)
    {
        if (udpClient == null || sendEndPoint == null)
        {
            LogError("Network not initialized for sending!");
            return;
        }

        try
        {
            TransformMessage message = new TransformMessage(data, SENDER_ID);
            string jsonData = message.ToJson();

            LogDebug($"[SENDING] To Scene1 ({targetIP}:{sendPort}):");
            LogDebug($"[SENDING JSON] {jsonData}");

            byte[] bytes = Encoding.UTF8.GetBytes(jsonData);
            udpClient.Send(bytes, bytes.Length, sendEndPoint);

            UpdateStatus($"Sent: {data.ToString()}");
            LogDebug($"[SENT] {bytes.Length} bytes successfully sent to Scene1");
        }
        catch (Exception e)
        {
            LogError($"Send failed: {e.Message}");
        }
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
        if (debugLogText != null)
        {
            debugLogText.text = $"Received: {receivedMessageCount} | Valid: {validMessageCount} | Errors: {errorMessageCount}";
        }
    }

    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[SCENE2-RECEIVER] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[SCENE2-RECEIVER] {message}");
    }

    private void OnApplicationQuit()
    {
        isReceiving = false;
        receiveThread?.Join(1000);
        udpClient?.Close();

        LogDebug("Application quit - Network closed");
    }

    // 테스트 및 디버그용 메서드들
    [ContextMenu("Send Test Data")]
    public void SendTestData()
    {
        TransformData testData = new TransformData();
        testData.posX = 1f; testData.posY = 2f; testData.posZ = 3f;
        testData.rotX = 45f; testData.rotY = 90f; testData.rotZ = 0f;
        testData.scaleX = 1.5f; testData.scaleY = 1.5f; testData.scaleZ = 1.5f;

        LogDebug("[TEST] Sending test data to Scene1");
        SendUIData(testData);
    }

    [ContextMenu("Print Network Status")]
    public void PrintNetworkStatus()
    {
        LogDebug($"=== NETWORK STATUS ===");
        LogDebug($"Target IP: {targetIP}:{sendPort}");
        LogDebug($"Listen Port: {receivePort}");
        LogDebug($"UDP Client: {(udpClient != null ? "Connected" : "Null")}");
        LogDebug($"Receive Thread: {(receiveThread != null && receiveThread.IsAlive ? "Running" : "Stopped")}");
        LogDebug($"Messages - Total: {receivedMessageCount}, Valid: {validMessageCount}, Errors: {errorMessageCount}");
        LogDebug($"UI Controller: {(uiController != null ? "Connected" : "Null")}");
        LogDebug($"======================");
    }

    [ContextMenu("Clear Debug Counters")]
    public void ClearDebugCounters()
    {
        receivedMessageCount = 0;
        validMessageCount = 0;
        errorMessageCount = 0;
        UpdateDebugDisplay();
        LogDebug("Debug counters cleared");
    }
}