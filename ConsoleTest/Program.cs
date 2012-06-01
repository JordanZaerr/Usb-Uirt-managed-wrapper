using System;
using UsbUirt;
using UsbUirt.Enums;
using UsbUirt.EventArgs;

namespace ConsoleTest
{
    class Program
    {
        static void Main()
        {
            using (var driver = new Driver())
            {
                Console.WriteLine(Driver.GetVersion(driver).ToString());

                Console.WriteLine("Receiving...");
                var receiver = new Receiver(driver);
                receiver.GenerateLegacyCodes = false;
                receiver.Received += OnReceive;

                var learner = new Learner(driver);
                learner.Learning += OnLearning;
                Console.WriteLine("Learning...");
                var result = learner.Learn();
                Console.WriteLine("Learned code: " + result);

                Console.WriteLine("Hit enter to Transmit");
                Console.ReadLine();
                var transmitter = new Transmitter(driver);
                transmitter.TransmitCompleted += OnTransmitComplete;
                transmitter.TransmitAsync(result, emitter: Emitter.Internal)
                    .ContinueWith(t => Console.WriteLine(t.Exception == null 
                                        ? "Transmit Complete - from task" 
                                        : t.Exception.ToString()));
                Console.ReadLine();
            }
        }

        private static void OnReceive(object sender, ReceivedEventArgs e)
        {
            Console.WriteLine("Received: " + e.IRCode);
        }

        private static void OnTransmitComplete(object sender, TransmitCompletedEventArgs e)
        {
            Console.WriteLine(e.Error == null ? "Transmit Complete - from event" : e.Error.ToString());
        }

        private static void OnLearning(object sender, LearningEventArgs e)
        {
            Console.WriteLine("Learning: {0}% freq={1} quality={2}",
                e.Progress,
                e.CarrierFrequency,
                e.SignalQuality);
        }
    }
}
