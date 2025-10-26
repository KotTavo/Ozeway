using UnityEngine;

public class QuitApplication : MonoBehaviour
{
    // Метод для закрытия приложения
    public void QuitGame()
    {
        // Закрыть приложение
        Application.Quit();

        // Для тестирования в редакторе Unity
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // Альтернативный метод с подтверждением (опционально)
    public void QuitWithConfirmation()
    {
        // Можно добавить окно подтверждения здесь
        // Для простоты сразу выходим
        QuitGame();
    }
}