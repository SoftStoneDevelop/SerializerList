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
            var head = ListNodeInstanceHelper.CreateRandomListNode(5732);

            var listSerializer = new ListSerializer.ListSerializer();
            var copyTask = listSerializer.DeepCopy(head);
            copyTask.Wait();

            var copy = copyTask.Result;

            ListEqualHelper.DeepEqual(head, copy);
        }
    }
}