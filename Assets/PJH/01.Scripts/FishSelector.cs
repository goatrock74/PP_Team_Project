using System;
using UnityEngine;

namespace PJH.Scripts
{
    public class FishSelector : MonoBehaviour
    {
        [SerializeField] private FishDataSO[] fishDataSO;
        public FishDataSO selctedFishDataSO { get; private set; }


        public FishDataSO RandomFish()
        {
            float random =  UnityEngine.Random.Range(0f, 100f);

            if (random <= 10)
            {
                // 10%물고기
                selctedFishDataSO = fishDataSO[2];
            }
            
            else if (random <= 40)
            {
                // 30%물고기
                selctedFishDataSO = fishDataSO[1];
            }

            else
            {
                //60% 물고기
                selctedFishDataSO = fishDataSO[0];
            }
            return selctedFishDataSO;
        }
    }
}