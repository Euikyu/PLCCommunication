using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace PLCCommunication.Panasonic.Controls
{
    internal class BoolToRadioButtonConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool val) return !val; else return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool val) return !val; else return false;
        }
    }

    internal class BoolToGreenRedColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool val)
            {
                if (val) return "LimeGreen"; else return "Red";
            }
            else return "Black";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    internal class DeviceCodeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is EBinaryDeviceCode binCode)
            {
                return (EPLCDeviceCode)((int)binCode);
            }
            else if (value is EContactReadableDeviceCode readableCode)
            {
                return (EPLCDeviceCode)((int)readableCode);
            }
            else if (value is EContactWritableDeviceCode writableCode)
            {
                return (EPLCDeviceCode)((int)writableCode);
            }
            else if (value is EDataDeviceCode dataCode)
            {
                return (EPLCDeviceCode)((int)dataCode);
            }
            else if (value is EIndexRegisterDeviceCode indexCode)
            {
                return (EPLCDeviceCode)((int)indexCode);
            }
            else if (value is EPLCDeviceCode code)
            {
                return code;
            }
            else return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
