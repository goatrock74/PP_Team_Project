using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class InGameUIManager : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private GameObject settingPanel;
    [SerializeField]private AudioClip closeSoundl;
    [SerializeField] private AudioClip settingpanel;
    [SerializeField] private GameObject checkPanel;
    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SoundManager.Instance.PlaySFX(settingpanel);
            ToggleSettingPanel();
        }
    }

    public void ToggleSettingPanel()
    {
        if (settingPanel == null) return;

        bool isCurrentlyActive = settingPanel.activeSelf;
        settingPanel.SetActive(!isCurrentlyActive);

    }

    public void CloseSettingPanel()
    {
        if (settingPanel != null)
        {
            SoundManager.Instance.PlaySFX(closeSoundl);
            settingPanel.SetActive(false);
        }
    }
    public void OpenCheckPanel()
    {
        SoundManager.Instance.PlaySFX(closeSoundl);
        checkPanel.SetActive(true);
    }
    public void CloseCheckPanel()
    {
        SoundManager.Instance.PlaySFX(closeSoundl);
        checkPanel.SetActive(false);
    }
    public void GoMain()
    {
        SoundManager.Instance.PlaySFX(closeSoundl);
        SceneManager.LoadScene(0);
    }
}
