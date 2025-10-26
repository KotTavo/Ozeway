using UnityEngine;
using UnityEngine.SceneManagement;

public class SimpleEscapeLoader : MonoBehaviour
{
    [SerializeField] private string targetSceneName = "MainMenu"; // —цена дл€ загрузки

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!string.IsNullOrEmpty(targetSceneName))
            {
                SceneManager.LoadScene(targetSceneName);
            }
            else
            {
                Debug.LogError("Target scene name is not set!");
            }
        }
    }
}