﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RabbitMQ.Client.Events;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ.Tests
{
    public class BasicDeliverEventArgsValueProviderTests
    {

        [Fact]
        public async Task Poco_Conversion_Succeeds()
        {
            TestClass expectedObject = new TestClass(1, 1);
            string expectedStringifiedJson = JsonConvert.SerializeObject(expectedObject);
            byte[] stringInBytes = Encoding.UTF8.GetBytes(expectedStringifiedJson);
            BasicDeliverEventArgs args = new BasicDeliverEventArgs("tag", 1, false, "", "queue", null, stringInBytes);
            BasicDeliverEventArgsValueProvider testValueProvider = new BasicDeliverEventArgsValueProvider(args, typeof(TestClass));

            TestClass actualObject = (TestClass)await testValueProvider.GetValueAsync();

            Assert.Equal(expectedStringifiedJson, JsonConvert.SerializeObject(actualObject));
            Assert.Equal(typeof(TestClass), testValueProvider.Type);
            Assert.Equal(expectedObject.X, actualObject.X);
            Assert.Equal(expectedObject.Y, actualObject.Y);
        }

        [Fact]
        public async Task StringifiedJson_Conversion_Succeeds()
        {
            TestClass expectedObject = new TestClass(1, 1);
            string expectedStringifiedJson = JsonConvert.SerializeObject(expectedObject);
            byte[] stringInBytes = Encoding.UTF8.GetBytes(expectedStringifiedJson);
            BasicDeliverEventArgs args = new BasicDeliverEventArgs("tag", 1, false, "", "queue", null, stringInBytes);
            BasicDeliverEventArgsValueProvider testValueProvider = new BasicDeliverEventArgsValueProvider(args, typeof(string));

            string actualStringifiedJson = (string)await testValueProvider.GetValueAsync();
            TestClass actualObject = (TestClass)JsonConvert.DeserializeObject(actualStringifiedJson, typeof(TestClass));

            Assert.Equal(expectedStringifiedJson, actualStringifiedJson);
            Assert.Equal(typeof(string), testValueProvider.Type);
            Assert.Equal(expectedObject.X, actualObject.X);
            Assert.Equal(expectedObject.Y, actualObject.Y);
        }

        [Fact]
        public async Task BasicDeliverEventArgs_NoConversion_Succeeds()
        {
            string expectedString = "someString";
            byte[] stringInBytes = Encoding.UTF8.GetBytes(expectedString);
            BasicDeliverEventArgs exceptedObject = new BasicDeliverEventArgs("tag", 1, false, "", "queue", null, stringInBytes);
            BasicDeliverEventArgsValueProvider testValueProvider = new BasicDeliverEventArgsValueProvider(exceptedObject, typeof(BasicDeliverEventArgs));

            BasicDeliverEventArgs actualObject = (BasicDeliverEventArgs)await testValueProvider.GetValueAsync();

            Assert.Equal(actualObject, exceptedObject);
            Assert.True(Object.ReferenceEquals(actualObject, exceptedObject));
            Assert.Equal(typeof(BasicDeliverEventArgs), testValueProvider.Type);
        }

        [Fact]
        public async Task ByteArray_Conversion_Succeeds()
        {
            string expectedString = "someString";
            byte[] stringInBytes = Encoding.UTF8.GetBytes(expectedString);
            BasicDeliverEventArgs exceptedObject = new BasicDeliverEventArgs("tag", 1, false, "", "queue", null, stringInBytes);
            BasicDeliverEventArgsValueProvider testValueProvider = new BasicDeliverEventArgsValueProvider(exceptedObject, typeof(byte[]));

            byte[] actualResult = (byte[])await testValueProvider.GetValueAsync();

            Assert.Equal(expectedString, Encoding.UTF8.GetString(actualResult));
            Assert.Equal(typeof(byte[]), testValueProvider.Type);
        }

        [Fact]
        public async Task ReadOnlyMemory_Byte_Conversion_Succeeds()
        {
            string expectedString = "someString";
            byte[] stringInBytes = Encoding.UTF8.GetBytes(expectedString);
            BasicDeliverEventArgs exceptedObject = new BasicDeliverEventArgs("tag", 1, false, "", "queue", null, stringInBytes);
            BasicDeliverEventArgsValueProvider testValueProvider = new BasicDeliverEventArgsValueProvider(exceptedObject, typeof(ReadOnlyMemory<byte>));

            ReadOnlyMemory<byte> actualResult = (ReadOnlyMemory<byte>)await testValueProvider.GetValueAsync();

            Assert.Equal(expectedString, Encoding.UTF8.GetString(actualResult.ToArray()));
            Assert.Equal(typeof(ReadOnlyMemory<byte>), testValueProvider.Type);
        }

        [Fact]
        public async Task NormalString_Conversion_Succeeds()
        {
            string expectedString = "someString";
            byte[] stringInBytes = Encoding.UTF8.GetBytes(expectedString);
            BasicDeliverEventArgs args = new BasicDeliverEventArgs("tag", 1, false, "", "queue", null, stringInBytes);
            BasicDeliverEventArgsValueProvider testValueProvider = new BasicDeliverEventArgsValueProvider(args, typeof(string));

            string actualString = (string)await testValueProvider.GetValueAsync();

            Assert.Equal(expectedString, actualString);
            Assert.Equal(typeof(string), testValueProvider.Type);
        }

        [Fact]
        public async Task InvalidFormat_Throws_JsonException()
        {
            string expectedString = "someString";
            byte[] objJsonBytes = Encoding.UTF8.GetBytes(expectedString);
            BasicDeliverEventArgs args = new BasicDeliverEventArgs("tag", 1, false, "", "queue", null, objJsonBytes);
            BasicDeliverEventArgsValueProvider testValueProvider = new BasicDeliverEventArgsValueProvider(args, typeof(TestClass));

            await Assert.ThrowsAsync<InvalidOperationException>(() => testValueProvider.GetValueAsync());
        }

        public class TestClass
        {
            public int X, Y;

            public TestClass(int x, int y)
            {
                X = x;
                Y = y;
            }
        }
    }
}
