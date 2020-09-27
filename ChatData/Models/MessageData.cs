using System;
using System.Runtime.Serialization;

namespace ChatData.Models
{
    [DataContract]
    public class MessageData
    {
        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public string SenderId { get; set; }

        [DataMember]
        public string RoomId { get; set; }

        [DataMember]
        public DateTime CreatedAt { get; set; }

        [DataMember]
        public string Content { get; set; }

        public MessageData(string senderId, string roomId, string content)
        {
            this.Id = Guid.NewGuid().ToString();
            this.SenderId = senderId;
            this.RoomId = roomId;
            this.CreatedAt = DateTime.Now;
            this.Content = content;
        }
    }
}
