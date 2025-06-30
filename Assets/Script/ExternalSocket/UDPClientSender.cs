using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class UDPClientSender : MonoBehaviour
{
    private UdpClient udpClient;
    private IPEndPoint targetEndPoint;
    private bool isInitialized = false;

    public void Initialize(string targetIP, int port)
    {
        try
        {
            udpClient = new UdpClient();
            targetEndPoint = new IPEndPoint(IPAddress.Parse(targetIP), port);
            isInitialized = true;

            Debug.Log($"UDP Client Sender initialized. Target: {targetIP}:{port}");
        }
        catch (Exception e)
        {
            Debug.LogError($"UDP Client Sender initialization failed: {e.Message}");
            isInitialized = false;
        }
    }

    public void SendTransform(Vector3 position, Vector3 eulerRotation)
    {
        if (!isInitialized)
        {
            Debug.LogError("UDP Client Sender not initialized!");
            return;
        }

        try
        {
            TransformData data = new TransformData
            {
                posX = position.x,
                posY = position.y,
                posZ = position.z,
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
            };

            // Euler 각도를 Quaternion으로 변환
            data.SetRotationFromEuler(eulerRotation);

            string jsonData = JsonUtility.ToJson(data);
            byte[] sendBytes = Encoding.UTF8.GetBytes(jsonData);

            udpClient.Send(sendBytes, sendBytes.Length, targetEndPoint);

            //Debug.Log($"Sent Transform - Pos: {position}, Rot: {eulerRotation}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to send transform data: {e.Message}");
        }
    }

    public void Close()
    {
        if (udpClient != null)
        {
            udpClient.Close();
            isInitialized = false;
            Debug.Log("UDP Client Sender closed");
        }
    }
}