using UnityEngine;

[System.Serializable]
public class TransformData
{
    [Header("Position")]
    public float posX;
    public float posY;
    public float posZ;

    [Header("Rotation")]
    public float rotX;
    public float rotY;
    public float rotZ;
    public float rotW;

    public string timestamp;

    // Transform에서 데이터 생성
    public static TransformData FromTransform(Transform transform)
    {
        return new TransformData
        {
            posX = transform.position.x,
            posY = transform.position.y,
            posZ = transform.position.z,
            rotX = transform.rotation.x,
            rotY = transform.rotation.y,
            rotZ = transform.rotation.z,
            rotW = transform.rotation.w,
            timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
        };
    }

    // Transform에 데이터 적용
    public void ApplyToTransform(Transform transform)
    {
        transform.position = new Vector3(posX, posY, posZ);
        transform.rotation = new Quaternion(rotX, rotY, rotZ, rotW);
    }

    // Euler 각도로 회전 설정 (외부에서 사용하기 편함)
    public void SetRotationFromEuler(Vector3 eulerAngles)
    {
        Quaternion quat = Quaternion.Euler(eulerAngles);
        rotX = quat.x;
        rotY = quat.y;
        rotZ = quat.z;
        rotW = quat.w;
    }
}