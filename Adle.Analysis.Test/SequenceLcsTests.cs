using Microsoft.VisualStudio.TestTools.UnitTesting;
using SequentialPattern;
using System.Collections.Generic;

namespace Adle.Analysis.Test
{
    [TestClass]
    public class SequenceLcsTests
    {
        private static Sequence<string> BuildSequence(params string[] items)
        {
            var sequence = new Sequence<string>();
            foreach (var item in items)
            {
                sequence.AddItemAsItemSet(item);
            }
            return sequence;
        }

        [TestMethod]
        public void CalculateLCS_IdenticalSequences_ReturnsFullLength()
        {
            var a = BuildSequence("1P", "1I", "2P", "2I");
            var b = BuildSequence("1P", "1I", "2P", "2I");

            Assert.AreEqual(4, a.CalculateLCS(b));
        }

        [TestMethod]
        public void CalculateLCS_NoCommonItems_ReturnsZero()
        {
            var a = BuildSequence("1P", "1I");
            var b = BuildSequence("2P", "2I");

            Assert.AreEqual(0, a.CalculateLCS(b));
        }

        [TestMethod]
        public void CalculateLCS_PartialOverlap_ReturnsCommonSubsequenceLength()
        {
            // common subsequence: 1P -> 2P -> 3P
            var a = BuildSequence("1P", "1I", "2P", "3P");
            var b = BuildSequence("1P", "2P", "2I", "3P");

            Assert.AreEqual(3, a.CalculateLCS(b));
        }

        [TestMethod]
        public void FindLCS_ReturnsTheCommonItems()
        {
            var a = BuildSequence("1P", "1I", "2P");
            var b = BuildSequence("1P", "2P", "2I");

            List<string> lcs = a.FindLCS(b);

            CollectionAssert.AreEqual(new List<string> { "1P", "2P" }, lcs);
        }

        [TestMethod]
        public void GetNextItemValue_ReturnsItemFollowingTheGivenOne()
        {
            var sequence = BuildSequence("1P", "1I", "2P");

            Assert.AreEqual("1I", sequence.GetNextItemValue("1P"));
            Assert.AreEqual("2P", sequence.GetNextItemValue("1I"));
        }
    }
}
