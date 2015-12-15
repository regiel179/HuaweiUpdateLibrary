﻿using System;
using System.IO;
using System.Runtime.InteropServices;
using HuaweiUpdateLibrary.Streams;

namespace HuaweiUpdateLibrary.Core
{
    public class UpdateEntry
    {
        private const UInt32 FileMagic = 0xA55AAA55;
        
        private FileHeader _fileHeader;
        private readonly long _dataOffset;
        private readonly ushort[] _checkSumTable;

        private UpdateEntry(Stream stream, bool checksum = true)
        {
            var reader = new BinaryReader(stream);

            // Read header
            if (!Utilities.ByteToType(reader, out _fileHeader))
                throw new Exception("ByteToType() failed @" + reader.BaseStream.Position);

            // Check header magic
            if (_fileHeader.HeaderId != FileMagic)
                throw new Exception("Invalid file.");

            // Validate checksum
            if (checksum)
            {

            }

            // Calculate checksum table size
            var checksumTableSize = _fileHeader.HeaderSize - Marshal.SizeOf(typeof(FileHeader));

            // Allocate checksum table
            _checkSumTable = new ushort[checksumTableSize / sizeof(ushort)];

            // Read checksum table
            for (var count = 0; count < _checkSumTable.Length; count++) { _checkSumTable[count] = reader.ReadUInt16(); }

            // Save position of file data
            _dataOffset = stream.Position;
        }
        
        /// <summary>
        /// Header Id
        /// </summary>
        public UInt32 HeaderId
        {
            get { return _fileHeader.HeaderId; }
        }

        /// <summary>
        /// Header size
        /// </summary>
        public UInt32 HeaderSize
        {
            get { return _fileHeader.HeaderSize; }
        }

        /// <summary>
        /// Hardware id
        /// </summary>
        public string HardwareId
        {
            get { return new string(_fileHeader.HardwareId);}
        }

        /// <summary>
        /// File sequence
        /// </summary>
        public UInt32 FileSequence
        {
            get { return _fileHeader.FileSequence; }
        }

        /// <summary>
        /// File size
        /// </summary>
        public UInt32 FileSize
        {
            get { return _fileHeader.FileSize; }
        }

        /// <summary>
        /// File date
        /// </summary>
        public string FileDate
        {
            get { return _fileHeader.FileDate; }
        }

        /// <summary>
        /// File time
        /// </summary>
        public string FileTime
        {
            get { return _fileHeader.FileTime; }
        }

        /// <summary>
        /// File type
        /// </summary>
        public string FileType
        {
            get { return _fileHeader.FileType; }
        }

        /// <summary>
        /// Header checksum
        /// </summary>
        public UInt16 HeaderChecksum
        {
            get { return _fileHeader.HeaderChecksum; }
        }

        /// <summary>
        /// Block size
        /// </summary>
        public UInt16 BlockSize
        {
            get { return _fileHeader.BlockSize; }
        }

        /// <summary>
        /// Read an <see cref="UpdateEntry"/> from a given <see cref="Stream"/>
        /// </summary>
        /// <param name="stream"><see cref="Stream"/> to read from</param>
        /// <param name="checksum">Verify header checksum</param>
        /// <returns><see cref="UpdateEntry"/></returns>
        public static UpdateEntry Read(Stream stream, bool checksum = true)
        {
            return new UpdateEntry(stream, checksum);
        }
        
        /// <summary>
        /// Get a <see cref="Stream"/> to the file data in the given <see cref="Stream"/>
        /// </summary>
        /// <param name="stream"><see cref="Stream"/> to read from</param>
        /// <returns><see cref="Stream"/></returns>
        public Stream GetDataStream(Stream stream)
        {
            // Seek to offset
            stream.Seek(_dataOffset, SeekOrigin.Begin);

            // Return stream
            return new PartialStream(stream, FileSize);
        }

        /// <summary>
        /// Get a <see cref="Stream"/> to the file data in the given file
        /// </summary>
        /// <param name="fileName">File to read from</param>
        /// <returns><see cref="Stream"/></returns>
        public Stream GetDataStream(string fileName)
        {
            var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);

            // Seek to offset
            stream.Seek(_dataOffset, SeekOrigin.Begin);

            // Return stream
            return new PartialStream(stream, FileSize);
        }

        /// <summary>
        /// Extract the current <see cref="UpdateEntry"/> from an input <see cref="Stream"/> to an output <see cref="Stream"/>
        /// </summary>
        /// <param name="input">Input <see cref="Stream"/></param>
        /// <param name="output">Output <see cref="Stream"/></param>
        /// <param name="checksum">Verify file data checksum</param>
        public void Extract(Stream input, Stream output, bool checksum = true)
        {
            // Get stream to file data
            var reader = GetDataStream(input);
            var buffer = new byte[BlockSize];
            var blockNumber = 0;
            int size;

            // Read file data
            while ((size = reader.Read(buffer, 0, BlockSize)) > 0)
            {
                // Verify crc
                if (checksum)
                {
                    var crc = _checkSumTable[blockNumber];
                }

                // Write to output file
                output.Write(buffer, 0, size);

                // Increase block
                blockNumber++;
            }
        }

        /// <summary>
        /// Extract the current <see cref="UpdateEntry"/> from an input <see cref="Stream"/> to an output file
        /// </summary>
        /// <param name="input">Input <see cref="Stream"/></param>
        /// <param name="output">Output file</param>
        /// <param name="checksum">Verify file data checksum</param>
        public void Extract(Stream input, string output, bool checksum = true)
        {
            using (var outputStream = new FileStream(output, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                // Extract
                Extract(input, outputStream, checksum);
            }
        }

        /// <summary>
        /// Extract the current <see cref="UpdateEntry"/> from an input file to an output <see cref="Stream"/>
        /// </summary>
        /// <param name="input">Input file</param>
        /// <param name="output">Output <see cref="Stream"/></param>
        /// <param name="checksum">Verify file data checksum</param>
        public void Extract(string input, Stream output, bool checksum = true)
        {
            using (var inputStream = new FileStream(input, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                // Extract
                Extract(inputStream, output, checksum);
            }
        }

        /// <summary>
        /// Extract the current <see cref="UpdateEntry"/> from an input file to an output file
        /// </summary>
        /// <param name="input">Input file</param>
        /// <param name="output">Output file</param>
        /// <param name="checksum">Verify file data checksum</param>
        public void Extract(string input, string output, bool checksum = true)
        {
            using (var inputStream = new FileStream(input, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var outputStream = new FileStream(output, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    // Extract
                    Extract(inputStream, outputStream, checksum);
                }
            }
        }
    }
}
