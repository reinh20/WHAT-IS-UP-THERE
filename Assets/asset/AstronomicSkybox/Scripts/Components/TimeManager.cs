using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Astronomy
{
    public enum TimeUnit { Days, Hours, Minutes, Seconds }

    /// <summary>
    /// Manage game time and schedule Actions to pe taken at certain times.
    /// </summary>
    public class TimeManager : MonoBehaviour
    {
        public string startTime = "2026-10-25T00:00:00";
        public bool startAtRealTime = false;
        public float initialTimeScale = 1f;

        [Header("Night Only Settings")]
        [Tooltip("If enabled, keeps the time strictly at night (loops between 21:00 and 04:00 so the sun never rises)")]
        public bool lockAtNight = true;

        public static DateTime currentTime = new DateTime();
        public static float timeScale = 1f;

        private void OnValidate()
        {
            if (!startAtRealTime)
            {
                if (!DateTime.TryParse(startTime, out DateTime result))
                    result = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);

                currentTime = result;
                startTime = result.ToString("s");
            }
        }

        private void Awake()
        {
            if (startAtRealTime || !DateTime.TryParse(startTime, out DateTime result))
                result = lockAtNight ? new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0) : DateTime.Now;

            currentTime = result;
            timeScale = initialTimeScale;
        }

        private void Update()
        {
            currentTime = currentTime.AddSeconds(Time.deltaTime * timeScale);

            if (lockAtNight)
            {
                // If it passes 04:30 AM (dawn), loop back to 21:00 PM (night)
                if (currentTime.Hour >= 5 && currentTime.Hour < 20)
                {
                    currentTime = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, 21, 0, 0);
                }
            }
        }

        public static void SetCurrentTime(DateTime t)
        {
            currentTime = t;
        }
    }
}
