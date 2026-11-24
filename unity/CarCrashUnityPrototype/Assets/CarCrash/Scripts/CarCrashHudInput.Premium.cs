using System;
using System.Globalization;
using UnityEngine;

namespace CarCrash
{
    public sealed partial class CarCrashHudInput
    {
        public bool SelectedRideLocked => RideLocked(Garage?.Current);

        public static bool RideLocked(GarageVehicle ride) => ride?.premium == true && RentalRemaining(ride) == TimeSpan.Zero;

        public static TimeSpan RentalRemaining(GarageVehicle ride)
        {
            if (ride?.premium != true || !long.TryParse(PlayerPrefs.GetString(RentalKey(ride.id)),
                NumberStyles.None, CultureInfo.InvariantCulture, out var expires) || expires > DateTime.MaxValue.Ticks)
                return TimeSpan.Zero;
            return TimeSpan.FromTicks(Math.Max(0L, expires - DateTime.UtcNow.Ticks));
        }

        public void RewardPremiumVideo(string id)
        {
            if (Phase != RunPhase.Showroom || Garage?.Current?.id != id || !SelectedRideLocked) return;
            PlayerPrefs.SetString(RentalKey(id), DateTime.UtcNow.AddDays(1).Ticks.ToString(CultureInfo.InvariantCulture));
            RememberRide();
            Hud.RefreshVehicle();
        }

        private static string RentalKey(string id) => "CarCrash.RentalUntil." + id;

        private int SavedUsableRide()
        {
            if (Garage.vehicles.Length == 0) return 0;
            var index = Mathf.Clamp(PlayerPrefs.GetInt("CarCrash.Vehicle", 0), 0, Garage.vehicles.Length - 1);
            return !RideLocked(Garage.vehicles[index]) ? index : 0;
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
