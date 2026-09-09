using UnityEngine;

[CreateAssetMenu(fileName = "ToolSO", menuName = "Scriptable Objects/ToolSO")]
public class ToolSOPJW : ScriptableObject
{
    [field:SerializeField]public string Name {  get;private set; }

    [field:SerializeField]public int Price {  get; private set; }

    [field: SerializeField] public string ToolMenu {  get; private set; }

    [field: SerializeField] public bool Itme_bought { get; set; }
}
