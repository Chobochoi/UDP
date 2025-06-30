using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UDPManager : MonoBehaviour
{
    [Header("Target Objects")]
    public Transform sendObject;        // Transform 값을 전송할 오브젝트
    public Transform receiveObject;     // Transform 값을 받아서 갱신할 오브젝트

    [Header("UI")]
    public Button startSendButton;
    public Button stopSendButton;
    public Text statusText;
    public Text lastReceivedText;       // 마지막 수신 데이터 표시

    [Header("UDP Settings")]
    public string targetIP = "192.168.156.252";
    public int sendPort = 12222;
    public int receivePort = 12223;

    [Header("Control Options")]
    public bool smoothTransition = true;    // 부드러운 전환
    public float transitionSpeed = 5f;      // 전환 속도

    private UDPSender udpSender;
    private UDPReceiver udpReceiver;
    private bool isSending = false;
    private Coroutine sendCoroutine;
    private Coroutine smoothMoveCoroutine;

    void Start()
    {
        // UDP 송수신 컴포넌트 초기화
        udpSender = gameObject.AddComponent<UDPSender>();
        udpReceiver = gameObject.AddComponent<UDPReceiver>();

        udpSender.Initialize(targetIP, sendPort);
        udpReceiver.Initialize(receivePort, OnTransformReceived);

        // UI 이벤트 연결
        startSendButton.onClick.AddListener(StartSending);
        stopSendButton.onClick.AddListener(StopSending);

        UpdateUI();
    }

    void StartSending()
    {
        if (!isSending)
        {
            isSending = true;
            sendCoroutine = StartCoroutine(SendTransformData());
            UpdateUI();
        }
    }

    void StopSending()
    {
        if (isSending)
        {
            isSending = false;
            if (sendCoroutine != null)
            {
                StopCoroutine(sendCoroutine);
                sendCoroutine = null;
            }
            UpdateUI();
        }
    }

    IEnumerator SendTransformData()
    {
        while (isSending)
        {
            if (sendObject != null)
            {
                TransformData data = TransformData.FromTransform(sendObject);
                udpSender.SendTransformData(data);
                Debug.Log($"Sent Transform - Pos: {sendObject.position}, Rot: {sendObject.rotation.eulerAngles}");
            }

            yield return new WaitForSeconds(0.25f); // 5초 간격
        }
    }

    void OnTransformReceived(TransformData receivedData)
    {
        if (receiveObject != null)
        {
            if (smoothTransition)
            {
                // 부드러운 전환
                if (smoothMoveCoroutine != null)
                    StopCoroutine(smoothMoveCoroutine);
                smoothMoveCoroutine = StartCoroutine(SmoothMoveToTarget(receivedData));
            }
            else
            {
                // 즉시 적용
                receivedData.ApplyToTransform(receiveObject);
            }

            // UI 업데이트
            Vector3 eulerAngles = new Quaternion(receivedData.rotX, receivedData.rotY, receivedData.rotZ, receivedData.rotW).eulerAngles;
            lastReceivedText.text = $"Pos: ({receivedData.posX:F2}, {receivedData.posY:F2}, {receivedData.posZ:F2})\n" +
                                   $"Rot: ({eulerAngles.x:F1}°, {eulerAngles.y:F1}°, {eulerAngles.z:F1}°)";

            Debug.Log($"Received Transform - Pos: ({receivedData.posX}, {receivedData.posY}, {receivedData.posZ}), " +
                     $"Rot: ({eulerAngles.x}°, {eulerAngles.y}°, {eulerAngles.z}°)");
        }
    }

    IEnumerator SmoothMoveToTarget(TransformData targetData)
    {
        Vector3 startPos = receiveObject.position;
        Quaternion startRot = receiveObject.rotation;

        Vector3 targetPos = new Vector3(targetData.posX, targetData.posY, targetData.posZ);
        Quaternion targetRot = new Quaternion(targetData.rotX, targetData.rotY, targetData.rotZ, targetData.rotW);

        float elapsed = 0f;
        float duration = 1f / transitionSpeed;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            receiveObject.position = Vector3.Lerp(startPos, targetPos, t);
            receiveObject.rotation = Quaternion.Lerp(startRot, targetRot, t);

            yield return null;
        }

        // 정확한 최종 위치 설정
        receiveObject.position = targetPos;
        receiveObject.rotation = targetRot;
    }

    void UpdateUI()
    {
        startSendButton.interactable = !isSending;
        stopSendButton.interactable = isSending;
        statusText.text = isSending ? "Sending..." : "Stopped";
    }

    void OnDestroy()
    {
        if (udpSender != null) udpSender.Close();
        if (udpReceiver != null) udpReceiver.Close();
    }
}