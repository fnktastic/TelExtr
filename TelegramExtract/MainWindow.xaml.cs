using ChannelWritter;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using TeleSharp.TL.Messages;

namespace TelegramExtract
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private AuthorizeDialog authorizeDialog;
        private IList<string> channels;
        private IList<string> selectedChannels;
        private static string authCode = string.Empty;
        private static Task Task;
        private TChannelWritter channelWritter;

        public MainWindow()
        {
            InitializeComponent();
            channelsListView.Visibility = Visibility.Collapsed;
            phoneTextBox.Text = "380636860418";
            apiIDTextBox.Text = "515461";
            apiHashTextBox.Text = "9ac4483e3706a42ae061aae60d1d585a";
            savePathTextBox.Text = @"C:\Users\fnkta\Documents\Telegram API\Out";
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
