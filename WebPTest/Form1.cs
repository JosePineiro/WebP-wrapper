// clsWebP, by Jose M. Piñeiro
// Website: https://github.com/JosePineiro/WebP-wapper
// Version: 1.0.0.0 (March 26, 2016)

using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using WebP;

/*
 * Another usefull functions
 * 
 
//Load WebP from file to bitmap
using (clsWebP webp = new clsWebP())
    Bitmap bmp = webp.Load("test.webp");

//Save WebP from bitmap to file
Bitmap bmp = new Bitmap("test.jpg");
using (clsWebP webp = new clsWebP())
    webp.Save(bmp, 80, "test.webp");
 */

namespace WebPTest
{
    public partial class Form1 : Form
    {
        #region | Constructors |
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                //Inform of execution mode
                if (IntPtr.Size == 8)
                    this.Text = Application.ProductName + " x64 " + Application.ProductVersion;
                else
                    this.Text = Application.ProductName + " x32 " + Application.ProductVersion;

                //Inform of libWebP version
                using (clsWebP webp = new clsWebP())
                    this.Text = this.Text + "WebP test (libwebp v" + webp.GetVersion() + ")";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\nIn Form1.Form1_Load", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion

        #region << Events >>
        private void buttonLoad_Click(object sender, System.EventArgs e)
        {
            try
            {
                using (OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog())
                {
                    this.openFileDialog.Filter = "Image files (*.webp, *.png)|*.webp;*.png";
                    this.openFileDialog.FileName = "";
                    if (this.openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        this.buttonSave.Enabled = true;
                        this.buttonSave.Enabled = true;
                        string pathFileName = this.openFileDialog.FileName;

                        if (Path.GetExtension(pathFileName) == ".webp")
                        {
                            byte[] rawWebP = File.ReadAllBytes(pathFileName);
                            using (clsWebP webp = new clsWebP())
                                this.pictureBox.Image = webp.Decode(rawWebP);
                        }
                        else
                            this.pictureBox.Image = Image.FromFile(pathFileName);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\nIn Form1.buttonLoad_Click", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void buttonSave_Click(object sender, System.EventArgs e)
        {
            byte[] rawWebP;

            try
            {
                //get the picturebox image
                Bitmap bmp = (Bitmap)pictureBox.Image;

                //Test encode near lossless mode in memory with quality 40 and speed 9
                // quality 100: No-loss (bit-stream same as -lossless).
                // quality 80: Very very high PSNR (around 54dB) and gets an additional 5-10% size reduction over WebP-lossless image.
                // quality 60: Very high PSNR (around 48dB) and gets an additional 20%-25% size reduction over WebP-lossless image.
                // quality 40: High PSNR (around 42dB) and gets an additional 30-35% size reduction over WebP-lossless image.
                // quality 20 (and below): Moderate PSNR (around 36dB) and gets an additional 40-50% size reduction over WebP-lossless image.
                string nearLosslessFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nearlossless.webp");
                using (clsWebP webp = new clsWebP())
                    rawWebP = webp.EncodeNearLossless(bmp, 40, 9);
                File.WriteAllBytes(nearLosslessFileName, rawWebP);
                MessageBox.Show("Made " + nearLosslessFileName);

                //Test advance encode lossless mode in memory with speed 9
                string losslessFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "advancelossless.webp");
                using (clsWebP webp = new clsWebP())
                    rawWebP = webp.EncodeLossless(bmp, 9);
                File.WriteAllBytes(losslessFileName, rawWebP);
                MessageBox.Show("Made " + losslessFileName);

                //Test simple encode lossless mode in memory
                string simpleLosslessFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "simpleLossless.webp");
                using (clsWebP webp = new clsWebP())
                    rawWebP = webp.EncodeLossless(bmp);
                File.WriteAllBytes(simpleLosslessFileName, rawWebP);
                MessageBox.Show("Made " + simpleLosslessFileName);

                //Test simple encode lossly mode in memory with quality 75
                string lossyFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "simpleLossy.webp");
                using (clsWebP webp = new clsWebP())
                    rawWebP = webp.EncodeLossy(bmp, 75);
                File.WriteAllBytes(lossyFileName, rawWebP);
                MessageBox.Show("Made " + lossyFileName);

                //Test simple encode lossly mode in memory with quality 75
                string advanceLossyFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "advanceLossy.webp");
                using (clsWebP webp = new clsWebP())
                    rawWebP = webp.EncodeLossy(bmp, 75, 9);
                File.WriteAllBytes(advanceLossyFileName, rawWebP);
                MessageBox.Show("Made " + advanceLossyFileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\nIn Form1.buttonSave_Click", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void buttonInfo_Click(object sender, EventArgs e)
        {
            int width;
            int height;
            bool has_alpha;
            bool has_animation;
            string format;

            try
            {
                using (OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog())
                {
                    this.openFileDialog.Filter = "WebP images (*.webp)|*.webp";
                    this.openFileDialog.FileName = "";
                    if (this.openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        string pathFileName = this.openFileDialog.FileName;

                        byte[] rawWebp = File.ReadAllBytes(pathFileName);
                        using (clsWebP webp = new clsWebP())
                            webp.GetInfo(rawWebp, out width, out height, out has_alpha, out has_animation, out format);
                        MessageBox.Show("Width: " + width + "\n" +
                                        "Height: " + height + "\n" +
                                        "Has alpha: " + has_alpha + "\n" +
                                        "Is animation: " + has_animation + "\n" +
                                        "Format: " + format, "Information");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\nIn Form1.buttonInfo_Click", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion
    }
}

