using UnityEngine;

public class ControllerManipulator : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 90f;
    public float scaleSpeed = 1f;

    [Header("Keyboard Controls")]
    public bool enableKeyboard = true;

    [Header("Gamepad Controls")]
    public bool enableGamepad = true;
    public float gamepadSensitivity = 1f;
    public float triggerSensitivity = 1f;
    public float stickDeadzone = 0.1f;

    [Header("Gamepad Button Mapping")]
    public KeyCode resetButton = KeyCode.JoystickButton0;      // A/X 버튼
    public KeyCode scaleUpButton = KeyCode.JoystickButton1;    // B/Circle 버튼
    public KeyCode scaleDownButton = KeyCode.JoystickButton2;  // X/Square 버튼
    public KeyCode sendButton = KeyCode.JoystickButton3;       // Y/Triangle 버튼

    [Header("Target Object")]
    public Transform targetObject;

    [Header("Debug")]
    public bool showDebugInfo = true;
    public bool enableDebugLogs = false;

    // 참조
    private TransformController transformController;

    // 입력 상태
    private Vector2 leftStick;
    private Vector2 rightStick;
    private float leftTrigger;
    private float rightTrigger;

    private void Start()
    {
        if (targetObject == null)
        {
            targetObject = transform;
        }

        //transformController = FindObjectOfType<TransformController>();
        transformController = FindFirstObjectByType<TransformController>();

        LogDebug("Controller Manipulator initialized");
        LogDebug($"Keyboard: {enableKeyboard}, Gamepad: {enableGamepad}");
    }

    private void Update()
    {
        // 키보드 입력
        if (enableKeyboard)
        {
            HandleKeyboardInput();
        }

        // 게임패드 입력
        if (enableGamepad)
        {
            HandleGamepadInput();
        }

        // 디버그 정보 표시
        if (showDebugInfo)
        {
            ShowDebugInfo();
        }
    }

    private void HandleKeyboardInput()
    {
        if (targetObject == null) return;

        float deltaTime = Time.deltaTime;

        // 이동 (WASD + QE)
        Vector3 movement = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) movement += Vector3.forward;
        if (Input.GetKey(KeyCode.S)) movement += Vector3.back;
        if (Input.GetKey(KeyCode.A)) movement += Vector3.left;
        if (Input.GetKey(KeyCode.D)) movement += Vector3.right;
        if (Input.GetKey(KeyCode.Q)) movement += Vector3.up;
        if (Input.GetKey(KeyCode.E)) movement += Vector3.down;

        targetObject.position += movement * moveSpeed * deltaTime;

        // 회전 (화살표 키)
        Vector3 rotation = Vector3.zero;
        if (Input.GetKey(KeyCode.UpArrow)) rotation += Vector3.right;
        if (Input.GetKey(KeyCode.DownArrow)) rotation += Vector3.left;
        if (Input.GetKey(KeyCode.LeftArrow)) rotation += Vector3.down;
        if (Input.GetKey(KeyCode.RightArrow)) rotation += Vector3.up;

        targetObject.eulerAngles += rotation * rotationSpeed * deltaTime;

        // 스케일 (Z, X)
        if (Input.GetKey(KeyCode.Z))
        {
            targetObject.localScale += Vector3.one * scaleSpeed * deltaTime;
        }
        if (Input.GetKey(KeyCode.X))
        {
            targetObject.localScale -= Vector3.one * scaleSpeed * deltaTime;
            targetObject.localScale = Vector3.Max(targetObject.localScale, Vector3.one * 0.1f);
        }

        // 리셋 (R)
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetTransform();
        }

        // 전송 (스페이스)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SendCurrentTransform();
        }
    }

    private void HandleGamepadInput()
    {
        if (targetObject == null) return;

        // 게임패드 연결 확인
        if (!IsGamepadConnected())
        {
            return;
        }

        float deltaTime = Time.deltaTime;

        // 스틱 입력 가져오기
        GetGamepadInput();

        // 이동 (왼쪽 스틱)
        if (leftStick.magnitude > stickDeadzone)
        {
            Vector3 movement = new Vector3(leftStick.x, 0, leftStick.y) * gamepadSensitivity;
            targetObject.position += movement * moveSpeed * deltaTime;
        }

        // 회전 (오른쪽 스틱)
        if (rightStick.magnitude > stickDeadzone)
        {
            Vector3 rotation = new Vector3(-rightStick.y, rightStick.x, 0) * gamepadSensitivity;
            targetObject.eulerAngles += rotation * rotationSpeed * deltaTime;
        }

        // 상하 이동 (트리거)
        float verticalMovement = (rightTrigger - leftTrigger) * triggerSensitivity;
        if (Mathf.Abs(verticalMovement) > 0.1f)
        {
            targetObject.position += Vector3.up * verticalMovement * moveSpeed * deltaTime;
        }

        // 버튼 입력
        HandleGamepadButtons();
    }

    private void GetGamepadInput()
    {
        // 왼쪽 스틱 (이동)
        leftStick.x = Input.GetAxis("Horizontal");
        leftStick.y = Input.GetAxis("Vertical");

        // 오른쪽 스틱 (회전) - 4번째, 5번째 축
        rightStick.x = Input.GetAxis("4th axis");  // 오른쪽 스틱 X
        rightStick.y = Input.GetAxis("5th axis");  // 오른쪽 스틱 Y

        // 트리거 (상하 이동)
        leftTrigger = Input.GetAxis("6th axis");   // 왼쪽 트리거 (보통 -1~1 범위)
        rightTrigger = Input.GetAxis("7th axis");  // 오른쪽 트리거

        // 트리거 값 정규화 (0~1 범위로)
        leftTrigger = (leftTrigger + 1f) / 2f;
        rightTrigger = (rightTrigger + 1f) / 2f;
    }

    private void HandleGamepadButtons()
    {
        // A/X 버튼 - 리셋
        if (Input.GetKeyDown(resetButton))
        {
            ResetTransform();
            LogDebug("[GAMEPAD] Reset button pressed");
        }

        // B/Circle 버튼 - 스케일 업
        if (Input.GetKey(scaleUpButton))
        {
            targetObject.localScale += Vector3.one * scaleSpeed * Time.deltaTime;
            LogDebug("[GAMEPAD] Scale up");
        }

        // X/Square 버튼 - 스케일 다운
        if (Input.GetKey(scaleDownButton))
        {
            targetObject.localScale -= Vector3.one * scaleSpeed * Time.deltaTime;
            targetObject.localScale = Vector3.Max(targetObject.localScale, Vector3.one * 0.1f);
            LogDebug("[GAMEPAD] Scale down");
        }

        // Y/Triangle 버튼 - 전송
        if (Input.GetKeyDown(sendButton))
        {
            SendCurrentTransform();
            LogDebug("[GAMEPAD] Send button pressed");
        }

        // D-PAD (방향키) - 추가 회전
        float dPadX = Input.GetAxis("6th axis");  // D-PAD 가로
        float dPadY = Input.GetAxis("7th axis");  // D-PAD 세로

        if (Mathf.Abs(dPadX) > 0.5f || Mathf.Abs(dPadY) > 0.5f)
        {
            Vector3 dPadRotation = new Vector3(dPadY, dPadX, 0) * rotationSpeed * Time.deltaTime;
            targetObject.eulerAngles += dPadRotation;
        }
    }

    private bool IsGamepadConnected()
    {
        return Input.GetJoystickNames().Length > 0 && !string.IsNullOrEmpty(Input.GetJoystickNames()[0]);
    }

    private void ResetTransform()
    {
        if (targetObject == null) return;

        targetObject.position = Vector3.zero;
        targetObject.eulerAngles = Vector3.zero;
        targetObject.localScale = Vector3.one;

        LogDebug("[RESET] Transform reset to default");
        SendCurrentTransform();
    }

    private void SendCurrentTransform()
    {
        if (transformController != null)
        {
            //transformController.ForceSendCurrent();
            LogDebug("[SEND] Current transform sent");
        }
        else
        {
            LogDebug("[SEND] TransformController not found");
        }
    }

    private void ShowDebugInfo()
    {
        // 화면에 디버그 정보 표시
        if (enableGamepad && IsGamepadConnected())
        {
            string debugText = $"Gamepad Connected: {Input.GetJoystickNames()[0]}\n";
            debugText += $"Left Stick: ({leftStick.x:F2}, {leftStick.y:F2})\n";
            debugText += $"Right Stick: ({rightStick.x:F2}, {rightStick.y:F2})\n";
            debugText += $"Triggers: L:{leftTrigger:F2} R:{rightTrigger:F2}";

            // GUI로 표시 (OnGUI에서 사용)
            debugInfo = debugText;
        }
    }

    private string debugInfo = "";

    private void OnGUI()
    {
        if (showDebugInfo && !string.IsNullOrEmpty(debugInfo))
        {
            GUI.Box(new Rect(10, 10, 300, 100), debugInfo);
        }

        // 게임패드 연결 상태 표시
        if (enableGamepad)
        {
            string connectionStatus = IsGamepadConnected() ?
                $"✓ Gamepad: {Input.GetJoystickNames()[0]}" :
                "✗ No Gamepad Connected";

            GUI.Label(new Rect(10, Screen.height - 30, 400, 20), connectionStatus);
        }
    }

    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[ControllerManipulator] {message}");
        }
    }

    // 컨텍스트 메뉴
    [ContextMenu("Test Gamepad Connection")]
    public void TestGamepadConnection()
    {
        string[] joysticks = Input.GetJoystickNames();
        Debug.Log($"Connected Joysticks: {joysticks.Length}");
        for (int i = 0; i < joysticks.Length; i++)
        {
            Debug.Log($"Joystick {i}: {joysticks[i]}");
        }
    }

    [ContextMenu("Reset Transform")]
    public void ContextResetTransform()
    {
        ResetTransform();
    }
}