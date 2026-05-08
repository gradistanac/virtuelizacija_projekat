using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace Shared.Models
{
    [DataContract]
    public class EegMeta
    {
        [DataMember]
        public string ParticipantId { get; set; }
        [DataMember]
        public string FileName { get; set; }
        [DataMember]
        public int TotalRows { get; set; }
        [DataMember]
        public string SchemaVersion { get; set; }
    }
}
