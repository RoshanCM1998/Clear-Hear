using Android.Net.Sip;

namespace ClearHear;

public partial class MainPage : ContentPage
{
    private readonly IAudioService _audioService;
    private bool isProcessing = false;
    private int currentProfile;
    private List<string> inputDevices;
    private List<string> outputDevices;

    public MainPage(IAudioService audioService)
    {
        InitializeComponent();
        _audioService = audioService;

        inputDevices = new List<string>();
        outputDevices = new List<string>();
        LoadDevices();

        int lastProfile = Preferences.Get("LastSelectedProfile", 1);
        currentProfile = lastProfile;
        SetProfile(currentProfile, currentProfile);
        LoadProfile(lastProfile);
    }

    private async Task CheckPermissionsAndStartAudio()
    {
        var status = await Permissions.RequestAsync<Permissions.Microphone>();
        if (status == PermissionStatus.Granted)
        {
            isProcessing = true;
            StartStopButton.Text = "Stop";
            StartStopButton.BackgroundColor = Colors.Red;
            _audioService.StartAudioProcessing();
        }
        else
        {
            await DisplayAlert("Permission Denied", "Microphone permission is required for the app to work.", "OK");
        }
    }

    private void LoadDevices()
    {
        (inputDevices, outputDevices) = _audioService.GetInputOutputDevices();

        InputDevicePicker.ItemsSource = inputDevices;
        OutputDevicePicker.ItemsSource = outputDevices;

        InputDevicePicker.SelectedIndex = 0;
        OutputDevicePicker.SelectedIndex = 0;
    }

    private void SelectInputDevice(object sender, EventArgs e)
    {
        if(sender is Picker picker)
        {
            var selectedDevice = inputDevices[picker.SelectedIndex];
            _audioService.SetInputDevice(selectedDevice);
        }
    }

    private void SelectOutputDevice(object sender, EventArgs e)
    {
        if (sender is Picker picker)
        {
            var selectedDevice = outputDevices[picker.SelectedIndex];
            _audioService.SetOutputDevice(selectedDevice);
        }
    }

    private void SaveProfile(int profileIndex)
    {
        Preferences.Set($"Profile{profileIndex}_Band1", Band1Slider.Value);
        Preferences.Set($"Profile{profileIndex}_Band2", Band2Slider.Value);
        Preferences.Set($"Profile{profileIndex}_Band3", Band3Slider.Value);
        Preferences.Set($"Profile{profileIndex}_Band4", Band4Slider.Value);
        Preferences.Set($"Profile{profileIndex}_Band5", Band5Slider.Value);
        Preferences.Set($"Profile{profileIndex}_Band6", Band6Slider.Value);
        Preferences.Set("LastSelectedProfile", profileIndex);
    }

    private void SetProfile(int newProfile, int oldProfile)
    {
        Button[] profileButtons = [ Profile1Button, Profile2Button, Profile3Button, Profile4Button, Profile5Button ];
        
        profileButtons[oldProfile - 1].BorderColor = null;
        profileButtons[oldProfile - 1].BorderWidth = 0;

        profileButtons[newProfile - 1].BorderColor = Colors.Black;
        profileButtons[newProfile - 1].BorderWidth = 2;
    }

    private void LoadProfile(int profileIndex)
    {
        string p1 = Preferences.Get($"Profile{profileIndex}_Band1", "100");
        string p2 = Preferences.Get($"Profile{profileIndex}_Band2", "100");
        string p3 = Preferences.Get($"Profile{profileIndex}_Band3", "100");
        string p4 = Preferences.Get($"Profile{profileIndex}_Band4", "100");
        string p5 = Preferences.Get($"Profile{profileIndex}_Band5", "100");
        string p6 = Preferences.Get($"Profile{profileIndex}_Band6", "100");

        if(double.TryParse(p1, out double pd1))
        {
            Band1Slider.Value = pd1;
        }
        else
        {
            Band1Slider.Value = 100;
        }

        if (double.TryParse(p2, out double pd2))
        {
            Band2Slider.Value = pd2;
        }
        else
        {
            Band2Slider.Value = 100;
        }

        if (double.TryParse(p3, out double pd3))
        {
            Band3Slider.Value = pd3;
        }
        else
        {
            Band3Slider.Value = 100;
        }

        if (double.TryParse(p4, out double pd4))
        {
            Band4Slider.Value = pd4;
        }
        else
        {
            Band4Slider.Value = 100;
        }

        if (double.TryParse(p5, out double pd5))
        {
            Band5Slider.Value = pd5;
        }
        else
        {
            Band5Slider.Value = 100;
        }

        if (double.TryParse(p6, out double pd6))
        {
            Band6Slider.Value = pd6;
        }
        else
        {
            Band6Slider.Value = 100;
        }
    }

