using UnityEngine;

[CreateAssetMenu(fileName = "FeedListSO", menuName = "Scriptable Objects/FeedListSO")]
public class FeedListSO : ScriptableObject
{
    [field: SerializeField] public FeedSO[] FeedList { get; private set; }

    private void OnValidate()
    {
        for (int i = 0; i < FeedList.Length; ++i)
        {
            if (FeedList[i] == null)
                continue;
            if (FeedList[i].GetType() == typeof(FeedSO))
            {
                Debug.LogError($"{name}의 목록에 맞지 않는 타입이 있습니다. {FeedList[i].name}");
                FeedList[i] = null;
            }
        }
    }
}
