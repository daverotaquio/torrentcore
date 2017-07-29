﻿// This file is part of TorrentCore.
//     https://torrentcore.org
// Copyright (c) 2016 Sam Fisher.
// 
// TorrentCore is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as
// published by the Free Software Foundation, version 3.
// 
// TorrentCore is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with TorrentCore.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TorrentCore.Data
{
    /// <summary>
    /// Provides access to a collection of files as a single block of data.
    /// </summary>
    class BlockDataHandler : IBlockDataHandler
    {
        /// <summary>
        /// Creates a new block data handler with the specified file handler as the data source.
        /// </summary>
        /// <param name="fileHandler">File handler to use as the data source.</param>
        /// <param name="metainfo">Metainfo file description.</param>
        public BlockDataHandler(IFileHandler fileHandler, Metainfo metainfo)
        {
            FileHandler = fileHandler;
            Metainfo = metainfo;
        }

        /// <summary>
        /// Gets the file handler used internally for data access.
        /// </summary>
        public IFileHandler FileHandler { get; }

        /// <summary>
        /// Gets the metainfo for this piece checker.
        /// </summary>
        public Metainfo Metainfo { get; }

        /// <summary>
        /// Returns a copy of the contiguous file data starting at the specified offset.
        /// </summary>
        /// <param name="offset">Offset to read from.</param>
        /// <param name="length">Number of bytes to read.</param>
        /// <returns>Block data from specified region.</returns>
        public byte[] ReadBlockData(long offset, long length)
        {
            byte[] data;
            TryReadBlockData(offset, length, out data);
            return data;
        }

        /// <summary>
        /// Returns a copy of the contiguous file data starting at the specified offset.
        /// </summary>
        /// <param name="offset">Offset to read from.</param>
        /// <param name="length">Number of bytes to read.</param>
        /// <returns>Block data from specified region.</returns>
        public bool TryReadBlockData(long offset, long length, out byte[] data)
        {
            // Open the first file for reading
            long remainder;
            int currentFile = Metainfo.FileIndex(offset, out remainder);
            Stream fileStream = FileHandler.GetFileStream(Metainfo.Files[currentFile].Name);
            fileStream.Seek(remainder, SeekOrigin.Begin);

            // Fill the current piece with file data
            int copied = 0;
            data = new byte[length];
            while (copied < length)
            {
                // Move to next file if necessary
                if (fileStream.Position == Metainfo.Files[currentFile].Size)
                {
                    currentFile++;
                    fileStream = FileHandler.GetFileStream(Metainfo.Files[currentFile].Name);
                    fileStream.Seek(0, SeekOrigin.Begin);
                }

                // Copy to end of file, or end of piece
                int toRead = (int)Math.Min(length, Metainfo.Files[currentFile].Size - fileStream.Position);

                // Check if going beyond end of file
                if (fileStream.Length - fileStream.Position < toRead)
                {
                    data = null;
                    return false;
                }

                fileStream.Read(data, copied, toRead);
                copied += toRead;
            }

            return true;
        }

        /// <summary>
        /// Writes the specified contiguous data from the given offset position.
        /// </summary>
        /// <param name="offset">Offset at which to start writing.</param>
        /// <param name="data">Block data to write.</param>
        public void WriteBlockData(long offset, byte[] data)
        {
            long remainder;
            int fileIndex = Metainfo.FileIndex(offset, out remainder);
            Stream fileStream = FileHandler.GetFileStream(Metainfo.Files[fileIndex].Name);
            if (fileStream.Length < remainder)
            {
                fileStream.Seek(fileStream.Length, SeekOrigin.Begin);
                long extra = remainder - fileStream.Length;
                byte[] padding = new byte[extra];
                fileStream.Write(padding, 0, padding.Length);
            }
            fileStream.Seek(remainder, SeekOrigin.Begin);

            long written = 0;
            while (written < data.Length) // Change to LongLength: dotnet/corefx#9998
            {
                // Move to next file if necessary
                if (fileStream.Position == Metainfo.Files[fileIndex].Size)
                {
                    fileIndex++;
                    fileStream = FileHandler.GetFileStream(Metainfo.Files[fileIndex].Name);
                }

                // Write to end of file, or end of data
                // Change to LongLength: dotnet/corefx#9998
                int toWrite = (int)Math.Min(data.Length - written, Metainfo.Files[fileIndex].Size - fileStream.Position);
                fileStream.Write(data, (int)written, toWrite);
                written += toWrite;
            }
        }
    }
}