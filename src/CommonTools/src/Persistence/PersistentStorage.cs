using System;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace mrHelper.CommonTools.Persistence
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
         try
         {
            OnSerialize?.Invoke(state);
            saveToFile(state);
         }
         catch (Exception ex)
         {
            throw new PersistenceStateSerializationException("Cannot serialize state", ex);
         }
      }

      /// <summary>
      /// Throws PersistenceStateDeserializationException
      /// </summary>
      public void Deserialize()
      {
         try
         {
            PersistentState state = loadFromFile();
            OnDeserialize?.Invoke(state);
         }
         catch (Exception ex)
         {
            throw new PersistenceStateDeserializationException("Cannot deserialize state", ex);
         }
      }

      private void saveToFile(PersistentState state)
      {
         System.IO.File.WriteAllText(getFilePath(), state.ToJson());
      }

      private PersistentState loadFromFile()
      {
         string filePath = getFilePath();
         if (!System.IO.File.Exists(filePath))
         {
            return new PersistentState();
         }

         string json = System.IO.File.ReadAllText(filePath);
         return new PersistentState(json);
      }

      private string getFilePath()
      {
         return System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "mrHelper", storageFileName);
      }
   }
}

