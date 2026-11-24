using System;
using System.Globalization;
using UnityEngine;

namespace CarCrash
{
    public static class TimeTrialCareer
    {
        private const string DailyDateKey = "CarCrash.Trial.DailyDate";
        private const string DailyBestKey = "CarCrash.Trial.DailyBest";
        public static readonly ImpactCourse[] Levels =
        {
            Trial(0, 74, 57, 45), Trial(1, 65, 49, 37),
            Trial(2, 63, 47, 35), Trial(3, 54, 40, 29)
        };

        private static ImpactCourse Trial(int index, int bronze, int silver, int gold)
        {
            var course = ImpactCourse.Catalog[index];
            return new ImpactCourse("trial-" + course.Id, course.Name,
                "Follow the arrows through each checkpoint. Cross the finish as fast as you can.",
                course.Start, course.Heading.eulerAngles.y, bronze, silver, gold, bronze + 45,
                course.Gates, Array.Empty<ImpactTargetSpec>());
        }

        public static int DailyLevel(DateTime utcDay) => (int)(utcDay.Date.Ticks / TimeSpan.TicksPerDay % Levels.Length);
        public static string DayKey(DateTime utcDay) => utcDay.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        public static bool Unlocked(int index) => index >= 0 && index < Levels.Length && (index == 0 || Best(index - 1) > 0f);
        public static float Best(int index) => PlayerPrefs.GetFloat("CarCrash.Trial.Best." + Levels[index].Id);
        public static float DailyBest(string day) => PlayerPrefs.GetString(DailyDateKey) == day ? PlayerPrefs.GetFloat(DailyBestKey) : 0f;

        public static bool Record(int index, string dailyDay, float time)
        {
            if (float.IsNaN(time) || float.IsInfinity(time) || time <= 0f) return false;
            var best = dailyDay == null ? Best(index) : DailyBest(dailyDay);
            if (best > 0f && time >= best) return false;
            if (dailyDay == null)
                PlayerPrefs.SetFloat("CarCrash.Trial.Best." + Levels[index].Id, time);
            else
            {
                PlayerPrefs.SetString(DailyDateKey, dailyDay);
                PlayerPrefs.SetFloat(DailyBestKey, time);
            }
            PlayerPrefs.Save();
            return true;
        }

        public static string Medal(ImpactCourse course, float time) =>
            time <= course.Gold ? "Gold" : time <= course.Silver ? "Silver" : time <= course.Bronze ? "Bronze" : "Finisher";

        public static string Format(float time)
        {
            var hundredths = Mathf.FloorToInt(time * 100f);
            return $"{hundredths / 6000:00}:{hundredths / 100 % 60:00}.{hundredths % 100:00}";
        }
    }
}
