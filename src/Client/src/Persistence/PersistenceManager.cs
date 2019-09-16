using System;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace mrHelper.Client.Persistence
{
   public class PersistenceStateSerializationException : Exception
   {
      internal PersistenceStateSerializationException(string message, Exception exception)
         : base(message, exception)
      {
      }
   }

   public class PersistenceStateDeserializationException : Exception
   {
      internal PersistenceStateDeserializationException(string message, Exception exception)
         : base(message, exception)
      {
      }
   }

   public class PersistenceManager
   {
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
         string json = String.Empty;
         try
         {
            json = state.ToJson();
         }
         catch (Exception ex)
         {
            throw new PersistenceStateSerializationException("Cannot serialize state", ex);
         }
         System.IO.File.WriteAllText(getFilePath(), json);
      }

      private PersistentState loadFromFile()
      {
         if (!System.IO.File.Exists(getFilePath()))
         {
            return new PersistentState();
         }
         else
         {
            string json = System.IO.File.ReadAllText(getFilePath());

            try
            {
               return new PersistentState(json);
            }
            catch (Exception ex)
            {
               throw new PersistenceStateDeserializationException("Cannot deserialize state", ex);
            }
         }
      }

      private string getFilePath()
      {
         string filename = "mrHelper.state.json";
         return System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "mrHelper", filename);
      }
   }
}

