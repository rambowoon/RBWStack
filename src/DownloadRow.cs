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
    public class DownloadRow
    {
        public string ComponentName;
        public string BaseDestFolder;
        public string ZipFileName;
        public List<VersionOption> Options;
        public Action<string> PostExtractAction;

        public ComboBox ComboSelector;
        public Label LabelDesc;
        public Panel CardPanel;
    }
}
