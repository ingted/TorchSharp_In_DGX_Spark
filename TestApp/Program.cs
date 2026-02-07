using System;
using System.Runtime.InteropServices;
using TorchSharp;

namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Initializing TorchSharp...");
            try 
            {
                // Manually load the native library to bypass the default loader which might look for specific filenames
                string nativeLibPath = "libLibTorchSharp.so";
                Console.WriteLine($"Manually loading {nativeLibPath}...");
                NativeLibrary.Load(nativeLibPath);

                // Check availability first to avoid crashing on init
                bool cudaAvailable = torch.cuda.is_available();
                Console.WriteLine($"CUDA Available (via torch.cuda): {cudaAvailable}");

                if (cudaAvailable)
                {
                    Console.WriteLine("Initializing CUDA device...");
                    torch.InitializeDeviceType(DeviceType.CUDA);
                    var device = torch.CUDA; 
                    using (var t = torch.randn(new long[] { 3, 3 }, device: device))
                    {
                        Console.WriteLine("Tensor on GPU success!");
                        Console.WriteLine(t.ToString());
                    }
                }
                else
                {
                    Console.WriteLine("!!! CUDA is NOT reachable. Falling back to CPU test...");
                    torch.InitializeDeviceType(DeviceType.CPU);
                    using (var t = torch.randn(new long[] { 3, 3 })) 
                    {
                        Console.WriteLine("Tensor on CPU success!");
                        Console.WriteLine(t.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[ERROR] Initialization failed:");
                Console.WriteLine($"Message: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"Inner: {ex.InnerException.Message}");
                Console.WriteLine($"\nStack Trace:\n{ex.StackTrace}");
            }
        }
    }
}
