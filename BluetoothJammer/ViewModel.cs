﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bluetooth.Services;
using Bluetooth.Model;
using GalaSoft.MvvmLight;
using System.Collections.ObjectModel;
using System.Diagnostics;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using InTheHand.Net;
using System.IO;
using System.Media;
using System.Windows.Media;

namespace BluetoothJammer
{
    public class ViewModel : ViewModelBase
    {
        public SenderBluetoothService Service { get; set; } = new SenderBluetoothService();
        public ObservableCollection<Device> Devices { get; set; } = new ObservableCollection<Device>();
        private string _status = "Hi. We are going to attempt a hack";
        public string Status
        {
            get { return _status; }
            set
            {
                Set(ref _status, value);
            }
        }
        public ViewModel()
        {
            GetDevices();
        }
        private async void GetDevices()
        {
            var isScanning = true;
            var oldDevices = new List<Device>();
            Status = "Scanning for bluetooth devices...";
            while (isScanning)
            {
                var devices = await Service.GetDevices();
                Status = $"{devices.Count} devices found. {devices.Count(x => x.DeviceInfo.ClassOfDevice.Device == DeviceClass.AudioVideoLoudSpeaker)} devices are speakers.";
                if (devices.SequenceEqual(oldDevices))
                    continue;
                
                oldDevices = new List<Device>(devices);
                await App.Current.Dispatcher.BeginInvoke((Action)delegate ()
                {
                    Devices.Clear();
                    foreach (var item in oldDevices)
                    {
                        Devices.Add(item);
                    }


                    foreach (var dev in Devices.Where(x =>
                        x.DeviceInfo.ClassOfDevice.Device == 
                            DeviceClass.AudioVideoLoudSpeaker && 
                                !x.IsConnected))
                    {
                        try
                        {
                            Status = $"Attempting to connect to {dev.DeviceName}...";
                            Guid serviceClass;
                            serviceClass = BluetoothService.SerialPort;
                            var ep = new BluetoothEndPoint(dev.DeviceInfo.DeviceAddress, serviceClass);
                            var cli = new BluetoothClient();
                            cli.Connect(ep);
                            dev.IsConnected = true;
                            isScanning = false;
                            Status = $"Connected to {dev.DeviceName}. Attempting to play silence.";
                            PlaySilence();
                            break;
                        }
                        catch(Exception ex)
                        {
                            Status = $"Failed to connect to device\n{ex.Message}";
                            continue;
                        }
                    }
                });
            }
        }

        private void PlaySilence()
        {
            var player = new SoundPlayer();
            player.SoundLocation = AppDomain.CurrentDomain.BaseDirectory + "silent.wav";
            player.PlayLooping();
        }
    }
}
