#if UNITY_EDITOR

using System.Collections.Generic;
using PJH._01.Scripts;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PJH.Scripts
{
    public class FishCollectionDebugTest : MonoBehaviour
    {
        [SerializeField]
        private FishCollectionManager collectionManager;

        [SerializeField]
        private FishDataSO[] fishDataList;

        private void Update()
        {
            if (Keyboard.current == null)
                return;

            if (Keyboard.current.fKey.wasPressedThisFrame)
            {
                DiscoverRandomFish();
            }
        }

        private void DiscoverRandomFish()
        {
            List<FishDataSO> undiscoveredFish = new();

            foreach (FishDataSO fishData in fishDataList)
            {
                if (fishData == null)
                    continue;

                if (!collectionManager.IsDiscovered(fishData))
                {
                    undiscoveredFish.Add(fishData);
                }
            }

            if (undiscoveredFish.Count == 0)
            {
                Debug.Log("모든 물고기를 발견했습니다.");
                return;
            }

            int randomIndex = Random.Range(
                0,
                undiscoveredFish.Count);

            FishDataSO selectedFish =
                undiscoveredFish[randomIndex];

            collectionManager.DiscoverFish(selectedFish);

            Debug.Log(
                $"테스트 발견: {selectedFish.displayName}");
        }
    }
}

#endif