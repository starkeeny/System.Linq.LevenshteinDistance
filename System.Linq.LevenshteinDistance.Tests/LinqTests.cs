using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq.LevenshteinDistance.Tests
{
    [TestClass]
    public class LinqTests
    {
        [TestMethod]
        public void GroupByTest()
        {
            var rawData = new string[] { "a", "a" };

            var data = rawData.GroupBy(keySelector: item => item);

            Assert.AreEqual(1, data.Count());
        }

        [TestMethod]
        public void GroupByWithDataTest()
        {
            var rawData = new string[] { "a", "a" };

            var data = rawData.GroupBy(keySelector: item => item,
                                       elementSelector: item => new { Data = item },
                                       resultSelector: (key, elements) => new { Key = key, List = elements });

            Assert.AreEqual(1, data.Count());
            foreach (var item in data)
            {
                Assert.AreEqual("a", item.Key);
                item.List.ToList().ForEach(x => Assert.AreEqual("a", x.Data));
                Assert.AreEqual(2, item.List.ToList().Count());
            }
        }

        [TestMethod]
        public void GroupByWithLDTest()
        {
            var rawData = new string[] { "a", "a" };

            var data = rawData.GroupBy(new LevenshteinDistanceOptions(distance: 0));

            Assert.AreEqual(1, data.Count());
            Assert.AreEqual("a", data.ToList().First().Key);
            Assert.AreEqual(2, data.ToList().First().ToList().Count());
            Assert.AreEqual("a", data.ToList().First().ToList().Skip(0).First());
            Assert.AreEqual("a", data.ToList().First().ToList().Skip(1).First());
        }

        [TestMethod]
        public void GroupByWithLDAndToleranceWithExplicitOptionTest()
        {
            var rawData = new string[] { "a", "b" };

            var data = rawData.GroupBy(new LevenshteinDistanceOptions(unit: LevenshteinDistanceUnit.Absolute, distance: 1));

            Assert.AreEqual(1, data.Count());
            Assert.AreEqual("a", data.ToList().First().Key);
            Assert.AreEqual(2, data.ToList().First().ToList().Count());
            Assert.AreEqual("a", data.ToList().First().ToList().Skip(0).First());
            Assert.AreEqual("b", data.ToList().First().ToList().Skip(1).First());
        }

        [TestMethod]
        public void GroupByWithLDAndToleranceTest()
        {
            var rawData = new string[] { "a", "b" };

            var data = rawData.GroupBy(new LevenshteinDistanceOptions(distance: 1));

            Assert.AreEqual(1, data.Count());
            Assert.AreEqual("a", data.ToList().First().Key);
            Assert.AreEqual(2, data.ToList().First().ToList().Count());
            Assert.AreEqual("a", data.ToList().First().ToList().Skip(0).First());
            Assert.AreEqual("b", data.ToList().First().ToList().Skip(1).First());
        }

        [TestMethod]
        public void GroupByWithLDAndToleranceInPercentTest()
        {
            var rawData = new string[] { "aaaa", "aaab" };

            var data = rawData.GroupBy(new LevenshteinDistanceOptions(unit: LevenshteinDistanceUnit.Percentage, distance: 25));

            Assert.AreEqual(1, data.Count());
            Assert.AreEqual("aaaa", data.ToList().First().Key);
            Assert.AreEqual(2, data.ToList().First().ToList().Count());
            Assert.AreEqual("aaaa", data.ToList().First().ToList().Skip(0).First());
            Assert.AreEqual("aaab", data.ToList().First().ToList().Skip(1).First());
        }

        [TestMethod]
        public void GroupByWithLDTimeoutMessages()
        {
            var rawData = new string[] {
                "timeout after 3 seconds",
                "timeout after 13 seconds",
                "timeout after 33 seconds",
                "timeout after 25 seconds",
            };

            var data = rawData.GroupBy(new LevenshteinDistanceOptions(2));
            Assert.AreEqual(1, data.Count());
            Assert.AreEqual(4, data.First().ToList().Count());
        }

        [TestMethod]
        public void GroupByWithLDDifferentExceptions()
        {
            var rawData = new string[] {
                "timeout after 03 seconds",
                "timeout after 13 seconds",
                "timeout after 33 seconds",
                "permission denied for drive C",
                "permission denied for drive D",
                "permission denied for drive E",
                "timeout after 25 seconds",
            };

            var data = rawData.GroupBy(new LevenshteinDistanceOptions(2));
            Assert.AreEqual(2, data.Count());
            Assert.AreEqual(4, data.First(x => x.Key.StartsWith("timeout")).ToList().Count());
            Assert.AreEqual(3, data.First(x => x.Key.StartsWith("permission")).ToList().Count());
        }

        [TestMethod]
        public void GroupByWithLDDifferentGuids()
        {
            byte m = byte.MaxValue;
            var rawData = new string[] {
                $"permission denied for item {new Guid(0,0,0,0,0,0,0,0,0,0,0)}", // e.g.: b94d02c5-49a1-4be0-a7dd-914d4346d125
                $"permission denied for item {new Guid(new byte[]{m,m,m,m,m,m,m,m,m,m,m,m,m,m,m,m, }).ToString()}",
            };

            var data = rawData.GroupBy(new LevenshteinDistanceOptions(32));
            Assert.AreEqual(1, data.Count());
        }


        [TestMethod]
        public void GroupByWithLDExceptionBasedOnTaskException()
        {
            var messages = new List<string>();
            CancellationTokenSource source10 = new CancellationTokenSource(TimeSpan.FromMilliseconds(10));
            CancellationTokenSource source40 = new CancellationTokenSource(TimeSpan.FromMilliseconds(40));

            try
            {
                Task.Run(() => Threading.Thread.Sleep(10000), source10.Token).Wait();
            }
            catch (Exception exc)
            {
                messages.Add(exc.Message);
            }

            try
            {
                Task.Run(() => Threading.Thread.Sleep(10000), source40.Token).Wait();
            }
            catch (Exception exc)
            {
                messages.Add(exc.Message);
            }

            var group = messages.GroupBy(new LevenshteinDistanceOptions(5));
            Assert.AreEqual(1, group.Count());
            Debug.Print(group.First().Key);
        }

        [TestMethod]
        public void GroupByWithRemovedNumber()
        {
            var rawData = new string[] {
                "timeout after 03 seconds",
                "timeout after 13 seconds",
                "timeout after 33 seconds",
                "timeout after 25 seconds",
            };

            var data = rawData.GroupBy(new LevenshteinDistanceOptions(LevenshteinDistanceUnit.Absolute, 0, removeAllDigits: true));
            Assert.AreEqual(1, data.Count());
            Debug.Print(data.First().Key);
        }

        [TestMethod]
        public void GroupByWithRemovedNumber_NegativeFloats()
        {
            var rawData = new string[] {
                "timeout delta 3.000,00 seconds",
                "timeout delta -3,87 seconds",
                "timeout delta 3.6 seconds",
                "timeout delta -5,0 seconds",
            };

            var data = rawData.GroupBy(new LevenshteinDistanceOptions(LevenshteinDistanceUnit.Absolute, 0, removeAllDigits: true));
            Assert.AreEqual(1, data.Count());
            Debug.Print(data.First().Key);
        }

        [TestMethod]
        public void GroupByWithRemovedGuid()
        {
            var rawData = new string[] {
                $"timeout for client {Guid.NewGuid()}",
                $"timeout for client {Guid.NewGuid()}",
                $"timeout for client {Guid.NewGuid()}",
                $"timeout for client F9168C5E-CEB2-4faa-B6BF-329BF39FA1E4",
            };

            var data = rawData.GroupBy(new LevenshteinDistanceOptions(LevenshteinDistanceUnit.Absolute, 0,
                removeStandardFormattedGuid: true));
            Assert.AreEqual(1, data.Count());
            Debug.Print(data.First().Key);
        }

        [TestMethod]
        public void GroupByWithRemovedGuidAndNumber()
        {
            var rawData = new string[] {
                $"timeout for client {Guid.NewGuid()} on server 1",
                $"timeout for client {Guid.NewGuid()} on server 1",
                $"timeout for client {Guid.NewGuid()} on server 1",
                $"timeout for client {Guid.NewGuid()} on server 1",
            };

            var data = rawData.GroupBy(new LevenshteinDistanceOptions(LevenshteinDistanceUnit.Absolute, 0,
                removeStandardFormattedGuid: true, removeAllDigits: true));
            Assert.AreEqual(1, data.Count());
        }
    }
}
