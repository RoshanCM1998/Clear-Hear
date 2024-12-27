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

        LoadDevices();

        int lastProfile = Preferences.Get("LastSelectedProfile", 1);
        currentProfile = lastProfile;
        LoadProfile(lastProfile);

        inputDevices = new List<string>();
        outputDevices = new List<string>();
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
        Preferences.Set("LastSelectedProfile", profileIndex);
    }

    private void LoadProfile(int profileIndex)
    {
        Band1Slider.Value = Preferences.Get($"Profile{profileIndex}_Band1", 50);
        Band2Slider.Value = Preferences.Get($"Profile{profileIndex}_Band2", 50);
        Band3Slider.Value = Preferences.Get($"Profile{profileIndex}_Band3", 50);
        Band4Slider.Value = Preferences.Get($"Profile{profileIndex}_Band4", 50);
        Band5Slider.Value = Preferences.Get($"Profile{profileIndex}_Band5", 50);
    }

    private void OnSliderValueChanged(object sender, ValueChangedEventArgs e)
    {
        Band1Entry.Text = Band1Slider.Value.ToString();
        Band2Entry.Text = Band2Slider.Value.ToString();
        Band3Entry.Text = Band3Slider.Value.ToString();
        Band4Entry.Text = Band4Slider.Value.ToString();
        Band5Entry.Text = Band5Slider.Value.ToString();

        UpdateAudioGains();
    }

    private void OnMasterVolumeChanged(object sender, ValueChangedEventArgs e)
    {
        MasterVolumeEntry.Text = MasterVolumeSlider.Value.ToString();
        float val = (float)MasterVolumeSlider.Value;
        _audioService.SetVolume(val);
    }

    private void OnEntryTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is Entry entry)
        {
            switch(entry.Text)
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
                case "MasterVolumeEntry":
                    OnBandValueChange(sender, MasterVolumeSlider);
                    break;
            }
        }
    }

    private void OnBandValueChange(object sender, Slider slider)
    {
        if (sender is Entry entry && double.TryParse(entry.Text, out double entryNumber))
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
            (float)(Band5Slider.Value / 100.0)
        };

        _audioService.SetBandGains(gains);
    }

    private void OnProfileClicked(object sender, EventArgs e)
    {
        if (sender is Button button && int.TryParse(button.Text, out int profileIndex))
        {
            SaveProfile(currentProfile);
            LoadProfile(profileIndex);
            currentProfile = profileIndex;
            DisplayAlert("Profile Loaded", $"Profile {profileIndex} loaded.", "OK");
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