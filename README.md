# WebP-wapper
Wapper for libwebp in C#. The most complete wapper in pure managed C#.
Exposes Simple Decoding API, Simple Encoding API, Advanced Encoding API (with stadistis of compresion), Get version library and WebPGetFeatures (info of any WebP file). In the future I´ll update for expose Advanced Decoding API.

The wapper are in safe managed code in one class. No need external dll except libwebp.dll (included). The wapper work in 32 and 64 bit system if you swith to the apropiate library.

The code are full comented and include simple example for using the wapper.

Use:
Load WebP image form WebP file
```C#
using (clsWebP webp = new clsWebP())
  Bitmap bmp = webp.Load("test.webp");
```

Save bitmap to WebP file
```C#
Bitmap bmp = new Bitmap("test.jpg");
using (clsWebP webp = new clsWebP())
  webp.Save(bmp, 80, "test.webp");
```

Decode WebP filename to bitmap and load in PictureBox container
```C#
byte[] rawWebP = File.ReadAllBytes("test.webp");
using (clsWebP webp = new clsWebP())
  this.pictureBox.Image = webp.Decode(rawWebP);
```

Encode to memory buffer in lossly mode with quality 75 and save to file
```C#
byte[] rawWebP = File.ReadAllBytes("test.jpg");
using (clsWebP webp = new clsWebP())
  rawWebP = webp.EncodeLossy(bmp, 75);
File.WriteAllBytes("test.webp", rawWebP); 
```

Encode to memory buffer in SimpleLossless mode and save to file
```C#
byte[] rawWebP = File.ReadAllBytes("test.jpg");
using (clsWebP webp = new clsWebP())
  rawWebP = webp.EncodeSimpleLossless(bmp);
File.WriteAllBytes("test.webp", rawWebP); 
```

Encode to memory buffer in EncodeLossless mode with speed 9 and save to file
```C#
byte[] rawWebP = File.ReadAllBytes("test.jpg");
using (clsWebP webp = new clsWebP())
  rawWebP = webp.EncodeLossless(bmp, 40, 9);
File.WriteAllBytes("test.webp", rawWebP); 
```

Encode to memory buffer in near lossless mode with quality 40 and speed 9 and save to file
```C#
byte[] rawWebP = File.ReadAllBytes("test.jpg");
using (clsWebP webp = new clsWebP())
  rawWebP = webp.EncodeNearLossless(bmp, 40, 9);
File.WriteAllBytes("test.webp", rawWebP); 
```

Get version of libwebp.dll
```C#
using (clsWebP webp = new clsWebP())
  string version = "libwebp.dll v" + webp.GetVersion();
```

Get info from WebP file
```C#
byte[] rawWebp = File.ReadAllBytes(pathFileName);
using (clsWebP webp = new clsWebP())
  webp.GetInfo(rawWebp, out width, out height, out has_alpha, out has_animation, out format);
MessageBox.Show("Width: " + width + "\n" +
                "Height: " + height + "\n" +
                "Has alpha: " + has_alpha + "\n" +
                "Is animation: " + has_animation + "\n" +
                "Format: " + format);
```
