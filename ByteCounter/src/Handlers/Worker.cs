using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;
using System.Threading.Tasks;

namespace ByteCounter.Handlers
{
    /// <summary>
    /// Main worker.
    /// </summary>
    public class Worker
    {
        private readonly string _file;
        private long _total;
        private readonly StartCallbackDelegate _startCallback;
        private readonly DoneCallbackDelegate _doneCallback;

        /// <summary>
        /// Main process handler.
        /// </summary>
        /// <param name="fileArg">File to process </param>
        /// <param name="startCallbackArg">Start callback delegate</param>
        /// <param name="doneCallbackArg">Done callback delegate</param>
        public Worker(string fileArg, StartCallbackDelegate startCallbackArg,
                      DoneCallbackDelegate doneCallbackArg)
        {
            _file = fileArg;
            _startCallback = startCallbackArg;
            _doneCallback = doneCallbackArg;
        }

        /// <summary>
        /// File process handler.
        /// </summary>
        public void FileHandler()
        {
            long total = 0;
            _startCallback(Thread.CurrentThread.ManagedThreadId, this);

            var file = new FileInfo(_file);

            if (file.Length != 0)
                // Create the memory-mapped file. 
                try
                {
                    using (var mmf =
                        MemoryMappedFile.CreateFromFile(_file, FileMode.Open))
                    {
                        using (var stream = mmf.CreateViewStream())
                        {
                            var reader = new BinaryReader(stream);

                            while (reader.BaseStream.Position < reader.BaseStream.Length)
                                total += reader.ReadByte();
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    var attr = new FileInfo(_file).Attributes;
                    Console.Write(
                        "UnAuthorizedAccessException: Unable to access file. ");
                    if ((attr & FileAttributes.ReadOnly) > 0)
                        Console.WriteLine($"The file {_file} is read-only.");
                }


            _total = total;

            AppBuilder.Results.Add(new Result(_file, _total));
            Console.WriteLine($"In file {file.Name} : {total} bytes");

            _doneCallback(Thread.CurrentThread.ManagedThreadId);
        }

        private static void LocalHandler(Stream stream, int length, long total)
        {
            var sliceBytes = new byte[length];

            stream.Read(sliceBytes);
            Parallel.ForEach<byte, long>(sliceBytes, () => 0,
                (i, loop, subtotal) =>
                {
                    subtotal += i;
                    return subtotal;
                },
                finalResult => Interlocked.Add(ref total, finalResult)
            );
        }
    }
}