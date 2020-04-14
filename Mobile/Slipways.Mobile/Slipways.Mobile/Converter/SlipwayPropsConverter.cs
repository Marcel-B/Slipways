using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Slipways.Mobile.Data.Models;
using Xamarin.Forms;

namespace Slipways.Mobile.Converter
{
    public class SlipwayPropsConverter : IValueConverter
    {
        public SlipwayPropsConverter()
        {
        }

        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            if(value is Slipway slip)
            {
                var extras = slip.Extras ?? new List<Extra>();
                var v =  parameter.ToString() switch
                {
                    "marina" => slip.Marina == null ? 0.1 : 1,
                    "pier" => extras.Any(_ => _.Name == "Steg") ? 1 : 0.1,
                    "parking" => extras.Any(_ => _.Name == "Parkplatz") ? 1 : 0.1,
                    "camping" => extras.Any(_ => _.Name == "Campingplatz") ? 1 : 0.1,
                    _ => 0.1
                };
            return v;
            }
            return 1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
