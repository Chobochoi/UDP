using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class UDPReceiver : MonoBehaviour
{
    private UdpClient udpClient;
    private Thread receiveThread;
    private bool isReceiving = false;
    private System.Action<TransformData> onTransformReceived;

    // ���� �����忡�� ������ �׼ǵ��� ����
    private readonly object lockObject = new object();
    private TransformData pendingData = null;

    public void Initialize(int port, System.Action<TransformData> callback)
    {
        try
        {
            onTransformReceived = callback;
            udpClient = new UdpClient(port);
            isReceiving = true;

            receiveThread = new Thread(ReceiveData);
            receiveThread.Start();

            Debug.Log($"UDP Receiver initialized on port {port}");
        }
        catch (Exception e)
        {
            Debug.LogError($"UDP Receiver initialization failed: {e.Message}");
        }
    }

    void ReceiveData()
    {
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

        while (isReceiving)
        {
            try
            {
                byte[] receivedBytes = udpClient.Receive(ref remoteEndPoint);
                string receivedText = Encoding.UTF8.GetString(receivedBytes);

                // JSON �����͸� �Ľ�
                TransformData data = JsonUtility.FromJson<TransformData>(receivedText);

                // ���� �����忡�� �����ϱ� ���� ����
                lock (lockObject)
                {
                    pendingData = data;
                }

                Debug.Log($"Received data from {remoteEndPoint}: Pos({data.posX}, {data.posY}, {data.posZ})");
            }
            catch (Exception e)
            {
                if (isReceiving)
                {
                    Debug.LogError($"Error receiving UDP data: {e.Message}");
                }
            }
        }
    }

    void Update()
    {
        // ���� �����忡�� ���� ������ ó��
        lock (lockObject)
        {
            if (pendingData != null)
            {
                onTransformReceived?.Invoke(pendingData);
                pendingData = null;
            }
        }
    }

    public void Close()
    {
        isReceiving = false;

        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Join(1000);
        }

        udpClient?.Close();
        Debug.Log("UDP Receiver closed");
    }

    void OnDestroy()
    {
        Close();
    }
}