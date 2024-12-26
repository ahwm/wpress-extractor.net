using System.Text;
using Wpress;

namespace WPress
{
    public sealed class Reader : IDisposable
    {
        private const char PATH_SEPARATOR_WIN = '\\';
        private const char PATH_SEPARATOR_UNIX = '/';

        public string Filename { get; private set; }
        public FileStream? File { get; private set; }
        public int NumberOfFiles { get; private set; }

        internal Reader(string filename)
        {
            Filename = filename;
            NumberOfFiles = 0;
        }

        public static Reader NewReader(string filename)
        {
            var reader = new Reader(filename);
            reader.Init();
            return reader;
        }

        private void Init()
        {
            try
            {
                File = System.IO.File.OpenRead(Filename);
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to open file: {ex.Message}");
            }
        }

        public byte[]? ExtractFile(string filename, string path)
        {
            File!.Seek(0, SeekOrigin.Begin);
            // TODO: implement
            return null;
        }

        public async Task<int> Extract(string? outDir = null)
        {
            File!.Seek(0, SeekOrigin.Begin);

            int iteration = 0;
            while (true)
            {
                iteration++;
                byte[] block = await GetHeaderBlock();
                if (block == null)
                {
                    return 0;
                }

                var header = new Header();

                if (block.SequenceEqual(header.GetEOFBlock()))
                {
                    break;
                }

                header.PopulateFromBytes(block);

                string pathToFile = Path.GetFullPath(Path.Combine(outDir ?? ".", 
                    TrimNullBytes(header.Prefix!), 
                    TrimNullBytes(header.Name!)));

                Console.WriteLine(pathToFile);

                Directory.CreateDirectory(Path.GetDirectoryName(pathToFile)!);

                using (var outputFile = System.IO.File.Create(pathToFile))
                {
                    int totalBytesToRead = header.GetSize();
                    while (true)
                    {
                        int bytesToRead = 512;
                        if (bytesToRead > totalBytesToRead)
                        {
                            bytesToRead = totalBytesToRead;
                        }

                        if (bytesToRead == 0)
                        {
                            break;
                        }

                        byte[] content = new byte[bytesToRead];
                        int bytesRead = await File.ReadAsync(content.AsMemory(0, bytesToRead));

                        totalBytesToRead -= bytesRead;
                        byte[] contentRead = [.. content.Take(bytesRead)];

                        await outputFile.WriteAsync(contentRead);
                    }
                }

                NumberOfFiles++;
            }

            return NumberOfFiles;
        }

        private async Task<byte[]> GetHeaderBlock()
        {
            byte[] block = new byte[Constants.HeaderSize];
            int bytesRead = await File!.ReadAsync(block.AsMemory(0, Constants.HeaderSize));

            if (bytesRead != Constants.HeaderSize)
            {
                throw new IOException("Unable to read header block size");
            }

            return block;
        }

        public async Task<int> GetFilesCount()
        {
            if (NumberOfFiles != 0)
            {
                return NumberOfFiles;
            }

            File!.Seek(0, SeekOrigin.Begin);

            while (true)
            {
                byte[] block = await GetHeaderBlock();
                var header = new Header();

                if (block.SequenceEqual(header.GetEOFBlock()))
                {
                    break;
                }

                header.PopulateFromBytes(block);

                int size = header.GetSize();
                File.Seek(size, SeekOrigin.Current);

                NumberOfFiles++;
            }

            return NumberOfFiles;
        }

        private static string TrimNullBytes(byte[] bytes)
        {
            int nullIndex = Array.IndexOf(bytes, (byte)0);
            return Encoding.UTF8.GetString(bytes, 0, nullIndex >= 0 ? nullIndex : bytes.Length);
        }

        public void Dispose()
        {
            File?.Dispose();
        }
    }
}
