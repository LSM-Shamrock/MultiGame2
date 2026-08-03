
public class GameConfig
{
    public const float DEFAULT_MP_REGEN_SPEED = 0.5f;
    public const float GAME_DURATION = 60 * 3;
    public const float MP_DOUBLE_START_TIME = 60 * 1;

    public const float X_MIN = -18;
    public const float X_MAX = 18;
    public const float Y_MIN = -5;
    public const float Y_MAX = 5;

    public const float CHASE_DISTANCE = 5;
    public const float BACKOFF_RATIO = 0.5f;
    public const float BACKOFF_SPEED_RATIO = 0.5f;

    public static readonly CoreData CORE_DATA_MAIN = new CoreData(
        scale: 1f,
        colliderWidth: 2.5f,
        colliderHeight: 3.5f,
        health: 6000);
    public static readonly CoreData CORE_DATA_SUB = new CoreData(
        scale: 0.75f,
        colliderWidth: 2.5f,
        colliderHeight: 3.5f,
        health: 4000);


    public static float GetUnitHeight(AltitudeType altitudeType)
    {
        float result = altitudeType switch
        {
            AltitudeType.Ground => 0,
            AltitudeType.Air => 1.5f,
            _ => 0,
        };
        return result;
    }
}