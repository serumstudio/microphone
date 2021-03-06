using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Speech.Synthesis;
using System.Text.RegularExpressions;
using CSCore.SoundOut;
using System.ComponentModel;
using System.IO;
using CSCore;
using CSCore.MediaFoundation;



namespace Serum_Microphone.View
{
    class PlayerSettings
    {
        public string text
        {
            get; set;
        }
        public int deviceId
        {
            get; set;
        }
        public int speakerId
        {
            get; set;
        }

        public bool isPreview
        {
            get; set;
        }

        public bool is_playing
        {
            get; set;
        }
        public bool isDownload
        {
            get; set;
        }
        public string downloadedFile
        {
            get; set;
        }
    }
    public partial class PlayerView : Page
    {

        PlayerSettings config = new PlayerSettings();

        public PlayerView()
        {
            InitializeComponent();
          
            

        }
        private string GetDownloadPath()
        {
            string downloadPath = Properties.Settings.Default.download_path;
            Directory.CreateDirectory(downloadPath);
            string output = String.Format("{0}/{1}.wav", downloadPath, DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss"));

            return output;
        }


        private void do_work(object sender, DoWorkEventArgs e)
        {

            using (var stream = new MemoryStream())
            using (var speechEngine = new SpeechSynthesizer())
            {
                foreach (var device in WaveOutDevice.EnumerateDevices())
                {


                    string devices = $"{device.DeviceId}: {device.Name}";

                    if (devices.Contains("CABLE"))
                    {
                        string resultString = Regex.Match(devices, @"\d+").Value;
                        config.deviceId = Int32.Parse(resultString);

                    }
                    else if (devices.Contains("Speaker"))
                    {
                        string _speakerId = Regex.Match(devices, @"\d+").Value;
                        config.speakerId = Int32.Parse(_speakerId);
                    }
                }
                speechEngine.SetOutputToWaveStream(stream);

                VoiceGender gender = VoiceGender.Neutral; 
                VoiceAge age = VoiceAge.NotSet; 

                switch (Properties.Settings.Default.voice_gender)
                {
                    case 0:
                        gender = VoiceGender.Female;
                        break;

                    case 1:
                        gender = VoiceGender.Male;
                        break;

                    case 2:
                        gender = VoiceGender.Neutral;
                        break;
                }


                switch (Properties.Settings.Default.voice_age)
                {
                    case 0:
                        age = VoiceAge.Adult;
                        break;

                    case 1:
                        age = VoiceAge.Senior;
                        break;

                    case 2:
                        age = VoiceAge.Teen;
                        break;

                    case 3:
                        age = VoiceAge.Child;
                        break;
                }

                speechEngine.SelectVoiceByHints(gender, age);
                speechEngine.Speak(config.text);

                int id;

                if (config.isPreview)
                {
                    id = config.speakerId;
                }
                else
                {
                    id = config.deviceId;
                }

                

                using (var waveOut = new WaveOut { Device = new WaveOutDevice(id) })
                using (var waveSource = new MediaFoundationDecoder(stream))
                {
                    
                    if (config.isDownload)
                    {
                        string path = GetDownloadPath();
                        waveSource.WriteToFile($"{path}");
                        config.downloadedFile = path;
                    }   
                    else
                    {
                       
                        waveOut.Initialize(waveSource);
                        waveOut.Play();
                        //config.is_playing = true;
                        waveOut.WaitForStopped();
                    }
                    
                    

                }
            }

        }
       

        private void work_completed(object sender, RunWorkerCompletedEventArgs e)
        {

        }

        private void Play()
        {
            config.text = _text.Text;
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += do_work;
            worker.RunWorkerCompleted += work_completed;
            worker.RunWorkerAsync();
        }

      

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            config.isPreview = false;
            config.isDownload = false;
            Play();
        }

        private void _previewButton_Click(object sender, RoutedEventArgs e)
        {
            config.isPreview = true;
            config.isDownload = false;
            Play();
        }

        private async void downloadBtn_Click(object sender, RoutedEventArgs e)
        {
            config.isDownload = true;
            config.isPreview = false;        
            Play();
            ModernWpf.Controls.ContentDialog dialog = new ModernWpf.Controls.ContentDialog();
            dialog.Title = "Serum Microphone";
            dialog.Content = $"File Saved: {config.downloadedFile}";
            dialog.PrimaryButtonText = "Ok";
            dialog.SecondaryButtonText = "Cancel";
            await dialog.ShowAsync();

        }
    }
}
