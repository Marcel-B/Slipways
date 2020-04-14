using com.b_velop.Slipways.Data.Models;

namespace com.b_velop.Slipways.Data.Extensions
{
    public static class StationWrapper
    {
        public static Station Copy(
            this Station s)
        {
            var station = new Station
            {
                Updated = s.Updated,
                Agency = s.Agency,
                Created = s.Created,
                Id = s.Id,
                Km = s.Km,
                Latitude = s.Latitude,
                Longitude = s.Longitude,
                Longname = s.Longname,
                Number = s.Number,
                Shortname = s.Shortname,
                Water = s.Water?.Copy(),
                WaterFk = s.WaterFk
            };
            return station;
        }
    }
}
