using System;
using System.IO;
using System.Text;
using System.Linq;

namespace Wpress
{
    public class Constants
    {
        public const int HeaderSize = 4377;   // length of the header
        public const int FilenameSize = 255;  // maximum number of bytes allowed for filename
        public const int ContentSize = 14;    // maximum number of bytes allowed for content size
        public const int MtimeSize = 12;      // maximum number of bytes allowed for last modified date
        public const int PrefixSize = 4096;   // maximum number of bytes allowed for prefix
    }

    /// <summary>
    /// Header block format of a file
    /// Field Name    Offset    Length    Contents
    /// Name               0       255    filename (no path, no slash)
    /// Size             255        14    length of file contents
    /// Mtime            269        12    last modification date
    /// Prefix           281      4096    path name, no trailing slashes
    /// </summary>
    public class Header
    {
        public byte[]? Name { get; set; }
        public byte[]? Size { get; set; }
        public byte[]? Mtime { get; set; }
        public byte[]? Prefix { get; set; }

        public void PopulateFromBytes(byte[] block)
        {
            Name = [.. block.Take(Constants.FilenameSize)];
            Size = [.. block.Skip(Constants.FilenameSize).Take(Constants.ContentSize)];
            Mtime = [.. block.Skip(Constants.FilenameSize + Constants.ContentSize).Take(Constants.MtimeSize)];
            Prefix = [.. block.Skip(Constants.FilenameSize + Constants.ContentSize + Constants.MtimeSize).Take(Constants.PrefixSize)];
        }

        public int GetSize()
        {
            // remove any trailing zero bytes, convert to string, then convert to integer
            string sizeStr = Encoding.ASCII.GetString([.. Size!.TakeWhile(b => b != 0)]);
            return int.Parse(sizeStr);
        }

        public byte[] GetHeaderBlock()
        {
            return [.. Name!, .. Size!, .. Mtime!, .. Prefix!];
        }

        public byte[] GetEOFBlock()
        {
            // generate zero-byte sequence of length headerSize
            return new byte[Constants.HeaderSize];
        }

        public int PopulateFromFilename(string filename)
        {
            try
            {
                FileInfo fi = new FileInfo(filename);

                // validate if filename fits the allowed length
                if (fi.Name.Length > Constants.FilenameSize)
                {
                    throw new Exception("filename is longer than max allowed");
                }

                // create filename buffer and copy filename
                Name = new byte[Constants.FilenameSize];
                Array.Copy(Encoding.ASCII.GetBytes(fi.Name), Name, fi.Name.Length);

                // get filesize as string
                string size = fi.Length.ToString();
                if (size.Length > Constants.ContentSize)
                {
                    throw new Exception("file size is larger than max allowed");
                }

                // create size buffer and copy content size
                Size = new byte[Constants.ContentSize];
                Array.Copy(Encoding.ASCII.GetBytes(size), Size, size.Length);

                // get last modified date as string
                string unixTime = ((DateTimeOffset)fi.LastWriteTime).ToUnixTimeSeconds().ToString();
                if (unixTime.Length > Constants.MtimeSize)
                {
                    throw new Exception("last modified date is after than max allowed");
                }

                // create mtime buffer and copy
                Mtime = new byte[Constants.MtimeSize];
                Array.Copy(Encoding.ASCII.GetBytes(unixTime), Mtime, unixTime.Length);

                // get the path to the file
                string path = Path.GetDirectoryName(filename)!;
                if (path.Length > Constants.PrefixSize)
                {
                    throw new Exception("prefix size is longer than max allowed");
                }

                // create prefix buffer and copy
                Prefix = new byte[Constants.PrefixSize];
                if (!string.IsNullOrEmpty(path))
                {
                    Array.Copy(Encoding.ASCII.GetBytes(path), Prefix, path.Length);
                }

                return 0;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
