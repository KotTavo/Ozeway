using UnityEngine;
using UnityEngine.UI;

public class SlimeInfoDisplay : MonoBehaviour
{
    public SlimeCharacterController slimeController;
    public Vector3 offset = new Vector3(0, 2.5f, 0);

    private Text infoText;
    private Canvas canvas;

    void Start()
    {
        CreateInfoDisplay();
    }

    void CreateInfoDisplay()
    {
        // ������� Canvas
        GameObject canvasObject = new GameObject("SlimeInfoCanvas");
        canvasObject.transform.SetParent(transform);
        canvasObject.transform.localPosition = offset;

        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 1000;

        // ����������� RectTransform ��� Canvas
        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(400, 150);
        canvasRect.localScale = new Vector3(0.01f, 0.01f, 1f);

        // ������� ���
        GameObject backgroundObject = new GameObject("Background");
        backgroundObject.transform.SetParent(canvasObject.transform);
        backgroundObject.transform.localPosition = Vector3.zero;

        Image backgroundImage = backgroundObject.AddComponent<Image>();
        backgroundImage.color = new Color(0, 0, 0, 0.7f);

        RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
        backgroundRect.sizeDelta = new Vector2(350, 120);
        backgroundRect.anchoredPosition = Vector2.zero;

        // ������� �����
        GameObject textObject = new GameObject("InfoText");
        textObject.transform.SetParent(canvasObject.transform);
        textObject.transform.localPosition = Vector3.zero;

        infoText = textObject.AddComponent<Text>();
        infoText.fontSize = 14;
        infoText.color = Color.white;
        infoText.alignment = TextAnchor.MiddleCenter;
        infoText.horizontalOverflow = HorizontalWrapMode.Overflow;
        infoText.verticalOverflow = VerticalWrapMode.Overflow;

        // ���������� ��������� ����� Arial ��� ��������� ���������
        infoText.font = Font.CreateDynamicFontFromOSFont("Arial", 14);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(330, 100);
        textRect.anchoredPosition = Vector2.zero;
    }

    void Update()
    {
        if (slimeController != null && infoText != null)
        {
            // �������� ���������� � �����
            float groundPercentage = slimeController.GetGroundContactPercentage();
            Vector2 velocity = slimeController.GetCenterVelocity();
            bool isLifted = slimeController.IsLifted();
            bool isJumping = slimeController.IsJumping();

            // ��������� ����� � ����������� �� �������
            string liftStatus = isLifted ? "��������: ��" : "��������: ���";
            string jumpStatus = isJumping ? "������: ��" : "������: ���";

            infoText.text = $"����� �� �����: {groundPercentage:F1}%\n" +
                          $"�������� X: {velocity.x:F2} Y: {velocity.y:F2}\n" +
                          $"{liftStatus} | {jumpStatus}";
        }

        // ������������ Canvas � ������
        if (canvas != null && Camera.main != null)
        {
            canvas.transform.rotation = Camera.main.transform.rotation;
        }
    }
}