﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using FluentAssertions;

using Microsoft.CodeAnalysis.Sarif;

using Xunit;

namespace Microsoft.CodeAnalysis.Test.UnitTests.Sarif.Core
{
    public class ResultTests
    {
        [Fact]
        public void Result_Kind_ResetsLevelValue()
        {
            // This test ensures NotYetAutoGenerated field 'Level' value is reset if 'Kind' value is changed to a non-default value.
            // see .src/sarif/NotYetAutoGenerated/Result.cs (Level property)
            var result = new Result();

            result.Level.Should().BeNull();

            result.Kind = ResultKind.Pass;

            result.Level.Should().Be(FailureLevel.None);
        }

        [Fact]
        public void Result_Level_ResetsKindValue()
        {
            // This test ensures NotYetAutoGenerated field 'Kind' value is reset if 'Level' value is changed to a non-default value.
            // see .src/sarif/NotYetAutoGenerated/Result.cs (Kind property)
            var result = new Result();

            result.Kind.Should().Be(ResultKind.Fail); // Default value

            result.Kind = ResultKind.Pass;

            result.Level = FailureLevel.Error;

            result.Kind.Should().Be(ResultKind.Fail);
        }

        [Fact]
        public void Result_TryIsSuppressed_ShouldReturnFalseWhenNoSuppressionsAreAvailable()
        {
            var result = new Result();
            result.TryIsSuppressed(out bool _).Should().BeFalse();
        }

        [Fact]
        public void Result_TryIsSuppressed_ShouldReturnTrueWhenSuppressionsAreAvailable()
        {
            var result = new Result
            {
                Suppressions = new List<Suppression>()
            };
            result.TryIsSuppressed(out bool isSuppressed).Should().BeTrue();
            isSuppressed.Should().BeFalse();

            // Suppression with 'UnderReview' only.
            result.Suppressions.Add(new Suppression { Status = SuppressionStatus.UnderReview });
            result.TryIsSuppressed(out isSuppressed).Should().BeTrue();
            isSuppressed.Should().BeFalse();

            // Suppression with 'UnderReview' and 'Accepted'.
            result.Suppressions.Add(new Suppression { Status = SuppressionStatus.Accepted });
            result.TryIsSuppressed(out isSuppressed).Should().BeTrue();
            isSuppressed.Should().BeFalse();

            // Suppression with 'Rejected' only.
            result.Suppressions.Clear();
            result.Suppressions.Add(new Suppression { Status = SuppressionStatus.Rejected });
            result.TryIsSuppressed(out isSuppressed).Should().BeTrue();
            isSuppressed.Should().BeFalse();

            // Suppression with 'Rejected' and 'Accepted'.
            result.Suppressions.Add(new Suppression { Status = SuppressionStatus.Accepted });
            result.TryIsSuppressed(out isSuppressed).Should().BeTrue();
            isSuppressed.Should().BeFalse();

            // Suppression with 'Accepted' only.
            result.Suppressions.Clear();
            result.Suppressions.Add(new Suppression { Status = SuppressionStatus.Accepted });
            result.TryIsSuppressed(out isSuppressed).Should().BeTrue();
            isSuppressed.Should().BeTrue();
        }
    }
}
