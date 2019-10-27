using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;

namespace demo01
{
    /// <summary>
    /// Yaml配置文件的提供者
    /// </summary>
    public class YamlConfigurationProvider : FileConfigurationProvider
    {

        public YamlConfigurationProvider(YamlConfigurationSource yamlConfigurationSource) : base(yamlConfigurationSource)
        {
        }

        public override void Load(Stream stream)
        {
            try
            {
                //读取到yaml内容
                var yaml = new StreamReader(stream).ReadToEnd();
                //创建yaml反序列化器，把内容信息转成对象
                var deserializer = new DeserializerBuilder().Build();
                var yamlObject = deserializer.Deserialize(new StringReader(yaml));
                //创建yaml转json序列化器,并把yaml对象转成json字符串
                var serializer = new SerializerBuilder()
                    .JsonCompatible()
                    .Build();
                var json = serializer.Serialize(yamlObject);
                //把json转成配置文件所需的键值表示方式
                Data = JsonConfigurationFileParser.Parse(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)));
            }
            catch (JsonReaderException e)
            {
                string errorLine = string.Empty;
                if (stream.CanSeek)
                {
                    stream.Seek(0, SeekOrigin.Begin);

                    IEnumerable<string> fileContent;
                    using (var streamReader = new StreamReader(stream))
                    {
                        fileContent = ReadLines(streamReader);
                        errorLine = RetrieveErrorContext(e, fileContent);
                    }
                }
                throw new FormatException(string.Format("Could not parse the JSON file. Error on line number '{0}': '{1}'.", e.LineNumber, errorLine), e);
            }
        }
        private static string RetrieveErrorContext(JsonReaderException e, IEnumerable<string> fileContent)
        {
            string errorLine = null;
            if (e.LineNumber >= 2)
            {
                var errorContext = fileContent.Skip(e.LineNumber - 2).Take(2).ToList();
                if (errorContext.Count() >= 2)
                {
                    errorLine = errorContext[0].Trim() + Environment.NewLine + errorContext[1].Trim();
                }
            }
            if (string.IsNullOrEmpty(errorLine))
            {
                var possibleLineContent = fileContent.Skip(e.LineNumber - 1).FirstOrDefault();
                errorLine = possibleLineContent ?? string.Empty;
            }
            return errorLine;
        }

        private static IEnumerable<string> ReadLines(StreamReader streamReader)
        {
            string line;
            do
            {
                line = streamReader.ReadLine();
                yield return line;
            } while (line != null);
        }

    }
}
