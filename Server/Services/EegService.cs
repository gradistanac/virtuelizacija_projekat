using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.Contracts;
using Shared.Models;

namespace Server.Services
{
    public class EegService : IEegService
    {
        public void EndSession()
        {
            throw new NotImplementedException();
        }

        public string PushSample(EegSample sample)
        {
            throw new NotImplementedException();
        }

        public string StartSession(EegMeta meta)
        {
            throw new NotImplementedException();
        }
    }
}
