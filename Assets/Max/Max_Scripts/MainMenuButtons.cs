using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuButtons : MonoBehaviour
{
    [SerializeField] private string gameSceneName;
    
    [SerializeField] private AudioClip buttonSound;
    
    public void StartGame()
    {
        if (SFXManager.Instance!=null) SFXManager.Instance.PlaySFXClip(buttonSound,0.8f);
        SceneManager.LoadScene(gameSceneName);
    }
    
    public void ExitGame()
    {
        Application.Quit();
    }
}
