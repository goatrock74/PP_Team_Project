using UnityEngine;

[CreateAssetMenu(fileName = "ToolListSO", menuName = "Scriptable Objects/ToolListSO")]
public class ToolListSO : ScriptableObject
{
    [field: SerializeField] public ToolSO[] ToolList { get; private set; }

    private void OnValidate()
    {
        for (int i = 0; i < ToolList.Length; ++i)
        {
            if (ToolList[i] == null)
                continue;
            if (ToolList[i].GetType() == typeof(ToolSO))
            {
                Debug.LogError($"{name}의 목록에 맞지 않는 타입이 있습니다. {ToolList[i].name}");
                ToolList[i] = null;
            }
        }
    }
}
