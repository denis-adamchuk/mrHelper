using System.Drawing;
using mrHelper.Common.Constants;
using mrHelper.Common.Tools;

namespace mrHelper.App.Helpers
{
   // https://pinetools.com/darken-image
   internal static class ToolbarIconSelector
   {
      internal static Image GetClipboardIcon()
      {
         Bitmap bitmap = Properties.Resources.clipboard_100x100;
         return isDarkMode() ? ImageUtils.DarkenBitmap(bitmap) : bitmap;
      }

      internal static Image GetCreateNewIcon()
      {
         Bitmap bitmap = Properties.Resources.create_new_100x100;
         return isDarkMode() ? ImageUtils.DarkenBitmap(bitmap) : bitmap;
      }

      internal static Image GetAddDiscussionIcon()
      {
         Bitmap bitmap = Properties.Resources.thread_100x100;
         return isDarkMode() ? ImageUtils.DarkenBitmap(bitmap) : bitmap;
      }

      internal static Image GetAddCommentIcon()
      {
         Bitmap bitmap = Properties.Resources.add_comment_100x100;
         return isDarkMode() ? ImageUtils.DarkenBitmap(bitmap) : bitmap;
      }

      internal static Image GetMergeIcon()
      {
         Bitmap bitmap = Properties.Resources.merge_100x100;
         return isDarkMode() ? ImageUtils.DarkenBitmap(bitmap) : bitmap;
      }

      internal static Image GetLinkIcon()
      {
         Bitmap bitmap = Properties.Resources.link_100x100;
         return isDarkMode() ? ImageUtils.DarkenBitmap(bitmap) : bitmap;
      }

      internal static Image GetDiscussionsIcon()
      {
         Bitmap bitmap = Properties.Resources.discussions_100x100;
         return isDarkMode() ? ImageUtils.DarkenBitmap(bitmap) : bitmap;
      }

      internal static Image GetDiffIcon()
      {
         Bitmap bitmap = Properties.Resources.diff_100x100;
         return isDarkMode() ? ImageUtils.DarkenBitmap(bitmap) : bitmap;
      }

      internal static Image GetPlayIcon()
      {
         Bitmap bitmap = Properties.Resources.play_100x100;
         return isDarkMode() ? ImageUtils.DarkenBitmap(bitmap) : bitmap;
      }

      internal static Image GetStopIcon()
      {
         Bitmap bitmap = Properties.Resources.stop_100x100;
         return isDarkMode() ? ImageUtils.DarkenBitmap(bitmap) : bitmap;
      }

      internal static Image GetCancelIcon()
      {
         Bitmap bitmap = Properties.Resources.cancel_100x100;
         return isDarkMode() ? ImageUtils.DarkenBitmap(bitmap) : bitmap;
      }

      internal static Image GetEditIcon()
      {
         Bitmap bitmap = Properties.Resources.edit_100x100;
         return isDarkMode() ? ImageUtils.DarkenBitmap(bitmap) : bitmap;
      }

      internal static Image GetEditMRIcon()
      {
         Bitmap bitmap = Properties.Resources.editmr_100x100;
         return isDarkMode() ? ImageUtils.DarkenBitmap(bitmap) : bitmap;
      }

      internal static Image GetRefreshIcon()
      {
         Bitmap bitmap = Properties.Resources.refresh_100x100;
         return isDarkMode() ? ImageUtils.DarkenBitmap(bitmap) : bitmap;
      }

      internal static Image GetHideIcon()
      {
         Bitmap bitmap = Properties.Resources.hide_100x100;
         return isDarkMode() ? ImageUtils.DarkenBitmap(bitmap) : bitmap;
      }

      internal static Image GetUnhideIcon()
      {
         Bitmap bitmap = Properties.Resources.unhide_100x100;
         return isDarkMode() ? ImageUtils.DarkenBitmap(bitmap) : bitmap;
      }

      internal static Image GetPinIcon()
      {
         Bitmap bitmap = Properties.Resources.pin_100x100;
         return isDarkMode() ? ImageUtils.DarkenBitmap(bitmap) : bitmap;
      }

      internal static Image GetUnpinIcon()
      {
         Bitmap bitmap = Properties.Resources.unpin_100x100;
         return isDarkMode() ? ImageUtils.DarkenBitmap(bitmap) : bitmap;
      }

      private static bool isDarkMode()
      {
         Constants.ColorMode colorMode = ConfigurationHelper.GetColorMode(Program.Settings);
         return colorMode == Constants.ColorMode.Dark;
      }
   }
}
