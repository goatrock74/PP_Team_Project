using System;
 
/// <summary>
/// 작물을 심을 수 있는 계절. 여러 계절을 동시에 고를 수 있도록 비트 플래그다.
///
/// 외부 시간 시스템의 계절 enum 을 직접 쓰지 않고 따로 두는 이유:
/// 그쪽 enum 이 바뀌거나 없어져도 CropSO 에셋이 깨지지 않게 하려고.
/// 둘 사이의 변환은 SeasonGrowthAdapter 한 곳에서만 한다.
/// </summary>
[Flags]
public enum CropSeason
{
    None = 0,
    Spring = 1 << 0,
    Summer = 1 << 1,
    Autumn = 1 << 2,
    Winter = 1 << 3,
 
    All = Spring | Summer | Autumn | Winter,
}