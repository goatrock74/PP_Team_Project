namespace KSM._00.Scripts.Crop
{
    /// <summary>
    /// 게임 내 시간의 출처. 외부 TimeManager 가 이걸 구현해서 CropManager 에 꽂으면
    /// 작물이 그 시계를 따라간다. 나중에 잠자기·시간 스킵을 넣어도 작물이 같이 움직인다.
    ///
    /// 안 꽂혀 있으면 CropManager 가 자체 시계(실제 시간)로 돈다.
    /// </summary>
    public interface IGameClock
    {
        /// <summary>
        /// 게임 시작부터 흐른 인게임 일수. 계속 증가하기만 하는 값이어야 한다.
        /// 예: 3일차 정오 → 2.5
        /// </summary>
        float TotalGameDays { get; }
    }
 
    /// <summary>
    /// 작물 성장에 영향을 주는 외부 시스템이 구현하는 인터페이스.
    /// 계절, 날씨, 비료, 스킬 등 무엇이든 이걸 구현해서 CropManager 에 꽂으면 된다.
    ///
    /// 작물 쪽은 "누가" 영향을 주는지 전혀 모른다. 배수 몇 개만 물어볼 뿐이다.
    /// 그래서 계절 시스템을 통째로 갈아엎어도 작물 코드는 안 바뀐다.
    /// </summary>
    public interface IGrowthModifier
    {
        /// <summary>성장 속도 배수. 1 = 보통, 1.5 = 1.5배 빠름, 0.5 = 절반 속도</summary>
        float GrowthSpeedMultiplier { get; }
 
        /// <summary>수확량 배수. 1 = 보통, 1.5 = 1.5배</summary>
        float YieldMultiplier { get; }
 
        /// <summary>품질 확률에 더할 보너스. 0 = 없음, 0.1 이면 좋음·최상 확률이 각각 +10%p</summary>
        float QualityBonus { get; }
 
        /// <summary>
        /// '최상' 등급 수확을 허용하는가. 제한할 이유가 없으면 <b>true</b> 를 돌려주면 된다.
        ///
        /// 꽂혀 있는 보정자가 <b>하나라도</b> false 를 내면 최상이 '좋음'으로 강등된다.
        /// (농사 마스터리 10레벨 해금이 이걸로 동작한다)
        /// </summary>
        bool AllowBestQuality { get; }
 
        /// <summary>지금 이 작물을 심을 수 있는가. 제철 제한이 없으면 항상 true 를 돌려주면 된다</summary>
        bool CanPlantNow(CropSO crop);
    }
}