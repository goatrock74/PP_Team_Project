using System;
using UnityEngine;
 
/// <summary>
/// 마스터리 하나의 <b>진행 상황</b>. 지금 몇 레벨이고 경험치가 얼마나 쌓였는지.
///
/// MasterySO 가 아니라 여기에 들어 있는 이유:
/// SO 는 에디터에서 값이 그대로 저장돼버려서 플레이할 때마다 레벨이 안 초기화되고,
/// 반대로 빌드에서는 저장이 안 돼서 게임을 끄면 사라진다. 런타임 상태를 담기에 부적합하다.
///
/// [Serializable] 이라 JsonUtility 로 그대로 세이브할 수 있고,
/// MasteryManager 인스펙터에서 플레이 중 값을 눈으로 확인할 수도 있다.
/// </summary>
[Serializable]
public class MasteryProgress
{
    public MasteryType type;
 
    [Min(1)] public int level = 1;
 
    [Tooltip("현재 레벨에서 쌓인 경험치. 다음 레벨에 필요한 양을 넘으면 레벨업")]
    [Min(0)] public int exp;
 
    public MasteryProgress() { }
 
    public MasteryProgress(MasteryType type)
    {
        this.type = type;
        level = 1;
        exp = 0;
    }
}
 
/// <summary>세이브/로드용 묶음. JsonUtility 는 최상위 배열을 못 다뤄서 클래스로 감싼다</summary>
[Serializable]
public class MasterySaveData
{
    public MasteryProgress[] entries;
}