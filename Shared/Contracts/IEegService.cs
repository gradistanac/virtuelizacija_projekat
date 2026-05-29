using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.ServiceModel;
using Common.Models;

namespace Common.Contracts
{
    [ServiceContract]
    public interface IEegService
    {
        [OperationContract]
        [FaultContract(typeof(ValidationFault))]
        string StartSession(EegMeta meta);

        [OperationContract]
        [FaultContract(typeof(DataFormatFault))]
        [FaultContract(typeof(ValidationFault))]
        string PushSample(EegSample sample);

        [OperationContract]
        [FaultContract(typeof(ValidationFault))]
        string EndSession();
    }
}