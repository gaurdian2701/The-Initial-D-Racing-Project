using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuButtons : MonoBehaviour
{
    [SerializeField] private string gameSceneName;
    
    
    public void StartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }
    
    public void ExitGame()
    {
        Application.Quit();
    }
}
