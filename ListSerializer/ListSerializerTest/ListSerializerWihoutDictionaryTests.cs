using Common;
using NUnit.Framework;

namespace ListSerializerTest
{
    [TestFixture]
    public class ListSerializerWihoutDictionaryTests
    {
        [Test]
        public void DeepCopyTest()//simple test, manual check nodes
        {
            var head = new ListNode {Data = "Head"};
            var next = new ListNode {Data = "Next" };

            head.Next = next;
            next.Previous = head;

            var next2 = new ListNode {Data = "Next"};
            next.Next = next2;
            next2.Previous = next;
            next2.Random = head;

            var next3 = new ListNode();
            next2.Next = next3;
            next3.Previous = next2;

            var tail = new ListNode {Data = "Tail"};
            next3.Next = tail;
            tail.Previous = next3;
            tail.Random = next;

            head.Random = tail;

            var listSerializer = new ListSerializer.ListSerializerWihoutDictionary();
            var copyTask = listSerializer.DeepCopy(head);
            copyTask.Wait();

            var copy = copyTask.Result;

            Assert.AreEqual("Head", copy.Data);
            Assert.IsNull(copy.Previous);
            Assert.AreEqual("Tail", copy.Next.Next.Next.Next.Data);
            Assert.IsNull(copy.Next.Next.Next.Next.Next);//tail.next always null

            Assert.AreEqual("Next", copy.Next.Data);

            Assert.AreSame(copy.Random, copy.Next.Next.Next.Next);
            Assert.AreSame(copy.Next.Next.Next.Next.Random, copy.Next);

            Assert.AreNotSame(copy.Next.Next.Next.Random, copy.Next);
            Assert.AreNotSame(copy.Next, copy.Next.Next);
            Assert.IsNull(copy.Next.Next.Next.Random);
        }
    }
}