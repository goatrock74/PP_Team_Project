using UnityEngine;
using UnityEngine.InputSystem;

public class InGameUIManager : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private GameObject settingPanel;
    [SerializeField]private AudioClip closeSoundl;
    [SerializeField] private AudioClip settingpanel;
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
}
