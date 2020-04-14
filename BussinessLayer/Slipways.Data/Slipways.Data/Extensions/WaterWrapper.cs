using com.b_velop.Slipways.Data.Models;

namespace com.b_velop.Slipways.Data.Extensions
{
    public static class WaterWrapper
    {
        public static Water Copy(
            this Water w)
        {
            var water = new Water
            {
                Id = w.Id,
                Created = w.Created,
                Longname = w.Longname,
                Shortname = w.Shortname,
                Updated = w.Updated,
            };
            return water;
        }
    }
}
