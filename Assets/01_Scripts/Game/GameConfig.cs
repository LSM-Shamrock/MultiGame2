
public class GameConfig
{
    public const float DEFAULT_MP_REGEN_SPEED = 0.5f;
    public const float GAME_DURATION = 60 * 3;
    public const float MP_DOUBLE_START_TIME = 60 * 1;

    public const float X_MIN = -18;
    public const float X_MAX = 18;
    public const float Y_MIN = -5;
    public const float Y_MAX = 5;

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