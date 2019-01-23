using ChannelWritter;
using Habanero.Licensing.Validation;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using TeleSharp.TL.Messages;

namespace TelegramExtract
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private LicenseDialog licenseDialog;
        private AuthorizeDialog authorizeDialog;
        private IList<string> channels;
        private IList<string> selectedChannels;
        private static string authCode = string.Empty;
        private static string licensePath = string.Empty;
        private static Task Task;
        private TChannelWritter channelWritter;
        private License license;

        string _filePath = Path.Combine(Directory.GetCurrentDirectory(), "licence.lic");
        string _productName = "Chammy";
        string _applicationSecret = "N6bdpAb5CkK3XUcvGuJEyg==";
        string _publicKey = "BgIAAACkAABSU0ExAAIAAAEAAQCR3Wybv4QNgVAO03uVRaiOnZKyYHUQOJsyR2DVnCnsbRa9ikYzPv9sdv3BjiLeDJdzyWan5kf1Uc6FYcayNV2a";
        const int invalidLife = 12;
        const int trialLife = 1200;

        public MainWindow()
        {
            InitializeComponent();

            license = new License(_filePath, _applicationSecret, _publicKey, _productName);
            license.DoLicenseCheck();

            if (license.Result.State == LicenseState.Invalid)
            {
                try
                {
                    licenseDialog = new LicenseDialog();
                    licenseDialog.ShowDialog();
                    if (licenseDialog.DialogResult == true)
                    {
                        licensePath = licenseDialog.ResponseText;
                        if (File.Exists(_filePath))
                            File.Delete(_filePath);

                        File.Copy(licensePath, _filePath);
                        license = new License(licensePath, _applicationSecret, _publicKey, _productName);
                        license.DoLicenseCheck();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }

                if (licenseDialog.DialogResult == null || licenseDialog.DialogResult == false)
                {
                    Environment.Exit(-1);
                }
            }
            if (license.Result.State == LicenseState.Invalid)
            {
                Task.Run(() =>
                {
                    try
                    {
                        int currentSecond = 0;
                        while (true)
                        {
                            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(delegate
                            {
                                shutdownTextBox.Foreground = new SolidColorBrush(Colors.Red);
                                shutdownTextBox.Text = string.Format("LICENSE INVALID \nShutdown after {0}", (invalidLife - currentSecond).ToString());
                            }));

                            currentSecond++;
                            Thread.Sleep(1000);
                            if (currentSecond == invalidLife)
                                Environment.Exit(-1);
                        }
                    }
                    catch
                    {
                        Environment.Exit(-1);
                    }
                });
            }


            if (license.Result.State == LicenseState.Trial)
            {
                Task.Run(() =>
                {
                    try
                    {
                        int currentSecond = 0;
                        while (true)
                        {
                            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(delegate
                            {
                                shutdownTextBox.Foreground = new SolidColorBrush(Colors.Gray);
                                shutdownTextBox.Text = string.Format("TRIAL LICENSE  \nShutdown after {0}", (trialLife - currentSecond).ToString());
                            }));

                            currentSecond++;
                            Thread.Sleep(1000);
                            if (currentSecond == trialLife)
                                Environment.Exit(-1);
                        }
                    }
                    catch
                    {
                        Environment.Exit(-1);
                    }
                });
            }

            if (license.Result.State == LicenseState.Valid)
            {
                shutdownTextBox.Foreground = new SolidColorBrush(Colors.LimeGreen);
                if (license.Validator.IsEdition("Standard"))
                {
                    shutdownTextBox.Text = string.Format("LICENSED\nSTANDARD Edition");
                }

                if (license.Validator.IsEdition("Lite"))
                {
                    shutdownTextBox.Text = string.Format("LICENSED\nLITE Edition");
                }
            }

        }

        private async Task Connect()
        {
            await channelWritter.Connect();
        }

        private async Task AuthorizeRequest()
        {
            await channelWritter.AuthorizeRequest();
        }

        private async Task AuthorizeConfirm(string authCode)
        {
            await channelWritter.AuthorizeConfirm(authCode);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string safePhone = phoneTextBox.Text.Replace("+", "");
            channelWritter = Worker.GetChannelWriter(phoneTextBox.Text, apiIDTextBox.Text, apiHashTextBox.Text);
            Task = new Task(() =>
            {
                try
                {
                    Application.Current.Dispatcher.Invoke((() =>
                    {
                        channelsListView.Visibility = Visibility.Collapsed;
                        progress1.IsActive = true;
                        progress1.Visibility = Visibility.Visible;
                    }));

                    var connectTask = Connect();
                    connectTask.Wait();

                    if (!channelWritter.IsAuthorized)
                    {
                        Task.Wait(10000);
                        var authTask = AuthorizeRequest();
                        authTask.Wait();

                        Application.Current.Dispatcher.Invoke((() =>
                        {
                            authorizeDialog = new AuthorizeDialog();
                            authorizeDialog.ShowDialog();
                            if (authorizeDialog.DialogResult == true)
                            {
                                authCode = authorizeDialog.ResponseText;
                            }
                        }));

                        Task.Wait(10000);
                        AuthorizeConfirm(authCode).Wait();
                    }

                    if (channelWritter.IsAuthorized)
                    {
                        var task = channelWritter.GetUserDialogs();
                        task.Wait();
                        channels = channelWritter.GetChannelList(task.Result);

                        Application.Current.Dispatcher.Invoke((() =>
                        {
                            channelsListView.ItemsSource = channels;
                        }));
                    }

                    Application.Current.Dispatcher.Invoke((() =>
                    {
                        channelsListView.Visibility = Visibility.Visible;
                        progress1.IsActive = false;
                        progress1.Visibility = Visibility.Collapsed;
                        saveButton.IsEnabled = true;
                    }));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            });

            Task.Start();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                channelWritter.SetSavePath(savePathTextBox.Text);
                selectedChannels = new List<string>();
                foreach (var listItem in channelsListView.SelectedItems)
                    selectedChannels.Add(listItem.ToString());
                if (selectedChannels.Count == 0)
                    channelWritter.SetChannelsForMonitoring(channels.ToArray());
                if (selectedChannels.Count > 0)
                    channelWritter.SetChannelsForMonitoring(selectedChannels.ToArray());

                progressingBar.Visibility = Visibility.Visible;
                Task.Run(() =>
                {
                    channelWritter.StartMonitor();
                });
            }
            catch (Exception ex)
            {
                progressingBar.Visibility = Visibility.Hidden;
            }
        }
    }
}
