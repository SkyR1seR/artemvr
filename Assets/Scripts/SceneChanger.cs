using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    // Выберите способ указания сцены
    public enum LoadMode
    {
        ByName,   // загрузка по имени
        ByIndex   // загрузка по индексу
    }

    [Header("Настройки сцены")]
    public LoadMode loadMode = LoadMode.ByName;

    // Поле для имени сцены (используется при loadMode = ByName)
    public string sceneName = "";

    // Поле для индекса сцены (используется при loadMode = ByIndex)
    public int sceneIndex = 0;

    // Этот метод вызывается через UI-кнопку (без параметров)
    public void LoadScene()
    {
        switch (loadMode)
        {
            case LoadMode.ByName:
                if (!string.IsNullOrEmpty(sceneName))
                    SceneManager.LoadScene(sceneName);
                else
                    Debug.LogWarning("Имя сцены не задано!");
                break;

            case LoadMode.ByIndex:
                SceneManager.LoadScene(sceneIndex);
                break;
        }
    }

    // Оставлены старые методы для обратной совместимости (можно привязать к кнопке с ручным вводом параметра)
    public void LoadSceneByName(string name)
    {
        SceneManager.LoadScene(name);
    }

    public void LoadSceneByIndex(int index)
    {
        SceneManager.LoadScene(index);
    }
}