/// <summary>
/// 도끼로 벨 수 있는 것. 나무, 그루터기, 나무 상자 등이 구현한다.
/// </summary>
public interface IChoppable
{
    bool CanChop(AxeSO axe);
 
    /// <summary>벴을 때. 뭐라도 일어났으면 true</summary>
    bool Chop(AxeSO axe, in ToolUseContext ctx);
}
 
/// <summary>
/// 긴낫으로 채집할 수 있는 것. 야생 풀숲, 덤불 등이 구현한다.
/// </summary>
public interface IForageable
{
    bool CanForage(ScytheSO scythe);
 
    /// <summary>채집했을 때. 뭐라도 얻었으면 true</summary>
    bool Forage(ScytheSO scythe, in ToolUseContext ctx);
}
 
// 인터페이스를 도구마다 따로 두는 이유:
// 하나의 큰 인터페이스에 Chop/Forage/Water 를 다 넣으면, 나무가 쓰지도 않는
// Forage 를 억지로 구현해야 한다. 작게 나누면 필요한 것만 골라 붙일 수 있고,
// "이 오브젝트에 도끼가 통하나?" 를 타입만으로 판정할 수 있다.