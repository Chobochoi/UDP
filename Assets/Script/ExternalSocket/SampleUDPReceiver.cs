using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SimpleUDPReceiver : MonoBehaviour
{
    [Header("Network Settings")]
    public string targetIP = "10.244.88.252";
    public int sendPort = 12223;
    public int receivePort = 12222;

    [Header("UI")]
    public Button sendButton;
    public TMP_Text statusText;
    public TMP_Text receivedText;
    public TMP_InputField messageInput;

    private UdpClient udpClient;
    private IPEndPoint sendEndPoint;
    private Thread receiveThread;
    private bool isReceiving = true;

    private string lastReceivedMessage = "";
    private bool hasNewMessage = false;

    private void Start()
    {
        InitializeNetwork();
        SetupUI();
    }

    private void InitializeNetwork()
    {
        try
        {
            // 송신용 설정
            sendEndPoint = new IPEndPoint(IPAddress.Parse(targetIP), sendPort);

            // 수신용 클라이언트 생성 (포트 바인딩)
            udpClient = new UdpClient(receivePort);

            // 수신 스레드 시작
            receiveThread = new Thread(ReceiveData);
            receiveThread.IsBackground = true;
            receiveThread.Start();

            UpdateStatus($"Network initialized - Listening on port {receivePort}");
            Debug.Log($"[RECEIVER] Network initialized - Listen: {receivePort}, Target: {targetIP}:{sendPort}");
        }
        catch (Exception e)
        {
            UpdateStatus($"Network Error: {e.Message}");
            Debug.LogError($"[RECEIVER] Network initialization failed: {e.Message}");
        }
    }

    private void SetupUI()
    {
        if (sendButton != null)
        {
            sendButton.onClick.AddListener(SendTestMessage);
        }

        if (messageInput != null)
        {
            messageInput.text = "Hello from Scene2!";
        }
    }

    private void ReceiveData()
    {
        while (isReceiving)
        {
            try
            {
                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udpClient.Receive(ref remoteEndPoint);
                string message = Encoding.UTF8.GetString(data);

                lock (this)
                {
                    lastReceivedMessage = $"From {remoteEndPoint}: {message}";
                    hasNewMessage = true;
                }

                Debug.Log($"[RECEIVER] Received from {remoteEndPoint}: {message}");
            }
            catch (Exception e)
            {
                if (isReceiving)
                {
                    Debug.LogError($"[RECEIVER] Receive error: {e.Message}");
                }
            }
        }
    }

    private void Update()
    {
        if (hasNewMessage)
        {
            lock (this)
            {
                if (receivedText != null)
                {
                    receivedText.text = lastReceivedMessage;
                }
                UpdateStatus($"Received: {lastReceivedMessage}");
                hasNewMessage = false;
            }
        }

        //if (Input.GetKeyDown(KeyCode.Space))
        //{
        //    SendTestMessage();
        //}
    }

    public void SendTestMessage()
    {
        if (udpClient == null || sendEndPoint == null)
        {
            UpdateStatus("Network not initialized!");
            return;
        }

        try
        {
            string message = messageInput != null ? messageInput.text : "Test Response";
            byte[] data = Encoding.UTF8.GetBytes(message);

            int sentBytes = udpClient.Send(data, data.Length, sendEndPoint);

            UpdateStatus($"Sent {sentBytes} bytes: {message}");
            Debug.Log($"[RECEIVER] Sent {sentBytes} bytes to {targetIP}:{sendPort} - Message: {message}");
        }
        catch (Exception e)
        {
            UpdateStatus($"Send Error: {e.Message}");
            Debug.LogError($"[RECEIVER] Send failed: {e.Message}");
        }
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log($"[RECEIVER STATUS] {message}");
    }

    private void OnApplicationQuit()
    {
        isReceiving = false;
        receiveThread?.Join(1000);
        udpClient?.Close();
    }
}