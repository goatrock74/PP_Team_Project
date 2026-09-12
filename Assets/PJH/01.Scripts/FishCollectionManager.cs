using System;
using System.Collections.Generic;
using PJH.Scripts;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PJH._01.Scripts
{
    public class FishCollectionManager : MonoBehaviour
    {
        public event Action OnCollectionChanged;
        
        private const string SaveKey = "FishCollection";

        private readonly HashSet<string> discoveredFishIds = new();
        [SerializeField] private GameObject fishUI;
        private bool isView = false;

        [Serializable]
        private class FishCollectionSaveData
        {
            public List<string> discoveredFishIds = new();
            
        }

        private void Awake()
        {
            LoadCollection();
        }

        private void Update()
        {
            if (Keyboard.current.tKey.wasPressedThisFrame)
            {
                isView = !isView;
                fishUI.gameObject.SetActive(isView);
            }
        }


        public bool IsDiscovered(FishDataSO fishData)
        {
            if (fishData == null)
            {
                return false;
            }

            return discoveredFishIds.Contains(fishData.fishID);
        }


        public void DiscoverFish(FishDataSO fishData)
        {
            if (fishData == null) return;

            if (string.IsNullOrEmpty(fishData.fishID))
            {
                Debug.LogWarning($"{fishData.name} ID 비어있음");
                return;
            }

            if (!discoveredFishIds.Add(fishData.fishID))
                return;

            SaveCollection();
            OnCollectionChanged?.Invoke();

            Debug.Log($"{fishData.displayName} 도감 저장 완료");
            
        }

        private void SaveCollection()
        {
            FishCollectionSaveData saveData = new()
            {
                discoveredFishIds = new List<string>(discoveredFishIds)
            };
            
            string json = JsonUtility.ToJson(saveData);
            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.Save();
            
            
        }

        private void LoadCollection()
        {
            discoveredFishIds.Clear();

            if (!PlayerPrefs.HasKey(SaveKey)) return;
            
            string json = PlayerPrefs.GetString(SaveKey);
            FishCollectionSaveData saveData = JsonUtility.FromJson<FishCollectionSaveData>(json);

            if (saveData?.discoveredFishIds == null) return;

            foreach (string fishId in saveData.discoveredFishIds)
            {
                discoveredFishIds.Add(fishId);
            }
        }
        [ContextMenu("도감 발견 초기화")]
        private void ResetCollection()
        {
            discoveredFishIds.Clear();
            PlayerPrefs.DeleteKey(SaveKey);
            OnCollectionChanged?.Invoke();
        }
    }
}