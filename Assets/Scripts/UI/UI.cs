using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public GameObject pauseCanvas;
    public GameObject gameOverCanvas;
    public GameObject victoryCanvas;
    public PlayerController playerController;
    public Stats playerStats;
    public Items playerItems;
    public Image PlayerhealthBar;
    public Image PlayerstaminaBar;
    public TMP_Text numHealthPot;
    public Image throggHealthBar;
    public Image rickHealthBar;
    public Stats throggStats;
    public Stats rickStats;
    public PlayerController player;
    public GameObject onScreenStuff;
    public GameObject ThroggBar;
    public GameObject rickBar;

    private bool isPaused = false;
    private float health;
    private float stamina;

    //Audio stuff
    public DoorController musicDoor;
    public AudioSource menueSource;
    public AudioClip gameOverClip;
    public AudioClip victoryClip;



    void Start()
    {
        pauseCanvas.SetActive(false);
        gameOverCanvas.SetActive(false);
        victoryCanvas.SetActive(false);
        onScreenStuff.SetActive(true);
        rickBar.SetActive(false);
        Time.timeScale = 1f;
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                Resume();
            else
                Pause();
        }

        DisplayHealth();
        DisplayStamina();
        DisplayHealthPotNum();
        if (throggStats.GetHealth() > 0)
        {
            ThrogDisplayHealth();
        }
        else
        {
            ThroggBar.SetActive(false);
        }
        if (rickStats.GetHealth() > 0)
        {
            RickDisplayHealth();
        }
        else
        {
            rickBar.SetActive(false);
        }

        if (player.getGameOverState())
        {
            GameOver();
        }
        if (rickStats.GetHealth() <= 0)
        {
            Victory();
        }
    }

    public void Pause()
    {
        // staminaBar.enabled = false;
        // healthBar.enabled = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        playerController.canAttack = false;
        pauseCanvas.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
        onScreenStuff.SetActive(false);
    }

    public void Resume()
    {
        // staminaBar.enabled = true;
        // healthBar.enabled = true;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        pauseCanvas.SetActive(false);
        Time.timeScale = 1f;
        playerController.canAttack = true;
        isPaused = false;
        onScreenStuff.SetActive(true);
    }

    public void GameOver()
    {

        musicDoor.StopAllMusic();
        menueSource.PlayOneShot(gameOverClip, 0.05f);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        onScreenStuff.SetActive(false);

        gameOverCanvas.SetActive(true);
    }


    public void Victory()
    {
        musicDoor.StopAllMusic();
        menueSource.PlayOneShot(victoryClip, 0.05f);
        StartCoroutine(DelayedAction());
    }

    public void Restart()
    {
        // Reset the game time scale to normal
        Time.timeScale = 1f;

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Quit()
    {
        Application.Quit();
    }

    public void DisplayHealth()
    {
        health = playerStats.GetHealth();
        PlayerhealthBar.fillAmount = ((float)health / (float) playerStats.GetMaxHealth());
    }

    public void DisplayStamina()
    {
        stamina = playerStats.GetStamina();
        PlayerstaminaBar.fillAmount = ((float)stamina / (float) playerStats.GetMaxStamina());

    }
    public void DisplayHealthPotNum()
    {
        numHealthPot.text = playerItems.GetNumPots().ToString();
    }

    public void ThrogDisplayHealth()
    {
        throggHealthBar.fillAmount = ((float)throggStats.GetHealth() / (float) throggStats.GetMaxHealth());
    }
    public void RickDisplayHealth()
    {
        rickHealthBar.fillAmount = ((float)rickStats.GetHealth() / (float) rickStats.GetMaxHealth());
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Home Screen");
    }
    IEnumerator DelayedAction()
    {
        yield return new WaitForSeconds(5f);
        victoryCanvas.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        onScreenStuff.SetActive(false);
    }

}
