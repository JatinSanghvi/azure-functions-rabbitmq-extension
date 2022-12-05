// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Xunit.Abstractions;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ.IntegrationTests;

internal sealed class TestHelper : IDisposable
{
    private static readonly Random Random = new();
    private static readonly HttpClient HttpClient;

    private readonly ITestOutputHelper testOutputHelper;
    private readonly string vhostName;

    private IConnection connection;
    private IModel channel;
    private IHost host;

    static TestHelper()
    {
        HttpClient = new() { BaseAddress = new Uri("http://localhost:15672/api/") };

        string authenticationHeaderParameter = Convert.ToBase64String(Encoding.UTF8.GetBytes("guest:guest"));
        HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authenticationHeaderParameter);
    }

    private TestHelper(ITestOutputHelper testOutputHelper)
    {
        this.testOutputHelper = testOutputHelper ?? throw new ArgumentNullException(nameof(testOutputHelper));
        this.vhostName = new string(Enumerable.Range(0, 5).Select(_ => (char)Random.Next('a', 'z' + 1)).ToArray());
    }

    public static async Task<TestHelper> CreateAsync<TFunctions>(ITestOutputHelper testOutputHelper)
    {
        TestHelper testHelper = new(testOutputHelper);
        await testHelper.InitializeAsync<TFunctions>();
        return testHelper;
    }

    public void Dispose()
    {
        this.host.StopAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
        this.host.Dispose();

        this.connection.Dispose();

        this.DeleteVhostAsync().GetAwaiter().GetResult();
    }

    public void CreateQueue(string queueName)
    {
        this.testOutputHelper.WriteLine($"Creating queue: [{queueName}].");
        this.channel.QueueDeclare(queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
    }

    public IReadOnlyList<string> GetMessages(string queueName)
    {
        List<string> messages = new();

        while (true)
        {
            BasicGetResult result = this.channel.BasicGet(queueName, autoAck: true);

            if (result == null)
            {
                break;
            }

            byte[] body = result.Body.ToArray();
            string message = Encoding.UTF8.GetString(body);
            messages.Add(message);
        }

        return messages;
    }

    public Task InvokeFunctionAsync(string functionName, Dictionary<string, object> arguments)
    {
        return this.host.Services.GetService<IJobHost>().CallAsync(functionName, arguments);
    }

    private async Task InitializeAsync<TFunctions>()
    {
        await this.CreateVhostAsync();

        Uri connectionUri = new($"amqp://guest:guest@localhost:5672/{this.vhostName}");
        ConnectionFactory factory = new() { Uri = connectionUri };

        this.connection = factory.CreateConnection();
        this.channel = this.connection.CreateModel();

        this.host = this.InitializeHost<TFunctions>(connectionUri);
        this.host.Start();
    }

    private async Task CreateVhostAsync()
    {
        this.testOutputHelper.WriteLine($"Creating virtual host: [{this.vhostName}].");
        HttpResponseMessage response = await HttpClient.PutAsync($"vhosts/{this.vhostName}", null);
        response.EnsureSuccessStatusCode();
    }

    private async Task DeleteVhostAsync()
    {
        this.testOutputHelper.WriteLine($"Deleting virtual host: [{this.vhostName}].");
        await HttpClient.DeleteAsync($"vhosts/{this.vhostName}");
    }

    private IHost InitializeHost<TFunctions>(Uri connectionUri)
    {
        this.testOutputHelper.WriteLine("Initializing host.");

        return new HostBuilder()
            .ConfigureWebJobs(webJobsBuilder =>
            {
                webJobsBuilder.AddRabbitMQ();
            })
            .ConfigureAppConfiguration(configurationBuilder =>
            {
                Dictionary<string, string> configuration = new()
                {
                    ["connectionStrings:RabbitMQ"] = connectionUri.ToString(),
                };

                configurationBuilder.AddInMemoryCollection(configuration);
            })
            .ConfigureServices(services =>
            {
                List<Type> types = new() { typeof(TFunctions) };
                ITypeLocator mockTypeLocator = Mock.Of<ITypeLocator>(locator => locator.GetTypes() == types);
                services.AddSingleton(mockTypeLocator);
            })
            .ConfigureLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders();
            })
            .Build();
    }
}
