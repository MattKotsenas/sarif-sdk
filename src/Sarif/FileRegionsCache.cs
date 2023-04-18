﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;

namespace Microsoft.CodeAnalysis.Sarif
{
    /// <summary>
    /// This class is a file cache that can be used to populate
    /// regions with comprehensive data, to retrieve file text
    /// associated with a SARIF log, and to construct text
    /// snippets associated with region instances.
    /// </summary>
    public class FileRegionsCache
    {
        public const int DefaultCacheCapacity = 100;
        private readonly IFileSystem _fileSystem;

        internal readonly Cache<string, string> _fileTextCache;
        internal readonly Cache<string, HashData> _hashDataCache;
        internal readonly Cache<string, NewLineIndex> _newLineIndexCache;

        /// <summary>
        /// Creates a new <see cref="FileRegionsCache"/> object.
        /// </summary>
        /// <param name="capacity">
        /// The initial capacity of the cache.
        /// </param>
        /// <param name="fileSystem">
        /// An object that provides access to file system services.
        /// </param>
        public FileRegionsCache(int capacity = DefaultCacheCapacity, IFileSystem fileSystem = null)
        {
            _fileSystem = fileSystem ?? FileSystem.Instance;

            _fileTextCache = new Cache<string, string>(RetrieveTextForFile, capacity);
            _hashDataCache = new Cache<string, HashData>(BuildHashDataForFile, capacity);
            _newLineIndexCache = new Cache<string, NewLineIndex>(BuildIndexForFile, capacity);
        }

        /// <summary>
        /// Creates a <see cref="Region"/> object, based on an existing Region, in which all
        /// text-related properties have been populated.
        /// </summary>
        /// <remarks>
        /// For example, if the input Region specifies only the StartLine property, the returned
        /// Region instance will have computed and populated other text-related properties, such
        /// as properties, such as CharOffset, CharLength, etc.
        /// </remarks>
        /// <param name="inputRegion">
        /// Region object that forms the basis of the returned Region object.
        /// </param>
        /// <param name="uri">
        /// URI of the artifact in which <paramref name="inputRegion"/> lies, used to retrieve
        /// from the cache the location of each newline in the artifact.
        /// </param>
        /// <param name="populateSnippet">
        /// Boolean that indicates if the region's Snippet property will be populated.
        /// </param>
        /// <param name="fileText">
        /// An optional argument that, if present, contains the text contents of the file
        /// specified by <paramref name="uri"/>.
        /// </param>
        /// <returns>
        /// A Region object whose text-related properties have been fully populated.
        /// </returns>
        public virtual Region PopulateTextRegionProperties(
            Region inputRegion,
            Uri uri,
            bool populateSnippet,
            string fileText = null)
        {
            if (inputRegion == null || inputRegion.IsBinaryRegion)
            {
                // For binary regions, only the byteOffset and byteLength properties
                // are relevant, and their values are always specified.
                return inputRegion;
            }

            NewLineIndex newLineIndex = GetNewLineIndex(uri, fileText);

            return PopulateTextRegionProperties(
                newLineIndex,
                inputRegion,
                newLineIndex?.Text,
                populateSnippet);
        }

        /// <summary>
        /// Clear current cache.
        /// </summary>
        public void ClearCache()
        {
            _fileTextCache.Clear();
            _hashDataCache.Clear();
            _newLineIndexCache.Clear();
        }

