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
    public int sendPort = 9999;
    public int receivePort = 9998;

    protected UdpClient udpClient;
    protected IPEndPoint sendEndPoint;
    protected Thread receiveThread;
    protected bool isReceiving = true;

    protected virtual void Start()
    {
        InitializeUDP();
        StartReceiveThread();
    }

    protected virtual void InitializeUDP()
    {
        try
        {
            sendEndPoint = new IPEndPoint(IPAddress.Parse(targetIP), sendPort);
            udpClient = new UdpClient(receivePort);

            Debug.Log($"UDP initialized - Send: {sendPort}, Receive: {receivePort}");
        }
        catch (Exception e)
        {
            Debug.LogError($"UDP initialization failed: {e.Message}");
        }
    }

    protected void StartReceiveThread()
    {
        receiveThread = new Thread(ReceiveData);
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    protected void SendData(string data)
    {
        try
        {
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            udpClient.Send(bytes, bytes.Length, sendEndPoint);
        }
        catch (Exception e)
        {
            Debug.LogError($"Send failed: {e.Message}");
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
                string jsonData = Encoding.UTF8.GetString(data);

                OnDataReceived(jsonData);
            }
            catch (Exception e)
            {
                if (isReceiving)
                {
                    Debug.LogError($"Receive failed: {e.Message}");
                }
            }
        }
    }

    // abstract���� virtual�� �����ϰ� �⺻ ���� ����
    protected virtual void OnDataReceived(string jsonData)
    {
        Debug.Log($"Data received: {jsonData}");
        // �⺻ ���� - ���� Ŭ�������� �������̵� ����
    }

    protected virtual void OnApplicationQuit()
    {
        isReceiving = false;
        receiveThread?.Join(1000);
        udpClient?.Close();
    }
}