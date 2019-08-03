namespace ByteCounter
{
    /// <summary>
    /// Model for output to xml file
    /// </summary>
    public class Result
    {
        /// <summary>
        /// Property for output file name to xml file.
        /// </summary>
        public string File { get; }

        /// <summary>
        /// Property for output total bytes to xml file.
        /// </summary>
        public long TotalBytes { get; }

        /// <summary>
        /// Model for output to xml file
        /// </summary>
        /// <param name="file"></param>
        /// <param name="totalBytes"></param>
        public Result(string file, long totalBytes)
        {
            File = file;
            TotalBytes = totalBytes;
        }
    }
}