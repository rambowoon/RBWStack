using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.IO.Compression;
using System.Threading;

namespace RBWStack
{
    public class TimeoutWebClient : WebClient
    {
        public int TimeoutMs { get; set; }

        public TimeoutWebClient(int timeoutMs = 600000)
        {
            this.TimeoutMs = timeoutMs;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest request = base.GetWebRequest(address);
            if (request != null)
            {
                request.Timeout = this.TimeoutMs;
            }
            return request;
        }
    }
}
