using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowDSIO.Json
{
    public class StringArrayConverter : JsonConverter
    {
        public override void WriteJson(
            JsonWriter writer,
            object value,
            JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            string[] data = (string[])value;

            var previousSerializerFormatting = serializer.Formatting;
            var previousWriterFormatting = writer.Formatting;

            if (data.Length <= 6)
            {
                serializer.Formatting = Formatting.None;
                writer.Formatting = Formatting.None;
            }

            // Compose an array.
            writer.WriteStartArray();

            for (var i = 0; i < data.Length; i++)
            {
                writer.WriteValue(data[i]);
            }

            writer.WriteEndArray();

            serializer.Formatting = previousSerializerFormatting;
            writer.Formatting = previousWriterFormatting;
        }

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartArray)
            {
                var stringList = new List<string>();

                while (reader.Read())
                {
                    switch (reader.TokenType)
                    {
                        case JsonToken.String:
                            stringList.Add(Convert.ToString(reader.Value));
                            break;
                        case JsonToken.EndArray:
                            return stringList.ToArray();
                        case JsonToken.Comment:
                            // skip
                            break;
                        default:
                            throw new Exception(
                            string.Format(
                                "Unexpected token when reading bytes: {0}",
                                reader.TokenType));
                    }
                }

                throw new Exception("Unexpected end when reading bytes.");
            }
            else
            {
                throw new Exception(
                    string.Format(
                        "Unexpected token parsing binary. "
                        + "Expected StartArray, got {0}.",
                        reader.TokenType));
            }
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string[]);
        }
    }
}
