﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Microsoft.CodeAnalysis.Sarif
{

    public class ZipArchiveArtifact : IEnumeratedArtifact
    {
        private readonly ISet<string> binaryExtensions;
        private readonly ZipArchive archive;
        private ZipArchiveEntry entry;
        private readonly Uri uri;
        private string contents;
        private byte[] bytes;
        private Stream stream;

        public ZipArchiveArtifact(ZipArchive archive, ZipArchiveEntry entry, ISet<string> binaryExtensions = null)
        {
            this.entry = entry ?? throw new ArgumentNullException(nameof(entry));
            this.archive = archive ?? throw new ArgumentNullException(nameof(archive));

            this.binaryExtensions = binaryExtensions ?? new HashSet<string>();
            this.uri = new Uri(entry.FullName, UriKind.RelativeOrAbsolute);
        }

        public Uri Uri => this.uri;

        public bool IsBinary
        {
            get
            {
                string extension = Path.GetExtension(Uri.ToString());
                return this.binaryExtensions.Contains(extension);
            }
        }

        public Stream Stream
        {
            get
            {
                if (this.entry == null)
                {
                    return null;
                }

                lock (this.archive)
                {
                    this.stream = entry.Open();
                }

                return this.Stream;
            }

            set
            {
                this.Stream = value;
            }
        }

        /// <summary>
        /// Raises NotImplementedException as we can't retrieve or set encoding
        /// currently. Assessing this data requires decompressing the archive
        /// stream. Currently, our encoding detection isn't highly developed.
        /// In the future, we should consider eliminating the Encoding property
        /// entirely from IEnumeratedArtifact or do the work of handling the
        /// range of text encodings.

        /// </summary>
        public Encoding Encoding
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

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

        private (string text, byte[] bytes) GetArtifactData()
        {
            if (this.contents == null && this.bytes == null)
            {
                lock (this.archive)
                {
                    if (this.contents == null && this.bytes == null)
                    {
                        EnumeratedArtifact.RetrieveDataFromStream(this);
                    }

                    this.entry = null;
                }
            }

            return (this.contents, this.bytes);
        }

        public long? SizeInBytes
        {
            get
            {
                if (this.contents != null)
                {
                    return this.contents.Length;
                }

                if (this.bytes != null)
                {
                    return this.bytes.Length;
                }

                lock (this.archive)
                {
                    if (this.entry != null)
                    {
                        return this.entry.Length;
                    }
                }

                return null;
            }

            set => throw new NotImplementedException();
        }
    }
}
