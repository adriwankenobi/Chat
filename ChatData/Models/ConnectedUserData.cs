using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ChatData.Models
{

    [DataContract]
    public class ConnectedUserData
    {
        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public string Ip { get; set; }

        public ConnectedUserData(string id, string ip)
        {
            this.Id = id;
            this.Ip = ip;
        }
    }
}
