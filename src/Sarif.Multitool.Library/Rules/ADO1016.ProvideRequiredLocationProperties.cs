﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Sarif.Multitool.Rules
{
    public class AdoProvideRequiredLocationProperties
        : SarifValidationSkimmerBase
    {
        public override string Id => RuleId.ADOProvideRequiredLocationProperties;

        public override MultiformatMessageString FullDescription => new MultiformatMessageString() { Text = RuleResources.ADO1016_ProvideRequiredLocationProperties_FullDescription_Text };

        public override HashSet<RuleKind> RuleKinds => new HashSet<RuleKind>(new[] { RuleKind.Ado });

        public AdoProvideRequiredLocationProperties()
        {
            this.DefaultConfiguration.Level = FailureLevel.Error;
        }

        protected override void Analyze(Location location, string locationPointer)
        {
            base.Analyze(location, locationPointer);
        }
    }
}
