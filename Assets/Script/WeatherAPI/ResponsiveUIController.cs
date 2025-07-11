using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResponsiveUIController : MonoBehaviour
{
    [Header("반응형 설정")]
    public RectTransform safeAreaRect;
    public CanvasScaler mainCanvasScaler;

    [Header("텍스트 크기 설정")]
    public TextMeshProUGUI[] largeTitleTextArray;
    public TextMeshProUGUI[] mediumTextArray;
    public TextMeshProUGUI[] smallTextArray;

    [Header("크기 설정")]
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

        // 1. 먼저 폰트 크기 저장
        StoreFontSizeValues();

        // 2. 반응형 UI 설정
        SetupResponsiveUIElements();

        // 3. 안전 영역 적용
        ApplySafeAreaSettings();

        isInitialized = true;
    }

    void StoreFontSizeValues()
    {
        // null 체크 추가
        if (largeTitleTextArray == null) largeTitleTextArray = new TextMeshProUGUI[0];
        if (mediumTextArray == null) mediumTextArray = new TextMeshProUGUI[0];
        if (smallTextArray == null) smallTextArray = new TextMeshProUGUI[0];

        int totalTextCount = largeTitleTextArray.Length + mediumTextArray.Length + smallTextArray.Length;

        if (totalTextCount == 0)
        {
            Debug.LogWarning("ResponsiveUIController: 텍스트 컴포넌트가 할당되지 않았습니다.");
            originalFontSizeArray = new float[0];
            return;
        }

        originalFontSizeArray = new float[totalTextCount];
        int arrayIndex = 0;

        // 큰 제목 텍스트들
        foreach (var textComponent in largeTitleTextArray)
        {
            if (textComponent != null)
            {
                originalFontSizeArray[arrayIndex] = textComponent.fontSize;
                if (enableDebugMode)
                    Debug.Log($"저장된 폰트 크기 [Large {arrayIndex}]: {textComponent.name} = {textComponent.fontSize}");
            }
            else
            {
                originalFontSizeArray[arrayIndex] = 24f; // 기본값
                Debug.LogWarning($"ResponsiveUIController: Large Title Text [{arrayIndex}]가 null입니다.");
            }
            arrayIndex++;
        }

        // 중간 텍스트들
        foreach (var textComponent in mediumTextArray)
        {
            if (textComponent != null)
            {
                originalFontSizeArray[arrayIndex] = textComponent.fontSize;
                if (enableDebugMode)
                    Debug.Log($"저장된 폰트 크기 [Medium {arrayIndex}]: {textComponent.name} = {textComponent.fontSize}");
            }
            else
            {
                originalFontSizeArray[arrayIndex] = 18f; // 기본값
                Debug.LogWarning($"ResponsiveUIController: Medium Text [{arrayIndex}]가 null입니다.");
            }
            arrayIndex++;
        }

        // 작은 텍스트들
        foreach (var textComponent in smallTextArray)
        {
            if (textComponent != null)
            {
                originalFontSizeArray[arrayIndex] = textComponent.fontSize;
                if (enableDebugMode)
                    Debug.Log($"저장된 폰트 크기 [Small {arrayIndex}]: {textComponent.name} = {textComponent.fontSize}");
            }
            else
            {
                originalFontSizeArray[arrayIndex] = 14f; // 기본값
                Debug.LogWarning($"ResponsiveUIController: Small Text [{arrayIndex}]가 null입니다.");
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

        // 폰트 크기 배열이 초기화되었는지 확인
        if (originalFontSizeArray != null && originalFontSizeArray.Length > 0)
        {
            AdjustFontSizeValues();
        }
        else
        {
            Debug.LogWarning("ResponsiveUIController: 원본 폰트 크기 배열이 초기화되지 않았습니다.");
        }

        AdjustLayoutPaddingValues();

        if (enableDebugMode)
        {
            Debug.Log($"현재 해상도: {currentResolutionSize}, 스케일 팩터: {currentScaleFactorValue}");
        }
    }

    void AdjustFontSizeValues()
    {
        if (originalFontSizeArray == null || originalFontSizeArray.Length == 0)
        {
            Debug.LogWarning("ResponsiveUIController: 원본 폰트 크기 배열이 비어있습니다.");
            return;
        }

        int originalArrayIndex = 0;

        // 큰 제목 텍스트들
        if (largeTitleTextArray != null)
        {
            for (int i = 0; i < largeTitleTextArray.Length; i++)
            {
                if (largeTitleTextArray[i] != null && originalArrayIndex < originalFontSizeArray.Length)
                {
                    float newFontSize = originalFontSizeArray[originalArrayIndex] * fontSizeMultiplierArray[0] * currentScaleFactorValue;
                    largeTitleTextArray[i].fontSize = newFontSize;

                    if (enableDebugMode)
                        Debug.Log($"폰트 크기 조정 [Large {i}]: {largeTitleTextArray[i].name} = {newFontSize}");
                }
                originalArrayIndex++;
            }
        }

        // 중간 텍스트들
        if (mediumTextArray != null)
        {
            for (int i = 0; i < mediumTextArray.Length; i++)
            {
                if (mediumTextArray[i] != null && originalArrayIndex < originalFontSizeArray.Length)
                {
                    float newFontSize = originalFontSizeArray[originalArrayIndex] * fontSizeMultiplierArray[1] * currentScaleFactorValue;
                    mediumTextArray[i].fontSize = newFontSize;

                    if (enableDebugMode)
                        Debug.Log($"폰트 크기 조정 [Medium {i}]: {mediumTextArray[i].name} = {newFontSize}");
                }
                originalArrayIndex++;
            }
        }

        // 작은 텍스트들
        if (smallTextArray != null)
        {
            for (int i = 0; i < smallTextArray.Length; i++)
            {
                if (smallTextArray[i] != null && originalArrayIndex < originalFontSizeArray.Length)
                {
                    float newFontSize = originalFontSizeArray[originalArrayIndex] * fontSizeMultiplierArray[2] * currentScaleFactorValue;
                    smallTextArray[i].fontSize = newFontSize;

                    if (enableDebugMode)
                        Debug.Log($"폰트 크기 조정 [Small {i}]: {smallTextArray[i].name} = {newFontSize}");
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
                Debug.Log("ResponsiveUIController: 레이아웃 그룹이 없습니다.");
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
                Debug.LogError($"ResponsiveUIController: 레이아웃 조정 중 오류 발생 - {e.Message}");
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
                Debug.Log("ResponsiveUIController: SafeArea RectTransform이 할당되지 않았습니다.");
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
                    Debug.Log($"SafeArea 적용: {anchorMinPoint} ~ {anchorMaxPoint}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"ResponsiveUIController: SafeArea 설정 중 오류 발생 - {e.Message}");
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

    [Header("디버그")]
    public bool enableDebugMode = false;

    // 수동으로 반응형 UI 재설정하는 메서드
    [ContextMenu("Refresh Responsive UI")]
    public void RefreshResponsiveUI()
    {
        isInitialized = false;
        InitializeResponsiveUI();
    }

    // Inspector에서 텍스트 배열 자동 할당
    [ContextMenu("Auto Assign Text Arrays")]
    public void AutoAssignTextArrays()
    {
        var allTexts = GetComponentsInChildren<TextMeshProUGUI>();

        if (allTexts.Length == 0)
        {
            Debug.LogWarning("ResponsiveUIController: TextMeshProUGUI 컴포넌트를 찾을 수 없습니다.");
            return;
        }

        // 임시로 모든 텍스트를 medium 배열에 할당
        mediumTextArray = allTexts;

        Debug.Log($"ResponsiveUIController: {allTexts.Length}개의 텍스트 컴포넌트를 자동 할당했습니다.");
    }
}