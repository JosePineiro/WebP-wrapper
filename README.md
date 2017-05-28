# WebP-wrapper
Wrapper for libwebp in C#. The most complete wapper in pure managed C#.

Exposes Simple Decoding API, Simple Encoding API, Advanced Encoding API (with stadistis of compresion), Get version library and WebPGetFeatures (info of any WebP file). In the future I´ll update for expose Advanced Decoding API.

The wapper is in safe managed code in one class. No need external dll except libwebp_x86.dll and libwebp_x64.dll (included v6.0). The wrapper work in 32, 64 bit or ANY (auto swith to the apropiate library).

The code is full comented and include simple example for using the wrapper.

## Use
Load WebP image for WebP file
```C#
using (WebP webp = new WebP())
  Bitmap bmp = webp.Load("test.webp");
```

Save bitmap to WebP file
```C#
Bitmap bmp = new Bitmap("test.jpg");
using (WebP webp = new WebP())
  webp.Save(bmp, 80, "test.webp");
```

Decode WebP filename to bitmap and load in PictureBox container
```C#
byte[] rawWebP = File.ReadAllBytes("test.webp");
using (WebP webp = new WebP())
  this.pictureBox.Image = webp.Decode(rawWebP);
```

Encode to memory buffer in lossly mode with quality 75 and save to file
```C#
byte[] rawWebP = File.ReadAllBytes("test.jpg");
using (WebP webp = new WebP())
  rawWebP = webp.EncodeLossy(bmp, 75);
File.WriteAllBytes("test.webp", rawWebP); 
```

Encode to memory buffer in lossly mode with quality 75 and speed 9. Save to file
```C#
byte[] rawWebP = File.ReadAllBytes("test.jpg");
using (WebP webp = new WebP())
  rawWebP = webp.EncodeLossy(bmp, 75, 9);
File.WriteAllBytes("test.webp", rawWebP); 
```
Encode to memory buffer in lossly mode with quality 75, speed 9 and get information. Save to file
```C#
byte[] rawWebP = File.ReadAllBytes("test.jpg");
using (WebP webp = new WebP())
  rawWebP = webp.EncodeLossy(bmp, 75, 9, true);
File.WriteAllBytes("test.webp", rawWebP); 
```

Encode to memory buffer in lossless mode and save to file
```C#
byte[] rawWebP = File.ReadAllBytes("test.jpg");
using (WebP webp = new WebP())
  rawWebP = webp.EncodeLossless(bmp);
File.WriteAllBytes("test.webp", rawWebP); 
```

Encode to memory buffer in lossless mode with speed 9 and save to file
```C#
byte[] rawWebP = File.ReadAllBytes("test.jpg");
using (WebP webp = new WebP())
  rawWebP = webp.EncodeLossless(bmp, 9);
File.WriteAllBytes("test.webp", rawWebP); 
```

Encode to memory buffer in near lossless mode with quality 40 and speed 9 and save to file
```C#
byte[] rawWebP = File.ReadAllBytes("test.jpg");
using (WebP webp = new WebP())
  rawWebP = webp.EncodeNearLossless(bmp, 40, 9);
File.WriteAllBytes("test.webp", rawWebP); 
```

Get version of libwebp.dll
```C#
using (WebP webp = new WebP())
  string version = "libwebp.dll v" + webp.GetVersion();
```

Get info from WebP file
```C#
byte[] rawWebp = File.ReadAllBytes(pathFileName);
using (WebP webp = new WebP())
  webp.GetInfo(rawWebp, out width, out height, out has_alpha, out has_animation, out format);
MessageBox.Show("Width: " + width + "\n" +
                "Height: " + height + "\n" +
                "Has alpha: " + has_alpha + "\n" +
                "Is animation: " + has_animation + "\n" +
                "Format: " + format);
```

Get PSNR, SSIM or LSIM distortion metric between two pictures
```C#
int metric = 0;  //0 = PSNR, 1= SSIM, 2=LSIM
Bitmap bmp1 = Bitmap.FromFile("image1.png");
Bitmap bmp2 = Bitmap.FromFile("image2.png");
using (WebP webp = new WebP())
	result = webp.GetPictureDistortion(source, reference, metric);
	                    MessageBox.Show("Red: " + result[0] + "dB.\nGreen: " + result[1] + "dB.\nBlue: " + result[2] + "dB.\nAlpha: " + result[3] + "dB.\nAll: " + result[4] + "dB.", "PSNR");

MessageBox.Show("Red: " + result[0] + dB\n" +
                "Green: " + result[1] + "dB\n" +
                "Blue: " + result[2] + "dB\n" +
                "Alpha: " + result[3] + "dB\n" +
                "All: " + result[4] + "dB\n");
```


## Thanks to jzern@google.com
Without their help this wapper would not have been possible.
