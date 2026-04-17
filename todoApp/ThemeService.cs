using System.Windows.Media;

namespace todoApp
{
    public class AppTheme
    {
        public SolidColorBrush WindowBackground { get; set; }
        public SolidColorBrush TopBarBackground { get; set; }
        public SolidColorBrush CalendarCellBackground { get; set; }
        public SolidColorBrush CalendarCellToday { get; set; }
        public SolidColorBrush CalendarCellSelected { get; set; }
        public SolidColorBrush CalendarCellBorder { get; set; }
        public SolidColorBrush TaskPanelBackground { get; set; }
        public SolidColorBrush TaskCardBackground { get; set; }
        public SolidColorBrush InputBackground { get; set; }
        public SolidColorBrush InputBorder { get; set; }
        public SolidColorBrush PrimaryText { get; set; }
        public SolidColorBrush SecondaryText { get; set; }
        public SolidColorBrush AccentColor { get; set; }
        public SolidColorBrush AccentHover { get; set; }
        public SolidColorBrush ButtonBackground { get; set; }
        public SolidColorBrush ButtonBorder { get; set; }
        public SolidColorBrush CompletedText { get; set; }
    }

    public static class ThemeService
    {
        public static AppTheme GetTheme(string themeName)
        {
            if (themeName == "Light")
                return LightTheme();
            return DarkTheme();
        }

        private static AppTheme DarkTheme() => new AppTheme
        {
            WindowBackground = Brush("#1F1F1F"),
            TopBarBackground = Brush("#141414"),
            CalendarCellBackground = Brush("#1A1A1A"),
            CalendarCellToday = Brush("#2A2A2A"),
            CalendarCellSelected = Brush("#E50914"),
            CalendarCellBorder = Brush("#333333"),
            TaskPanelBackground = Brush("#141414"),
            TaskCardBackground = Brush("#2A2A2A"),
            InputBackground = Brush("#2A2A2A"),
            InputBorder = Brush("#444444"),
            PrimaryText = Brush("#FFFFFF"),
            SecondaryText = Brush("#AAAAAA"),
            AccentColor = Brush("#E50914"),
            AccentHover = Brush("#FF1A1A"),
            ButtonBackground = Brush("#2A2A2A"),
            ButtonBorder = Brush("#444444"),
            CompletedText = Brush("#666666")
        };

        private static AppTheme LightTheme() => new AppTheme
        {
            WindowBackground = Brush("#F0F4FF"),
            TopBarBackground = Brush("#FFFFFF"),
            CalendarCellBackground = Brush("#FFFFFF"),
            CalendarCellToday = Brush("#E8EEFF"),
            CalendarCellSelected = Brush("#1A56DB"),
            CalendarCellBorder = Brush("#D0D9F0"),
            TaskPanelBackground = Brush("#FFFFFF"),
            TaskCardBackground = Brush("#F0F4FF"),
            InputBackground = Brush("#FFFFFF"),
            InputBorder = Brush("#C0CCEE"),
            PrimaryText = Brush("#1A1A2E"),
            SecondaryText = Brush("#5566AA"),
            AccentColor = Brush("#1A56DB"),
            AccentHover = Brush("#1446BB"),
            ButtonBackground = Brush("#FFFFFF"),
            ButtonBorder = Brush("#C0CCEE"),
            CompletedText = Brush("#AAAAAA")
        };

        private static SolidColorBrush Brush(string hex)
        {
            return (SolidColorBrush)new BrushConverter().ConvertFrom(hex)!;
        }
    }
}