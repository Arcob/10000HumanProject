using UnityEngine;
using System;
using System.IO;
using System.Xml.Serialization;
 public class XMLUtil
{
    public static bool SaveSetting<T>(string file_, T setting_)
    {
        TextWriter writer = null;

        try
        {
            writer = new StreamWriter(file_);
            XmlSerializer xml = new XmlSerializer(typeof(T));
            xml.Serialize(writer, setting_);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError(string.Format("Error <msg:{0}>", ex.ToString()));
            return false;
        }

        finally
        {
            if (writer != null)
            {
                writer.Close();
            }
        }
    }

    public static T LoadSetting<T>(string file_)
    {
        T result = default(T);

        TextReader reader = null;
        try
        {
            reader = new StreamReader(file_);
            XmlSerializer xml = new XmlSerializer(typeof(T));
            result = (T)xml.Deserialize(reader);
        }
        catch (Exception ex)
        {
            Debug.LogError(string.Format("Error <msg:{0}>", ex.ToString()));
        }

        if (reader != null)
        {
            reader.Close();
        }

        return result;
    }
}