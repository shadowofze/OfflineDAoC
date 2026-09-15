namespace WPFLauncher
{
    /// <summary>
    ///     The class used to model the info of a file.
    /// </summary>
    public class FileData : IFile
    {
        /// <summary>
        ///     Creates a new instance of the class File.
        /// </summary>
        public FileData(string filename, string hash, string size)
        {
            FileName = filename;
            Hash = hash;
            Size = size;
        }

        /// <summary>
        ///     The hash of the file.
        /// </summary>
        public string Hash { get; }

        /// <summary>
        ///     The file name of the file.
        /// </summary>
        public string FileName { get; }

        /// <summary>
        ///     The size of the file.
        /// </summary>
        public string Size { get; }

        /// <summary>
        /// The current state of the file after checking the local folder
        /// </summary>
        public FileState FileState { get; set; } = FileState.None;
    }

    public enum FileState
    {
        None,
        Found,
        Missing
    }
}