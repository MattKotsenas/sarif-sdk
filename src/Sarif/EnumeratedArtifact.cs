﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.CodeAnalysis.Sarif
{
    // TBD: this class should probably be a generic, with
    // EnumeratedArtifact<string> being a commonly utilized thing.
    public class EnumeratedArtifact : IEnumeratedArtifact
    {
        private const int BinarySniffingHeaderSizeBytes = 1024;

        public EnumeratedArtifact(IFileSystem fileSystem)
        {
            FileSystem = fileSystem;
        }

        internal byte[] bytes;
        internal string contents;
        internal string cleanPath = string.Empty;

        private Encoding encoding;

        public Uri Uri { get; set; }

        public bool IsBinary
        {
            get
            {
                GetArtifactData();
                return this.contents == null;
            }
        }

        public Stream Stream { get; set; }

        public Encoding Encoding
        {
            get
            {
                if (encoding == null)
                {
                    GetArtifactData();
                }
                return this.encoding;
            }

            set => this.encoding = value;
        }

        internal IFileSystem FileSystem { get; set; }

        public string Contents
        {
            get => GetArtifactData().text;
            set => this.contents = value;
        }

        public byte[] Bytes
        {
            get => GetArtifactData().bytes;
            set => this.bytes = value;
        }

        public string CleanPath
        {
            get => GetCleanFilePath(Uri.OriginalString);
            set => this.cleanPath = value;
        }

        private readonly static Regex AbsoluteFilePathWithFileSchemeRegex = new Regex(@"file:(?:\/{3}|\/{2}\w+\/|\/{1})?(.+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private string GetCleanFilePath(string originalString)
        {
            if (this.cleanPath != string.Empty)
            {
                return this.cleanPath;
            }

            if (originalString.StartsWith("file:"))
            {
                Match match = AbsoluteFilePathWithFileSchemeRegex.Match(originalString);
                if (match.Success)
                {
                    string separator = Path.DirectorySeparatorChar.ToString();
                    this.cleanPath = match.Groups[1].Value.Replace("/", separator);
                    return this.cleanPath;
                }

                throw new IOException("Invalid file path format.");
            }
            else
            {
                return originalString;
            }
        }

        private (string text, byte[] bytes) GetArtifactData()
        {
            if (this.contents != null)
            {
                return (this.contents, bytes: null);
            }

            if (this.bytes != null)
            {
                return (text: null, this.bytes);
            }

            if (Stream == null && this.contents == null && this.bytes == null)
            {
                if (Uri == null ||
                    !Uri.IsAbsoluteUri ||
                    (Uri.IsAbsoluteUri && !Uri.IsFile))
                {
                    throw new InvalidOperationException("An absolute URI pointing to a file location was not available.");
                }

                // This is our client-side, disk-based file retrieval case.
                this.Stream = FileSystem.FileOpenRead(CleanPath);
            }

            RetrieveDataFromStream();

            this.Stream = null;

            return (this.contents, this.bytes);
        }

        private void RetrieveDataFromStream()
        {
            if (!this.Stream.CanSeek)
            {
                this.Stream = new PeekableStream(this.Stream, BinarySniffingHeaderSizeBytes);
            }

            byte[] header = new byte[BinarySniffingHeaderSizeBytes];
            int readLength = this.Stream.Read(header, 0, header.Length);

            bool isText = FileEncoding.IsTextualData(header, 0, readLength);

            TryRewindStream();

            if (isText)
            {
                using var contentReader = new StreamReader(Stream);
                this.contents = contentReader.ReadToEnd();
            }
            else
            {
                this.bytes = new byte[Stream.Length];
                var memoryStream = new MemoryStream(this.bytes);
                this.Stream.CopyTo(memoryStream);
            }
        }

        private void TryRewindStream()
        {
            if (this.Stream.CanSeek)
            {
                this.Stream.Seek(0, SeekOrigin.Begin);
            }
            else
            {
                var peekable = this.Stream as PeekableStream;
                if (peekable != null)
                {
                    peekable.Rewind();
                }
            }
        }

        public long? sizeInBytes;

        public long? SizeInBytes
        {
            get
            {
                if (this.sizeInBytes != null)
                {
                    return this.sizeInBytes.Value;
                };

                if (this.contents != null)
                {
                    this.sizeInBytes = (long)this.contents.Length;
                }
                else if (this.bytes != null)
                {
                    this.sizeInBytes = (int)this.bytes.Length;
                }
                else if (this.Stream != null)
                {
                    this.SizeInBytes = (long)this.Stream.Length;
                }
                else if (Uri != null && Uri.IsAbsoluteUri && Uri.IsFile)
                {
                    this.sizeInBytes = (long)FileSystem.FileInfoLength(CleanPath);
                }
                else if (this.Contents != null)
                {
                    this.SizeInBytes = (long)this.Contents.Length;
                }

                return this.sizeInBytes;
            }
            set
            {
                this.sizeInBytes = value;
            }
        }
    }
}
