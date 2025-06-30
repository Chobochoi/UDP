using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class UDPSender : MonoBehaviour
{
    private UdpClient udpClient;
    private IPEndPoint targetEndPoint;
    private string csvFilePath;

    public void Initialize(string targetIP, int port)
    {
        try
        {
            udpClient = new UdpClient();
            targetEndPoint = new IPEndPoint(IPAddress.Parse(targetIP), port);

            // CSV ���� ��θ� ���� ������ ����
            string fileName = $"TransformData_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            csvFilePath = GetBuildFolderPath(fileName);

            // CSV ��� �ۼ�
            WriteCSVHeader();

            Debug.Log($"UDP Sender initialized. Target: {targetIP}:{port}");
            Debug.Log($"CSV file path: {csvFilePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"UDP Sender initialization failed: {e.Message}");
        }
    }

    /// <summary>
    /// ���� ���� ��θ� �������� �Լ�
    /// </summary>
    string GetBuildFolderPath(string fileName)
    {
        string buildFolderPath;

#if UNITY_EDITOR
        // �����Ϳ����� ������Ʈ ��Ʈ ������ CSVData ������ ����
        buildFolderPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "CSVData");
#else
        // ����� ���¿����� ���� ������ �ִ� ������ ����
        buildFolderPath = Path.GetDirectoryName(Application.dataPath);
#endif

        // ������ ������ ����
        if (!Directory.Exists(buildFolderPath))
        {
            Directory.CreateDirectory(buildFolderPath);
            Debug.Log($"Created directory: {buildFolderPath}");
        }

        return Path.Combine(buildFolderPath, fileName);
    }

    public void SendTransformData(TransformData data)
    {
        try
        {
            string jsonData = JsonUtility.ToJson(data);
            byte[] sendBytes = Encoding.UTF8.GetBytes(jsonData);

            // UDP�� ����
            udpClient.Send(sendBytes, sendBytes.Length, targetEndPoint);

            // CSV ���Ͽ� ����
            SaveToCSV(data);

        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to send transform data: {e.Message}");
        }
    }

    void WriteCSVHeader()
    {
        try
        {
            string header = "Timestamp,PosX,PosY,PosZ,RotX,RotY,RotZ,RotW,EulerX,EulerY,EulerZ";
            File.WriteAllText(csvFilePath, header + Environment.NewLine);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to write CSV header: {e.Message}");
        }
    }

    void SaveToCSV(TransformData data)
    {
        try
        {
            // Euler ���� ��� (�б� ������)
            Quaternion quat = new Quaternion(data.rotX, data.rotY, data.rotZ, data.rotW);
            Vector3 euler = quat.eulerAngles;

            string csvLine = $"{data.timestamp}," +
                           $"{data.posX:F6},{data.posY:F6},{data.posZ:F6}," +
                           $"{data.rotX:F6},{data.rotY:F6},{data.rotZ:F6},{data.rotW:F6}," +
                           $"{euler.x:F2},{euler.y:F2},{euler.z:F2}";

            File.AppendAllText(csvFilePath, csvLine + Environment.NewLine);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save to CSV: {e.Message}");
        }
    }

    public void Close()
    {
        udpClient?.Close();
        Debug.Log("UDP Sender closed");
    }
}