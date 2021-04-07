using System;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace mrHelper.Common.Tools
{
   public class PersistenceStateSerializationException : Exception
   {
      internal PersistenceStateSerializationException(string msg, Exception exception) : base(msg, exception) { }
   }

   public class PersistenceStateDeserializationException : Exception
   {
      internal PersistenceStateDeserializationException(string msg, Exception exception) : base(msg, exception) { }
   }

   public class PersistentStorage
   {
      private readonly string storageFileName = "mrhelper.state.json";

      public event Action<IPersistentStateSetter> OnSerialize;
      public event Action<IPersistentStateGetter> OnDeserialize;

      /// <summary>
      /// Throws PersistenceStateSerializationException
      /// </summary>
      public void Serialize()
      {
         PersistentState state = new PersistentState();
         OnSerialize?.Invoke(state);
         saveToFile(state);
      }

      /// <summary>
      /// Throws PersistenceStateDeserializationException
      /// </summary>
      public void Deserialize()
      {
         PersistentState state = loadFromFile();
         OnDeserialize?.Invoke(state);
      }

      private void saveToFile(PersistentState state)
      {
         try
         {
            System.IO.File.WriteAllText(getFilePath(), state.ToJson());
         }
         catch (Exception ex) // Any exception from System.IO.File.WriteAllText()
         {
            throw new PersistenceStateSerializationException("Cannot serialize state", ex);
         }
      }

      private PersistentState loadFromFile()
      {
         string filePath = getFilePath();
         if (!System.IO.File.Exists(filePath))
         {
            return new PersistentState();
         }

         string json;
         try
         {
            json = System.IO.File.ReadAllText(filePath);
         }
         catch (Exception ex) // Any exception from System.IO.File.ReadAllText()
         {
            throw new PersistenceStateDeserializationException("Cannot deserialize state", ex);
         }
         return new PersistentState(json);
      }

      private string getFilePath()
      {
         return System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Constants.Constants.ApplicationDataFolderName, storageFileName);
      }
   }
}

