﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;

namespace Microsoft.CodeAnalysis.Sarif
{
    // TBD: this class should probably be a generic, with
    // EnumeratedArtifact<string> being a commonly utilized thing.
    public class EnumeratedArtifact : IEnumeratedArtifact
    {
        public EnumeratedArtifact(IFileSystem fileSystem)
        {
            FileSystem = fileSystem;
        }

        internal byte[] bytes;
        internal string contents;

        public Uri Uri { get; set; }

        public Stream Stream { get; set; }

        public Encoding Encoding { get; set; }

        internal IFileSystem FileSystem { get; set; }

        public bool IsBinary { get; set; }

        public string Contents
        {
            get => !IsBinary? GetContents() : throw new InvalidOperationException();
            set => this.contents = value;
        }

        public byte[] Bytes
        {
            get => IsBinary ? GetBytes() : throw new InvalidOperationException();
            set => this.bytes = value;
        }

        private string GetContents()
        {
            if (this.contents != null) { return this.contents; }

            if (Stream == null && this.contents == null)
            {
                // TBD we actually have no validation URI is non-null yet.
                contents = Uri!.IsFile
                    ? FileSystem.FileReadAllText(Uri.LocalPath)
                    : null;

                this.sizeInBytes = (long?)this.contents?.Length;
            }
            else
            {
                if (Stream.CanSeek) { this.Stream.Seek(0, SeekOrigin.Begin); }
                using var contentReader = new StreamReader(Stream);
                this.contents = contentReader.ReadToEnd();
                Stream.Close();
                Stream = null;
            }

            return this.contents;
        }

        private byte[] GetBytes()
        {
            if (this.bytes != null) { return this.bytes; }

            if (Stream == null && this.bytes == null)
            {
                bytes = Uri!.IsFile
                    ? FileSystem.FileReadAllBytes(Uri.LocalPath)
                    : null;

                this.sizeInBytes = (long?)this.bytes?.Length;
            }
            else
            {
                if (Stream.CanSeek) { this.Stream.Seek(0, SeekOrigin.Begin); }
                this.bytes = new byte[Stream.Length];
                this.sizeInBytes = Stream.Read(this.bytes, 0, this.bytes.Length);
                Stream.Close();
                Stream = null;
            }

            return this.bytes;
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
                else if (this.Stream != null)
                {
                    this.SizeInBytes = (long)this.Stream.Length;
                }
                else if (Uri!.IsAbsoluteUri && Uri!.IsFile)
                {
                    this.sizeInBytes = (long)FileSystem.FileInfoLength(Uri.LocalPath);
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
