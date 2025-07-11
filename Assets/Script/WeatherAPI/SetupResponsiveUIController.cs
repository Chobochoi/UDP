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

    // null üũ �߰�
    List<TextMeshProUGUI> largeTitleList = new List<TextMeshProUGUI>();
    List<TextMeshProUGUI> mediumList = new List<TextMeshProUGUI>();
    List<TextMeshProUGUI> smallList = new List<TextMeshProUGUI>();

    // ū ���� �ؽ�Ʈ��
    if (displayDateText != null) largeTitleList.Add(displayDateText);
    if (displayTimeText != null) largeTitleList.Add(displayTimeText);
    if (displayTemperatureText != null) largeTitleList.Add(displayTemperatureText);

    // �߰� �ؽ�Ʈ��
    if (displaySkyConditionText != null) mediumList.Add(displaySkyConditionText);
    if (displayPrecipitationText != null) mediumList.Add(displayPrecipitationText);
    if (displayHumidityText != null) mediumList.Add(displayHumidityText);
    if (displayWindSpeedText != null) mediumList.Add(displayWindSpeedText);
    if (displayLocationText != null) mediumList.Add(displayLocationText);

    // ���� �ؽ�Ʈ��
    if (displayStatusText != null) smallList.Add(displayStatusText);

    responsiveController.largeTitleTextArray = largeTitleList.ToArray();
    responsiveController.mediumTextArray = mediumList.ToArray();
    responsiveController.smallTextArray = smallList.ToArray();

    // ����� ��� ����
    responsiveController.enableDebugMode = enableDebugMode;

    if (enableDebugMode)
    {
        Debug.Log($"ResponsiveUIController ���� �Ϸ� - Large: {largeTitleList.Count}, Medium: {mediumList.Count}, Small: {smallList.Count}");
    }
}