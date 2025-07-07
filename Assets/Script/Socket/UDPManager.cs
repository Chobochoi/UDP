using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UDPManager : MonoBehaviour
{
    [Header("Network Settings")]
    public string targetIP = "127.0.0.1";
    public int sendPort = 12222;
    public int receivePort = 12223;

    [Header("UI Controls")]
    public TMP_InputField targetIPInput;
    public TMP_InputField sendPortInput;
    public TMP_InputField receivePortInput;
    public Button connectButton;
    public Button disconnectButton;
    public TMP_Text statusText;

    [Header("Debug")]
    public bool enableDebugLogs = true;

    // Network components
    private UdpClient udpClient;
    private IPEndPoint sendEndPoint;
    private Thread receiveThread;
    private bool isReceiving = false;

    // Events
    public event Action<string> OnDataReceived;
    public event Action<string> OnNetworkError;
    public event Action OnNetworkConnected;
    public event Action OnNetworkDisconnected;

    // Statistics
    private int sentMessageCount = 0;
    private int receivedMessageCount = 0;
    private int errorCount = 0;

    public int SentMessageCount => sentMessageCount;
    public int ReceivedMessageCount => receivedMessageCount;
    public int ErrorCount => errorCount;
    public bool IsConnected => udpClient != null && isReceiving;

    private void Start()
    {
        InitializeUI();
        UpdateConnectionStatus();
    }

    private void InitializeUI()
    {
        // InputField �ʱⰪ ����
        if (targetIPInput != null)
        {
            targetIPInput.text = targetIP;
        }

        if (sendPortInput != null)
        {
            sendPortInput.text = sendPort.ToString();
        }

        if (receivePortInput != null)
        {
            receivePortInput.text = receivePort.ToString();
        }

        // ��ư �̺�Ʈ ����
        if (connectButton != null)
        {
            connectButton.onClick.AddListener(ConnectFromUI);
        }

        if (disconnectButton != null)
        {
            disconnectButton.onClick.AddListener(Disconnect);
        }

        LogDebug("UI initialized - Ready for manual connection");
    }

    public void ConnectFromUI()
    {
        try
        {
            // UI���� �� ��������
            if (targetIPInput != null)
            {
                targetIP = targetIPInput.text;
            }

            if (sendPortInput != null && int.TryParse(sendPortInput.text, out int newSendPort))
            {
                sendPort = newSendPort;
            }

            if (receivePortInput != null && int.TryParse(receivePortInput.text, out int newReceivePort))
            {
                receivePort = newReceivePort;
            }

            LogDebug($"Attempting to connect - Target: {targetIP}:{sendPort}, Listen: {receivePort}");

            // ���� ������ ������ ����
            if (IsConnected)
            {
                Disconnect();
            }

            // ���ο� ���� �õ�
            InitializeNetwork();
        }
        catch (Exception e)
        {
            string errorMsg = $"Connection failed: {e.Message}";
            LogError(errorMsg);
            OnNetworkError?.Invoke(errorMsg);
            UpdateConnectionStatus();
        }
    }

    public void InitializeNetwork()
    {
        try
        {
            // IP �ּ� ��ȿ�� �˻�
            if (!IPAddress.TryParse(targetIP, out IPAddress address))
            {
                throw new Exception($"Invalid IP address: {targetIP}");
            }

            // ��Ʈ ��ȿ�� �˻�
            if (sendPort < 1 || sendPort > 65535)
            {
                throw new Exception($"Invalid send port: {sendPort}");
            }

            if (receivePort < 1 || receivePort > 65535)
            {
                throw new Exception($"Invalid receive port: {receivePort}");
            }

            // Send endpoint ����
            sendEndPoint = new IPEndPoint(address, sendPort);

            // UDP Ŭ���̾�Ʈ ���� �� ���ε�
            udpClient = new UdpClient(receivePort);

            // ���� �÷��� ����
            isReceiving = true;

            // ���� ������ ����
            StartReceiveThread();

            OnNetworkConnected?.Invoke();
            UpdateConnectionStatus();
            LogDebug($"Network connected - Send to: {targetIP}:{sendPort}, Listen on: {receivePort}");
        }
        catch (Exception e)
        {
            errorCount++;
            string errorMsg = $"Network initialization failed: {e.Message}";
            LogError(errorMsg);
            OnNetworkError?.Invoke(errorMsg);
            UpdateConnectionStatus();
        }
    }

    private void StartReceiveThread()
    {
        receiveThread = new Thread(ReceiveData);
        receiveThread.IsBackground = true;
        receiveThread.Start();

        LogDebug("Receive thread started");
    }

    private void ReceiveData()
    {
        while (isReceiving)
        {
            try
            {
                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udpClient.Receive(ref remoteEndPoint);
                string receivedData = Encoding.UTF8.GetString(data);

                receivedMessageCount++;
                LogDebug($"[RECEIVED #{receivedMessageCount}] From {remoteEndPoint}: {receivedData}");

                // ���� �����忡�� �̺�Ʈ ȣ��
                UnityMainThreadDispatcher.Instance.Enqueue(() => {
                    OnDataReceived?.Invoke(receivedData);
                });
            }
            catch (Exception e)
            {
                if (isReceiving)
                {
                    errorCount++;
                    string errorMsg = $"Receive failed: {e.Message}";
                    LogError(errorMsg);

                    UnityMainThreadDispatcher.Instance.Enqueue(() => {
                        OnNetworkError?.Invoke(errorMsg);
                    });
                }
            }
        }
    }

    public void SendData(string data)
    {
        if (udpClient == null || sendEndPoint == null)
        {
            string errorMsg = "Network not connected";
            LogError(errorMsg);
            OnNetworkError?.Invoke(errorMsg);
            return;
        }

        try
        {
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            udpClient.Send(bytes, bytes.Length, sendEndPoint);

            sentMessageCount++;
            LogDebug($"[SENT #{sentMessageCount}] To {targetIP}:{sendPort}: {data}");
        }
        catch (Exception e)
        {
            errorCount++;
            string errorMsg = $"Send failed: {e.Message}";
            LogError(errorMsg);
            OnNetworkError?.Invoke(errorMsg);
        }
    }

    public void UpdateTargetIP(string newIP)
    {
        targetIP = newIP;
        if (sendEndPoint != null)
        {
            sendEndPoint = new IPEndPoint(IPAddress.Parse(targetIP), sendPort);
        }
        LogDebug($"Target IP updated to: {targetIP}");
    }

    public void UpdateSendPort(int newPort)
    {
        sendPort = newPort;
        if (sendEndPoint != null)
        {
            sendEndPoint = new IPEndPoint(IPAddress.Parse(targetIP), sendPort);
        }
        LogDebug($"Send port updated to: {sendPort}");
    }

    public void Disconnect()
    {
        isReceiving = false;

        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Join(1000);
        }

        if (udpClient != null)
        {
            udpClient.Close();
            udpClient = null;
        }

        sendEndPoint = null;

        OnNetworkDisconnected?.Invoke();
        UpdateConnectionStatus();
        LogDebug("Network disconnected");
    }

    public void Reconnect()
    {
        Disconnect();
        ConnectFromUI();
    }

    private void UpdateConnectionStatus()
    {
        if (statusText != null)
        {
            if (IsConnected)
            {
                statusText.text = $"Connected - Send: {targetIP}:{sendPort}, Listen: {receivePort}";
                statusText.color = Color.green;
            }
            else
            {
                statusText.text = "Disconnected";
                statusText.color = Color.red;
            }
        }

        // ��ư ���� ������Ʈ
        if (connectButton != null)
        {
            connectButton.interactable = !IsConnected;
        }

        if (disconnectButton != null)
        {
            disconnectButton.interactable = IsConnected;
        }
    }

    private void Update()
    {
        // ���� ���� ������Ʈ (�� �������� �ƴϰ� ������)
        if (Time.frameCount % 60 == 0) // 1�ʸ���
        {
            UpdateConnectionStatus();
        }
    }

    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[UDPManager] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[UDPManager] {message}");
    }

    private void OnApplicationQuit()
    {
        Disconnect();
    }

    private void OnDestroy()
    {
        Disconnect();
    }

    // ���� ���� ���
    public string GetStatusInfo()
    {
        return $"Connected: {IsConnected}, Sent: {sentMessageCount}, Received: {receivedMessageCount}, Errors: {errorCount}";
    }

    // ���ؽ�Ʈ �޴�
    [ContextMenu("Test Connection")]
    public void TestConnection()
    {
        ConnectFromUI();
    }

    [ContextMenu("Force Disconnect")]
    public void ForceDisconnect()
    {
        Disconnect();
    }

    [ContextMenu("Print Network Info")]
    public void PrintNetworkInfo()
    {
        LogDebug($"=== NETWORK STATUS ===");
        LogDebug($"Target IP: {targetIP}");
        LogDebug($"Send Port: {sendPort}");
        LogDebug($"Receive Port: {receivePort}");
        LogDebug($"Connected: {IsConnected}");
        LogDebug($"Sent Messages: {sentMessageCount}");
        LogDebug($"Received Messages: {receivedMessageCount}");
        LogDebug($"Errors: {errorCount}");
        LogDebug($"======================");
    }
}