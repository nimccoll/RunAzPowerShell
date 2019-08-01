//===============================================================================
// Microsoft FastTrack for Azure
// Azure Service Bus Samples
//===============================================================================
// Copyright © Microsoft Corporation.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
//===============================================================================
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RunAzPowerShell
{
    class Program
    {
        private static IConfigurationRoot _configuration;
        private static IQueueClient _queueClient;
        private static bool _isStopped = false;

        static void Main(string[] args)
        {
            IConfigurationBuilder builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            _configuration = builder.Build();

            MainAsync().GetAwaiter().GetResult();
        }

        static async Task MainAsync()
        {
            string serviceBusConnectionString = _configuration["serviceBusConnectionString"];
            string queueName = _configuration["queueName"];
            _queueClient = new QueueClient(serviceBusConnectionString, queueName);

            Console.WriteLine("======================================================");
            Console.WriteLine("Listening for messages. Press Ctrl-C to exit.");
            Console.WriteLine("======================================================");

            // Register the queue message handler and receive messages in a loop
            RegisterOnMessageHandlerAndReceiveMessages();

            // Handle process exit
            AppDomain.CurrentDomain.ProcessExit += (s, e) =>
            {
                Console.WriteLine("Exiting...");
                _queueClient.CloseAsync().Wait();
            };

            // Handle Ctrl-C or Ctrl-Break
            Console.CancelKeyPress += (s, e) =>
            {
                _isStopped = true;
                e.Cancel = true;
            };

            // Continue running while messages are being received
            while (!_isStopped)
            {
                await Task.Delay(3000);
            }
        }

        static void RegisterOnMessageHandlerAndReceiveMessages()
        {
            // Configure the message handler options in terms of exception handling, number of concurrent messages to deliver, etc.
            MessageHandlerOptions messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                // Maximum number of concurrent calls to the callback ProcessMessagesAsync(), set to 1 for simplicity.
                // Set it according to how many messages the application wants to process in parallel.
                MaxConcurrentCalls = 1,

                // Indicates whether the message pump should automatically complete the messages after returning from user callback.
                // False below indicates the complete operation is handled by the user callback as in ProcessMessagesAsync().
                AutoComplete = false
            };

            // Register the function that processes messages.
            _queueClient.RegisterMessageHandler(ProcessMessagesAsync, messageHandlerOptions);
        }

        static async Task ProcessMessagesAsync(Message message, CancellationToken cancellationToken)
        {
            // Process the message.
            Console.WriteLine($"Received message: SequenceNumber:{message.SystemProperties.SequenceNumber} Body:{Encoding.UTF8.GetString(message.Body)}");

            // Complete the message so that it is not received again.
            // This can be done only if the queue Client is created in ReceiveMode.PeekLock mode (which is the default).
            await _queueClient.CompleteAsync(message.SystemProperties.LockToken);
            RunAzPowerShell();
            
            // Note: Use the cancellationToken passed as necessary to determine if the queueClient has already been closed.
            // If queueClient has already been closed, you can choose to not call CompleteAsync() or AbandonAsync() etc.
            // to avoid unnecessary exceptions.
        }

        static Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            Console.WriteLine($"Message handler encountered an exception {exceptionReceivedEventArgs.Exception}.");
            var context = exceptionReceivedEventArgs.ExceptionReceivedContext;
            Console.WriteLine("Exception context for troubleshooting:");
            Console.WriteLine($"- Endpoint: {context.Endpoint}");
            Console.WriteLine($"- Entity Path: {context.EntityPath}");
            Console.WriteLine($"- Executing Action: {context.Action}");
            return Task.CompletedTask;
        }

        static void RunAzPowerShell()
        {
            string powerShell = _configuration["PowerShell"];
            string powerShellScript = _configuration["PowerShellScript"];
            Process azPowerShell = new Process();
            azPowerShell.StartInfo.FileName = powerShell;
            azPowerShell.StartInfo.Arguments = $"-File {powerShellScript}"; // -File must be last parameter include any parameters after the script name
            azPowerShell.StartInfo.CreateNoWindow = true;
            azPowerShell.StartInfo.UseShellExecute = false;
            azPowerShell.StartInfo.RedirectStandardOutput = true;
            azPowerShell.StartInfo.RedirectStandardError = true;
            azPowerShell.Start();
            Console.WriteLine(azPowerShell.StandardOutput.ReadToEnd());
            Console.WriteLine(azPowerShell.StandardError.ReadToEnd());
            azPowerShell.WaitForExit();
        }
    }
}
