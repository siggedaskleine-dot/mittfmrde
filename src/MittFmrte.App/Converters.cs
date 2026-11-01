using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using MittFmrte.Core.Models;

namespace MittFmrte.App;

/// <summary>Colours 1-20 attributes like FM: low red, mid white, good yellow, great green.</summary>
public sealed class AttributeColorConverter : IValueConverter
{
    private static readonly Brush Low = Freeze(Color.FromRgb(0xE0, 0x6C, 0x5A));
    private static readonly Brush Mid = Freeze(Color.FromRgb(0xD8, 0xD8, 0xD8));
    private static readonly Brush Good = Freeze(Color.FromRgb(0xF2, 0xC9, 0x4C));
    private static readonly Brush Great = Freeze(Color.FromRgb(0x4C, 0xD9, 0x64));

    private static Brush Freeze(Color c) { var b = new SolidColorBrush(c); b.Freeze(); return b; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value switch
    {
        int v when v >= 16 => Great,
        int v when v >= 11 => Good,
        int v when v >= 6 => Mid,
        _ => Low,
    };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

public sealed class MoneyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value switch
    {
        long v => Money.Format(v),
        int v => Money.Format(v),
        _ => "okänt",
    };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

public sealed class DateConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is DateOnly d ? d.ToString("yyyy-MM-dd") : "okänt";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

/// <summary>Shows the age of a player on the loaded game's date.</summary>
public sealed class AgeConverter : IValueConverter
{
    public static DateOnly GameDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is Player p ? p.AgeOn(GameDate) : "";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

public sealed class NullToCollapsedConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is null || value as string == "" ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
