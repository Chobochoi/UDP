using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SimpleUDPSender : MonoBehaviour
{
    [Header("Network Settings")]
    public string targetIP = "10.244.88.65";
    public int sendPort = 12222;
    public int receivePort = 12223;

    [Header("UI")]
    public Button sendButton;
    public TMP_Text statusText;
    public TMP_InputField messageInput;

    private UdpClient udpClient;
    private IPEndPoint sendEndPoint;

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

            UpdateStatus($"Network initialized - Sending to {targetIP}:{sendPort}");
            Debug.Log($"[SENDER] Network initialized - Target: {targetIP}:{sendPort}, Listen: {receivePort}");
        }
        catch (Exception e)
        {
            UpdateStatus($"Network Error: {e.Message}");
            Debug.LogError($"[SENDER] Network initialization failed: {e.Message}");
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
            messageInput.text = "Hello from Scene1!";
        }
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
            string message = messageInput != null ? messageInput.text : "Test Message";
            byte[] data = Encoding.UTF8.GetBytes(message);

            int sentBytes = udpClient.Send(data, data.Length, sendEndPoint);

            UpdateStatus($"Sent {sentBytes} bytes: {message}");
            Debug.Log($"[SENDER] Sent {sentBytes} bytes to {targetIP}:{sendPort} - Message: {message}");
        }
        catch (Exception e)
        {
            UpdateStatus($"Send Error: {e.Message}");
            Debug.LogError($"[SENDER] Send failed: {e.Message}");
        }
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log($"[SENDER STATUS] {message}");
    }

    private void OnApplicationQuit()
    {
        udpClient?.Close();
    }

    // 테스트용 - 1초마다 자동 전송
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SendTestMessage();
        }
    }
}