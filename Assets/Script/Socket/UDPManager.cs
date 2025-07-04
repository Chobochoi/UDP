using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class UDPManager : MonoBehaviour
{
    [Header("Network Settings")]
    public string targetIP = "127.0.0.1";
    public int sendPort = 12222;
    public int receivePort = 12223;

    [Header("Debug")]
    public bool enableDebugLogs = true;

    // Network components
    private UdpClient udpClient;
    private IPEndPoint sendEndPoint;
    private Thread receiveThread;
    private bool isReceiving = true;

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
    public bool IsConnected => udpClient != null;

    private void Start()
    {
        InitializeNetwork();
    }

    public void InitializeNetwork()
    {
        try
        {
            // Send endpoint 설정
            sendEndPoint = new IPEndPoint(IPAddress.Parse(targetIP), sendPort);

            // UDP 클라이언트 생성 및 바인딩
            udpClient = new UdpClient(receivePort);

            // 수신 스레드 시작
            StartReceiveThread();

            OnNetworkConnected?.Invoke();
            LogDebug($"Network initialized - Send to: {targetIP}:{sendPort}, Listen on: {receivePort}");
        }
        catch (Exception e)
        {
            errorCount++;
            string errorMsg = $"Network initialization failed: {e.Message}";
            LogError(errorMsg);
            OnNetworkError?.Invoke(errorMsg);
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

                // 메인 스레드에서 이벤트 호출
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
            string errorMsg = "Network not initialized";
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
        sendEndPoint = new IPEndPoint(IPAddress.Parse(targetIP), sendPort);
        LogDebug($"Target IP updated to: {targetIP}");
    }

    public void UpdateSendPort(int newPort)
    {
        sendPort = newPort;
        sendEndPoint = new IPEndPoint(IPAddress.Parse(targetIP), sendPort);
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

        OnNetworkDisconnected?.Invoke();
        LogDebug("Network disconnected");
    }

    public void Reconnect()
    {
        Disconnect();
        InitializeNetwork();
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

    // 상태 정보 출력
    public string GetStatusInfo()
    {
        return $"Connected: {IsConnected}, Sent: {sentMessageCount}, Received: {receivedMessageCount}, Errors: {errorCount}";
    }
}