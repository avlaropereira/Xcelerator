using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media;

namespace Xcelerator.Views
{
    /// <summary>
    /// Interaction logic for MainWindowView.xaml
    /// </summary>
    public partial class MainWindowView : UserControl
    {
        public MainWindowView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Shows a loading indicator in the status bar
        /// </summary>
        public void ShowLoading()
        {
            StatusBar.Visibility = Visibility.Visible;
            // You can add animation here if needed
        }

        /// <summary>
        /// Hides the loading indicator
        /// </summary>
        public void HideLoading()
        {
            StatusBar.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Performs a smooth transition animation (optional)
        /// </summary>
        public void AnimateTransition()
        {
            var animation = new DoubleAnimation
            {
                From = -50,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            ContentTransform.BeginAnimation(TranslateTransform.XProperty, animation);
        }
    }
}
