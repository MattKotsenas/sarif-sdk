﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using FluentAssertions;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.CodeAnalysis.Sarif.Comparers;
using Microsoft.CodeAnalysis.Test.Utilities.Sarif;

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Test.UnitTests.Sarif.Comparers
{
    public class ComparersTests
    {
        private readonly Random random;

        public ComparersTests(ITestOutputHelper outputHelper)
        {
            this.random = RandomSarifLogGenerator.GenerateRandomAndLog(outputHelper);
        }

        [Fact]
        public void CompareList__Shuffle_Tests()
        {
            IList<int> originalList = Enumerable.Range(-100, 200).ToList();

            IList<int> shuffledList = originalList.ToList().Shuffle(this.random);

            int result = 0, newResult = 0;

            result = ComparerHelper.CompareList(originalList, shuffledList);
            result.Should().NotBe(0);

            newResult = ComparerHelper.CompareList(shuffledList, originalList);
            newResult.Should().Be(result * -1);

            IList<int> sortedList = shuffledList.OrderBy(i => i).ToList();

            result = ComparerHelper.CompareList(originalList, sortedList);
            result.Should().Be(0);

            newResult = ComparerHelper.CompareList(originalList, sortedList);
            newResult.Should().Be(0);
        }

        [Fact]
        public void CompareList_BothNull_Tests()
        {
            IList<int> list1 = null;
            IList<int> list2 = null;

            ComparerHelper.CompareList(list1, list2).Should().Be(0);
            ComparerHelper.CompareList(list2, list1).Should().Be(0);
        }

        [Fact]
        public void CompareList_CompareNullToNotNull_Tests()
        {
            IList<int> list1 = null;
            IList<int> list2 = Enumerable.Range(-10, 20).ToList();

            ComparerHelper.CompareList(list1, list2).Should().Be(-1);
            ComparerHelper.CompareList(list2, list1).Should().Be(1);
        }

        [Fact]
        public void CompareList_DifferentCount_Tests()
        {
            IList<int> list1 = Enumerable.Range(0, 11).ToList();
            IList<int> list2 = Enumerable.Range(0, 10).ToList();

            ComparerHelper.CompareList(list1, list2).Should().Be(1);
            ComparerHelper.CompareList(list2, list1).Should().Be(-1);
        }

        [Fact]
        public void CompareList_SameCountDifferentElement_Tests()
        {
            IList<int> list1 = Enumerable.Range(0, 10).ToList();
            IList<int> list2 = Enumerable.Range(1, 10).ToList();

            ComparerHelper.CompareList(list1, list2).Should().Be(-1);
            ComparerHelper.CompareList(list2, list1).Should().Be(1);
        }

        [Fact]
        public void CompareList_WithNullComparer_Tests()
        {
            ToolComponent tool = new ToolComponent { Guid = Guid.Empty.ToString() };

            IList<Run> runs1 = new[] { new Run { Tool = new Tool { Driver = tool } } };
            IList<Run> runs2 = Array.Empty<Run>();

            Action act = () => ComparerHelper.CompareList(runs1, runs2, comparer: null);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void CompareList_WithComparer_Tests()
        {
            IList<Run> run1 = new List<Run>();
            run1.Add(null);
            run1.Add(new Run { Tool = new Tool { Driver = new ToolComponent { Guid = Guid.NewGuid().ToString() } } });

            IList<Run> run2 = new List<Run>();
            run2.Add(new Run { Tool = new Tool { Driver = new ToolComponent { Guid = Guid.NewGuid().ToString() } } });
            run2.Add(null);

            int result = ComparerHelper.CompareList(run1, run2, RunComparer.Instance);
            result.Should().Be(-1);

            result = ComparerHelper.CompareList(run2, run1, RunComparer.Instance);
            result.Should().Be(1);
        }

        [Fact]
        public void CompareDictionary_Shuffle_Tests()
        {
            IDictionary<string, string> dict1 = new Dictionary<string, string>();
            dict1.Add("a", "a");
            dict1.Add("b", "b");
            dict1.Add("c", "c");

            IDictionary<string, string> dict2 = new Dictionary<string, string>();
            dict2.Add("b", "b");
            dict2.Add("c", "c");
            dict2.Add("a", "a");

            int result = ComparerHelper.CompareDictionary(dict1, dict2);
            result.Should().Be(0);

            dict2["c"] = "d";

            result = ComparerHelper.CompareDictionary(dict1, dict2);
            result.Should().Be(-1);

            result = ComparerHelper.CompareDictionary(dict2, dict1);
            result.Should().Be(1);
        }

        [Fact]
        public void CompareDictionary_BothNull_Tests()
        {
            IDictionary<string, string> list1 = null;
            IDictionary<string, string> list2 = null;

            ComparerHelper.CompareDictionary(list1, list2).Should().Be(0);
            ComparerHelper.CompareDictionary(list2, list1).Should().Be(0);
        }

        [Fact]
        public void CompareDictionary_CompareNullToNotNull_Tests()
        {
            IDictionary<string, string> list1 = null;
            IDictionary<string, string> list2 = new Dictionary<string, string>() { { "a", "a" } };

            ComparerHelper.CompareDictionary(list1, list2).Should().Be(-1);
            ComparerHelper.CompareDictionary(list2, list1).Should().Be(1);
        }

        [Fact]
        public void CompareDictionary_DifferentCount_Tests()
        {
            IDictionary<string, string> list1 = new Dictionary<string, string>() { { "a", "a" }, { "b", "b" } };
            IDictionary<string, string> list2 = new Dictionary<string, string>() { { "c", "c" } };

            ComparerHelper.CompareDictionary(list1, list2).Should().Be(1);
            ComparerHelper.CompareDictionary(list2, list1).Should().Be(-1);
        }

        [Fact]
        public void CompareDictionary_SameCountDifferentElement_Tests()
        {
            IDictionary<string, string> list1 =
                new Dictionary<string, string>() { { "a", "a" }, { "b", "b" } };
            IDictionary<string, string> list2 =
                new Dictionary<string, string>() { { "c", "c" }, { "d", "d" } };

            ComparerHelper.CompareDictionary(list1, list2).Should().Be(-1);
            ComparerHelper.CompareDictionary(list2, list1).Should().Be(1);
        }

        [Fact]
        public void CompareDictionary_WithNullComparer_Tests()
        {
            IDictionary<string, Location> loc1 = new Dictionary<string, Location>();
            IDictionary<string, Location> loc2 = new Dictionary<string, Location>();

            Action act = () => ComparerHelper.CompareDictionary(loc1, loc2, comparer: null);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void CompareDictionary_WithComparer_Tests()
        {
            IDictionary<string, Location> loc1 = new Dictionary<string, Location>();
            loc1.Add("1", null);
            loc1.Add("2", new Location { Id = 2 });
            IDictionary<string, Location> loc2 = new Dictionary<string, Location>();
            loc2.Add("1", new Location { Id = 1 });
            loc2.Add("2", new Location { Id = 2 });

            int result = ComparerHelper.CompareDictionary(loc1, loc2, LocationComparer.Instance);
            result.Should().Be(-1);

            result = ComparerHelper.CompareDictionary(loc2, loc1, LocationComparer.Instance);
            result.Should().Be(1);
        }

        [Fact]
        public void ArtifactContentComparer_Tests()
        {
            var list1 = new List<ArtifactContent>();
            var list2 = new List<ArtifactContent>();

            list1.Add(null);
            list2.Add(null);
            ComparerHelper.CompareList(list1, list2, ArtifactContentComparer.Instance).Should().Be(0);

            list1.Add(new ArtifactContent() { Text = "content 1" });
            list2.Add(new ArtifactContent() { Text = "content 2" });

            ComparerHelper.CompareList(list1, list2, ArtifactContentComparer.Instance).Should().Be(-1);
            ComparerHelper.CompareList(list2, list1, ArtifactContentComparer.Instance).Should().Be(1);
            list1.Clear();
            list2.Clear();

            list1.Add(new ArtifactContent() { Binary = "WUJDMTIz" });
            list2.Add(new ArtifactContent() { Binary = "QUJDMTIz" });

            ComparerHelper.CompareList(list1, list2, ArtifactContentComparer.Instance).Should().Be(1);
            ComparerHelper.CompareList(list2, list1, ArtifactContentComparer.Instance).Should().Be(-1);
            list1.Clear();
            list2.Clear();

            list1.Add(new ArtifactContent() { Text = "content 1", Rendered = new MultiformatMessageString { Markdown = "`markdown`" } });
            list2.Add(new ArtifactContent() { Text = "content 1", Rendered = new MultiformatMessageString { Markdown = "title" } });
            ComparerHelper.CompareList(list1, list2, ArtifactContentComparer.Instance).Should().Be(-1);
            ComparerHelper.CompareList(list2, list1, ArtifactContentComparer.Instance).Should().Be(1);
        }

        [Fact]
        public void ReportingConfigurationComparer_Tests()
        {
            var rules1 = new List<ReportingConfiguration>();
            var rules2 = new List<ReportingConfiguration>();

            rules1.Add(null);
            rules2.Add(null);

            ComparerHelper.CompareList(rules1, rules2, ReportingConfigurationComparer.Instance).Should().Be(0);

            rules1.Add(new ReportingConfiguration() { Rank = 26.648d });
            rules2.Add(new ReportingConfiguration() { Rank = 87.1d });

            ComparerHelper.CompareList(rules1, rules2, ReportingConfigurationComparer.Instance).Should().Be(-1);
            ComparerHelper.CompareList(rules2, rules1, ReportingConfigurationComparer.Instance).Should().Be(1);

            rules1.Insert(0, new ReportingConfiguration() { Level = FailureLevel.Error });
            rules2.Insert(0, new ReportingConfiguration() { Level = FailureLevel.Warning });

            ComparerHelper.CompareList(rules1, rules2, ReportingConfigurationComparer.Instance).Should().Be(1);
            ComparerHelper.CompareList(rules2, rules1, ReportingConfigurationComparer.Instance).Should().Be(-1);

            rules1.Insert(0, new ReportingConfiguration() { Enabled = false, Rank = 80d });
            rules2.Insert(0, new ReportingConfiguration() { Enabled = true, Rank = 80d });

            ComparerHelper.CompareList(rules1, rules2, ReportingConfigurationComparer.Instance).Should().Be(-1);
            ComparerHelper.CompareList(rules2, rules1, ReportingConfigurationComparer.Instance).Should().Be(1);
        }

        [Fact]
        public void ToolComponentComparer_Tests()
        {
            var list1 = new List<ToolComponent>();
            var list2 = new List<ToolComponent>();

            list1.Add(null);
            list2.Add(null);

            ComparerHelper.CompareList(list1, list2, ToolComponentComparer.Instance).Should().Be(0);

            list1.Insert(0, new ToolComponent() { Guid = Guid.Empty.ToString() });
            list2.Insert(0, new ToolComponent() { Guid = Guid.NewGuid().ToString() });

            ComparerHelper.CompareList(list1, list2, ToolComponentComparer.Instance).Should().Be(-1);
            ComparerHelper.CompareList(list2, list1, ToolComponentComparer.Instance).Should().Be(1);

            list1.Insert(0, new ToolComponent() { Name = "scan tool" });
            list2.Insert(0, new ToolComponent() { Name = "code scan tool" });

            ComparerHelper.CompareList(list1, list2, ToolComponentComparer.Instance).Should().Be(1);
            ComparerHelper.CompareList(list2, list1, ToolComponentComparer.Instance).Should().Be(-1);

            list1.Insert(0, new ToolComponent() { Organization = "MS", Name = "scan tool" });
            list2.Insert(0, new ToolComponent() { Organization = "Microsoft", Name = "scan tool" });

            ComparerHelper.CompareList(list1, list2, ToolComponentComparer.Instance).Should().Be(1);
            ComparerHelper.CompareList(list2, list1, ToolComponentComparer.Instance).Should().Be(-1);

            list1.Insert(0, new ToolComponent() { Product = "PREfast", Name = "scan tool" });
            list2.Insert(0, new ToolComponent() { Product = "prefast", Name = "scan tool" });

            ComparerHelper.CompareList(list1, list2, ToolComponentComparer.Instance).Should().Be(1);
            ComparerHelper.CompareList(list2, list1, ToolComponentComparer.Instance).Should().Be(-1);

            list1.Insert(0, new ToolComponent() { FullName = "Analysis Linter", Name = "scan tool" });
            list2.Insert(0, new ToolComponent() { FullName = "Analysis Linter Tool", Name = "scan tool" });

            ComparerHelper.CompareList(list1, list2, ToolComponentComparer.Instance).Should().Be(-1);
            ComparerHelper.CompareList(list2, list1, ToolComponentComparer.Instance).Should().Be(1);

            list1.Insert(0, new ToolComponent() { Version = "CWR-2022-01", Name = "scan tool" });
            list2.Insert(0, new ToolComponent() { Version = "CWR-2021-12", Name = "scan tool" });

            ComparerHelper.CompareList(list1, list2, ToolComponentComparer.Instance).Should().Be(1);
            ComparerHelper.CompareList(list2, list1, ToolComponentComparer.Instance).Should().Be(-1);

            list1.Insert(0, new ToolComponent() { SemanticVersion = "1.0.1", Name = "scan tool" });
            list2.Insert(0, new ToolComponent() { SemanticVersion = "1.0.3", Name = "scan tool" });

            ComparerHelper.CompareList(list1, list2, ToolComponentComparer.Instance).Should().Be(-1);
            ComparerHelper.CompareList(list2, list1, ToolComponentComparer.Instance).Should().Be(1);

            list1.Insert(0, new ToolComponent() { ReleaseDateUtc = "2/8/2022", Name = "scan tool" });
            list2.Insert(0, new ToolComponent() { ReleaseDateUtc = "1/1/2022", Name = "scan tool" });

            ComparerHelper.CompareList(list1, list2, ToolComponentComparer.Instance).Should().Be(1);
            ComparerHelper.CompareList(list2, list1, ToolComponentComparer.Instance).Should().Be(-1);

            list1.Insert(0, new ToolComponent() { DownloadUri = new Uri("https://example/download/v1"), Name = "scan tool" });
            list2.Insert(0, new ToolComponent() { DownloadUri = new Uri("https://example/download/v2"), Name = "scan tool" });

            ComparerHelper.CompareList(list1, list2, ToolComponentComparer.Instance).Should().Be(-1);
            ComparerHelper.CompareList(list2, list1, ToolComponentComparer.Instance).Should().Be(1);

            list1.Insert(0, new ToolComponent() { Rules = new ReportingDescriptor[] { new ReportingDescriptor { Id = "TESTRULE001" } }, Name = "scan tool" });
            list2.Insert(0, new ToolComponent() { Rules = new ReportingDescriptor[] { new ReportingDescriptor { Id = "TESTRULE002" } }, Name = "scan tool" });

            ComparerHelper.CompareList(list1, list2, ToolComponentComparer.Instance).Should().Be(-1);
            ComparerHelper.CompareList(list2, list1, ToolComponentComparer.Instance).Should().Be(1);
        }

        [Fact]
        public void ReportingDescriptorComparer_Tests()
        {
            var rules1 = new List<ReportingDescriptor>();
            var rules2 = new List<ReportingDescriptor>();

            rules1.Add(null);
            rules2.Add(null);

            ComparerHelper.CompareList(rules1, rules2, ReportingDescriptorComparer.Instance).Should().Be(0);

            rules1.Insert(0, new ReportingDescriptor() { Id = "TestRule1" });
            rules2.Insert(0, new ReportingDescriptor() { Id = "TestRule2" });

            ComparerHelper.CompareList(rules1, rules2, ReportingDescriptorComparer.Instance).Should().Be(-1);
            ComparerHelper.CompareList(rules2, rules1, ReportingDescriptorComparer.Instance).Should().Be(1);

            rules1.Insert(0, new ReportingDescriptor() { DeprecatedIds = new string[] { "OldRuleId3" }, Id = "TestRule1" });
            rules2.Insert(0, new ReportingDescriptor() { DeprecatedIds = new string[] { "OldRuleId2" }, Id = "TestRule1" });

            ComparerHelper.CompareList(rules1, rules2, ReportingDescriptorComparer.Instance).Should().Be(1);
            ComparerHelper.CompareList(rules2, rules1, ReportingDescriptorComparer.Instance).Should().Be(-1);

            rules1.Insert(0, new ReportingDescriptor() { Guid = Guid.NewGuid().ToString(), Id = "TestRule1" });
            rules2.Insert(0, new ReportingDescriptor() { Guid = Guid.Empty.ToString(), Id = "TestRule1" });

            ComparerHelper.CompareList(rules1, rules2, ReportingDescriptorComparer.Instance).Should().Be(1);
            ComparerHelper.CompareList(rules2, rules1, ReportingDescriptorComparer.Instance).Should().Be(-1);

            rules1.Insert(0, new ReportingDescriptor() { DeprecatedIds = new string[] { Guid.Empty.ToString() }, Id = "TestRule1" });
            rules2.Insert(0, new ReportingDescriptor() { DeprecatedIds = new string[] { Guid.NewGuid().ToString() }, Id = "TestRule1" });

            ComparerHelper.CompareList(rules1, rules2, ReportingDescriptorComparer.Instance).Should().Be(-1);
            ComparerHelper.CompareList(rules2, rules1, ReportingDescriptorComparer.Instance).Should().Be(1);

            rules1.Insert(0, new ReportingDescriptor() { Name = "UnusedVariable", Id = "TestRule1" });
            rules2.Insert(0, new ReportingDescriptor() { Name = "", Id = "TestRule1" });

            ComparerHelper.CompareList(rules1, rules2, ReportingDescriptorComparer.Instance).Should().Be(1);
            ComparerHelper.CompareList(rules2, rules1, ReportingDescriptorComparer.Instance).Should().Be(-1);

            rules1.Insert(0, new ReportingDescriptor() { ShortDescription = new MultiformatMessageString { Text = "Remove unused variable" }, Id = "TestRule1" });
            rules2.Insert(0, new ReportingDescriptor() { ShortDescription = new MultiformatMessageString { Text = "Wrong description" }, Id = "TestRule1" });

            ComparerHelper.CompareList(rules1, rules2, ReportingDescriptorComparer.Instance).Should().Be(-1);
            ComparerHelper.CompareList(rules2, rules1, ReportingDescriptorComparer.Instance).Should().Be(1);

            rules1.Insert(0, new ReportingDescriptor() { FullDescription = new MultiformatMessageString { Text = "Remove unused variable" }, Id = "TestRule1" });
            rules2.Insert(0, new ReportingDescriptor() { FullDescription = new MultiformatMessageString { Text = "Wrong description" }, Id = "TestRule1" });

            ComparerHelper.CompareList(rules1, rules2, ReportingDescriptorComparer.Instance).Should().Be(-1);
            ComparerHelper.CompareList(rules2, rules1, ReportingDescriptorComparer.Instance).Should().Be(1);

            rules1.Insert(0, new ReportingDescriptor() { DefaultConfiguration = new ReportingConfiguration { Level = FailureLevel.Note }, Id = "TestRule1" });
            rules2.Insert(0, new ReportingDescriptor() { DefaultConfiguration = new ReportingConfiguration { Level = FailureLevel.Error }, Id = "TestRule1" });

            ComparerHelper.CompareList(rules1, rules2, ReportingDescriptorComparer.Instance).Should().Be(-1);
            ComparerHelper.CompareList(rules2, rules1, ReportingDescriptorComparer.Instance).Should().Be(1);

            rules1.Insert(0, new ReportingDescriptor() { HelpUri = new Uri("http://example.net/rule/id"), Id = "TestRule1" });
            rules2.Insert(0, new ReportingDescriptor() { HelpUri = new Uri("http://example.net"), Id = "TestRule1" });

            ComparerHelper.CompareList(rules1, rules2, ReportingDescriptorComparer.Instance).Should().Be(1);
            ComparerHelper.CompareList(rules2, rules1, ReportingDescriptorComparer.Instance).Should().Be(-1);

            rules1.Insert(0, new ReportingDescriptor() { Help = new MultiformatMessageString { Text = "Helping texts." }, Id = "TestRule1" });
            rules2.Insert(0, new ReportingDescriptor() { Help = new MultiformatMessageString { Text = "For customers." }, Id = "TestRule1" });

            ComparerHelper.CompareList(rules1, rules2, ReportingDescriptorComparer.Instance).Should().Be(1);
            ComparerHelper.CompareList(rules2, rules1, ReportingDescriptorComparer.Instance).Should().Be(-1);
        }

        [Fact]
        public void RegionComparer_Tests()
        {
            var regions1 = new List<Region>();
            var regions2 = new List<Region>();

            regions1.Add(null);
            regions2.Add(null);

            ComparerHelper.CompareList(regions1, regions2, RegionComparer.Instance).Should().Be(0);

            regions1.Insert(0, new Region() { StartLine = 0, StartColumn = 0 });
            regions2.Insert(0, new Region() { StartLine = 1, StartColumn = 0 });

            ComparerHelper.CompareList(regions1, regions2, RegionComparer.Instance).Should().Be(-1);
            ComparerHelper.CompareList(regions2, regions1, RegionComparer.Instance).Should().Be(1);

            regions1.Insert(0, new Region() { StartLine = 0, StartColumn = 1 });
            regions2.Insert(0, new Region() { StartLine = 0, StartColumn = 0 });

            ComparerHelper.CompareList(regions1, regions2, RegionComparer.Instance).Should().Be(1);
            ComparerHelper.CompareList(regions2, regions1, RegionComparer.Instance).Should().Be(-1);

            regions1.Insert(0, new Region() { StartLine = 10, EndLine = 11, StartColumn = 0 });
            regions2.Insert(0, new Region() { StartLine = 10, EndLine = 10, StartColumn = 0 });

            ComparerHelper.CompareList(regions1, regions2, RegionComparer.Instance).Should().Be(1);
            ComparerHelper.CompareList(regions2, regions1, RegionComparer.Instance).Should().Be(-1);

            regions1.Insert(0, new Region() { StartLine = 10, EndLine = 10, StartColumn = 5, EndColumn = 23 });
            regions2.Insert(0, new Region() { StartLine = 10, EndLine = 10, StartColumn = 5, EndColumn = 7 });

            ComparerHelper.CompareList(regions1, regions2, RegionComparer.Instance).Should().Be(1);
            ComparerHelper.CompareList(regions2, regions1, RegionComparer.Instance).Should().Be(-1);

            regions1.Insert(0, new Region() { CharOffset = 100, CharLength = 30 });
            regions2.Insert(0, new Region() { CharOffset = 36, CharLength = 30 });

            ComparerHelper.CompareList(regions1, regions2, RegionComparer.Instance).Should().Be(1);
            ComparerHelper.CompareList(regions2, regions1, RegionComparer.Instance).Should().Be(-1);

            regions1.Insert(0, new Region() { CharOffset = 100, CharLength = 47 });
            regions2.Insert(0, new Region() { CharOffset = 100, CharLength = 326 });

            ComparerHelper.CompareList(regions1, regions2, RegionComparer.Instance).Should().Be(-1);
            ComparerHelper.CompareList(regions2, regions1, RegionComparer.Instance).Should().Be(1);

            regions1.Insert(0, new Region() { ByteOffset = 226, ByteLength = 11 });
            regions2.Insert(0, new Region() { ByteOffset = 1623, ByteLength = 11 });

            ComparerHelper.CompareList(regions1, regions2, RegionComparer.Instance).Should().Be(-1);
            ComparerHelper.CompareList(regions2, regions1, RegionComparer.Instance).Should().Be(1);

            regions1.Insert(0, new Region() { ByteOffset = 67, ByteLength = 9 });
            regions2.Insert(0, new Region() { ByteOffset = 67, ByteLength = 11 });

            ComparerHelper.CompareList(regions1, regions2, RegionComparer.Instance).Should().Be(-1);
            ComparerHelper.CompareList(regions2, regions1, RegionComparer.Instance).Should().Be(1);
        }

        [Fact]
        public void ArtifactComparer_Tests()
        {
            var artifacts1 = new List<Artifact>();
            var artifacts2 = new List<Artifact>();

            artifacts1.Add(null);
            artifacts2.Add(null);

            ComparerHelper.CompareList(artifacts1, artifacts2, ArtifactComparer.Instance).Should().Be(0);

            artifacts1.Insert(0, new Artifact() { Description = new Message { Text = "Represents for an artifact" } });
            artifacts2.Insert(0, new Artifact() { Description = new Message { Text = "A source file artifact" } });

            ComparerHelper.CompareList(artifacts1, artifacts2, ArtifactComparer.Instance).Should().Be(1);
            ComparerHelper.CompareList(artifacts2, artifacts1, ArtifactComparer.Instance).Should().Be(-1);

            artifacts1.Insert(0, new Artifact() { Location = new ArtifactLocation { Index = 0 } });
            artifacts2.Insert(0, new Artifact() { Location = new ArtifactLocation { Index = 1 } });

            ComparerHelper.CompareList(artifacts1, artifacts2, ArtifactComparer.Instance).Should().Be(-1);
            ComparerHelper.CompareList(artifacts2, artifacts1, ArtifactComparer.Instance).Should().Be(1);

            artifacts1.Insert(0, new Artifact() { ParentIndex = 0 });
            artifacts2.Insert(0, new Artifact() { ParentIndex = 1 });

            ComparerHelper.CompareList(artifacts1, artifacts2, ArtifactComparer.Instance).Should().Be(-1);
            ComparerHelper.CompareList(artifacts2, artifacts1, ArtifactComparer.Instance).Should().Be(1);

            artifacts1.Insert(0, new Artifact() { Offset = 2 });
            artifacts2.Insert(0, new Artifact() { Offset = 1 });

            ComparerHelper.CompareList(artifacts1, artifacts2, ArtifactComparer.Instance).Should().Be(1);
            ComparerHelper.CompareList(artifacts2, artifacts1, ArtifactComparer.Instance).Should().Be(-1);

            artifacts1.Insert(0, new Artifact() { Length = 102542 });
            artifacts2.Insert(0, new Artifact() { Length = -1 });

            ComparerHelper.CompareList(artifacts1, artifacts2, ArtifactComparer.Instance).Should().Be(1);
            ComparerHelper.CompareList(artifacts2, artifacts1, ArtifactComparer.Instance).Should().Be(-1);

            artifacts1.Insert(0, new Artifact() { Roles = ArtifactRoles.AnalysisTarget | ArtifactRoles.Attachment });
            artifacts2.Insert(0, new Artifact() { Roles = ArtifactRoles.Policy });

            ComparerHelper.CompareList(artifacts1, artifacts2, ArtifactComparer.Instance).Should().Be(-1);
            ComparerHelper.CompareList(artifacts2, artifacts1, ArtifactComparer.Instance).Should().Be(1);

            artifacts1.Insert(0, new Artifact() { MimeType = "text" });
            artifacts2.Insert(0, new Artifact() { MimeType = "video" });

            ComparerHelper.CompareList(artifacts1, artifacts2, ArtifactComparer.Instance).Should().Be(-1);
            ComparerHelper.CompareList(artifacts2, artifacts1, ArtifactComparer.Instance).Should().Be(1);

            artifacts1.Insert(0, new Artifact() { Contents = new ArtifactContent { Text = "\"string\"" } });
            artifacts2.Insert(0, new Artifact() { Contents = new ArtifactContent { Text = "var result = 0;" } });

            ComparerHelper.CompareList(artifacts1, artifacts2, ArtifactComparer.Instance).Should().Be(-1);
            ComparerHelper.CompareList(artifacts2, artifacts1, ArtifactComparer.Instance).Should().Be(1);

            artifacts1.Insert(0, new Artifact() { Encoding = "UTF-16BE" });
            artifacts2.Insert(0, new Artifact() { Encoding = "UTF-16LE" });

            ComparerHelper.CompareList(artifacts1, artifacts2, ArtifactComparer.Instance).Should().Be(-1);
            ComparerHelper.CompareList(artifacts2, artifacts1, ArtifactComparer.Instance).Should().Be(1);

            artifacts1.Insert(0, new Artifact() { SourceLanguage = "html" });
            artifacts2.Insert(0, new Artifact() { SourceLanguage = "csharp/7" });

            ComparerHelper.CompareList(artifacts1, artifacts2, ArtifactComparer.Instance).Should().Be(1);
            ComparerHelper.CompareList(artifacts2, artifacts1, ArtifactComparer.Instance).Should().Be(-1);

            artifacts1.Insert(0, new Artifact() { Hashes = new Dictionary<string, string> { { "sha-256", "..." } } });
            artifacts2.Insert(0, new Artifact() { Hashes = new Dictionary<string, string> { { "sha-512", "..." } } });

            ComparerHelper.CompareList(artifacts1, artifacts2, ArtifactComparer.Instance).Should().Be(-1);
            ComparerHelper.CompareList(artifacts2, artifacts1, ArtifactComparer.Instance).Should().Be(1);

            artifacts1.Insert(0, new Artifact() { LastModifiedTimeUtc = DateTime.UtcNow });
            artifacts2.Insert(0, new Artifact() { LastModifiedTimeUtc = DateTime.UtcNow.AddDays(-1) });

            ComparerHelper.CompareList(artifacts1, artifacts2, ArtifactComparer.Instance).Should().Be(1);
            ComparerHelper.CompareList(artifacts2, artifacts1, ArtifactComparer.Instance).Should().Be(-1);
        }

        [Fact]
        public void ThreadFlowLocationComparer_Tests()
        {
            var locations1 = new List<ThreadFlowLocation>();
            var locations2 = new List<ThreadFlowLocation>();

            locations1.Add(null);
            locations2.Add(null);

            ComparerHelper.CompareList(locations1, locations2, ThreadFlowLocationComparer.Instance).Should().Be(0);

            Location loc1 = new Location
            {
                PhysicalLocation = new PhysicalLocation
                {
                    ArtifactLocation = new ArtifactLocation
                    {
                        Uri = new Uri("path/to/file1.c", UriKind.Relative)
                    }
                }
            };

            Location loc2 = new Location
            {
                PhysicalLocation = new PhysicalLocation
                {
                    ArtifactLocation = new ArtifactLocation
                    {
                        Uri = new Uri("path/to/file2.c", UriKind.Relative)
                    }
                }
            };

            locations1.Insert(0, new ThreadFlowLocation() { Location = loc1 });
            locations2.Insert(0, new ThreadFlowLocation() { Location = loc2 });

            ComparerHelper.CompareList(locations1, locations2, ThreadFlowLocationComparer.Instance).Should().Be(-1);
            ComparerHelper.CompareList(locations2, locations1, ThreadFlowLocationComparer.Instance).Should().Be(1);

            locations1.Insert(0, new ThreadFlowLocation() { Index = 2, Location = loc1 });
            locations2.Insert(0, new ThreadFlowLocation() { Index = 1, Location = loc2 });

            ComparerHelper.CompareList(locations1, locations2, ThreadFlowLocationComparer.Instance).Should().Be(1);
            ComparerHelper.CompareList(locations2, locations1, ThreadFlowLocationComparer.Instance).Should().Be(-1);

            locations1.Insert(0, new ThreadFlowLocation() { Kinds = new string[] { "memory" }, Location = loc1 });
            locations2.Insert(0, new ThreadFlowLocation() { Kinds = new string[] { "call", "branch" }, Location = loc2 });

            ComparerHelper.CompareList(locations1, locations2, ThreadFlowLocationComparer.Instance).Should().Be(-1);
            ComparerHelper.CompareList(locations2, locations1, ThreadFlowLocationComparer.Instance).Should().Be(1);

            locations1.Insert(0, new ThreadFlowLocation() { NestingLevel = 3 });
            locations2.Insert(0, new ThreadFlowLocation() { NestingLevel = 2 });

            ComparerHelper.CompareList(locations1, locations2, ThreadFlowLocationComparer.Instance).Should().Be(1);
            ComparerHelper.CompareList(locations2, locations1, ThreadFlowLocationComparer.Instance).Should().Be(-1);

            locations1.Insert(0, new ThreadFlowLocation() { ExecutionOrder = 2 });
            locations2.Insert(0, new ThreadFlowLocation() { ExecutionOrder = 1 });

            ComparerHelper.CompareList(locations1, locations2, ThreadFlowLocationComparer.Instance).Should().Be(1);
            ComparerHelper.CompareList(locations2, locations1, ThreadFlowLocationComparer.Instance).Should().Be(-1);

            locations1.Insert(0, new ThreadFlowLocation() { ExecutionTimeUtc = DateTime.UtcNow });
            locations2.Insert(0, new ThreadFlowLocation() { ExecutionTimeUtc = DateTime.UtcNow.AddHours(-2) });

            ComparerHelper.CompareList(locations1, locations2, ThreadFlowLocationComparer.Instance).Should().Be(1);
            ComparerHelper.CompareList(locations2, locations1, ThreadFlowLocationComparer.Instance).Should().Be(-1);

            locations1.Insert(0, new ThreadFlowLocation() { Importance = ThreadFlowLocationImportance.Essential });
            locations2.Insert(0, new ThreadFlowLocation() { Importance = ThreadFlowLocationImportance.Unimportant });

            ComparerHelper.CompareList(locations1, locations2, ThreadFlowLocationComparer.Instance).Should().Be(-1);
            ComparerHelper.CompareList(locations2, locations1, ThreadFlowLocationComparer.Instance).Should().Be(1);
        }

        [Fact]
        public void RunComparer_Tests()
        {
            var runs1 = new List<Run>();
            var runs2 = new List<Run>();

            runs1.Add(null);
            runs2.Add(null);

            ComparerHelper.CompareList(runs1, runs2, RunComparer.Instance).Should().Be(0);

            runs1.Insert(0, new Run() { Artifacts = new Artifact[] { new Artifact { Description = new Message { Text = "artifact 1" } } } });
            runs2.Insert(0, new Run() { Artifacts = new Artifact[] { new Artifact { Description = new Message { Text = "artifact 2" } } } });

            ComparerHelper.CompareList(runs1, runs2, RunComparer.Instance).Should().Be(-1);
            ComparerHelper.CompareList(runs2, runs1, RunComparer.Instance).Should().Be(1);

            Tool tool1 = new Tool { Driver = new ToolComponent { Name = "PREFast", Version = "1.0" } };
            Tool tool2 = new Tool { Driver = new ToolComponent { Name = "PREFast", Version = "1.3" } };

            runs1.Insert(0, new Run() { Tool = tool1 });
            runs2.Insert(0, new Run() { Tool = tool2 });

            ComparerHelper.CompareList(runs1, runs2, RunComparer.Instance).Should().Be(-1);
            ComparerHelper.CompareList(runs2, runs1, RunComparer.Instance).Should().Be(1);

            Result result1 = new Result { RuleId = "CS001", Message = new Message { Text = "Issue of C# code" } };
            Result result2 = new Result { RuleId = "IDE692", Message = new Message { Text = "Issue by IDE" } };

            runs1.Insert(0, new Run() { Results = new Result[] { result1 } });
            runs2.Insert(0, new Run() { Results = new Result[] { result2 } });

            ComparerHelper.CompareList(runs1, runs2, RunComparer.Instance).Should().Be(-1);
            ComparerHelper.CompareList(runs2, runs1, RunComparer.Instance).Should().Be(1);
        }

        [Fact]
        public void ComparerHelp_CompareUir_Tests()
        {
            var testUris = new List<(Uri, int)>()
            {
                (null, -1),
                (null, 0),
                (new Uri(@"", UriKind.RelativeOrAbsolute), 1),
                (new Uri(string.Empty, UriKind.RelativeOrAbsolute), 0),
                (new Uri(@"file.ext", UriKind.RelativeOrAbsolute), 1),
                (new Uri(@"C:\path\file.ext", UriKind.RelativeOrAbsolute), -1),
                (new Uri(@"\\hostname\path\file.ext", UriKind.RelativeOrAbsolute), -1),
                (new Uri(@"file:///C:/path/file.ext", UriKind.RelativeOrAbsolute), 1),
                (new Uri(@"file.ext?some-query-string", UriKind.RelativeOrAbsolute), -1),
                (new Uri(@"\\hostname\c:\path\file.ext", UriKind.RelativeOrAbsolute), -1),
                (new Uri(@"/home/username/path/file.ext", UriKind.RelativeOrAbsolute), -1),
                (new Uri(@"nfs://servername/folder/file.ext", UriKind.RelativeOrAbsolute), 1),
                (new Uri(@"file://hostname/C:/path/file.ext", UriKind.RelativeOrAbsolute), -1),
                (new Uri(@"file:///home/username/path/file.ext", UriKind.RelativeOrAbsolute), -1),
                (new Uri(@"ftp://ftp.example.com/folder/file.ext", UriKind.RelativeOrAbsolute), 1),
                (new Uri(@"smb://servername/Share/folder/file.ext", UriKind.RelativeOrAbsolute), 1),
                (new Uri(@"dav://example.hostname.com/folder/file.ext", UriKind.RelativeOrAbsolute), -1),
                (new Uri(@"file://hostname/home/username/path/file.ext", UriKind.RelativeOrAbsolute), 1),
                (new Uri(@"ftp://username@ftp.example.com/folder/file.ext", UriKind.RelativeOrAbsolute), 1),
                (new Uri(@"scheme://servername.example.com/folder/file.ext", UriKind.RelativeOrAbsolute), 1),
                (new Uri(@"https://github.com/microsoft/sarif-sdk/file.ext", UriKind.RelativeOrAbsolute), -1),
                (new Uri(@"ssh://username@servername.example.com/folder/file.ext", UriKind.RelativeOrAbsolute), 1),
                (new Uri(@"scheme://username@servername.example.com/folder/file.ext", UriKind.RelativeOrAbsolute), -1),
                (new Uri(@"https://github.com/microsoft/sarif-sdk/file.ext?some-query-string", UriKind.RelativeOrAbsolute), -1),
            };

            for (int i = 1; i < testUris.Count; i++)
            {
                int result = ComparerHelper.CompareUri(testUris[i].Item1, testUris[i - 1].Item1);
                result.Should().Be(testUris[i].Item2);
            }
        }
    }
}