        private static Region PopulateTextRegionProperties(NewLineIndex lineIndex, Region inputRegion, string fileText, bool populateSnippet)
        {
            // A GENERAL NOTE ON THE PROPERTY POPULATION PROCESS:
            //
            // As a rule, if we find some existing data on the region, we will trust it
            // and avoid overwriting it. We will take every opportunity, however, to
            // validate that the existing information matches what the new line index
            // computes. Note that we could consider making the new line index more
            // efficient by deferring its newline computations until they are
            // actually requested. If we do so, we could update this code to
            // avoid verifying region data in cases where regions are fully
            // populated (and we can skip file parsing required to build
            // the map of new line offsets).
            Assert(!inputRegion.IsBinaryRegion);

            // If we have no input source file, there is no work to do
            if (lineIndex == null) { return inputRegion; }

            Debug.Assert(fileText != null);

            Region region = inputRegion.DeepClone();

            if (region.StartLine == 0)
            {
                // This means we have a region specified entirely via charOffset
                PopulatePropertiesFromCharOffsetAndLength(lineIndex, region);
            }
            else
            {
                PopulatePropertiesFromStartAndEndProperties(lineIndex, region, fileText);
            }

            if (populateSnippet
                && region.CharOffset >= 0
                && region.CharLength >= 0
                && (region.CharOffset + region.CharLength <= fileText.Length))
            {
                region.Snippet ??= new ArtifactContent();

                string snippetText = fileText.Substring(region.CharOffset, region.CharLength);
                if (region.Snippet.Text == null)
                {
                    region.Snippet.Text = snippetText;
                }
                Assert(region.Snippet.Text == snippetText);
            }

            return region;
        }

        internal const int BIGSNIPPETLENGTH = 512;
        internal const int SMALLSNIPPETLENGTH = 128;

        public Region ConstructMultilineContextSnippet(Region inputRegion, Uri uri, string fileText = null)
        {
            if (inputRegion?.IsBinaryRegion != false)
            {
                // Context snippets are relevant only for textual regions.
                return null;
            }

            NewLineIndex newLineIndex = GetNewLineIndex(uri, fileText);
            if (newLineIndex == null)
            {
                return null;
            }

            fileText ??= newLineIndex.Text;

            // Generating full inputRegion to prevent issues.
            Region originalRegion = this.PopulateTextRegionProperties(inputRegion, uri, populateSnippet: true, fileText);

            if (originalRegion.CharLength >= BIGSNIPPETLENGTH)
            {
                return originalRegion.DeepClone();
            }

            int maxLineNumber = newLineIndex.MaximumLineNumber;

            var region = new Region
            {
                StartLine = inputRegion.StartLine == 1 ? 1 : inputRegion.StartLine - 1,
                EndLine = inputRegion.EndLine == maxLineNumber ? maxLineNumber : inputRegion.EndLine + 1
            };

            // Generating multilineRegion with one line before and after.
            Region multilineContextSnippet = this.PopulateTextRegionProperties(region, uri, populateSnippet: true, fileText);

            if (originalRegion.CharLength <= multilineContextSnippet.CharLength &&
                multilineContextSnippet.CharLength <= BIGSNIPPETLENGTH)
            {
                return multilineContextSnippet;
            }

            // We need this to re-calculate the region values when we call PopulateTextRegionProperties.
            region.StartColumn = 0;
            region.EndColumn = 0;
            region.StartLine = 0;
            region.EndLine = 0;
            region.CharOffset = Math.Max(0, originalRegion.CharOffset - SMALLSNIPPETLENGTH);

            region.CharLength = Math.Min(BIGSNIPPETLENGTH, fileText.Length - region.CharOffset);

            // Generating multiline region with 128 characters to the left and right from the
            // originalRegion if possible.
            multilineContextSnippet = this.PopulateTextRegionProperties(region, uri, populateSnippet: true, fileText);

            // We can't generate a contextRegion which is smaller than the original region.
            Debug.Assert(originalRegion.CharLength <= multilineContextSnippet.CharLength);
            return multilineContextSnippet;
        }

