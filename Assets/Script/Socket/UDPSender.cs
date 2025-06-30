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

            // CSV 파일 경로를 빌드 폴더로 설정
            string fileName = $"TransformData_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            csvFilePath = GetBuildFolderPath(fileName);

            // CSV 헤더 작성
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
    /// 빌드 폴더 경로를 가져오는 함수
    /// </summary>
    string GetBuildFolderPath(string fileName)
    {
        string buildFolderPath;

#if UNITY_EDITOR
        // 에디터에서는 프로젝트 루트 폴더의 CSVData 폴더에 저장
        buildFolderPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "CSVData");
#else
        // 빌드된 상태에서는 실행 파일이 있는 폴더에 저장
        buildFolderPath = Path.GetDirectoryName(Application.dataPath);
#endif

        // 폴더가 없으면 생성
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

            // UDP로 전송
            udpClient.Send(sendBytes, sendBytes.Length, targetEndPoint);

            // CSV 파일에 저장
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
            // Euler 각도 계산 (읽기 쉽도록)
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