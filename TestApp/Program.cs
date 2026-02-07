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

                // This will fail if native lib is missing
                torch.InitializeDeviceType(DeviceType.CUDA);
                
                Console.WriteLine($"CUDA Available: {torch.cuda.is_available()}");
                if (torch.cuda.is_available())
                {
                    Console.WriteLine($"Device Count: {torch.cuda.device_count()}");
                    
                    var device = torch.CUDA; 
                    Console.WriteLine($"Testing tensor allocation on {device}...");
                    
                    using (var t = torch.randn(new long[] { 3, 3 }, device: device))
                    {
                        Console.WriteLine("Tensor on GPU:");
                        Console.WriteLine(t.ToString());
                        
                        var sum = t.sum();
			sum.cpu();
                        Console.WriteLine($"Sum of tensor: {sum.item<float>()}");
                    }
                    Console.WriteLine("GPU tensor operation successful!");
                }
                else
                {
                    Console.WriteLine("CUDA is NOT available (native backend might be CPU-only or CUDA missing).");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
