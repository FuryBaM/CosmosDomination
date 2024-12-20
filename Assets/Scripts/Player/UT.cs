using System;
using UnityEngine;

public static class UT
{
    public static bool InBox(float x, float y, float startX, float startY, float width, float height)
    {
        return x > startX && x < startX + width && y > startY && y < startY + height;
    }
    public static float GetRotation(Vector2 from, Vector2 to)
    {
        float deltaX = from.x - to.x;
        float deltaY = from.y - to.y;
        float distance = MathF.Sqrt(deltaX * deltaX + deltaY * deltaY);

        float rotation;
        if (deltaY < 0)
        {
            rotation = 2 * MathF.PI - MathF.Acos(deltaX / distance);
        }
        else
        {
            rotation = MathF.Acos(deltaX / distance);
        }

        return FixRotation(rotation * 180 / MathF.PI - 90);
    }

    public static float FixRotation(float angle)
    {
        if (angle > 180)
        {
            angle -= 360;
        }
        if (angle < -180)
        {
            angle += 360;
        }
        return angle;
    }
}
