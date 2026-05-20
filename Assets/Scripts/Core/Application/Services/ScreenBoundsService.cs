using System;

public class ScreenBoundsService
{
    public float MinX { get; }
    public float MaxX { get; }
    public float MinY { get; }
    public float MaxY { get; }

    public ScreenBoundsService(float minX, float maxX, float minY, float maxY)
    {
        MinX = minX;
        MaxX = maxX;
        MinY = minY;
        MaxY = maxY;
    }

    public float ClampX(float x)
    {
        return Math.Clamp(x, MinX, MaxX);
    }

    public float ClampY(float y)
    {
        return Math.Clamp(y, MinY, MaxY);
    }
}