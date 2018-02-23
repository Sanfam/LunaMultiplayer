﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace LunaCommon.Xml
{
    public class LunaXmlSerializer
    {
        #region Read

        public static T ReadXmlFromPath<T>(string path) where T : class, new()
        {
            if (!File.Exists(path))
                return null;
            try
            {
                using (TextReader r = new StreamReader(path))
                {
                    var deserializer = new XmlSerializer(typeof(T));
                    var structure = (T)deserializer.Deserialize(r);
                    return structure;
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Could not open and read file from path {path}. Details: {e}");
            }
        }

        public static T ReadXmlFromString<T>(string content) where T : class, new()
        {
            if (string.IsNullOrEmpty(content))
                return null;
            try
            {
                using (TextReader r = new StringReader(content))
                {
                    var deserializer = new XmlSerializer(typeof(T));
                    var structure = (T)deserializer.Deserialize(r);
                    return structure;
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Could not open and read file content. Details: {e}");
            }
        }

        public static object ReadXmlFromPath(Type classType, string path)
        {
            if (!File.Exists(path))
                return null;
            try
            {
                using (TextReader r = new StreamReader(path))
                {
                    var deserializer = new XmlSerializer(classType);
                    var structure = deserializer.Deserialize(r);
                    return structure;
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Could not open and read file from path {path}. Details: {e}");
            }
        }

        #endregion

        #region Write

        public static void WriteToXmlFile(object objectToSerialize, string path)
        {
            File.WriteAllText(path, SerializeToXml(objectToSerialize));
        }

        public static string SerializeToXml(object objectToSerialize)
        {
            try
            {
                using (var s = new StringWriter())
                using (var w = new XmlTextWriter(s))
                {
                    w.Formatting = Formatting.Indented;
                    var serializer = new XmlSerializer(objectToSerialize.GetType());
                    serializer.Serialize(w, objectToSerialize);

                   return WriteComments(objectToSerialize, s.ToString());
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Could not write xml. Details: {e}");
            }
        }

        #endregion

        #region Private
        
        private static string WriteComments(object objectToSerialize, string contents)
        {
            try
            {
                using (var stringWriter = new StringWriter())
                {
                    var propertyComments = GetPropertiesAndComments(objectToSerialize);
                    if (!propertyComments.Any()) return contents;

                    var doc = new XmlDocument();
                    doc.LoadXml(contents);

                    var parent = doc.SelectSingleNode(objectToSerialize.GetType().Name);
                    if (parent == null) return contents;

                    var childNodes = parent.ChildNodes.Cast<XmlNode>().Where(n => propertyComments.ContainsKey(n.Name));
                    foreach (var child in childNodes)
                    {
                        parent.InsertBefore(doc.CreateComment(propertyComments[child.Name]), child);
                    }

                    doc.Save(stringWriter);

                    return stringWriter.ToString();
                }
            }
            catch (Exception)
            {
                // ignored
            }

            return contents;
        }

        private static Dictionary<string, string> GetPropertiesAndComments(object objectToSerialize)
        {
            var propertyComments = objectToSerialize.GetType().GetProperties()
                .Where(p => p.GetCustomAttributes(typeof(XmlCommentAttribute), false).Any())
                .Select(v => new
                {
                    v.Name,
                    ((XmlCommentAttribute) v.GetCustomAttributes(typeof(XmlCommentAttribute), false)[0]).Value
                })
                .ToDictionary(t => t.Name, t => t.Value);
            return propertyComments;
        }

        #endregion
    }
}
