﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

using FluentAssertions;

using Microsoft.CodeAnalysis.Sarif;

using Xunit;

namespace Test.UnitTests.Sarif
{
    public class ArtifactProviderTests
    {

        [Fact]
        public void MultithreadedZipArchiveArtifactProvider_RetrieveSizeInBytesBeforeRetrievingContents()
        {
            string entryContents = $"{Guid.NewGuid}";
            ZipArchive zip = CreateZipArchiveWithTextContents("test.txt", entryContents);
            var artifactProvider = new MultithreadedZipArchiveArtifactProvider(zip, FileSystem.Instance);

            ValidateTextContents(artifactProvider.Artifacts, entryContents);
        }

        [Fact]
        public void MultithreadedZipArchiveArtifactProvider_RetrieveSizeInBytesBeforeRetrievingBytes()
        {
            string filePath = this.GetType().Assembly.Location;
            using FileStream reader = File.OpenRead(filePath);

            int headerSize = 1024;
            byte[] data = new byte[headerSize];
            reader.Read(data, 0, data.Length);

            // Note that even thought we populate an archive with binary contents, the extension
            // of the archive entry indicates a text file. We still expect binary data on expansion.
            ZipArchive zip = CreateZipArchiveWithBinaryContents("test.txt", data);
            var artifactProvider = new MultithreadedZipArchiveArtifactProvider(zip, FileSystem.Instance);

            ValidateBinaryContents(artifactProvider.Artifacts, data);
        }

        [Fact]
        public void MultithreadedZipArchiveArtifactProvider_RetrieveSizeInBytesAfterRetrievingContents()
        {
            string entryContents = $"{Guid.NewGuid()}";
            ZipArchive zip = CreateZipArchiveWithTextContents("test.txt", entryContents);
            var artifactProvider = new MultithreadedZipArchiveArtifactProvider(zip, FileSystem.Instance);

            ValidateTextContents(artifactProvider.Artifacts, entryContents);
        }

        [Fact]
        public void MultithreadedZipArchiveArtifactProvider_RetrieveSizeInBytesAfterRetrievingBytes()
        {
            string filePath = this.GetType().Assembly.Location;
            using FileStream reader = File.OpenRead(filePath);

            int headerSize = 1024;
            byte[] data = new byte[headerSize];
            reader.Read(data, 0, data.Length);

            ZipArchive zip = CreateZipArchiveWithBinaryContents("test.dll", data);
            var artifactProvider = new MultithreadedZipArchiveArtifactProvider(zip, FileSystem.Instance);
            foreach (IEnumeratedArtifact artifact in artifactProvider.Artifacts)
            {
                artifact.Bytes.Should().NotBeNull();
                artifact.Bytes.Length.Should().Be(headerSize);
                artifact.SizeInBytes.Should().Be(headerSize);

                artifact.Contents.Should().BeNull();
            }
        }

        [Fact]
        public void MultithreadedZipArchiveArtifactProvider_SizeInBytesAndContentsAreAvailable()
        {
            string entryContents = $"{Guid.NewGuid()}";
            ZipArchive zip = CreateZipArchiveWithTextContents("test.csv", entryContents);
            var artifactProvider = new MultithreadedZipArchiveArtifactProvider(zip, FileSystem.Instance);

            ValidateTextContents(artifactProvider.Artifacts, entryContents);
        }

        [Fact]
        public void MultithreadedZipArchiveArtifact_IllegalFileAndPathCharacters()
        {
            string text = $"{Guid.NewGuid()}";
            byte[] contents = Encoding.UTF8.GetBytes(text);
            string filePath = $"{Path.GetInvalidPathChars()[0]}{Path.GetInvalidFileNameChars()[1]}MyZippedFile.txt";
            ZipArchive zip = EnumeratedArtifactTests.CreateZipArchive(filePath, contents);

            foreach (IEnumeratedArtifact entry in new MultithreadedZipArchiveArtifactProvider(zip, new FileSystem()).Artifacts)
            {
                entry.IsBinary.Should().BeFalse();
                entry.Contents.Should().BeEquivalentTo(text);
            }
        }

        private void ValidateTextContents(IEnumerable<IEnumeratedArtifact> artifacts, string entryContents)
        {
            artifacts.Count().Should().Be(1);
            IEnumeratedArtifact artifact = artifacts.First();
            artifact.Contents.Should().Be(entryContents);
            artifact.SizeInBytes.Should().Be(entryContents.Length);
            artifact.Bytes.Should().BeNull();
        }

        private void ValidateBinaryContents(IEnumerable<IEnumeratedArtifact> artifacts, byte[] bytes)
        {
            artifacts.Count().Should().Be(1);
            IEnumeratedArtifact artifact = artifacts.First();
            artifact.Bytes.Should().BeEquivalentTo(bytes);
            artifact.SizeInBytes.Should().Be(bytes.Length);
            artifact.Contents.Should().BeNull();
        }

        private static ZipArchive CreateZipArchiveWithTextContents(string fileName, string contents)
        {
            var stream = new MemoryStream();
            using (var populateArchive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
            {
                ZipArchiveEntry entry = populateArchive.CreateEntry(fileName, CompressionLevel.NoCompression);
                using (var errorWriter = new StreamWriter(entry.Open()))
                {
                    errorWriter.Write(contents);
                }
            }
            stream.Flush();
            stream.Position = 0;

            return new ZipArchive(stream, ZipArchiveMode.Read);
        }

        private static ZipArchive CreateZipArchiveWithBinaryContents(string fileName, byte[] bytes)
        {
            var stream = new MemoryStream();
            using (var populateArchive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
            {
                ZipArchiveEntry entry = populateArchive.CreateEntry(fileName, CompressionLevel.NoCompression);
                entry.Open().Write(bytes);
            }
            stream.Flush();
            stream.Position = 0;

            return new ZipArchive(stream, ZipArchiveMode.Read);
        }
    }
}
