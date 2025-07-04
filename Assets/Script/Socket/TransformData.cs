// TransformData.cs
using UnityEngine;
using System.Globalization;

[System.Serializable]
public class TransformData
{
    public float posX, posY, posZ;
    public float rotX, rotY, rotZ;
    public float scaleX, scaleY, scaleZ;

    public TransformData()
    {
        posX = posY = posZ = 0f;
        rotX = rotY = rotZ = 0f;
        scaleX = scaleY = scaleZ = 1f;
    }

    public TransformData(Transform transform)
    {
        if (transform != null)
        {
            posX = transform.position.x;
            posY = transform.position.y;
            posZ = transform.position.z;

            rotX = transform.eulerAngles.x;
            rotY = transform.eulerAngles.y;
            rotZ = transform.eulerAngles.z;

            scaleX = transform.localScale.x;
            scaleY = transform.localScale.y;
            scaleZ = transform.localScale.z;
        }
        else
        {
            posX = posY = posZ = 0f;
            rotX = rotY = rotZ = 0f;
            scaleX = scaleY = scaleZ = 1f;
        }
    }

    public TransformData(Vector3 pos, Vector3 rot, Vector3 scale)
    {
        posX = pos.x; posY = pos.y; posZ = pos.z;
        rotX = rot.x; rotY = rot.y; rotZ = rot.z;
        scaleX = scale.x; scaleY = scale.y; scaleZ = scale.z;
    }

    // Vector3 변환 메서드
    public Vector3 GetPosition() => new Vector3(posX, posY, posZ);
    public Vector3 GetRotation() => new Vector3(rotX, rotY, rotZ);
    public Vector3 GetScale() => new Vector3(scaleX, scaleY, scaleZ);

    public void SetPosition(Vector3 pos) { posX = pos.x; posY = pos.y; posZ = pos.z; }
    public void SetRotation(Vector3 rot) { rotX = rot.x; rotY = rot.y; rotZ = rot.z; }
    public void SetScale(Vector3 scale) { scaleX = scale.x; scaleY = scale.y; scaleZ = scale.z; }

    // CSV 변환 메서드
    public string ToCSV()
    {
        return $"{posX.ToString("F3", CultureInfo.InvariantCulture)}," +
               $"{posY.ToString("F3", CultureInfo.InvariantCulture)}," +
               $"{posZ.ToString("F3", CultureInfo.InvariantCulture)}," +
               $"{rotX.ToString("F3", CultureInfo.InvariantCulture)}," +
               $"{rotY.ToString("F3", CultureInfo.InvariantCulture)}," +
               $"{rotZ.ToString("F3", CultureInfo.InvariantCulture)}," +
               $"{scaleX.ToString("F3", CultureInfo.InvariantCulture)}," +
               $"{scaleY.ToString("F3", CultureInfo.InvariantCulture)}," +
               $"{scaleZ.ToString("F3", CultureInfo.InvariantCulture)}";
    }

    public static TransformData FromCSV(string csvData)
    {
        try
        {
            string[] values = csvData.Split(',');
            if (values.Length < 9)
            {
                Debug.LogError($"CSV data incomplete: {csvData}");
                return new TransformData();
            }

            TransformData data = new TransformData();
            data.posX = float.Parse(values[0], CultureInfo.InvariantCulture);
            data.posY = float.Parse(values[1], CultureInfo.InvariantCulture);
            data.posZ = float.Parse(values[2], CultureInfo.InvariantCulture);
            data.rotX = float.Parse(values[3], CultureInfo.InvariantCulture);
            data.rotY = float.Parse(values[4], CultureInfo.InvariantCulture);
            data.rotZ = float.Parse(values[5], CultureInfo.InvariantCulture);
            data.scaleX = float.Parse(values[6], CultureInfo.InvariantCulture);
            data.scaleY = float.Parse(values[7], CultureInfo.InvariantCulture);
            data.scaleZ = float.Parse(values[8], CultureInfo.InvariantCulture);

            return data;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing CSV: {e.Message}, Data: {csvData}");
            return new TransformData();
        }
    }

    public override string ToString()
    {
        return $"Pos({posX:F2},{posY:F2},{posZ:F2}) Rot({rotX:F2},{rotY:F2},{rotZ:F2}) Scale({scaleX:F2},{scaleY:F2},{scaleZ:F2})";
    }
}

[System.Serializable]
public class TransformMessage
{
    public string senderType;
    public TransformData transformData;
    public float timestamp;

    public TransformMessage()
    {
        senderType = "";
        transformData = new TransformData();
        timestamp = 0f;
    }

    public TransformMessage(TransformData data, string sender)
    {
        senderType = sender;
        transformData = data;
        timestamp = Time.time;
    }

    // CSV 형식: senderType,posX,posY,posZ,rotX,rotY,rotZ,scaleX,scaleY,scaleZ,timestamp
    public string ToCSV()
    {
        return $"{senderType},{transformData.ToCSV()},{timestamp.ToString("F3", CultureInfo.InvariantCulture)}";
    }

    public static TransformMessage FromCSV(string csvData)
    {
        try
        {
            string[] parts = csvData.Split(',');
            if (parts.Length < 11)
            {
                Debug.LogError($"CSV message incomplete: {csvData}");
                return new TransformMessage();
            }

            TransformMessage message = new TransformMessage();
            message.senderType = parts[0];

            // TransformData 파싱 (1~9번 인덱스)
            string transformCSV = string.Join(",", parts, 1, 9);
            message.transformData = TransformData.FromCSV(transformCSV);

            message.timestamp = float.Parse(parts[10], CultureInfo.InvariantCulture);

            return message;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing CSV message: {e.Message}, Data: {csvData}");
            return new TransformMessage();
        }
    }
}