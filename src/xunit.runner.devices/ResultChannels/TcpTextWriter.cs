// this is an adaptation of NUnitLite's TcpWriter.cs with an additional 
// overrides and with network-activity UI enhancement

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if __IOS__ || MAC
#if __UNIFIED__
using UIKit;
#else
using MonoTouch.UIKit;
#endif
#endif
#if WINDOWS_PHONE || NETFX_CORE
using System.Globalization;
using Windows.Networking;
using Windows.Networking.Sockets;
#else
using System.Net.Sockets;

#endif

namespace Xunit.Runners.UI
{
    public class TcpTextWriter : TextWriter
    {
        //private TcpClient client;
        StreamWriter writer;

        public TcpTextWriter(string hostName, int port)
        {
            if ((port < 0) || (port > ushort.MaxValue))
                throw new ArgumentException("port");

            HostName = hostName ?? throw new ArgumentNullException(nameof(hostName));
            Port = port;

#if __IOS__ || MAC
            UIApplication.SharedApplication.NetworkActivityIndicatorVisible = true;
#endif
            try
            {
#if __IOS__ || MAC || __ANDROID__ || __MACOS__
                var client = new TcpClient(hostName, port);
                writer = new StreamWriter(client.GetStream());
#elif WINDOWS_PHONE || NETFX_CORE
               
                var socket = new StreamSocket();
                socket.ConnectAsync(new HostName(hostName), port.ToString(CultureInfo.InvariantCulture))
                    .AsTask()
                    .ContinueWith( _ => writer = new StreamWriter(socket.OutputStream.AsStreamForWrite()));
#endif
            }
            catch
            {
#if __IOS__ || MAC
                UIApplication.SharedApplication.NetworkActivityIndicatorVisible = false;
#endif
                throw;
            }
        }

        // we override everything that StreamWriter overrides from TextWriter

        public override Encoding Encoding
        {
            // hardcoded to UTF8 so make it easier on the server side
            get { return Encoding.UTF8; }
        }

        public string HostName { get; private set; }

        public int Port { get; private set; }

        public override void Flush()
        {
            writer.Flush();
        }

        // minimum to override - see http://msdn.microsoft.com/en-us/library/system.io.textwriter.aspx
        public override void Write(char value)
        {
            writer.Write(value);
        }

        public override void Write(char[] buffer)
        {
            writer.Write(buffer);
        }

        public override void Write(char[] buffer, int index, int count)
        {
            writer.Write(buffer, index, count);
        }

        public override void Write(string value)
        {
            writer.Write(value);
        }

        // special extra override to ensure we flush data regularly

        public override void WriteLine()
        {
            writer.WriteLine();
            writer.Flush();
        }

        protected override void Dispose(bool disposing)
        {
#if __IOS__ || MAC
            UIApplication.SharedApplication.NetworkActivityIndicatorVisible = false;
#endif
            writer.Dispose();
        }
    }
}