using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class SerializationManager
{
    public static bool Save(string _saveName, object _saveData)
    {
        BinaryFormatter formatter = GetBinaryFormatter();

        if(!Directory.Exists(Application.persistentDataPath + "/saves")) 
        {
            Directory.CreateDirectory(Application.persistentDataPath + "/saves");
        }

        string path = Application.persistentDataPath + "/saves/" + _saveName + ".save";

        FileStream file = File.Create(path);
        formatter.Serialize(file, _saveData);
        file.Close();

        return true;
    }

    public static object Load(string _path)
    {
        if(!File.Exists(_path))
        {
            return null;
        }

        BinaryFormatter formatter = GetBinaryFormatter();

        FileStream file = File.Open(_path, FileMode.Open);

        try
        {
            object save = formatter.Deserialize(file);
            file.Close();
            return save;
        }
        catch
        {
            Debug.LogErrorFormat("Failed to load file at {0}.", _path);
            file.Close();
            return null;
        }
    }

    private static BinaryFormatter GetBinaryFormatter()
    {
        BinaryFormatter formatter = new BinaryFormatter();

        SurrogateSelector selector = new SurrogateSelector();

        Vector3SerializationSurrogate v3S = new Vector3SerializationSurrogate();
        QuaternionSerializationSurrogate qS = new QuaternionSerializationSurrogate();

        selector.AddSurrogate(typeof(Vector3), new StreamingContext(StreamingContextStates.All), v3S);
        selector.AddSurrogate(typeof(Quaternion), new StreamingContext(StreamingContextStates.All), qS);

        formatter.SurrogateSelector = selector;

        return formatter;
    }
}
