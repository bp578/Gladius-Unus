using UnityEngine;
using UnityEngine.SceneManagement;
public class MainMenu : MonoBehaviour {
    
    public AudioSource audioSource;
    public AudioClip onClickClip;
    
    public void StartGame() {
        audioSource.PlayOneShot(onClickClip);
        Invoke("LoadGame", 0.5f);
    }
    
    public void LoadGame() {
        SceneManager.LoadScene("Game Scene 1");
    }
    public void ExitGame() {
        //todo
    }
}