using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.ServiceModel;
using Shared.Models;

namespace Shared.Contracts
{
    [ServiceContract]
    public interface IEegService
    {
        [OperationContract]
        string StartSession(EegMeta meta);
        [OperationContract]
        string PushSample(EegSample sample);
        [OperationContract]
        void EndSession();
    }
}