        private static void PopulatePropertiesFromCharOffsetAndLength(NewLineIndex newLineIndex, Region region)
        {
            Assert(!region.IsBinaryRegion);
            Assert(region.StartLine == 0);
            Assert(region.CharLength >= 0 || region.CharOffset >= 0);

            int startLine, startColumn, endLine, endColumn;

            // Retrieve start and end line and column information from the new line index
            OffsetInfo offsetInfo = newLineIndex.GetOffsetInfoForOffset(region.CharOffset);
            startLine = offsetInfo.LineNumber;
            startColumn = offsetInfo.ColumnNumber;

            offsetInfo = newLineIndex.GetOffsetInfoForOffset(region.CharOffset + region.CharLength);
            endLine = offsetInfo.LineNumber;

            // The computation above points one past our actual region, because endColumn
            // is exclusive of the region. This allows for length to easily be computed
            // for single line regions: region.EndColumn - region.StartColumn
            endColumn = offsetInfo.ColumnNumber;

            // Only set values if they aren't already specified
            if (region.StartLine == 0) { region.StartLine = startLine; }
            if (region.StartColumn == 0) { region.StartColumn = startColumn; }
            if (region.EndLine == 0) { region.EndLine = endLine; }
            if (region.EndColumn == 0) { region.EndColumn = endColumn; }

            // Validate cases where new line index disagrees with explicit values
            Assert(region.StartLine == startLine);
            Assert(region.StartColumn == startColumn);
            Assert(region.EndLine == endLine);
            Assert(region.EndColumn == endColumn);
        }

        private static void PopulatePropertiesFromStartAndEndProperties(NewLineIndex lineIndex, Region region, string fileText)
        {
            Assert(region.StartLine > 0);

            // Note: execution order of these helpers is important, as some
            // calls assume that certain preceding helpers have executed,
            // with the result that certain properties are populated

            // Populated at this point: StartLine
            PopulateEndLine(region);

            // Populated at this point: StartLine, EndLine
            PopulateStartColumn(region);

            // Populated at this point: StartLine, EndLine, StartColumn
            PopulateEndColumn(lineIndex, region, fileText);

            // Populated at this point: StartLine, EndLine, StartColumn, EndColumn
            PopulateCharOffset(lineIndex, region);

            // Populated at this point: StartLine, EndLine, StartColumn, EndColumn, CharOffset
            PopulateCharLength(lineIndex, region);

            // Populated at this point: StartLine, EndLine, StartColumn, EndColumn, CharOffset, CharLength
            Assert(region.StartLine > 0);
            Assert(region.EndLine > 0);
            Assert((region.CharOffset + region.CharLength) <= fileText.Length);
            Assert(region.StartColumn > 0);
            Assert(region.CharLength > 0 || (region.StartColumn == region.EndColumn && region.StartLine == region.EndLine));
            Assert(region.EndColumn > 0);
        }

        private static void PopulateEndLine(Region region)
        {
            // Populated at this point: StartLine
            Assert(region.StartLine > 0);

            region.EndLine = region.EndLine == 0 ? region.StartLine : region.EndLine;
        }

        private static void PopulateStartColumn(Region region)
        {
            // Populated at this point: StartLine, EndLine
            Assert(region.StartLine > 0);
            Assert(region.EndLine > 0);

            region.StartColumn = region.StartColumn == 0 ? 1 : region.StartColumn;
        }

        private static void PopulateEndColumn(NewLineIndex lineIndex, Region region, string fileText)
        {
            // Populated at this point: StartLine, EndLine, StartColumn
            Assert(region.StartLine > 0);
            Assert(region.StartColumn > 0);
            Assert(region.EndLine > 0);

            if (region.EndColumn == 0)
            {
                // No explicit end column. Increment from end line through
                // the end of the line, excluding new line characters
                LineInfo lineInfo = lineIndex.GetLineInfoForLine(region.EndLine);
                int endColumnOffset = lineInfo.StartOffset;

                while (endColumnOffset < fileText.Length &&
                       !NewLineIndex.s_newLineCharSet.Contains(fileText[endColumnOffset]))
                {
                    endColumnOffset++;
                }

                // End columns are 1-indexed
                region.EndColumn = endColumnOffset - lineInfo.StartOffset + 1;
            }
        }

