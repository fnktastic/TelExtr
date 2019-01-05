using ChannelWritter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramExtract
{
    public static class Worker
    {
        private static string _phone;
        private static int _apiID;
        private static string _apiHash;
        private static TChannelWritter channelWritter;

        public static TChannelWritter GetChannelWriter(string phone, string apiID, string apiHash)
        {
            _phone = phone;
            bool converted = int.TryParse(apiID, out _apiID);
            _apiHash = apiHash;
            channelWritter = new TChannelWritter(_phone, _apiID, _apiHash);
            return channelWritter;
        }
    }
    }
