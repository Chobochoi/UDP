using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResponsiveUIController : MonoBehaviour
{
    [Header("������ ����")]
    public RectTransform safeAreaRect;
    public CanvasScaler mainCanvasScaler;

    [Header("�ؽ�Ʈ ũ�� ����")]
    public TextMeshProUGUI[] largeTitleTextArray;
    public TextMeshProUGUI[] mediumTextArray;
    public TextMeshProUGUI[] smallTextArray;

    [Header("ũ�� ����")]
    public float[] fontSizeMultiplierArray = { 1.5f, 1.0f, 0.8f };
    public float iconSizeMultiplierValue = 1.0f;

    private Vector2 baseResolutionSize = new Vector2(1920, 1080);
    private float currentScaleFactorValue = 1.0f;
    private float[] originalFontSizeArray;
    private bool isInitialized = false;

    void Start()
    {
        InitializeResponsiveUI();
    }

    void InitializeResponsiveUI()
    {
        if (isInitialized) return;

        // 1. ���� ��Ʈ ũ�� ����
        StoreFontSizeValues();

        // 2. ������ UI ����
        SetupResponsiveUIElements();

        // 3. ���� ���� ����
        ApplySafeAreaSettings();

        isInitialized = true;
    }

    void StoreFontSizeValues()
    {
        // null üũ �߰�
        if (largeTitleTextArray == null) largeTitleTextArray = new TextMeshProUGUI[0];
        if (mediumTextArray == null) mediumTextArray = new TextMeshProUGUI[0];
        if (smallTextArray == null) smallTextArray = new TextMeshProUGUI[0];

        int totalTextCount = largeTitleTextArray.Length + mediumTextArray.Length + smallTextArray.Length;

        if (totalTextCount == 0)
        {
            Debug.LogWarning("ResponsiveUIController: �ؽ�Ʈ ������Ʈ�� �Ҵ���� �ʾҽ��ϴ�.");
            originalFontSizeArray = new float[0];
            return;
        }

        originalFontSizeArray = new float[totalTextCount];
        int arrayIndex = 0;

        // ū ���� �ؽ�Ʈ��
        foreach (var textComponent in largeTitleTextArray)
        {
            if (textComponent != null)
            {
                originalFontSizeArray[arrayIndex] = textComponent.fontSize;
                if (enableDebugMode)
                    Debug.Log($"����� ��Ʈ ũ�� [Large {arrayIndex}]: {textComponent.name} = {textComponent.fontSize}");
            }
            else
            {
                originalFontSizeArray[arrayIndex] = 24f; // �⺻��
                Debug.LogWarning($"ResponsiveUIController: Large Title Text [{arrayIndex}]�� null�Դϴ�.");
            }
            arrayIndex++;
        }

        // �߰� �ؽ�Ʈ��
        foreach (var textComponent in mediumTextArray)
        {
            if (textComponent != null)
            {
                originalFontSizeArray[arrayIndex] = textComponent.fontSize;
                if (enableDebugMode)
                    Debug.Log($"����� ��Ʈ ũ�� [Medium {arrayIndex}]: {textComponent.name} = {textComponent.fontSize}");
            }
            else
            {
                originalFontSizeArray[arrayIndex] = 18f; // �⺻��
                Debug.LogWarning($"ResponsiveUIController: Medium Text [{arrayIndex}]�� null�Դϴ�.");
            }
            arrayIndex++;
        }

        // ���� �ؽ�Ʈ��
        foreach (var textComponent in smallTextArray)
        {
            if (textComponent != null)
            {
                originalFontSizeArray[arrayIndex] = textComponent.fontSize;
                if (enableDebugMode)
                    Debug.Log($"����� ��Ʈ ũ�� [Small {arrayIndex}]: {textComponent.name} = {textComponent.fontSize}");
            }
            else
            {
                originalFontSizeArray[arrayIndex] = 14f; // �⺻��
                Debug.LogWarning($"ResponsiveUIController: Small Text [{arrayIndex}]�� null�Դϴ�.");
            }
            arrayIndex++;
        }
    }

    void SetupResponsiveUIElements()
    {
        Vector2 currentResolutionSize = new Vector2(Screen.width, Screen.height);
        currentScaleFactorValue = Mathf.Min(currentResolutionSize.x / baseResolutionSize.x,
                                           currentResolutionSize.y / baseResolutionSize.y);

        currentScaleFactorValue = Mathf.Clamp(currentScaleFactorValue, 0.5f, 2.0f);

        // ��Ʈ ũ�� �迭�� �ʱ�ȭ�Ǿ����� Ȯ��
        if (originalFontSizeArray != null && originalFontSizeArray.Length > 0)
        {
            AdjustFontSizeValues();
        }
        else
        {
            Debug.LogWarning("ResponsiveUIController: ���� ��Ʈ ũ�� �迭�� �ʱ�ȭ���� �ʾҽ��ϴ�.");
        }

        AdjustLayoutPaddingValues();

        if (enableDebugMode)
        {
            Debug.Log($"���� �ػ�: {currentResolutionSize}, ������ ����: {currentScaleFactorValue}");
        }
    }

    void AdjustFontSizeValues()
    {
        if (originalFontSizeArray == null || originalFontSizeArray.Length == 0)
        {
            Debug.LogWarning("ResponsiveUIController: ���� ��Ʈ ũ�� �迭�� ����ֽ��ϴ�.");
            return;
        }

        int originalArrayIndex = 0;

        // ū ���� �ؽ�Ʈ��
        if (largeTitleTextArray != null)
        {
            for (int i = 0; i < largeTitleTextArray.Length; i++)
            {
                if (largeTitleTextArray[i] != null && originalArrayIndex < originalFontSizeArray.Length)
                {
                    float newFontSize = originalFontSizeArray[originalArrayIndex] * fontSizeMultiplierArray[0] * currentScaleFactorValue;
                    largeTitleTextArray[i].fontSize = newFontSize;

                    if (enableDebugMode)
                        Debug.Log($"��Ʈ ũ�� ���� [Large {i}]: {largeTitleTextArray[i].name} = {newFontSize}");
                }
                originalArrayIndex++;
            }
        }

        // �߰� �ؽ�Ʈ��
        if (mediumTextArray != null)
        {
            for (int i = 0; i < mediumTextArray.Length; i++)
            {
                if (mediumTextArray[i] != null && originalArrayIndex < originalFontSizeArray.Length)
                {
                    float newFontSize = originalFontSizeArray[originalArrayIndex] * fontSizeMultiplierArray[1] * currentScaleFactorValue;
                    mediumTextArray[i].fontSize = newFontSize;

                    if (enableDebugMode)
                        Debug.Log($"��Ʈ ũ�� ���� [Medium {i}]: {mediumTextArray[i].name} = {newFontSize}");
                }
                originalArrayIndex++;
            }
        }

        // ���� �ؽ�Ʈ��
        if (smallTextArray != null)
        {
            for (int i = 0; i < smallTextArray.Length; i++)
            {
                if (smallTextArray[i] != null && originalArrayIndex < originalFontSizeArray.Length)
                {
                    float newFontSize = originalFontSizeArray[originalArrayIndex] * fontSizeMultiplierArray[2] * currentScaleFactorValue;
                    smallTextArray[i].fontSize = newFontSize;

                    if (enableDebugMode)
                        Debug.Log($"��Ʈ ũ�� ���� [Small {i}]: {smallTextArray[i].name} = {newFontSize}");
                }
                originalArrayIndex++;
            }
        }
    }

    void AdjustLayoutPaddingValues()
    {
        var layoutGroupArray = GetComponentsInChildren<LayoutGroup>();

        if (layoutGroupArray == null || layoutGroupArray.Length == 0)
        {
            if (enableDebugMode)
                Debug.Log("ResponsiveUIController: ���̾ƿ� �׷��� �����ϴ�.");
            return;
        }

        foreach (var layoutComponent in layoutGroupArray)
        {
            if (layoutComponent == null) continue;

            try
            {
                if (layoutComponent is HorizontalLayoutGroup horizontalLayout)
                {
                    horizontalLayout.padding = ScalePaddingValue(horizontalLayout.padding);
                    horizontalLayout.spacing *= currentScaleFactorValue;
                }
                else if (layoutComponent is VerticalLayoutGroup verticalLayout)
                {
                    verticalLayout.padding = ScalePaddingValue(verticalLayout.padding);
                    verticalLayout.spacing *= currentScaleFactorValue;
                }
                else if (layoutComponent is GridLayoutGroup gridLayout)
                {
                    gridLayout.padding = ScalePaddingValue(gridLayout.padding);
                    gridLayout.spacing *= currentScaleFactorValue;
                    gridLayout.cellSize *= currentScaleFactorValue;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"ResponsiveUIController: ���̾ƿ� ���� �� ���� �߻� - {e.Message}");
            }
        }
    }

    RectOffset ScalePaddingValue(RectOffset originalPadding)
    {
        if (originalPadding == null)
            return new RectOffset(0, 0, 0, 0);

        return new RectOffset(
            Mathf.RoundToInt(originalPadding.left * currentScaleFactorValue),
            Mathf.RoundToInt(originalPadding.right * currentScaleFactorValue),
            Mathf.RoundToInt(originalPadding.top * currentScaleFactorValue),
            Mathf.RoundToInt(originalPadding.bottom * currentScaleFactorValue)
        );
    }

    void ApplySafeAreaSettings()
    {
        if (safeAreaRect == null)
        {
            if (enableDebugMode)
                Debug.Log("ResponsiveUIController: SafeArea RectTransform�� �Ҵ���� �ʾҽ��ϴ�.");
            return;
        }

        try
        {
            var safeAreaRectangle = Screen.safeArea;
            var anchorMinPoint = safeAreaRectangle.position;
            var anchorMaxPoint = safeAreaRectangle.position + safeAreaRectangle.size;

            if (Screen.width > 0 && Screen.height > 0)
            {
                anchorMinPoint.x /= Screen.width;
                anchorMinPoint.y /= Screen.height;
                anchorMaxPoint.x /= Screen.width;
                anchorMaxPoint.y /= Screen.height;

                safeAreaRect.anchorMin = anchorMinPoint;
                safeAreaRect.anchorMax = anchorMaxPoint;

                if (enableDebugMode)
                    Debug.Log($"SafeArea ����: {anchorMinPoint} ~ {anchorMaxPoint}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"ResponsiveUIController: SafeArea ���� �� ���� �߻� - {e.Message}");
        }
    }

    void OnRectTransformDimensionsChange()
    {
        if (isInitialized && originalFontSizeArray != null && originalFontSizeArray.Length > 0)
        {
            SetupResponsiveUIElements();
            ApplySafeAreaSettings();
        }
    }

    [Header("�����")]
    public bool enableDebugMode = false;

    // �������� ������ UI �缳���ϴ� �޼���
    [ContextMenu("Refresh Responsive UI")]
    public void RefreshResponsiveUI()
    {
        isInitialized = false;
        InitializeResponsiveUI();
    }

    // Inspector���� �ؽ�Ʈ �迭 �ڵ� �Ҵ�
    [ContextMenu("Auto Assign Text Arrays")]
    public void AutoAssignTextArrays()
    {
        var allTexts = GetComponentsInChildren<TextMeshProUGUI>();

        if (allTexts.Length == 0)
        {
            Debug.LogWarning("ResponsiveUIController: TextMeshProUGUI ������Ʈ�� ã�� �� �����ϴ�.");
            return;
        }

        // �ӽ÷� ��� �ؽ�Ʈ�� medium �迭�� �Ҵ�
        mediumTextArray = allTexts;

        Debug.Log($"ResponsiveUIController: {allTexts.Length}���� �ؽ�Ʈ ������Ʈ�� �ڵ� �Ҵ��߽��ϴ�.");
    }
}