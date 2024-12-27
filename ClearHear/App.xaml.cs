using Microsoft.Maui.Controls;

namespace ClearHear
{
    public partial class App : Application
    {
        public App(IAudioService audioService)
        {
            InitializeComponent();

            MainPage = new MainPage(audioService);
        }
    }
}
