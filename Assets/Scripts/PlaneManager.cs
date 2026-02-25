using System;
using UnityEngine;

public class PlaneManager : MonoBehaviour
{
    [System.Serializable]
    public class PlaneData
    {
        public GameObject planePrefab;

        [Header("UI Toolkit")]
        public string uiNameElement; // MUST match the UXML element name (ex: "LearJet45")

        [Header("Shop")]
        public int price; // Plane 0 will be free automatically (price ignored for index 0)

        public int speed;
        public int acceleration;
        public int deceleration;
        public float rotation;
        public float levelSpeed;

        [Header("Camera Settings")]
        public Vector3 shoulderOffset = new Vector3(-0.06f, 2.47f, -20f);
    }

    [Header("Planes")]
    public PlaneData[] planes;

    [Header("Economy")]
    [SerializeField] private int startingCoins = 0;

    public static PlaneManager Instance { get; private set; }

    public event Action OnEconomyChanged;

    public int Coins { get; private set; }

    private bool[] purchasedPlanes;

    // PlayerPrefs keys
    private const string CoinsKey = "PM_Coins";
    private const string EconomyInitKey = "PM_EconomyInitialized";
    private const string PlanePurchasedPrefix = "PM_PlanePurchased_";

    private void Awake()
    {

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeEconomy();
    }

    private void InitializeEconomy()
    {
        int planeCount = planes != null ? planes.Length : 0;
        purchasedPlanes = new bool[Mathf.Max(planeCount, 0)];

        bool initialized = PlayerPrefs.GetInt(EconomyInitKey, 0) == 1;

        if (!initialized)
        {
            // First time setup
            Coins = startingCoins;

            // First plane is always free
            if (purchasedPlanes.Length > 0)
                purchasedPlanes[0] = true;

            SaveEconomy();
            return;
        }

        // Load existing data
        Coins = PlayerPrefs.GetInt(CoinsKey, startingCoins);

        for (int i = 0; i < purchasedPlanes.Length; i++)
        {
            purchasedPlanes[i] = PlayerPrefs.GetInt(GetPlanePurchasedKey(i), 0) == 1;
        }

        // Safety: first plane always free
        if (purchasedPlanes.Length > 0)
            purchasedPlanes[0] = true;
    }

    private string GetPlanePurchasedKey(int planeIndex)
    {
        return PlanePurchasedPrefix + planeIndex;
    }

    public bool IsValidPlaneIndex(int index)
    {
        return planes != null && index >= 0 && index < planes.Length;
    }

    public int GetPlanePrice(int index)
    {
        if (!IsValidPlaneIndex(index)) return 0;
        if (index == 0) return 0; // first plane is free
        return Mathf.Max(0, planes[index].price);
    }

    public bool IsPlanePurchased(int index)
    {
        if (!IsValidPlaneIndex(index)) return false;
        if (index == 0) return true; // first plane is always free

        if (purchasedPlanes == null || purchasedPlanes.Length != planes.Length)
            InitializeEconomy();

        return purchasedPlanes[index];
    }

    public bool TryPurchasePlane(int index)
    {
        if (!IsValidPlaneIndex(index))
        {
            Debug.LogWarning($"PlaneManager: Invalid plane index {index}");
            return false;
        }

        // First plane is always free
        if (index == 0)
        {
            if (purchasedPlanes != null && purchasedPlanes.Length > 0)
                purchasedPlanes[0] = true;

            SaveEconomy();
            OnEconomyChanged?.Invoke();
            return true;
        }

        if (IsPlanePurchased(index))
        {
            // Already purchased
            return true;
        }

        int cost = GetPlanePrice(index);

        if (Coins < cost)
        {
            Debug.Log($"Not enough coins. Need {cost}, have {Coins}");
            return false;
        }

        Coins -= cost;
        purchasedPlanes[index] = true;

        SaveEconomy();
        OnEconomyChanged?.Invoke();

        Debug.Log($"✅ Purchased plane {index} for {cost} coins. Remaining coins: {Coins}");
        return true;
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0) return;

        Coins += amount;
        SaveEconomy();
        OnEconomyChanged?.Invoke();

        Debug.Log($"✅ Added {amount} coins. Total: {Coins}");
    }

    public bool TrySpendCoins(int amount)
    {
        if (amount <= 0) return true;
        if (Coins < amount) return false;

        Coins -= amount;
        SaveEconomy();
        OnEconomyChanged?.Invoke();
        return true;
    }

    public void SaveEconomy()
    {
        PlayerPrefs.SetInt(EconomyInitKey, 1);
        PlayerPrefs.SetInt(CoinsKey, Coins);

        if (purchasedPlanes != null)
        {
            for (int i = 0; i < purchasedPlanes.Length; i++)
            {
                // Force first plane to stay free
                bool purchased = (i == 0) || purchasedPlanes[i];
                PlayerPrefs.SetInt(GetPlanePurchasedKey(i), purchased ? 1 : 0);
            }
        }

        PlayerPrefs.Save();
    }

    // Optional helper for testing in editor
    [ContextMenu("Reset Economy (Debug)")]
    public void ResetEconomyDebug()
    {
        PlayerPrefs.DeleteKey(EconomyInitKey);
        PlayerPrefs.DeleteKey(CoinsKey);

        if (planes != null)
        {
            for (int i = 0; i < planes.Length; i++)
            {
                PlayerPrefs.DeleteKey(GetPlanePurchasedKey(i));
            }
        }

        PlayerPrefs.Save();
        InitializeEconomy();
        OnEconomyChanged?.Invoke();

        Debug.Log("🧹 Economy reset.");
    }
}