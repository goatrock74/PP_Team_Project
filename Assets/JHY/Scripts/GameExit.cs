using UnityEngine;

public class GameExit : MonoBehaviour
{
    [SerializeField] private GameObject isQuitGame;
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
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
