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
    public class VersionOption
    {
        public string DisplayName { get; set; }
        public string Url { get; set; }
        public string FolderName { get; set; }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
