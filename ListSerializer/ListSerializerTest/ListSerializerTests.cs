using Common;
using NUnit.Framework;

namespace ListSerializerTest
{
    [TestFixture]
    public class ListSerializerTests
    {
        [Test]
        public void DeepCopyTest()//simple test, manual check nodes
        {
            var head = new ListNode {Data = "Head"};
            var next = new ListNode {Data = "Next ¹1" };

            head.Next = next;
            next.Previous = head;

            var next2 = new ListNode {Data = "Next ¹2"};
            next.Next = next2;
            next2.Previous = next;
            next2.Random = head;

            var tail = new ListNode {Data = "Tail"};
            next2.Next = tail;
            tail.Previous = next2;
            tail.Random = next;

            head.Random = tail;

            var listSerializer = new ListSerializer.ListSerializer();
            var copyTask = listSerializer.DeepCopy(head);
            copyTask.Wait();

            var copy = copyTask.Result;

            Assert.AreEqual("Head", copy.Data);
            Assert.IsNull(copy.Previous);
            Assert.AreEqual("Tail", copy.Next.Next.Next.Data);
            Assert.IsNull(copy.Next.Next.Next.Next);//tail.next always null

            Assert.AreEqual("Next ¹1", copy.Next.Data);
            Assert.AreEqual(copy, copy.Next.Next.Random);

            copy.Next.Next.Random = copy;
            Assert.AreSame(copy.Random, copy.Next.Next.Next);
            Assert.AreSame(copy.Next.Next.Next.Random, copy.Next);
        }
    }
}