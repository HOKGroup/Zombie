#region References

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

#endregion

namespace Zombie.Utilities.Wpf
{
    /// <summary>
    /// Sets proper asset icon based on it's file extension.
    /// </summary>
    public class AssetObjectToSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return new Uri("pack://application:,,,/Zombie;component/Resources/unknown_32x32.png");

            var asset = (AssetObject)value;
            var extension = Path.GetExtension(asset.Name.ToLower());
            switch (extension)
            {
                case ".dll":
                    return new Uri("pack://application:,,,/Zombie;component/Resources/dll_32x32.png");
                case ".zip":
                case ".rar":
                    return new Uri("pack://application:,,,/Zombie;component/Resources/zip_32x32.png");
                case ".msi":
                    return new Uri("pack://application:,,,/Zombie;component/Resources/msi_32x32.png");
                case ".jpg":
                case ".jpeg":
                case ".png":
                    return new Uri("pack://application:,,,/Zombie;component/Resources/image_32x32.png");
                case ".exe":
                    return new Uri("pack://application:,,,/Zombie;component/Resources/exe_32x32.png");
                default:
                    return new Uri("pack://application:,,,/Zombie;component/Resources/unknown_32x32.png");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Inverts the standard Boolean to Visibility Converter. Things are hidden if bool is True.
    /// </summary>
    public class BoolToVisInverterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool && (bool)value) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// If Asset is a Placeholder sets Cursor to "No" otherwise it's draggable so it returns a "Hand".
    /// </summary>
    public class BoolToCursorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool && (bool)value) ? "No" : "Hand";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Simply inverts the bool property. 
    /// </summary>
    public class BooleanInverterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool) value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Returns Green brush when AssetCount is at 0. Otherwise it's a Zombie Red.
    /// </summary>
    public class CountToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var count = (int) value;
            return count == 0
                ? new SolidColorBrush(Color.FromRgb(154, 204, 121))
                : new SolidColorBrush(Color.FromRgb(242, 73, 109));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts an Enum instance to Collection so that it can be displayed in a Combobox.
    /// </summary>
    [ValueConversion(typeof(Enum), typeof(IEnumerable<ValueDescription>))]
    public class EnumToCollectionConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return EnumHelper.GetAllValuesAndDescriptions(value.GetType());
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}
