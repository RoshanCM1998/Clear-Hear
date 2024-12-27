using Android.Content;
using Android.Media;
using MediaStream = Android.Media.Stream;
using AndroidApp = Android.App;
using NAudio.Dsp;

namespace ClearHear.Platforms.Android
{
    public class AudioService : IAudioService
    {
        private AudioRecord? _audioRecord;
        private AudioTrack? _audioTrack;
        private bool _isProcessing;
        private Thread? _processingThread;
        private float _masterVolume = 1.0f;
        private float[] _bandGains = [1, 1, 1, 1, 1];
        private readonly AudioManager _audioManager;
        private List<AudioDeviceInfo> _inputDevices;
        private List<AudioDeviceInfo> _outputDevices;

        public AudioService()
        {
            var temp = AndroidApp.Application.Context.GetSystemService(Context.AudioService);

            if (temp == null) {
                throw new NullReferenceException("Null audioManager from audio Service");
            }

            _audioManager = (AudioManager)temp;
            _inputDevices = new List<AudioDeviceInfo>();
            _outputDevices = new List<AudioDeviceInfo>();
        }

        public void StartAudioProcessing()
        {
            if (_isProcessing) return;

            int bufferSize = AudioRecord.GetMinBufferSize(44100, ChannelIn.Mono, Encoding.Pcm16bit);
            _audioRecord = new AudioRecord(AudioSource.Mic, 44100, ChannelIn.Mono, Encoding.Pcm16bit, bufferSize);
            //_audioTrack = new AudioTrack(MediaStream.Music, 44100, ChannelOut.Mono, Encoding.Pcm16bit, bufferSize, AudioTrackMode.Stream);

            // Safely build AudioTrack using nullable handling
            var audioAttributes = new AudioAttributes.Builder()?
                .SetUsage(AudioUsageKind.Media)?
                .SetContentType(AudioContentType.Music)?
                .Build();

            var audioFormat = new AudioFormat.Builder()?
                .SetSampleRate(44100)?
                .SetChannelMask(ChannelOut.Mono)?
                .SetEncoding(Encoding.Pcm16bit)?
                .Build();

            if (audioAttributes != null && audioFormat != null)
            {
                _audioTrack = new AudioTrack.Builder()
                    .SetAudioAttributes(audioAttributes)
                    .SetAudioFormat(audioFormat)
                    .SetBufferSizeInBytes(bufferSize)
                    .SetTransferMode(AudioTrackMode.Stream)
                    .Build();
            } else
            {
                throw new NullReferenceException("Audio Track is null in StartAudioProcessing");
            }

            _audioRecord.StartRecording();
            _audioTrack?.Play();

            _isProcessing = true;
            _processingThread = new Thread(() => ProcessAudio(bufferSize));
            _processingThread.Start();
        }

        public void StopAudioProcessing()
        {
            _isProcessing = false;
            _audioRecord?.Stop();
            _audioTrack?.Stop();
            _audioRecord?.Dispose();
            _audioTrack?.Dispose();
            _audioRecord = null;
            _audioTrack = null;
            _processingThread?.Join();
        }

        public void SetVolume(float volume) => _masterVolume = volume;
        public void SetBandGains(float[] gains) => _bandGains = gains;

