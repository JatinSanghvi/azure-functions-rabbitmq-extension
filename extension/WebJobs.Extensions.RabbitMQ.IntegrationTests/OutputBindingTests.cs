// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ.IntegrationTests;

public sealed class OutputBindingTests : IDisposable
{
    private readonly TestHelper testHelper;

    public OutputBindingTests(ITestOutputHelper testOutputHelper)
    {
        this.testHelper = TestHelper.CreateAsync<Functions>(testOutputHelper).GetAwaiter().GetResult();
    }

    void IDisposable.Dispose()
    {
        this.testHelper.Dispose();
    }

    [Fact]
    public async Task SupportsStringOutput()
    {
        this.testHelper.CreateQueue("testQueue");
        await this.testHelper.InvokeFunctionAsync(nameof(Functions.StringOutput), new Dictionary<string, object> { ["input"] = "Test String" });
        IReadOnlyList<string> messages = this.testHelper.GetMessages("testQueue");
        Assert.Equal(1, messages.Count);
        Assert.Equal("Test String", messages[0]);
    }

    public class Functions
    {
        [NoAutomaticTrigger]
        public static void StringOutput(
            string input,
            [RabbitMQ(QueueName = "testQueue")] out string outputMessage)
        {
            outputMessage = input;
        }
    }
}
