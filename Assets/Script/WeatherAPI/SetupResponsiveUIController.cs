using System.Collections.Generic;
using TMPro;
using UnityEngine;

void SetupResponsiveUIController()
{
    responsiveController = GetComponent<ResponsiveUIController>();
    if (responsiveController == null)
    {
        responsiveController = gameObject.AddComponent<ResponsiveUIController>();
    }

    responsiveController.safeAreaRect = safeAreaDisplayPanel;

    // null 체크 추가
    List<TextMeshProUGUI> largeTitleList = new List<TextMeshProUGUI>();
    List<TextMeshProUGUI> mediumList = new List<TextMeshProUGUI>();
    List<TextMeshProUGUI> smallList = new List<TextMeshProUGUI>();

    // 큰 제목 텍스트들
    if (displayDateText != null) largeTitleList.Add(displayDateText);
    if (displayTimeText != null) largeTitleList.Add(displayTimeText);
    if (displayTemperatureText != null) largeTitleList.Add(displayTemperatureText);

    // 중간 텍스트들
    if (displaySkyConditionText != null) mediumList.Add(displaySkyConditionText);
    if (displayPrecipitationText != null) mediumList.Add(displayPrecipitationText);
    if (displayHumidityText != null) mediumList.Add(displayHumidityText);
    if (displayWindSpeedText != null) mediumList.Add(displayWindSpeedText);
    if (displayLocationText != null) mediumList.Add(displayLocationText);

    // 작은 텍스트들
    if (displayStatusText != null) smallList.Add(displayStatusText);

    responsiveController.largeTitleTextArray = largeTitleList.ToArray();
    responsiveController.mediumTextArray = mediumList.ToArray();
    responsiveController.smallTextArray = smallList.ToArray();

    // 디버그 모드 설정
    responsiveController.enableDebugMode = enableDebugMode;

    if (enableDebugMode)
    {
        Debug.Log($"ResponsiveUIController 설정 완료 - Large: {largeTitleList.Count}, Medium: {mediumList.Count}, Small: {smallList.Count}");
    }
}