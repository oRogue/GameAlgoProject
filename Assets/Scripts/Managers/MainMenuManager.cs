using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    public void StartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Game");
    }

   public void QuitGame()
   {
        Application.Quit();
   }
}
