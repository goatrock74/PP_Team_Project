using UnityEngine;

[CreateAssetMenu(fileName = "FeedSO", menuName = "Scriptable Objects/FeedSO")]
public class FeedSO : ScriptableObject
{
    [field: SerializeField] public string Name { get; private set; }

    [field: SerializeField] public int Price { get; private set; }

    [field: SerializeField] public string FeedMenu { get; private set; }

    [field: SerializeField] public bool Itme_bought { get; set; }
}
