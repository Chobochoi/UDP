// TransformData.cs
using UnityEngine;

[System.Serializable]
public class TransformData
{
    // Vector3 대신 개별 float 사용
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

    public string ToJson()
    {
        return JsonUtility.ToJson(this);
    }

    public static TransformData FromJson(string json)
    {
        return JsonUtility.FromJson<TransformData>(json);
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

    public string ToJson()
    {
        return JsonUtility.ToJson(this);
    }

    public static TransformMessage FromJson(string json)
    {
        return JsonUtility.FromJson<TransformMessage>(json);
    }
}