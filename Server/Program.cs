using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using Server.Services;
using Shared.Contracts;
using System.ServiceModel.Channels;

namespace Server
{
    public class Program
    {
        static void Main(string[] args)
        {
            ServiceHost host = new ServiceHost(typeof(EegService));

            try
            {
                host.Open();

                Console.WriteLine("Server je pokrenut.");
                Console.WriteLine("Pritisnite ENTER za gasenje.");

                Console.ReadLine();

                host.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());

                Console.ReadLine();

                host.Abort();
            }
        }
    }
}
