using Common;
using NUnit.Framework;

namespace ListSerializerTest
{
    [TestFixture]
    public class ListSerializerTests
    {
        [Test]
        public void DeepCopyTest()
        {
            var head = ListNodeInstanceHelper.CreateRandomListNode(5000);

            var listSerializer = new ListSerializer.ListSerializerV1();
            var copyTask = listSerializer.DeepCopy(head);
            copyTask.Wait();

            var copy = copyTask.Result;

            ListEqualHelper.DeepEqual(head, copy);
        }
    }
}