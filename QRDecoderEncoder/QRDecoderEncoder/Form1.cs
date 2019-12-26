using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using MessagingToolkit.QRCode.Codec;
using MessagingToolkit.QRCode.Codec.Data;


namespace QRDecoderEncoder
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            pb = pictureBox1;
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.OnDragEnter);
            this.DragLeave += new System.EventHandler(this.OnDragLeave);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.OnDragDrop);
            this.DragOver += new System.Windows.Forms.DragEventHandler(this.OnDragOver);
            thumbnail = new PictureBox();
            thumbnail.SizeMode = PictureBoxSizeMode.CenterImage;
            pb.Controls.Add(thumbnail);
            thumbnail.Visible = false;
        }

        private QRCodeDecoder decoder = new QRCodeDecoder();
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (textBox1.Text != String.Empty)
            {
                string url = textBox1.Text;
                QRCodeEncoder en = new QRCodeEncoder();
                Bitmap qrcode = en.Encode(url);
                pictureBox1.Image = qrcode;
                bool error = false;
                string decodedtitle = decode(pictureBox1.Image, out error);
                if (!error && !string.IsNullOrEmpty(decodedtitle))
                    toolTip1.ToolTipTitle = decodedtitle;
            }
            else
            {
                toolTip1.ToolTipTitle = "";
                pictureBox1.Image = null;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {

            if (pictureBox1.Image != null)
            {
                saveimage();
            }
            else MessageBox.Show("QR code not found");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            bool error = false;
            string decodedtitle = null;
            Image picture = null;
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Image files (*.jpg, *.jpeg, *.jpe, *.jfif, *.png) | *.jpg; *.jpeg; *.jpe; *.jfif; *.png";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                picture = Image.FromFile(dialog.FileName);
                decodedtitle = decode(picture, out error);

            }

            if (error)
            {
                pictureBox1.Image = null;
                toolTip1.ToolTipTitle = null;
                textBox1.Text = null;
            }
            if (!string.IsNullOrEmpty(dialog.FileName) && !error)
            {
                toolTip1.ToolTipTitle = decodedtitle;
                textBox1.Text = decodedtitle;
                pictureBox1.Image = picture;
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

            if (pictureBox1.Image != null)
            {
                try
                {
                    bool error = false;
                    string decodedtitle = decode(pictureBox1.Image, out error);
                    if (!error && !string.IsNullOrEmpty(decodedtitle))
                    {
                        toolTip1.ToolTipTitle = decodedtitle;

                        Process.Start("https://www.google.com/search?q=" + decodedtitle);
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("An exception has been occured");
                }

            }
            else
                MessageBox.Show("QR code not found");


        }
        [Pure]
        private string decode(Image QRPicture, out bool error)
        {

            error = false;
            try
            {
                return decoder.Decode(new QRCodeBitmapImage(QRPicture as Bitmap));
            }
            catch (Exception e)
            {
                MessageBox.Show("The file you have picked is not a QR");
                error = true;
                return null;
            }

        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {


            if ((e.KeyCode == Keys.Back) && e.Control)//allow to use ctrl+backspace 
            {
                e.SuppressKeyPress = true;
                int selStart = textBox1.SelectionStart;
                while (selStart > 0 && textBox1.Text.Substring(selStart - 1, 1) == " ")
                {
                    selStart--;
                }
                int prevSpacePos = -1;
                if (selStart != 0)
                {
                    prevSpacePos = textBox1.Text.LastIndexOf(' ', selStart - 1);
                }
                textBox1.Select(prevSpacePos + 1, textBox1.SelectionStart - prevSpacePos - 1);
                textBox1.SelectedText = "";
            }
            else
           if (e.KeyCode == Keys.Enter && e.Control)
            {
                if (pictureBox1.Image != null)
                {
                    try
                    {
                        bool error = false;
                        string decodedtitle = decode(pictureBox1.Image, out error);
                        if (!error && !string.IsNullOrEmpty(decodedtitle))
                        {
                            toolTip1.ToolTipTitle = decodedtitle;
                            Process.Start("https://www.google.com/search?q=" + decodedtitle);
                        }
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Something happened");
                    }
                }
                else
                    MessageBox.Show("QR code not found");
            }


        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {

            if (e.KeyCode == Keys.S && e.Control)

                if (pictureBox1.Image != null)
                {
                    saveimage();
                }
                else
                {
                    MessageBox.Show("QR code not found");
                }


        }
        private void saveimage()
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "Image files (*.jpg, *.jpeg, *.jpe, *.jfif, *.png) | *.jpg; *.jpeg; *.jpe; *.jfif; *.png";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                pictureBox1.Image.Save(dialog.FileName);
            }
        }



        //-----------------------------Drag & Drop-------------------------------------------------


        protected int lastX = 0;
        protected int lastY = 0;
        protected string lastFilename = String.Empty;
        protected PictureBox thumbnail;
        protected DragDropEffects effect;
        protected bool validData;
        protected Image image;
        protected Image nextImage;
        protected Thread getImageThread;

        private PictureBox pb;
        /// <summary>
        /// Required designer variable.
        /// </summary>



        private void OnDragDrop(object sender, System.Windows.Forms.DragEventArgs e)
        {
            Debug.WriteLine("OnDragDrop");
            if (validData)
            {
                while (getImageThread.IsAlive)
                {
                    Application.DoEvents();
                    Thread.Sleep(0);
                }
                thumbnail.Visible = false;
                image = nextImage;

                if ((pb.Image != null) && (pb.Image != nextImage))
                {
                    pb.Image.Dispose();
                }

                bool error = false;
                string decodedtitle = null;
                decode(image, out error);
                if (!error)
                    pb.Image = image;
                else
                {
                    pb.Image = null;
                }

            }
        }

        private void OnDragEnter(object sender, System.Windows.Forms.DragEventArgs e)
        {

            string filename;
            validData = GetFilename(out filename, e);
            if (validData)
            {
                if (lastFilename != filename)
                {
                    thumbnail.Image = null;
                    thumbnail.Visible = false;
                    lastFilename = filename;
                    getImageThread = new Thread(new ThreadStart(LoadImage));
                    getImageThread.Start();
                }
                else
                {
                    thumbnail.Visible = true;
                }
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void OnDragLeave(object sender, System.EventArgs e)
        {
            Debug.WriteLine("OnDragLeave");
            thumbnail.Visible = false;
        }

        private void OnDragOver(object sender, System.Windows.Forms.DragEventArgs e)
        {
            Debug.WriteLine("OnDragOver");
            if (validData)
            {
                if ((e.X != lastX) || (e.Y != lastY))
                {
                    thumbnail.Location = pictureBox1.Location;
                    // SetThumbnailLocation(this.PointToClient(new Point(e.X, e.Y)));
                }
            }
        }

        protected bool GetFilename(out string filename, DragEventArgs e)
        {
            bool ret = false;
            filename = String.Empty;

            if ((e.AllowedEffect & DragDropEffects.Copy) == DragDropEffects.Copy)
            {
                Array data = ((IDataObject)e.Data).GetData("FileDrop") as Array;
                if (data != null)
                {
                    if ((data.Length == 1) && (data.GetValue(0) is String))
                    {
                        filename = ((string[])data)[0];
                        string ext = Path.GetExtension(filename).ToLower();
                        if ((ext == ".jpg") || (ext == ".png") || (ext == ".bmp"))
                        {
                            ret = true;
                        }
                    }
                }
            }
            return ret;
        }

        protected void SetThumbnailLocation(Point p)
        {
            if (thumbnail.Image == null)
            {
                thumbnail.Visible = false;
            }
            else
            {
                p.X -= thumbnail.Width / 2;
                p.Y -= thumbnail.Height / 2;
                thumbnail.Location = p;
                thumbnail.Visible = true;
            }
        }





        public delegate void AssignImageDlgt();

        protected void LoadImage()
        {
            nextImage = new Bitmap(lastFilename);
            this.Invoke(new AssignImageDlgt(AssignImage));
        }

        protected void AssignImage()
        {

            thumbnail.Height = pictureBox1.Height;
            thumbnail.Width = pictureBox1.Width;
            thumbnail.Location = pictureBox1.Location;
            //SetThumbnailLocation(this.PointToClient(new Point(lastX, lastY)));
            thumbnail.Image = nextImage;
        }

        private void pictureBox1_MouseEnter(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null)
            {

                bool error = false;
                string decodedtitle = decode(pictureBox1.Image, out error);
                if (!error && !string.IsNullOrEmpty(decodedtitle))
                {
                    toolTip1.ToolTipTitle = decodedtitle;
                    textBox1.Text = decodedtitle;
                }
            }
        }
    }
}
