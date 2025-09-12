using UnityEngine;
using System;

public class TimeManager : MonoBehaviour
{
    #region Singleton
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
    #endregion

    #region Properties
    /// <summary>
    /// Current UTC time.
    /// </summary>
    public DateTime UtcNow => DateTime.UtcNow;

    /// <summary>
    /// Current UTC time as Unix timestamp (seconds since 1970-01-01).
    /// </summary>
    public long NowUnix => new DateTimeOffset(UtcNow).ToUnixTimeSeconds();
    #endregion

    #region Public Methods
    /// <summary>
    /// Converts a DateTime to Unix timestamp.
    /// </summary>
    public long ToUnix(DateTime dt)
    {
        if (dt == DateTime.MinValue)
            dt = DateTime.UtcNow;

        if (dt.Kind != DateTimeKind.Utc)
            dt = dt.ToUniversalTime();

        return new DateTimeOffset(dt).ToUnixTimeSeconds();
    }

    /// <summary>
    /// Converts a Unix timestamp to UTC DateTime.
    /// </summary>
    public DateTime FromUnix(long unix) => DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime;
    #endregion
}
