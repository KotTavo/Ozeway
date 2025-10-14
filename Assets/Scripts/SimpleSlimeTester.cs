using UnityEngine;

public class SlimeAbilityTester : MonoBehaviour
{
    private SlimeCharacterController slime;

    void Start()
    {
        slime = GetComponent<SlimeCharacterController>();
    }

    void Update()
    {
        TestRigidityPresets();
        TestAbilityActivation();
    }

    void TestRigidityPresets()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            // Solid mode - tight core, stiff surface
            Debug.Log("Solid Mode Activated");
            // You would call slime modification methods here
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            // Liquid mode - loose surface, fluid movement
            Debug.Log("Liquid Mode Activated");
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            // Balanced mode - default
            Debug.Log("Balanced Mode Activated");
        }
    }

    void TestAbilityActivation()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            slime.ModifySlimeProperties(2f, 1.2f, Color.red, 4f);
            Debug.Log("Heavy Stone Form");
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            slime.ModifySlimeProperties(0.5f, 0.8f, Color.blue, 4f);
            Debug.Log("Light Water Form");
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            slime.ModifySlimeProperties(1.5f, 1.5f, Color.green, 3f);
            Debug.Log("Growth Form");
        }
    }
}