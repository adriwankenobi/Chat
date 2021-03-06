﻿using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ChatData.Models
{

    [DataContract]
    public class RoomData
    {
        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public DateTime CreatedAt { get; set; }

        [DataMember]
        public HashSet<string> Members { get; set; }

        public RoomData(string name)
        {
            this.Id = Guid.NewGuid().ToString();
            this.Name = name;
            this.CreatedAt = DateTime.Now;
            this.Members = new HashSet<string>();
        }
    }
}
