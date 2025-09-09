using UnityEngine;
using System;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// เวลาปัจจุบัน UTC
    public DateTime UtcNow => DateTime.UtcNow;

    /// แปลงเป็น Unix
    public long NowUnix => new DateTimeOffset(UtcNow).ToUnixTimeSeconds();

    public long ToUnix(DateTime dt)
    {
        if (dt == DateTime.MinValue)
            dt = DateTime.UtcNow;

        if (dt.Kind != DateTimeKind.Utc)
            dt = dt.ToUniversalTime();

        return new DateTimeOffset(dt).ToUnixTimeSeconds();
    }

    public DateTime FromUnix(long unix) => DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime;
}
