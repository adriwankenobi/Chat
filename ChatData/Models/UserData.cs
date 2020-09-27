using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ChatData.Models
{

    [DataContract]
    public class UserData
    {
        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public DateTime RegisteredAt { get; set; }

        [DataMember]
        public DateTime LastLoginAt { get; set; }

        [DataMember]
        public HashSet<string> Rooms { get; set; }

        public UserData(string name)
        {
            this.Id = Guid.NewGuid().ToString();
            this.Name = name;
            this.RegisteredAt = DateTime.Now;
            this.LastLoginAt = DateTime.Now;
            this.Rooms = new HashSet<string>();
        }
    }
}
