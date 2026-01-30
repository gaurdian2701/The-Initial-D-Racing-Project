using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuButtons : MonoBehaviour
{
    [SerializeField] private string gameSceneName;
    
    [SerializeField] private AudioClip buttonSound;
    
    [SerializeField] private AudioClip clickSound;
    
    [SerializeField] private Animator cntrlsAnimator;
    bool cntrlsOpen = false;
    
    
    public void StartGame()
    {
        if (SFXManager.Instance!=null) SFXManager.Instance.PlaySFXClip(buttonSound,0.8f);
        SceneManager.LoadScene(gameSceneName);
    }
    
    public void ExitGame()
    {
        if (SFXManager.Instance!=null) SFXManager.Instance.PlaySFXClip(buttonSound,0.8f);
        Application.Quit();
    }

    public void ToggleControls()
    {
        if (SFXManager.Instance!=null) SFXManager.Instance.PlaySFXClip(clickSound,0.8f);
        
        cntrlsOpen = !cntrlsOpen;
        cntrlsAnimator.SetBool("In", cntrlsOpen);
        
    }
}
