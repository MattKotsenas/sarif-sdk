// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis.Sarif.Baseline.ResultMatching;

namespace Microsoft.CodeAnalysis.Sarif.Baseline
{
    internal static class WhatComparer
    {
        private const string LocationNonSpecific = "";
        private const string PropertySetBase = "Base";
        private const string PropertySetFingerprint = "Fingerprint";
        private const string PropertySetPartialFingerprint = "PartialFingerprint";
        private const string PropertySetProperty = "Property";

        public static IEnumerable<WhatComponent> WhatProperties(this ExtractedResult result, string locationSpecifier = LocationNonSpecific)
        {
            if (result?.Result == null) { yield break; }

            // Add Guid
            if (result.Result.Guid != null)
            {
                yield return new WhatComponent(result.RuleId, locationSpecifier, PropertySetBase, "Guid", result.Result.Guid?.ToString(SarifConstants.GuidFormat));
            }

            // Add Message Text
            string messageText = result.Result.GetMessageText(result.Result.GetRule(result.OriginalRun));
            if (!string.IsNullOrEmpty(messageText))
            {
                yield return new WhatComponent(result.RuleId, locationSpecifier, PropertySetBase, "Message", messageText);
            }

            // Add each Fingerprint
            if (result.Result.Fingerprints != null)
            {
                foreach (KeyValuePair<string, string> fingerprint in result.Result.Fingerprints)
                {
                    yield return new WhatComponent(result.RuleId, locationSpecifier, PropertySetFingerprint, fingerprint.Key, fingerprint.Value);
                }
            }

            // Add each PartialFingerprint
            if (result.Result.PartialFingerprints != null)
            {
                foreach (KeyValuePair<string, string> fingerprint in result.Result.PartialFingerprints)
                {
                    yield return new WhatComponent(result.RuleId, locationSpecifier, PropertySetPartialFingerprint, fingerprint.Key, fingerprint.Value);
                }
            }

            string snippet = GetFirstSnippet(result);
            if (snippet != null)
            {
                yield return new WhatComponent(result.RuleId, locationSpecifier, PropertySetBase, "Location.Snippet", snippet);
            }

            // Add each Property
            if (result.Result.Properties != null)
            {
                foreach (KeyValuePair<string, SerializedPropertyInfo> property in result.Result.Properties)
                {
                    yield return new WhatComponent(result.RuleId, locationSpecifier, PropertySetProperty, property.Key, property.Value?.SerializedValue);
                }
            }
        }

        /// <summary>
        ///  Match the 'What' properties of two ExtractedResults.
        /// </summary>
        /// <param name="left">ExtractedResult to match</param>
        /// <param name="right">ExtractedResult to match</param>
        /// <returns>True if *any* 'What' property matches, False otherwise</returns>
        public static bool MatchesWhat(ExtractedResult left, ExtractedResult right, TrustMap trustMap = null)
        {
            if (left?.Result == null || right?.Result == null) { return false; }

            // Match Guid
            if (left.Result.Guid != null)
            {
                if (string.Equals(left.Result.Guid, right.Result.Guid))
                {
                    return true;
                }

                // Non-Match doesn't force false, as GUIDs are often re-generated by tools.
            }

            // Match Fingerprints (any one match is a match)
            if (left.Result.Fingerprints != null && right.Result.Fingerprints != null)
            {
                int correspondingFingerprintCount = 0;

                foreach (KeyValuePair<string, string> fingerprint in left.Result.Fingerprints)
                {
                    if (right.Result.Fingerprints.TryGetValue(fingerprint.Key, out string otherFingerprint))
                    {
                        correspondingFingerprintCount++;

                        if (string.Equals(fingerprint.Value, otherFingerprint))
                        {
                            return true;
                        }
                    }
                }

                // Force non-match if there were fingerprints but none of them matched
                if (correspondingFingerprintCount > 0) { return false; }
            }

            // Match PartialFingerprints (50% must match)
            if (left.Result.PartialFingerprints != null && right.Result.PartialFingerprints != null)
            {
                float weightOfComparableValues = 0;
                float matchWeight = 0;

                foreach (KeyValuePair<string, string> fingerprint in left.Result.PartialFingerprints)
                {
                    if (right.Result.PartialFingerprints.TryGetValue(fingerprint.Key, out string otherFingerprint))
                    {
                        float trust = trustMap?.Trust(PropertySetPartialFingerprint, fingerprint.Key) ?? TrustMap.DefaultTrust;
                        weightOfComparableValues += trust;

                        if (string.Equals(fingerprint.Value, otherFingerprint))
                        {
                            matchWeight += trust;
                        }
                    }
                }

                if (weightOfComparableValues > 0)
                {
                    // Return whether at least half of the partialFingerprints matched weighted by trust
                    return matchWeight * 2 >= weightOfComparableValues;
                }
            }

            // At this point, no high confidence properties matched or failed to match
            string leftMessage = GetCanonicalizedMessage(left);
            string rightMessage = GetCanonicalizedMessage(right);

            string leftSnippet = GetFirstSnippet(left);
            string rightSnippet = GetFirstSnippet(right);

            return string.Equals(leftMessage, rightMessage) && string.Equals(leftSnippet, rightSnippet);
        }

        private static string GetFirstSnippet(ExtractedResult result)
        {
            if (result.Result.Locations != null)
            {
                foreach (Location loc in result.Result.Locations)
                {
                    string snippet = loc?.PhysicalLocation?.Region?.Snippet?.Text;
                    if (snippet != null) { return snippet; }
                }
            }

            return null;
        }

        private static string GetCanonicalizedMessage(ExtractedResult result)
        {
            string rawMessage = result.Result.GetMessageText(result.Result.GetRule(result.OriginalRun));

            // Canonicalize the message by replacing any line numbers in it with consistent markers
            Region firstRegion = result.Result?.Locations?.FirstOrDefault()?.PhysicalLocation?.Region;
            if (firstRegion != null)
            {
                if (firstRegion.StartLine != null)
                {
                    rawMessage = rawMessage
                    .Replace(firstRegion.StartLine.Value.ToString(CultureInfo.InvariantCulture), "~SL~");
                }

                if (firstRegion.StartColumn != null)
                {
                    rawMessage = rawMessage
                    .Replace(firstRegion.StartColumn.Value.ToString(CultureInfo.InvariantCulture), "~SC~");
                }

                if (firstRegion.EndLine != null)
                {
                    rawMessage = rawMessage
                    .Replace(firstRegion.EndLine.Value.ToString(CultureInfo.InvariantCulture), "~EL~");
                }

                if (firstRegion.EndColumn != null)
                {
                    rawMessage = rawMessage
                    .Replace(firstRegion.EndColumn.Value.ToString(CultureInfo.InvariantCulture), "~EC~");
                }
            }

            return rawMessage;
        }
    }
}