    private void OnSliderValueChanged(object sender, ValueChangedEventArgs e)
    {
        Band1Entry.Text = Math.Round(Band1Slider.Value, 0).ToString();
        Band2Entry.Text = Math.Round(Band2Slider.Value, 0).ToString();
        Band3Entry.Text = Math.Round(Band3Slider.Value, 0).ToString();
        Band4Entry.Text = Math.Round(Band4Slider.Value, 0).ToString();
        Band5Entry.Text = Math.Round(Band5Slider.Value, 0).ToString();
        Band6Entry.Text = Math.Round(Band6Slider.Value, 0).ToString();

        UpdateAudioGains();
    }

    private void OnMasterVolumeChanged(object sender, ValueChangedEventArgs e)
    {
        double temp = MasterVolumeSlider.Value;
        temp = Math.Round(temp, 0);
        MasterVolumeEntry.Text = temp.ToString();
        float val = (float)MasterVolumeSlider.Value;
        _audioService.SetVolume(val);
    }

    private void OnEntryTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is Entry entry)
        {
            switch (entry.AutomationId)
            {
                case "Band1Entry": 
                    OnBandValueChange(sender, Band1Slider);
                    break;
                case "Band2Entry":
                    OnBandValueChange(sender, Band2Slider);
                    break;
                case "Band3Entry":
                    OnBandValueChange(sender, Band3Slider);
                    break;
                case "Band4Entry":
                    OnBandValueChange(sender, Band4Slider);
                    break;
                case "Band5Entry":
                    OnBandValueChange(sender, Band5Slider);
                    break;
                case "Band6Entry":
                    OnBandValueChange(sender, Band6Slider);
                    break;
                case "MasterVolumeEntry":
                    OnBandValueChange(sender, MasterVolumeSlider);
                    break;
            }
        }
    }

    private void OnBandValueChange(object sender, Slider slider)
    {
        if (sender is Entry entry && double.TryParse(entry.Text, out double entryNumber) && 50 <= entryNumber && entryNumber <= 500)
        {
            slider.Value = entryNumber;
            if (slider == MasterVolumeSlider)
            {
                float val = (float)MasterVolumeSlider.Value;
                _audioService.SetVolume(val);
            }
            else
            {
                UpdateAudioGains();
            }
        }
    }

    private void UpdateAudioGains()
    {
        var gains = new float[]
        {
            (float)(Band1Slider.Value / 100.0),
            (float)(Band2Slider.Value / 100.0),
            (float)(Band3Slider.Value / 100.0),
            (float)(Band4Slider.Value / 100.0),
            (float)(Band5Slider.Value / 100.0),
            (float)(Band6Slider.Value / 100.0)
        };

        _audioService.SetBandGains(gains);
    }

    private void OnProfileClicked(object sender, EventArgs e)
    {
        if (sender is Button button && int.TryParse(button.Text, out int profileIndex) && currentProfile != profileIndex)
        {
            SaveProfile(currentProfile);
            LoadProfile(profileIndex);
            SetProfile(profileIndex, currentProfile);
            currentProfile = profileIndex;
            DisplayAlert("Profile Loaded", $"Profile {profileIndex} loaded.", "OK");
        }
    }

    private void OnSaveClicked(object sender, EventArgs e)
    {
        if (sender is Button button)
        {
            SaveProfile(currentProfile);
        }
    }

    private async void OnStartStopClicked(object sender, EventArgs e)
    {
        if (isProcessing)
        {
            isProcessing = false;
            StartStopButton.Text = "Start";
            StartStopButton.BackgroundColor = Colors.Green;
            _audioService.StopAudioProcessing();
        }
        else
        {
            await CheckPermissionsAndStartAudio();  // Call the permissions check before starting audio
        }
    }
}