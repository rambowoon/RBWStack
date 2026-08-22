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
    public class LocalFontGroup
    {
        public string Id { get; set; }
        public string Family { get; set; }
        public string Category { get; set; }
        public List<LocalFontFile> Files { get; set; }
    }
}
