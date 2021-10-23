﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using XOutput.Websocket;

namespace XOutput.Serialization
{
    public class MessageReader
    {
        private readonly Dictionary<string, Type> mapping;
        private readonly JsonSerializerOptions serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public MessageReader(Dictionary<string, Type> mapping)
        {
            this.mapping = mapping;
            foreach (var type in mapping.Values)
            {
                if (!typeof(MessageBase).IsAssignableFrom(type))
                {
                    throw new ArgumentException("Invalid mapping");
                }
            }
        }

        public MessageBase ReadString(string input)
        {
            var message = JsonSerializer.Deserialize<MessageBase>(input, serializerOptions);
            if (!mapping.ContainsKey(message.Type))
            {
                return message;
            }
            var type = mapping[message.Type];
            return JsonSerializer.Deserialize(input, type, serializerOptions) as MessageBase;
        }

        public MessageBase Read(StreamReader input)
        {
            string text = input.ReadToEnd();
            return ReadString(text);
        }
    }
}