        private static void PopulateCharOffset(NewLineIndex lineIndex, Region region)
        {
            // Populated at this point: StartLine, EndLine, StartColumn, EndColumn
            Assert(region.StartLine > 0);
            Assert(region.EndLine > 0);
            Assert(region.StartColumn > 0);
            Assert(region.EndColumn > 0);

            LineInfo lineInfo = lineIndex.GetLineInfoForLine(region.StartLine);

            // Now we have the offset of the starting line. Populate region.CharOffset.
            int offset = lineInfo.StartOffset;
            offset += region.StartColumn - 1;

            if (region.CharOffset == 0 || region.CharOffset == -1)
            {
                region.CharOffset = offset;
            }

            Assert(region.CharOffset == offset);
        }

        private static void PopulateCharLength(NewLineIndex lineIndex, Region region)
        {
            // Populated at this point: StartLine, EndLine, StartColumn, EndColumn, CharOffset
            Assert(region.StartLine > 0);
            Assert(region.EndLine > 0);
            Assert(region.StartColumn > 0);
            Assert(region.EndColumn > 0);
            Assert(region.CharOffset > 0 || (region.StartLine == 1 && region.StartColumn == 1));

            LineInfo lineInfo = lineIndex.GetLineInfoForLine(region.EndLine);
            int charLength = lineInfo.StartOffset;
            charLength -= region.CharOffset;
            charLength += region.EndColumn - 1;

            if (region.CharLength == 0)
            {
                region.CharLength = charLength;
            }
            Assert(region.CharLength == charLength);
        }

        public HashData GetHashData(Uri uri, string fileText = null)
        {
            string path = uri.GetFilePath();
            if (fileText != null)
            {
                _fileTextCache[path] = fileText;
            }

            fileText = _fileTextCache[path];

            return HashUtilities.ComputeHashesForText(fileText);
        }

        public NewLineIndex GetNewLineIndex(Uri uri, string fileText = null)
        {
            string path = uri.GetFilePath();
            if (fileText != null)
            {
                _fileTextCache[path] = fileText;
            }
            fileText = _fileTextCache[path];

            NewLineIndex newLineIndex;
            if (!_newLineIndexCache.ContainsKey(path) && fileText != null)
            {
                newLineIndex = new NewLineIndex(fileText);

                _newLineIndexCache[path] = newLineIndex;
            }
            else
            {
                newLineIndex = _newLineIndexCache[path];
            }

            return newLineIndex;
        }

        private string RetrieveTextForFile(string path)
        {
            string fileText = null;

            // We will expand this code later to construct all possible URLs from
            // the log file, bearing in mind things like uriBaseIds. Also, we could
            // consider downloading and caching web-hosted source files.
            try
            {
                if (_fileSystem.FileExists(path))
                {
                    fileText = _fileSystem.FileReadAllText(path);
                }
            }
            catch (IOException) { }
            catch (SecurityException) { }
            catch (UnauthorizedAccessException) { }

            return fileText;
        }

        private HashData BuildHashDataForFile(string path)
        {
            string fileText = _fileTextCache[path];
            return HashUtilities.ComputeHashesForText(fileText);
        }

        /// <summary>
        ///  Method to build cache entries which aren't already in the cache.
        /// </summary>
        /// <param name="path">Uri.LocalPath for the file to load</param>
        /// <returns>Cache entry to add to cache with file contents and NewLineIndex</returns>
        private NewLineIndex BuildIndexForFile(string path)
        {
            string fileText = _fileTextCache[path];
            return fileText != null ? new NewLineIndex(fileText) : null;
        }

        private static void Assert(bool _)
        {
            // Placeholder to report issues in a situationally appropriate way.
            //  We don't want Multitool rewrite to blow up.
            //  We don't want unit tests for invalid Regions to block on asserts; we want to verify the code leaves those results alone.
            //  We may want console or output log output when invalid Regions are detected during Multitool rewrite use.
            // https://github.com/microsoft/sarif-sdk/issues/1784

            //Debug.Assert(condition);
        }

        internal void SetTextForFile(Uri uri, string contents)
        {
            string path = uri.GetFilePath();
            _fileTextCache[path] = contents;
        }
    }
}
