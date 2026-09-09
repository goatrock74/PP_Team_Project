using System;
 
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