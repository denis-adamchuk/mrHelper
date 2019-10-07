using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace mrHelper.CommonControls
{
   public class ItemSelectionChangingEventArgs : CancelEventArgs
   {
      public int Index { get; private set; }
      public bool NewValue { get; private set; }
      public bool CurrentValue { get; private set; }

      public ItemSelectionChangingEventArgs(int index, bool newValue, bool currentValue)
      {
         Index = index;
         NewValue = newValue;
         CurrentValue = currentValue;
      }

      public override string ToString()
      {
         return String.Format("Index={0}, NewValue={1}, CurrentValue={2}", Index, NewValue, CurrentValue);
      }
   }

   public class ListViewEx : ListView
   {
      public ListViewEx()
      {
         DoubleBuffered = true;
      }

      private static readonly Object ItemSelectionChangingEvent = new Object();
      public event EventHandler<ItemSelectionChangingEventArgs> ItemSelectionChanging
      {
         add { Events.AddHandler(ItemSelectionChangingEvent, value); }
         remove { Events.RemoveHandler(ItemSelectionChangingEvent, value); }
      }

      protected virtual void OnItemSelectionChanging(ItemSelectionChangingEventArgs e)
      {
         EventHandler<ItemSelectionChangingEventArgs> handler =
             (EventHandler<ItemSelectionChangingEventArgs>)Events[ItemSelectionChangingEvent];
         if (handler != null)
            handler(this, e);
      }

      protected override void WndProc(ref Message m)
      {
         if (m.Msg == 0x2000 + 0x004E) // [reflected] WM_NOTIFY
         {
            uint nmhdrCode = (uint)Marshal.ReadInt32(m.LParam, NmHdrCodeOffset);
            if (nmhdrCode == LVN_ITEMCHANGING)
            {
               NMLISTVIEW nmlv = (NMLISTVIEW)Marshal.PtrToStructure(m.LParam, typeof(NMLISTVIEW));
               if ((nmlv.uChanged & LVIF_STATE) != 0)
               {
                  bool currentSel = (nmlv.uOldState & LVIS_SELECTED) == LVIS_SELECTED;
                  bool newSel = (nmlv.uNewState & LVIS_SELECTED) == LVIS_SELECTED;

                  if (newSel != currentSel)
                  {
                     ItemSelectionChangingEventArgs e = new ItemSelectionChangingEventArgs(nmlv.iItem, newSel, currentSel);
                     OnItemSelectionChanging(e);
                     m.Result = e.Cancel ? (IntPtr)1 : IntPtr.Zero;
                     return;
                  }
               }
            }
         }

         base.WndProc(ref m);
      }

      const int LVIF_STATE = 8;

      const int LVIS_FOCUSED = 1;
      const int LVIS_SELECTED = 2;

      const uint LVN_FIRST = unchecked(0U - 100U);
      const uint LVN_ITEMCHANGING = unchecked(LVN_FIRST - 0);
      const uint LVN_ITEMCHANGED = unchecked(LVN_FIRST - 1);

      static readonly int NmHdrCodeOffset = IntPtr.Size * 2;

      [StructLayout(LayoutKind.Sequential)]
      struct NMHDR
      {
         public IntPtr hwndFrom;
         public IntPtr idFrom;
         public uint code;
      }

      [StructLayout(LayoutKind.Sequential)]
      struct NMLISTVIEW
      {
         public NMHDR hdr;
         public int iItem;
         public int iSubItem;
         public int uNewState;
         public int uOldState;
         public int uChanged;
         public IntPtr lParam;
      }
   }
}
