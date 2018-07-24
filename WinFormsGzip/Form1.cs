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
                try
                {
                    _zipper = new MyGzip(openFileDialog1.FileName)
                    {
                        progressAction = (a) => labelProcessed.Text = a.ToString(),
                        cancellationToken = ct,
                        RezFileLength = (a)=> labelAll.Text = a.ToString()
                    };
                }
                catch (Exception exception)
                {
                    MessageBox.Show(exception.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if(textBox1.Text=="") return;
            try
            {
                if (_zipper.FunctionFlg)
                {
                    _zipper.Decompress();
                }
                else
                {
                    _zipper.Compress();
                }
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
    }
    
}
