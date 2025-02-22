// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// <auto-generated/>

#nullable disable

namespace Azure.AI.TextAnalytics.Models
{
    /// <summary> The EntityLinkingTaskResult. </summary>
    internal partial class EntityLinkingTaskResult
    {
        /// <summary> Initializes a new instance of EntityLinkingTaskResult. </summary>
        internal EntityLinkingTaskResult()
        {
        }

        /// <summary> Initializes a new instance of EntityLinkingTaskResult. </summary>
        /// <param name="results"></param>
        internal EntityLinkingTaskResult(EntityLinkingResult results)
        {
            Results = results;
        }

        public EntityLinkingResult Results { get; }
    }
}
