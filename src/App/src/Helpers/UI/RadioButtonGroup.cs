using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;

namespace mrHelper.App.Helpers
{
   internal class RadioButtonGroup
   {
      internal void AddRadioButton(RadioButton radioButton, int flag)
      {
         _radioToFlags.Add(radioButton, flag);
      }

      internal void UpdateCheckedState(int value)
      {
         foreach (KeyValuePair<RadioButton, int> radio in _radioToFlags)
         {
            radio.Key.Checked = radio.Value == (int)value;
         }
      }

      internal T GetState<T>()
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

