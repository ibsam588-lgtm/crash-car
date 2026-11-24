using System;
using System.Globalization;
using UnityEngine;

namespace CarCrash
{
    public sealed partial class CarCrashHudInput
    {
        public bool SelectedRideLocked => Garage?.Current != null && RideLocked(Garage.Current.id);

        public static bool IsPremiumRide(string id) => id is "velocity" or "bulwark";

        public static bool RideLocked(string id) => IsPremiumRide(id) &&
            (!long.TryParse(PlayerPrefs.GetString(RentalKey(id)), NumberStyles.None, CultureInfo.InvariantCulture, out var expires) ||
                expires <= DateTime.UtcNow.Ticks);

        public void RewardPremiumVideo(string id)
        {
            if (Phase != RunPhase.Showroom || Garage?.Current?.id != id || !RideLocked(id)) return;
            PlayerPrefs.SetString(RentalKey(id), DateTime.UtcNow.AddDays(1).Ticks.ToString(CultureInfo.InvariantCulture));
            RememberRide();
            Hud.RefreshVehicle();
        }

        private static string RentalKey(string id) => "CarCrash.RentalUntil." + id;

        private int SavedUsableRide()
        {
            if (Garage.vehicles.Length == 0) return 0;
            var index = Mathf.Clamp(PlayerPrefs.GetInt("CarCrash.Vehicle", 0), 0, Garage.vehicles.Length - 1);
            return !RideLocked(Garage.vehicles[index].id) ? index : 0;
        }

        private void RememberRide()
        {
            PlayerPrefs.SetInt("CarCrash.Vehicle", Garage.SelectedIndex);
            PlayerPrefs.Save();
        }

        private void RestoreUsableRide()
        {
            if (!SelectedRideLocked) return;
            Garage.Select(SavedUsableRide());
            RestoreVehiclePaint();
            Hud.RefreshPaint();
            Hud.RefreshVehicle();
        }
    }
}
