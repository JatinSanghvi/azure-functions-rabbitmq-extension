﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ.Tests
{
    public class PocoToBytesConverterTests
    {
        [Fact]
        public void Converts_String_Correctly()
        {
            TestClass sampleObj = new TestClass(1, 1);
            string res = JsonConvert.SerializeObject(sampleObj);
            byte[] expectedRes = Encoding.UTF8.GetBytes(res);

            PocoToBytesConverter<TestClass> converter = new PocoToBytesConverter<TestClass>();
            byte[] actualRes = converter.Convert(sampleObj).ToArray();

            Assert.Equal(expectedRes, actualRes);
        }

        [Fact]
        public void NullString_Throws_Exception()
        {
            PocoToBytesConverter<TestClass> converter = new PocoToBytesConverter<TestClass>();
            Assert.Throws<ArgumentNullException>(() => converter.Convert(null));
        }

        public class TestClass
        {
            private readonly int _x;
            private readonly int _y;

            public TestClass(int x, int y)
            {
                _x = x;
                _y = y;
            }

            public int X => _x;

            public int Y => _y;
        }
    }
}
