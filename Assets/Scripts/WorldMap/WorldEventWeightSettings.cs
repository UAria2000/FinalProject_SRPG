using UnityEngine;

[CreateAssetMenu(menuName = "World Map/Event Weight Settings", fileName = "WorldEventWeightSettings")]
public class WorldEventWeightSettings : ScriptableObject
{
    [Min(0)] public int battleWeight = 50;
    [Min(0)] public int restWeight = 10;
    [Min(0)] public int treasureWeight = 10;
    [Min(0)] public int merchantWeight = 10;
    [Min(0)] public int questWeight = 10;
    [Min(0)] public int eliteWeight = 10;

    public int GetWeight(WorldTileEventType eventType)
    {
        switch (eventType)
        {
            case WorldTileEventType.Battle: return battleWeight;
            case WorldTileEventType.Rest: return restWeight;
            case WorldTileEventType.Treasure: return treasureWeight;
            case WorldTileEventType.Merchant: return merchantWeight;
            case WorldTileEventType.Quest: return questWeight;
            case WorldTileEventType.EliteBattle: return eliteWeight;
            default: return 0;
        }
    }
}
