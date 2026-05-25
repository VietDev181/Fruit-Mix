using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneService : MonoBehaviour, ISceneService
{
    public void LoadStartScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("StartScene");
    }

    public void LoadSelectScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("SelectScene");
    }

    public void ReloadCurrentScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
