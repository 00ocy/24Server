using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NetworkLibrary
{
    internal class MessageProcessor
    {
        public class ClientJson
        {
            public string? Time { get; set; }
            public string? ID { get; set; }
            public string? Name { get; set; }           
        }
  
        public List<string> ProcessMessage(string message)
        {
            // 메시지를 \r\n으로 분리하여 각각의 메시지 처리
            string[] messageParts = message.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            List<string> messagePartsList= new List<string>();

            foreach (var part in messageParts)
            {
                part.Trim('\n');
                if (part.StartsWith("{") && part.Contains("::"))
                {
                    string processedMessage = ReciveByJson(part);
                    messagePartsList.Add(processedMessage);
                }
                else
                {
                    messagePartsList.Add(part);
                }
            }

            return messagePartsList;
        }

        private string ReciveByJson(string message)
        {
            ClientJson clientJson = new ClientJson();
            string[]? messages;
            message = message.Trim();
            messages = message.Split("::", StringSplitOptions.RemoveEmptyEntries);
            if (messages.Length == 2)
            {
                char[] trimChar = { '{', '}' };
                clientJson.ID = messages[0].Trim(trimChar);
                clientJson.Time = messages[1].Trim(trimChar);
            }
            string jsonToString = JsonSerializer.Serialize(clientJson, new JsonSerializerOptions { WriteIndented = true });
            return jsonToString;
        }
    }
}
