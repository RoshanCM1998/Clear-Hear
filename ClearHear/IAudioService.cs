namespace ClearHear;

public interface IAudioService
{
    void StartAudioProcessing();
    void StopAudioProcessing();
    void SetVolume(float volume);
    void SetBandGains(float[] gains); // Pass slider values for frequency bands

    (List<string> inputDevices, List<string> outputDevices) GetInputOutputDevices();

    void SetInputDevice(string device);

    void SetOutputDevice(string device);
}
