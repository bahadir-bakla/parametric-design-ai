using System;
using Rhino.Geometry;

namespace RoofAI
{
    public class SunPosition
    {
        public double Altitude { get; set; }
        public double Azimuth { get; set; }
        public Vector3d SunVector { get; set; }
        public bool IsAboveHorizon => Altitude > 0;
    }

    public static class SunPathCalculator
    {
        public static SunPosition CalculateSunPosition(double latitude, double longitude,
            int year, int month, int day, double hour, int timezone)
        {
            int dayOfYear = GetDayOfYear(year, month, day);

            double B = (360.0 / 365.0) * (dayOfYear - 81) * Math.PI / 180.0;

            double EoT = 9.87 * Math.Sin(2 * B) - 7.53 * Math.Cos(B) - 1.5 * Math.Sin(B);

            double LSTM = 15.0 * timezone;
            double TC = 4 * (longitude - LSTM) + EoT;

            double LST = hour + TC / 60.0;
            double HRA = 15.0 * (LST - 12.0);

            double declination = 23.45 * Math.Sin((360.0 / 365.0) * (dayOfYear - 81) * Math.PI / 180.0);

            double latRad = latitude * Math.PI / 180.0;
            double decRad = declination * Math.PI / 180.0;
            double hraRad = HRA * Math.PI / 180.0;

            double sinAlt = Math.Sin(latRad) * Math.Sin(decRad) +
                           Math.Cos(latRad) * Math.Cos(decRad) * Math.Cos(hraRad);
            double altitude = Math.Asin(Math.Max(-1, Math.Min(1, sinAlt))) * 180.0 / Math.PI;

            double cosAz = (Math.Sin(decRad) - Math.Sin(latRad) * Math.Sin(altitude * Math.PI / 180.0)) /
                          (Math.Cos(latRad) * Math.Cos(altitude * Math.PI / 180.0));
            cosAz = Math.Max(-1, Math.Min(1, cosAz));
            double azimuth = Math.Acos(cosAz) * 180.0 / Math.PI;

            if (HRA > 0)
                azimuth = 360.0 - azimuth;

            double altRad = altitude * Math.PI / 180.0;
            double azRad = azimuth * Math.PI / 180.0;

            Vector3d sunVector = new Vector3d(
                -Math.Sin(azRad) * Math.Cos(altRad),
                -Math.Cos(azRad) * Math.Cos(altRad),
                -Math.Sin(altRad)
            );

            return new SunPosition
            {
                Altitude = altitude,
                Azimuth = azimuth,
                SunVector = sunVector
            };
        }

        public static SunPosition[] CalculateSunPath(double latitude, double longitude,
            int year, int month, int day, int timezone, double startHour = 6, double endHour = 20, double step = 0.5)
        {
            int count = (int)((endHour - startHour) / step) + 1;
            var positions = new SunPosition[count];

            for (int i = 0; i < count; i++)
            {
                double hour = startHour + i * step;
                positions[i] = CalculateSunPosition(latitude, longitude, year, month, day, hour, timezone);
            }

            return positions;
        }

        public static Curve CreateSunPathCurve(SunPosition[] positions, double radius = 50)
        {
            var points = new System.Collections.Generic.List<Point3d>();

            foreach (var pos in positions)
            {
                if (!pos.IsAboveHorizon) continue;

                double altRad = pos.Altitude * Math.PI / 180.0;
                double azRad = pos.Azimuth * Math.PI / 180.0;

                double projectedRadius = radius * Math.Cos(altRad);
                double x = projectedRadius * Math.Sin(azRad);
                double y = projectedRadius * Math.Cos(azRad);
                double z = radius * Math.Sin(altRad);

                points.Add(new Point3d(x, y, z));
            }

            if (points.Count < 2) return null;

            return Curve.CreateInterpolatedCurve(points, 3);
        }

        public static double CalculateDirectIrradiance(double altitude)
        {
            if (altitude <= 0) return 0;

            double solarConstant = 1367.0;
            double altRad = altitude * Math.PI / 180.0;

            double AM = 1.0 / (Math.Sin(altRad) + 0.50572 * Math.Pow(6.07995 + altitude, -1.6364));

            double DNI = solarConstant * Math.Pow(0.7, Math.Pow(AM, 0.678));

            return DNI * Math.Sin(altRad);
        }

        public static double CalculateDailyIrradiance(double latitude, double longitude,
            int year, int month, int day, int timezone)
        {
            double totalIrradiance = 0;
            double step = 0.25;

            for (double hour = 5; hour <= 20; hour += step)
            {
                var pos = CalculateSunPosition(latitude, longitude, year, month, day, hour, timezone);
                if (pos.IsAboveHorizon)
                {
                    totalIrradiance += CalculateDirectIrradiance(pos.Altitude) * step;
                }
            }

            return totalIrradiance;
        }

        private static int GetDayOfYear(int year, int month, int day)
        {
            int[] daysInMonth = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

            if (year % 4 == 0 && (year % 100 != 0 || year % 400 == 0))
                daysInMonth[1] = 29;

            int doy = 0;
            for (int i = 0; i < month - 1; i++)
                doy += daysInMonth[i];
            doy += day;

            return doy;
        }

        public static double[] GetSunriseAndSunset(double latitude, double longitude,
            int year, int month, int day, int timezone)
        {
            double sunrise = -1;
            double sunset = -1;

            for (double h = 0; h < 24; h += 0.1)
            {
                var pos = CalculateSunPosition(latitude, longitude, year, month, day, h, timezone);
                var nextPos = CalculateSunPosition(latitude, longitude, year, month, day, h + 0.1, timezone);

                if (!pos.IsAboveHorizon && nextPos.IsAboveHorizon)
                    sunrise = h;
                if (pos.IsAboveHorizon && !nextPos.IsAboveHorizon)
                    sunset = h;
            }

            return new[] { sunrise, sunset };
        }
    }
}
