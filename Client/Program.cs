using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using Shared.Contracts;
using Shared.Models;

namespace Client
{
    public class Program
    {
        static void Main(string[] args)
        {
            ChannelFactory<IEegService> factory =
               new ChannelFactory<IEegService>("EegServiceEndpoint");

            IEegService proxy = factory.CreateChannel();

            try
            {
                string odgovor = proxy.Ping("Zdravo servere");

                Console.WriteLine(odgovor);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.ReadLine();
        }
    }
}
