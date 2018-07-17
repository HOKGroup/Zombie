#region References

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;

#endregion

namespace Zombie.Utilities.Wpf
{
    /// <summary>
    /// Sets proper asset icon based on it's file extension.
    /// </summary>
    public class AssetObjectToSourceConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] == null || values[1] == null)
                return new BitmapImage(new Uri("pack://application:,,,/Zombie;component/Resources/unknown_32x32.png", UriKind.Absolute));

            var asset = (AssetObject)values[0];
            var isPlaceholder = (bool)values[1];
            var extension = Path.GetExtension(asset.Name.ToLower());
            switch (extension)
            {
                case ".dll":
                    // (Konrad) Placeholder Asset is *.dll so this will be the only one that can be disabled
                    return isPlaceholder 
                        ? new BitmapImage(new Uri("pack://application:,,,/Zombie;component/Resources/dllDisabled_32x32.png", UriKind.Absolute))  
                        : new BitmapImage(new Uri("pack://application:,,,/Zombie;component/Resources/dll_32x32.png", UriKind.Absolute));
                case ".zip":
                case ".rar":
                    return new BitmapImage(new Uri("pack://application:,,,/Zombie;component/Resources/zip_32x32.png", UriKind.Absolute));
                case ".msi":
                    return new BitmapImage(new Uri("pack://application:,,,/Zombie;component/Resources/msi_32x32.png", UriKind.Absolute));
                case ".jpg":
                case ".jpeg":
                case ".png":
                case ".tiff":
                case ".gif":
                case ".svg":
                    return new BitmapImage(new Uri("pack://application:,,,/Zombie;component/Resources/image_32x32.png", UriKind.Absolute));
                case ".exe":
                    return new BitmapImage(new Uri("pack://application:,,,/Zombie;component/Resources/exe_32x32.png", UriKind.Absolute));
                case "":
                    return new BitmapImage(new Uri("pack://application:,,,/Zombie;component/Resources/folderNarrow_32x32.png", UriKind.Absolute));
                default:
                    return new BitmapImage(new Uri("pack://application:,,,/Zombie;component/Resources/unknown_32x32.png", UriKind.Absolute));
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Sets the Grid Row Height based on Asset count. 
    /// </summary>
    public class ContentCountToRowHeightConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] == null || values[1] == null)
                return new GridLength(0);

            var isContentVisible = (bool)values[0];
            var count = 0;
            if (values[1] is int i)
            {
                count = i;
            }

            return isContentVisible 
                ? new GridLength(count*24) // typical asset view is 24px
                : new GridLength(0);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Sets the Grid Row Width based on whether an Asset is a ZIP/RAR file.
    /// </summary>
    public class AssetNameToRowWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return false;

            var name = (string)value;
            var extension = Path.GetExtension(name.ToLower());
            return (extension == ".zip" || extension == ".rar") 
                ? new GridLength(18) 
                : new GridLength(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Sets the Grid Row Width based on a boolean value.
    /// </summary>
    public class BoolToRowWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool && (bool) value) 
                ? new GridLength(0) 
                : new GridLength(15);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Returns file source for Draggable image if False, otherwise DraggableDisabled source.
    /// </summary>
    public class BooleanToDragableSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool && (bool) value)
                ? new Uri("pack://application:,,,/Zombie;component/Resources/dragableDisabled_32x32.png")
                : new Uri("pack://application:,,,/Zombie;component/Resources/dragable_32x32.png");
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
    /// Returns a Zombie Navy if True otherwise a Transparent Brush.
    /// </summary>
    public class BoolToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool) value
                ? new SolidColorBrush(Color.FromRgb(43, 55, 79))
                : new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Returns True if file path is a path to GitHub content. 
    /// </summary>
    public class FilePathToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((string) value).StartsWith("https://raw.githubusercontent.com", StringComparison.OrdinalIgnoreCase);
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
