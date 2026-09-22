using TMPro;
using UnityEngine;

// The one persistent balance used by every shop. Scene UI only observes it.
[DefaultExecutionOrder(-500)]
public class PlayerWallet : MonoBehaviour
{
    private const string SaveKey = "TotalMoney";
    private const int StartingMoney = 10000;
    private static PlayerWallet instance;

    public static PlayerWallet Instance
    {
        get
        {
            if (instance == null)
                new GameObject("PlayerWallet (Shared)").AddComponent<PlayerWallet>();
            return instance;
        }
    }

    private int currentMoney;
    public int CurrentMoney => instance == this ? currentMoney : Instance.CurrentMoney;
    public event System.Action<int> OnMoneyChanged;

    // Retained for older scenes. Their text is migrated to a scene-local display.
    [SerializeField] private TextMeshProUGUI moneyText;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic() => instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap() { _ = Instance; }

    private void Awake()
    {
        if (moneyText != null)
        {
            // Never move a Canvas child into DontDestroyOnLoad.
            var display = moneyText.GetComponent<Walletudisplay>();
            if (display == null) display = moneyText.gameObject.AddComponent<Walletudisplay>();
            display.SetTextTarget(moneyText);
            _ = Instance;
            Destroy(this);
            return;
        }
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }
        instance = this;
        currentMoney = Mathf.Max(0, PlayerPrefs.GetInt(SaveKey, StartingMoney));
        DontDestroyOnLoad(gameObject);
    }

    public bool TrySpendMoney(int amount)
    {
        if (instance != this) return Instance.TrySpendMoney(amount);
        if (amount < 0 || currentMoney < amount) return false;
        if (amount == 0) return true;
        currentMoney -= amount;
        SaveAndNotify();
        return true;
    }

    public void AddMoney(int amount)
    {
        if (instance != this) { Instance.AddMoney(amount); return; }
        if (amount <= 0) return;
        currentMoney = (int)System.Math.Min(int.MaxValue, (long)currentMoney + amount);
        SaveAndNotify();
    }

    private void SaveAndNotify()
    {
        Save();
        OnMoneyChanged?.Invoke(currentMoney);
    }

    private void Save()
    {
        if (instance != this) return;
        PlayerPrefs.SetInt(SaveKey, currentMoney);
        PlayerPrefs.Save();
    }

    private void OnApplicationPause(bool paused) { if (paused) Save(); }
    private void OnApplicationQuit() => Save();

    private void OnDestroy()
    {
        if (instance != this) return;
        Save();
        instance = null;
    }
}
