using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFormsGzip
{
    
    public partial class Form1 : Form
    {
        private MyGzip _zipper;
        private CompressionMode _compressionMode=CompressionMode.Compress;
        private string _source;
        private string _dest;
        private static CancellationTokenSource cts = new CancellationTokenSource();
        private CancellationToken ct = cts.Token;

        public Form1()
        {
            InitializeComponent();
        }
        
        private void button1_Click(object sender, EventArgs e)
        {
            var dr = openFileDialog1.ShowDialog(this);
            if (dr == DialogResult.OK)
            {
                textBox1.Text = openFileDialog1.FileName;
                _source= openFileDialog1.FileName;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            _dest = folderBrowserDialog1.SelectedPath + openFileDialog1.SafeFileName;
            if (_compressionMode == CompressionMode.Compress)
            {
                _dest += ".gz";
            }
            else
            {
                _dest = _dest.Remove(_dest.Length - 3);
            }
            _zipper = new MyGzip(_source, _dest, _compressionMode)
            {
                ProgressAction = (a) => labelProcessed.Text = a.ToString(),
                CancellationToken = ct,
                RezFileLength = (a) => labelAll.Text = a.ToString()
            };

            if (textBox1.Text=="") return;
            try
            {
                _zipper.DoWork();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            cts.Cancel();
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            _compressionMode = CompressionMode.Decompress;
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            _compressionMode = CompressionMode.Compress;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            var dr = folderBrowserDialog1.ShowDialog(this);
            if (dr == DialogResult.OK)
            {
                _dest = folderBrowserDialog1.SelectedPath;
                textBox2.Text = _dest;
                
            }
        }
    }
    
}
