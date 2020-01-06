using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;

namespace mrHelper.CommonControls.Tools
{
   public class RadioButtonGroup
   {
      public void AddRadioButton(RadioButton radioButton, int flag)
      {
         _radioToFlags.Add(radioButton, flag);
      }

      public void UpdateCheckedState(int value)
      {
         foreach (KeyValuePair<RadioButton, int> radio in _radioToFlags)
         {
            radio.Key.Checked = radio.Value == (int)value;
         }
      }

      public T GetState<T>()
      {
         foreach (KeyValuePair<RadioButton, int> radio in _radioToFlags)
         {
            if (radio.Key.Checked)
            {
               return (T)(object)radio.Value;
            }
         }

         Debug.Assert(false);
         return (T)(object)0;
      }

      private readonly Dictionary<RadioButton, int> _radioToFlags = new Dictionary<RadioButton, int>();
   }
}

