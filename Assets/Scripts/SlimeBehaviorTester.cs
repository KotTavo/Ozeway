using UnityEngine;

public class SimpleSlimeTester : MonoBehaviour
{
    private DynamicLayeredSlimeController controller;

    void Start()
    {
        controller = GetComponent<DynamicLayeredSlimeController>();
    }

    void Update()
    {
        // ������� ������������ ������� ���������
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            controller.SetInnerStiffness(2000f);
            controller.SetMiddleStiffness(600f);
            controller.SetOuterStiffness(100f);
            Debug.Log("�����: �����������");
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            controller.SetInnerStiffness(3000f);
            controller.SetMiddleStiffness(800f);
            controller.SetOuterStiffness(150f);
            Debug.Log("�����: Ƹ�����");
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            controller.SetInnerStiffness(1000f);
            controller.SetMiddleStiffness(300f);
            controller.SetOuterStiffness(50f);
            Debug.Log("�����: ������");
        }

        // ������������ ������������
        if (Input.GetKeyDown(KeyCode.Q))
        {
            controller.ApplyAbilityModifiers(2f, 1.5f, Color.red, 3f);
            Debug.Log("�����������: ������� � �������");
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            controller.ApplyAbilityModifiers(0.5f, 0.7f, Color.blue, 3f);
            Debug.Log("�����������: ������ � ���������");
        }
    }
}