using System.Threading;
using ByteCounter.Handlers;

namespace ByteCounter
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var mainThread = new Thread(() => AppBuilder.CreateMenu(args));
            mainThread.Start();
        }
    }
}