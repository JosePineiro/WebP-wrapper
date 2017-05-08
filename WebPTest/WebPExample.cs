// clsWebP, by Jose M. Piñeiro
// Website: https://github.com/JosePineiro/WebP-wapper
// Version: 1.0.0.2 (May 1, 2017)

using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using WebPWrapper;


namespace WebPTest
{
    public partial class WebPExample : Form
    {
        #region | Constructors |
        public WebPExample()
        {
            InitializeComponent();
        }

        private void WebPExample_Load(object sender, EventArgs e)
        {
            try
            {
                //Inform of execution mode
                if (IntPtr.Size == 8)
                    this.Text = Application.ProductName + " x64 " + Application.ProductVersion;
                else
                    this.Text = Application.ProductName + " x32 " + Application.ProductVersion;

                //Inform of libWebP version
                using (WebP webp = new WebP())
                    this.Text = this.Text + " (libwebp v" + webp.GetVersion() + ")";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\nIn WebPExample.WebPExample_Load", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    openFileDialog.Filter = "Image files (*.webp, *.png)|*.webp;*.png";
                    openFileDialog.FileName = "";
                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        this.buttonSave.Enabled = true;
                        this.buttonSave.Enabled = true;
                        string pathFileName = openFileDialog.FileName;

                        if (Path.GetExtension(pathFileName) == ".webp")
                        {
                            using (WebP webp = new WebP())
                                this.pictureBox.Image = webp.Load(pathFileName);
                        }
                        else
                            this.pictureBox.Image = Image.FromFile(pathFileName);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\nIn WebPExample.buttonLoad_Click", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void buttonSave_Click(object sender, System.EventArgs e)
        {
            byte[] rawWebP;

            try
            {
                //get the picturebox image
                Bitmap bmp = (Bitmap)pictureBox.Image;

                //Test simple encode lossly mode in memory with quality 75
                string lossyFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SimpleLossy.webp");
                using (WebP webp = new WebP())
                    rawWebP = webp.EncodeLossy(bmp, 75);
                File.WriteAllBytes(lossyFileName, rawWebP);
                MessageBox.Show("Made " + lossyFileName);

                //Test encode lossly mode in memory with quality 75 and speed 9
                string advanceLossyFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AdvanceLossy.webp");
                using (WebP webp = new WebP())
                    rawWebP = webp.EncodeLossy(bmp, 75, 9, true);
                File.WriteAllBytes(advanceLossyFileName, rawWebP);
                MessageBox.Show("Made " + advanceLossyFileName);
                
                //Test simple encode lossless mode in memory
                string simpleLosslessFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SimpleLossless.webp");
                using (WebP webp = new WebP())
                    rawWebP = webp.EncodeLossless(bmp);
                File.WriteAllBytes(simpleLosslessFileName, rawWebP);
                MessageBox.Show("Made " + simpleLosslessFileName);
                
                //Test advance encode lossless mode in memory with speed 9
                string losslessFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AdvanceLossless.webp");
                using (WebP webp = new WebP())
                    rawWebP = webp.EncodeLossless(bmp, 9, true);
                File.WriteAllBytes(losslessFileName, rawWebP);
                MessageBox.Show("Made " + losslessFileName);
                
                //Test encode near lossless mode in memory with quality 40 and speed 9
                // quality 100: No-loss (bit-stream same as -lossless).
                // quality 80: Very very high PSNR (around 54dB) and gets an additional 5-10% size reduction over WebP-lossless image.
                // quality 60: Very high PSNR (around 48dB) and gets an additional 20%-25% size reduction over WebP-lossless image.
                // quality 40: High PSNR (around 42dB) and gets an additional 30-35% size reduction over WebP-lossless image.
                // quality 20 (and below): Moderate PSNR (around 36dB) and gets an additional 40-50% size reduction over WebP-lossless image.
                string nearLosslessFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NearLossless.webp");
                using (WebP webp = new WebP())
                    rawWebP = webp.EncodeNearLossless(bmp, 40, 9, true);
                File.WriteAllBytes(nearLosslessFileName, rawWebP);
                MessageBox.Show("Made " + nearLosslessFileName);

                MessageBox.Show("End of Test");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\nIn WebPExample.buttonSave_Click", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    openFileDialog.Filter = "WebP images (*.webp)|*.webp";
                    openFileDialog.FileName = "";
                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        string pathFileName = openFileDialog.FileName;

                        byte[] rawWebp = File.ReadAllBytes(pathFileName);
                        using (WebP webp = new WebP())
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
                MessageBox.Show(ex.Message + "\r\nIn WebPExample.buttonInfo_Click", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion
    }
}

