using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeleSharp.TL;
using TeleSharp.TL.Messages;
using TLSharp.Core;

namespace ChannelWritter
{
    public class TChannelWritter
    {
        private string _phone = "";
        private int _appId = -1;
        private string _appHash = "";
        private string _authHash = "";
        private TelegramClient _client;
        private TLChannel[] _channels;
        private int[] _channelOffsets;
        private string _savePath;

        public TChannelWritter(string Phone = "", int AppId = -1, string AppHash = "")
        {
            _phone = Phone;
            _appId = AppId;
            _appHash = AppHash;

            _client = new TelegramClient(_appId, _appHash);
        }

        public bool IsAuthorized
        {
            get { return _client.IsUserAuthorized(); }
        }

        public async Task Connect()
        {
            await _client.ConnectAsync();
        }

        public async Task AuthorizeRequest()
        {
            _authHash = await _client.SendCodeRequestAsync(_phone);
        }

        public async Task AuthorizeConfirm(string code)
        {
            var user = await _client.MakeAuthAsync(_phone, _authHash, code);
        }

        public async Task<TLAbsDialogs> GetUserDialogs()
        {
            return await _client.GetUserDialogsAsync();
        }

        public string[] GetChannelList(TLAbsDialogs tLAbsDialogs)
        {
            var dialogs = (TLDialogs)tLAbsDialogs;
            var channels = dialogs.Chats.OfType<TLChannel>();
            _channels = channels.Select(x => x).ToArray();

            var dialogs2 = dialogs.Dialogs.OfType<TLDialog>();

            var offsets = dialogs2.Where(x => x.Peer is TLPeerChannel).Select(x => new { channelId = ((TLPeerChannel)x.Peer).ChannelId, offset = x.TopMessage });
            _channelOffsets = offsets.Select(x => x.offset).ToArray();


            return _channels.Select(x => x.Title).ToArray();
        }

        public void SetChannelsForMonitoring(string[] channels)
        {
            _channels = _channels.Where(x => channels.Any(y => y == x.Title)).ToArray();
        }

        public void StartMonitor()
        {
            var timer = new System.Timers.Timer { AutoReset = true };
            timer.Interval = 1000 * 60 * 3;
            timer.Elapsed += (sender, args) =>
            {
                SaveChannelMessages();
            };
            timer.Start();
        }

        private async void SaveChannelMessages()
        {
            try
            {
                var index = 0;
                foreach (var channel in _channels)
                {
                    var peerChannel = new TLInputPeerChannel()
                    {
                        ChannelId = channel.Id,
                        AccessHash = channel.AccessHash.Value
                    };

                    var dialogs = (TLDialogs)await _client.GetUserDialogsAsync();

                    var offset = dialogs.Dialogs.Where(x => (x.Peer is TLPeerChannel) && ((TLPeerChannel)x.Peer).ChannelId == channel.Id)
                        .Select(x => x.TopMessage).FirstOrDefault();

                    if (_channelOffsets[index] == offset)
                    {
                        index++;
                        continue;
                    }

                    var history = (TLChannelMessages)await _client.GetHistoryAsync(peerChannel, 0, -1, 0, 100, offset + 1, _channelOffsets[index]);

                    var path = Path.Combine(_savePath, GetSafeFilename(channel.Title));

                    var lines = history.Messages.OfType<TLMessage>().Select(x => ((TLMessage)x).Message).Reverse();
                    List<string> newLines = new List<string>();
                    if (lines != null && lines.Count() > 0)
                        foreach (var line in lines)
                        {
                            newLines.Add("——-new message ——-");
                            newLines.Add(line);
                        }

                    File.AppendAllLines($"{path}.txt", newLines);
                    _channelOffsets[index++] = offset;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private string GetSafeFilename(string filename)
        {
            return string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));
        }

        public void SetSavePath(string path)
        {
            _savePath = path;
        }
    }
}
