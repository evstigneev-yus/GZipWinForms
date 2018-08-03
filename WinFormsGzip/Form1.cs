using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using pgzip;

namespace WinFormsGzip
{
    
    public partial class Form1 : Form
    {
        private CompressionMode _compressionMode=CompressionMode.Compress;
        private string _source;
        private string _dest;
        private static readonly CancellationTokenSource cts = new CancellationTokenSource();
        private readonly CancellationToken ct = cts.Token;
        private readonly SynchronizationContext syncContext;

        public Form1()
        {
            syncContext = SynchronizationContext.Current;
            InitializeComponent();
        }
        
        private void button1_Click(object sender, EventArgs e)
        {
            var dr = openFileDialog1.ShowDialog(this);
            if (dr != DialogResult.OK) return;
            textBox1.Text = openFileDialog1.FileName;
            _source= openFileDialog1.FileName;
        }

        
        private void button2_Click(object sender, EventArgs e)
        {
            DoWork();
        }

        private async void DoWork()
        {
            var fi = new FileInfo(_source);
            labelAll.Text = fi.Length.ToString();
            _dest = folderBrowserDialog1.SelectedPath + '\\' + openFileDialog1.SafeFileName;
            using (var gz = new ThreadedGzip(_source,
                a => { syncContext.Post(o => { labelProcessed.Text = o.ToString(); }, a); }))
            {
                if (_compressionMode == CompressionMode.Compress)
                {
                    _dest += ".gz";
                    gz.OutFileName = _dest;
                    gz.BlockSize = (int) numericUpDown1.Value * 1048576;
                    await Task.Run(() => gz.Compress(ct), ct);
                }
                else
                {
                    _dest = _dest.Remove(_dest.Length - 3);
                    gz.OutFileName = _dest;
                    await Task.Run(() => gz.Decompress(ct), ct);
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            cts.Cancel();
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            _compressionMode = CompressionMode.Decompress;
            numericUpDown1.Enabled = false;
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            _compressionMode = CompressionMode.Compress;
            numericUpDown1.Enabled = true;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            var dr = folderBrowserDialog1.ShowDialog(this);
            if (dr != DialogResult.OK) return;
            _dest = folderBrowserDialog1.SelectedPath;
            textBox2.Text = _dest;
        }
    }
    
}
