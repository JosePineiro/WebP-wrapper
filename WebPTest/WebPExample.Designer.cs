namespace WebPTest
{
    partial class WebPExample
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.pictureBox = new System.Windows.Forms.PictureBox();
            this.buttonLoad = new System.Windows.Forms.Button();
            this.buttonSave = new System.Windows.Forms.Button();
            this.buttonInfo = new System.Windows.Forms.Button();
            this.buttonMeasure = new System.Windows.Forms.Button();
            this.buttonThumbnail = new System.Windows.Forms.Button();
            this.buttonCropFlip = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox
            // 
            this.pictureBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox.Location = new System.Drawing.Point(0, 0);
            this.pictureBox.Name = "pictureBox";
            this.pictureBox.Size = new System.Drawing.Size(586, 490);
            this.pictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox.TabIndex = 0;
            this.pictureBox.TabStop = false;
            // 
            // buttonLoad
            // 
            this.buttonLoad.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonLoad.Location = new System.Drawing.Point(12, 496);
            this.buttonLoad.Name = "buttonLoad";
            this.buttonLoad.Size = new System.Drawing.Size(81, 30);
            this.buttonLoad.TabIndex = 1;
            this.buttonLoad.Text = "Load image";
            this.buttonLoad.UseVisualStyleBackColor = true;
            this.buttonLoad.Click += new System.EventHandler(this.ButtonLoad_Click);
            // 
            // buttonSave
            // 
            this.buttonSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonSave.Enabled = false;
            this.buttonSave.Location = new System.Drawing.Point(99, 496);
            this.buttonSave.Name = "buttonSave";
            this.buttonSave.Size = new System.Drawing.Size(81, 30);
            this.buttonSave.TabIndex = 2;
            this.buttonSave.Text = "Save WEBP";
            this.buttonSave.UseVisualStyleBackColor = true;
            this.buttonSave.Click += new System.EventHandler(this.ButtonSave_Click);
            // 
            // buttonInfo
            // 
            this.buttonInfo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonInfo.Location = new System.Drawing.Point(493, 496);
            this.buttonInfo.Name = "buttonInfo";
            this.buttonInfo.Size = new System.Drawing.Size(81, 30);
            this.buttonInfo.TabIndex = 3;
            this.buttonInfo.Text = "Info WEBP";
            this.buttonInfo.UseVisualStyleBackColor = true;
            this.buttonInfo.Click += new System.EventHandler(this.ButtonInfo_Click);
            // 
            // buttonMeasure
            // 
            this.buttonMeasure.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonMeasure.Location = new System.Drawing.Point(394, 496);
            this.buttonMeasure.Name = "buttonMeasure";
            this.buttonMeasure.Size = new System.Drawing.Size(93, 30);
            this.buttonMeasure.TabIndex = 4;
            this.buttonMeasure.Text = "Measure WEBP";
            this.buttonMeasure.UseVisualStyleBackColor = true;
            this.buttonMeasure.Click += new System.EventHandler(this.ButtonMeasure_Click);
            // 
            // buttonThumbnail
            // 
            this.buttonThumbnail.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonThumbnail.Location = new System.Drawing.Point(288, 496);
            this.buttonThumbnail.Name = "buttonThumbnail";
            this.buttonThumbnail.Size = new System.Drawing.Size(100, 30);
            this.buttonThumbnail.TabIndex = 5;
            this.buttonThumbnail.Text = "Load Thumbnail";
            this.buttonThumbnail.UseVisualStyleBackColor = true;
            this.buttonThumbnail.Click += new System.EventHandler(this.ButtonThumbnail_Click);
            // 
            // buttonCropFlip
            // 
            this.buttonCropFlip.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonCropFlip.Location = new System.Drawing.Point(186, 496);
            this.buttonCropFlip.Name = "buttonCropFlip";
            this.buttonCropFlip.Size = new System.Drawing.Size(96, 30);
            this.buttonCropFlip.TabIndex = 6;
            this.buttonCropFlip.Text = "Load (Crop && flip)";
            this.buttonCropFlip.UseVisualStyleBackColor = true;
            this.buttonCropFlip.Click += new System.EventHandler(this.ButtonCropFlip_Click);
            // 
            // WebPExample
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(586, 538);
            this.Controls.Add(this.buttonCropFlip);
            this.Controls.Add(this.buttonThumbnail);
            this.Controls.Add(this.buttonMeasure);
            this.Controls.Add(this.buttonInfo);
            this.Controls.Add(this.buttonSave);
            this.Controls.Add(this.buttonLoad);
            this.Controls.Add(this.pictureBox);
            this.Name = "WebPExample";
            this.Text = "WebP test";
            this.Load += new System.EventHandler(this.WebPExample_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox;
        private System.Windows.Forms.Button buttonLoad;
        private System.Windows.Forms.Button buttonSave;
        private System.Windows.Forms.Button buttonInfo;
        private System.Windows.Forms.Button buttonMeasure;
        private System.Windows.Forms.Button buttonThumbnail;
        private System.Windows.Forms.Button buttonCropFlip;
    }
}