        public (List<string> inputDevices, List<string> outputDevices) GetInputOutputDevices()
        {
            var idTemp = _audioManager.GetDevices(GetDevicesTargets.Inputs);
            var odTemp = _audioManager.GetDevices(GetDevicesTargets.Outputs);

            if(idTemp == null ||  odTemp == null)
            {
                throw new NullReferenceException("Null inputDevices or outputDevices in GetInputOutputDevices");
            }

            _inputDevices = idTemp.Where(device => device.Type == AudioDeviceType.BuiltinMic ||
                                            device.Type == AudioDeviceType.WiredHeadset ||
                                            device.Type == AudioDeviceType.UsbHeadset ||
                                            device.Type == AudioDeviceType.BleHeadset ||
                                            device.Type == AudioDeviceType.BluetoothA2dp ||
                                            device.Type == AudioDeviceType.BluetoothSco)
                                    .ToList();

            // Fetching output devices (Speakers, Bluetooth)
            _outputDevices = odTemp.Where(device => device.Type == AudioDeviceType.BuiltinSpeaker ||
                                            device.Type == AudioDeviceType.BluetoothA2dp ||
                                            device.Type == AudioDeviceType.BleHeadset ||
                                            device.Type == AudioDeviceType.BluetoothSco ||
                                            device.Type == AudioDeviceType.HearingAid ||
                                            device.Type == AudioDeviceType.WiredHeadphones ||
                                            device.Type == AudioDeviceType.WiredHeadset)
                                    .ToList();

            var nameOfInputDevices = _inputDevices.Select(device => device.ProductName?.ToString() ?? "")
                                            .Where(device => !string.IsNullOrWhiteSpace(device)).ToList();

            var nameOfOutputDevices = _outputDevices.Select(device => device.ProductName?.ToString() ?? "")
                                            .Where(device => !string.IsNullOrWhiteSpace(device)).ToList();


            return (nameOfInputDevices, nameOfOutputDevices);
        }

        public void SetInputDevice(string device)
        {
            var selectedInputDevice = _inputDevices.FirstOrDefault(d => d.ProductName?.ToString() == device);

            if (selectedInputDevice != null)
            {
                _audioRecord?.SetPreferredDevice(selectedInputDevice);
            }
        }

        public void SetOutputDevice(string device)
        {
            var selectedDevice = _outputDevices.FirstOrDefault(d => d.ProductName?.ToString() == device);

            if (selectedDevice != null)
            {
                _audioTrack?.SetPreferredDevice(selectedDevice);
            }
        }

        // Main recording and playback method
        private void ProcessAudio(int bufferSize)
        {
            var buffer = new short[bufferSize / 2];
            var floatBuffer = new float[buffer.Length];

            while (_isProcessing)
            {
                // Read audio data from the microphone
                int read = _audioRecord?.Read(buffer, 0, buffer.Length) ?? 0;
                if (read > 0)
                {
                    // Convert short data to float data for processing
                    for (int i = 0; i < read; i++)
                    {
                        floatBuffer[i] = buffer[i] / 32768f; // Normalize to -1.0 to 1.0
                    }

                    // Process audio with frequency bands
                    ApplyEqualizer(floatBuffer, read);

                    // Apply master volume
                    for (int i = 0; i < read; i++)
                    {
                        floatBuffer[i] *= _masterVolume;
                        buffer[i] = (short)(floatBuffer[i] * 32768); // Convert back to short
                    }

                    // Write processed audio to the output
                    _audioTrack?.Write(buffer, 0, read);
                }
            }
        }

        // Apply frequency band processing
        private void ApplyEqualizer(float[] buffer, int length)
        {
            var fft = new Complex[length];
            for (int i = 0; i < length; i++)
            {
                fft[i].X = buffer[i];
                fft[i].Y = 0;
            }

            // Perform FFT
            FastFourierTransform.FFT(true, (int)Math.Log(length, 2), fft);

            // Process each frequency band
            int bandSize = length / 5;
            for (int band = 0; band < 5; band++)
            {
                int start = band * bandSize;
                int end = (band + 1) * bandSize;

                for (int i = start; i < end; i++)
                {
                    fft[i].X *= _bandGains[band];
                    fft[i].Y *= _bandGains[band];
                }
            }

            // Perform Inverse FFT
            FastFourierTransform.FFT(false, (int)Math.Log(length, 2), fft);

            // Convert back to real audio data
            for (int i = 0; i < length; i++)
            {
                buffer[i] = fft[i].X;
            }
        }

    }

}