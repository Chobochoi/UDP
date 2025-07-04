using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TransformUIReceiver : MonoBehaviour
{
    [Header("Network")]
    public UDPManager udpManager;

    [Header("UI Controller")]
    public TransformUIController uiController;

    [Header("Status UI")]
    public TMP_Text statusText;
    public Button connectButton;
    public TMP_InputField ipInputField;

    [Header("Debug Settings")]
    public bool enableDebugLogs = true;
    public TMP_Text debugLogText;

    // Message handling
    private const string SENDER_ID = "SCENE2_UI";
    private int validMessageCount = 0;
    private int errorMessageCount = 0;

    private void Start()
    {
        InitializeComponents();
        SetupNetworkEvents();
    }

    private void InitializeComponents()
    {
        if (uiController != null)
        {
            uiController.SetReceiver(this);
        }

        if (udpManager == null)
        {
            //udpManager = FindObjectOfType<UDPManager>();
            udpManager = FindFirstObjectByType<UDPManager>();

            if (udpManager == null)
            {
                GameObject udpGO = new GameObject("UDPManager");
                udpManager = udpGO.AddComponent<UDPManager>();
            }
        }

        if (ipInputField != null)
        {
            ipInputField.text = udpManager.targetIP;
        }

        if (connectButton != null)
        {
            connectButton.onClick.AddListener(UpdateTargetIP);
        }

        LogDebug("UI Components initialized");
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

            if (message != null && message.transformData != null)
            {
                if (message.senderType == "SCENE1_CONTROLLER")
                {
                    validMessageCount++;

                    LogDebug($"[VALID CSV MESSAGE] From Scene1: {message.transformData.ToString()}");

                    if (uiController != null)
                    {
                        uiController.UpdateUIFromNetwork(message.transformData);
                    }

                    UpdateStatus($"Applied CSV: {message.transformData.ToString()}");
                }
                else
                {
                    LogDebug($"[IGNORED] Message from: {message.senderType}");
                }
            }
            else
            {
                errorMessageCount++;
                LogError("Invalid message received");
            }
        }
        catch (System.Exception e)
        {
            errorMessageCount++;
            LogError($"Error processing received data: {e.Message}");
        }

        UpdateDebugDisplay();
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

    // TransformUIController에서 호출하는 메서드
    public void SendUIData(TransformData data)
    {
        if (udpManager == null)
        {
            LogError("UDP Manager is null!");
            return;
        }

        try
        {
            TransformMessage message = new TransformMessage(data, SENDER_ID);
            string csvData = message.ToCSV();

            LogDebug($"[SENDING CSV] {csvData}");

            udpManager.SendData(csvData);
            UpdateStatus($"Sent CSV: {data.ToString()}");
        }
        catch (System.Exception e)
        {
            LogError($"Send failed: {e.Message}");
        }
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
        if (debugLogText != null && udpManager != null)
        {
            debugLogText.text = $"Valid: {validMessageCount} | Errors: {errorMessageCount} | {udpManager.GetStatusInfo()}";
        }
    }

    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[SCENE2-CSV-RECEIVER] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[SCENE2-CSV-RECEIVER] {message}");
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
}