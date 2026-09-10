using UnityEngine;

public class GameExit : MonoBehaviour
{
    [SerializeField] private GameObject isQuitGame;
    public void QuitGame()
    {
        UnityEditor.EditorApplication.isPlaying = false;
        Application.Quit();
    }
    public void IsQuitGame()
    {
        isQuitGame.SetActive(true);
    }
    public void NoQuitGame()
    {
        isQuitGame.SetActive(false);
    }
}
