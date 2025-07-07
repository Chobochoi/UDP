using UnityEngine;
using UnityEngine.InputSystem;

public class ControllerManipulator : MonoBehaviour
{
    // Move Setting
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 90f;
    public float scaleSpeed = 1f;

    // 키보드 연동
    [Header("Keyboard Controls")]
    public bool enableKeyboard = true;

    // 로지텍 컨트롤러와 연동
    [Header("Gamepad Controls")]
    public bool enableGamepad = true;
    public float gamepadSensitivity = 1f;
    public float triggerSensitivity = 1f;
    public float stickDeadzone = 0.1f;

    // 움직일 대상이 될 Target Object
    [Header("Target Object")]
    public Transform targetObject;

    [Header("Debug")]
    public bool showDebugInfo = true;
    public bool enableDebugLogs = false;

    // 참조
    private TransformController transformController;

    // Input System 참조
    private Keyboard keyboard;
    private Gamepad gamepad;

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

        transformController = FindFirstObjectByType<TransformController>();

        // Input System 초기화
        keyboard = Keyboard.current;
        gamepad = Gamepad.current;

        LogDebug("Controller Manipulator initialized with Input System");
        LogDebug($"Keyboard: {enableKeyboard}, Gamepad: {enableGamepad}");
    }

    private void Update()
    {
        // 키보드 입력
        if (enableKeyboard && keyboard != null)
        {
            HandleKeyboardInput();
        }

        // 게임패드 입력
        if (enableGamepad && gamepad != null)
        {
            HandleGamepadInput();
        }

        // 게임패드 연결 상태 업데이트
        if (gamepad == null)
        {
            gamepad = Gamepad.current;
        }

        // 디버그 정보 업데이트
        if (showDebugInfo)
        {
            UpdateDebugInfo();
        }
    }

    private void HandleKeyboardInput()
    {
        if (targetObject == null || keyboard == null) return;

        float deltaTime = Time.deltaTime;

        // 이동 (WASD + QE)
        Vector3 movement = Vector3.zero;
        if (keyboard.wKey.isPressed) movement += Vector3.forward;
        if (keyboard.sKey.isPressed) movement += Vector3.back;
        if (keyboard.aKey.isPressed) movement += Vector3.left;
        if (keyboard.dKey.isPressed) movement += Vector3.right;
        if (keyboard.qKey.isPressed) movement += Vector3.up;
        if (keyboard.eKey.isPressed) movement += Vector3.down;

        targetObject.position += movement * moveSpeed * deltaTime;

        // 회전 (화살표 키)
        Vector3 rotation = Vector3.zero;
        if (keyboard.upArrowKey.isPressed) rotation += Vector3.right;
        if (keyboard.downArrowKey.isPressed) rotation += Vector3.left;
        if (keyboard.leftArrowKey.isPressed) rotation += Vector3.down;
        if (keyboard.rightArrowKey.isPressed) rotation += Vector3.up;

        targetObject.eulerAngles += rotation * rotationSpeed * deltaTime;

        // 스케일 (Z, X)
        if (keyboard.zKey.isPressed)
        {
            targetObject.localScale += Vector3.one * scaleSpeed * deltaTime;
        }
        if (keyboard.xKey.isPressed)
        {
            targetObject.localScale -= Vector3.one * scaleSpeed * deltaTime;
            targetObject.localScale = Vector3.Max(targetObject.localScale, Vector3.one * 0.1f);
        }

        // 리셋 (R)
        if (keyboard.rKey.wasPressedThisFrame)
        {
            ResetTransform();
        }

        // 전송 (스페이스)
        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            SendCurrentTransform();
        }
    }

    private void HandleGamepadInput()
    {
        if (targetObject == null || gamepad == null) return;

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
        if (gamepad == null) return;

        // 스틱 입력
        leftStick = gamepad.leftStick.ReadValue();
        rightStick = gamepad.rightStick.ReadValue();

        // 트리거 입력
        leftTrigger = gamepad.leftTrigger.ReadValue();
        rightTrigger = gamepad.rightTrigger.ReadValue();
    }

    private void HandleGamepadButtons()
    {
        if (gamepad == null) return;

        // A 버튼 - 리셋
        if (gamepad.aButton.wasPressedThisFrame)
        {
            ResetTransform();
            LogDebug("[GAMEPAD] A button pressed - Reset");
        }

        // B 버튼 - 스케일 업
        if (gamepad.bButton.isPressed)
        {
            targetObject.localScale += Vector3.one * scaleSpeed * Time.deltaTime;
            LogDebug("[GAMEPAD] B button pressed - Scale up");
        }

        // X 버튼 - 스케일 다운
        if (gamepad.xButton.isPressed)
        {
            targetObject.localScale -= Vector3.one * scaleSpeed * Time.deltaTime;
            targetObject.localScale = Vector3.Max(targetObject.localScale, Vector3.one * 0.1f);
            LogDebug("[GAMEPAD] X button pressed - Scale down");
        }

        // Y 버튼 - 전송
        if (gamepad.yButton.wasPressedThisFrame)
        {
            SendCurrentTransform();
            LogDebug("[GAMEPAD] Y button pressed - Send");
        }

        // D-PAD 추가 회전
        Vector2 dpad = gamepad.dpad.ReadValue();
        if (dpad.magnitude > 0.5f)
        {
            Vector3 dPadRotation = new Vector3(dpad.y, dpad.x, 0) * rotationSpeed * Time.deltaTime;
            targetObject.eulerAngles += dPadRotation;
        }

        // 어깨 버튼 (추가 기능)
        if (gamepad.leftShoulder.wasPressedThisFrame)
        {
            LogDebug("[GAMEPAD] Left shoulder pressed");
        }

        if (gamepad.rightShoulder.wasPressedThisFrame)
        {
            LogDebug("[GAMEPAD] Right shoulder pressed");
        }
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
            transformController.ForceSendCurrent();
            LogDebug("[SEND] Current transform sent");
        }
        else
        {
            LogDebug("[SEND] TransformController not found");
        }
    }

    // Gamepad의 움직임에 관한 디버깅
    private void UpdateDebugInfo()
    {
        if (enableGamepad && gamepad != null)
        {
            string debugText = $"Gamepad: {gamepad.name}\n";
            debugText += $"Left Stick: ({leftStick.x:F2}, {leftStick.y:F2})\n";
            debugText += $"Right Stick: ({rightStick.x:F2}, {rightStick.y:F2})\n";
            debugText += $"Triggers: L:{leftTrigger:F2} R:{rightTrigger:F2}\n";
            debugText += $"Buttons: A:{gamepad.aButton.isPressed} B:{gamepad.bButton.isPressed} X:{gamepad.xButton.isPressed} Y:{gamepad.yButton.isPressed}";

            debugInfo = debugText;
        }
        else
        {
            debugInfo = "No Gamepad Connected";
        }
    }

    private string debugInfo = "";

    private void OnGUI()
    {
        if (showDebugInfo)
        {
            GUI.Box(new Rect(10, 10, 400, 120), debugInfo);
        }

        // 게임패드 연결 상태 표시
        if (enableGamepad)
        {
            string connectionStatus = gamepad != null ?
                $" Gamepad: {gamepad.name}" :
                " No Gamepad Connected";

            GUI.Label(new Rect(10, Screen.height - 30, 400, 20), connectionStatus);
        }

        // 키보드 상태 표시
        if (enableKeyboard)
        {
            string keyboardStatus = keyboard != null ?
                " Keyboard: Connected" :
                " Keyboard: Not Available";

            GUI.Label(new Rect(10, Screen.height - 50, 400, 20), keyboardStatus);
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
    [ContextMenu("Test Input System")]
    public void TestInputSystem()
    {
        Debug.Log("=== Input System Status ===");
        Debug.Log($"Keyboard: {(keyboard != null ? "Connected" : "Not Available")}");
        Debug.Log($"Gamepad: {(gamepad != null ? gamepad.name : "Not Connected")}");

        if (gamepad != null)
        {
            Debug.Log($"Gamepad Type: {gamepad.GetType().Name}");
            Debug.Log($"Left Stick: {gamepad.leftStick.ReadValue()}");
            Debug.Log($"Right Stick: {gamepad.rightStick.ReadValue()}");
        }
    }

    [ContextMenu("Reset Transform")]
    public void ContextResetTransform()
    {
        ResetTransform();
    }
}