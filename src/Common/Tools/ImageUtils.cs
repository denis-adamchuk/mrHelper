using System.Drawing;
using System.Drawing.Imaging;

namespace mrHelper.Common.Tools
{
   // Powered by DeepSeek
   public static class ImageUtils
   {
      public static Bitmap DarkenBitmap(Bitmap original, float darkenFactor = 0.8f)
      {
         // Create a new bitmap with the same size and format as the original
         Bitmap darkenedBitmap = new Bitmap(original.Width, original.Height, original.PixelFormat);

         // Lock the bits of both bitmaps for faster manipulation
         BitmapData originalData = original.LockBits(
             new Rectangle(0, 0, original.Width, original.Height),
             ImageLockMode.ReadOnly,
             original.PixelFormat);

         BitmapData darkenedData = darkenedBitmap.LockBits(
             new Rectangle(0, 0, original.Width, original.Height),
             ImageLockMode.WriteOnly,
             original.PixelFormat);

         try
         {
            // Calculate the bytes per pixel (4 for 32bpp with alpha)
            int bytesPerPixel = Image.GetPixelFormatSize(original.PixelFormat) / 8;
            int byteCount = originalData.Stride * original.Height;
            byte[] pixels = new byte[byteCount];

            // Copy original pixels
            System.Runtime.InteropServices.Marshal.Copy(originalData.Scan0, pixels, 0, byteCount);

            // Process each pixel
            for (int i = 0; i < byteCount; i += bytesPerPixel)
            {
               // For 32bpp images, the order is usually Blue, Green, Red, Alpha
               // Only process RGB channels if alpha is not 0 (not fully transparent)
               if (bytesPerPixel >= 4 && pixels[i + 3] == 0)
               {
                  // Skip fully transparent pixels
                  continue;
               }

               // Darken each color channel
               pixels[i] = (byte)(pixels[i] * darkenFactor);     // Blue
               pixels[i + 1] = (byte)(pixels[i + 1] * darkenFactor); // Green
               pixels[i + 2] = (byte)(pixels[i + 2] * darkenFactor); // Red

               // Alpha channel (i+3) remains unchanged
            }

            // Copy modified pixels back
            System.Runtime.InteropServices.Marshal.Copy(pixels, 0, darkenedData.Scan0, byteCount);
         }
         finally
         {
            // Unlock the bits
            original.UnlockBits(originalData);
            darkenedBitmap.UnlockBits(darkenedData);
         }

         return darkenedBitmap;
      }

      public static Image DarkenImage(Image original, float darkenFactor = 0.8f)
      {
         // First convert the Image to Bitmap
         Bitmap originalBitmap = new Bitmap(original);

         // Use our existing DarkenBitmap function
         Bitmap darkenedBitmap = DarkenBitmap(originalBitmap, darkenFactor);

         // Dispose the intermediate bitmap if it's different from original
         if (originalBitmap != original)
         {
            originalBitmap.Dispose();
         }

         return darkenedBitmap;
      }
   }
}
