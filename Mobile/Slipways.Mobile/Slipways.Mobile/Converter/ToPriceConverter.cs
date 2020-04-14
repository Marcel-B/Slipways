using System;
using System.Globalization;
using Xamarin.Forms;

namespace Slipways.Mobile.Converter
{
    public class ToPriceConverter : IValueConverter
    {
        public ToPriceConverter()
        {
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal price)
                if (price == 0)
                    return "kostenlos";
                else if (price > 0)
                    return price.ToString("C");
            return "n/a";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
