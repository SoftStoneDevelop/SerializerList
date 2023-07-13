using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ListSerializer
{
    public class ListSerializerV2 : IListSerializer
    {
        /// <summary>
        /// Serializes all nodes in the list, including topology of the Random links, into stream
        /// </summary>
        public Task Serialize(ListNode head, Stream s)
        {
            return Task.Factory.StartNew(() =>
            {
                SerializeInternal(in head, s);
            });
        }

        [SkipLocalsInit]
        private void SerializeInternal(in ListNode head, Stream s)
        {
            var globalLinkId = 0;
            Span<byte> buffer = stackalloc byte[500];

            //package [linkBytes 4byte][length 4byte][data]
            //All stream packages...link datas 'idLinkNodeHead'...'idLinkNodeTail'
            s.Position = 0;
            s.Write(buffer.Slice(0, sizeof(int)));//reserved for count all unique nodes

            ListNode current = null;
            do
            {
                if (current == null)
                {
                    current = head;
                }
                else
                {
                    current = current.Next;
                }

                //write id unique Node
                Unsafe.As<byte, int>(ref buffer[0]) = globalLinkId++;
                s.Write(buffer.Slice(0, sizeof(int)));

                if (current.Data == null)
                {
                    WriteNullRefferenceValue(in s);
                }
                else
                {
                    var size = current.Data.Length * sizeof(char);
                    Unsafe.As<byte, int>(ref buffer[0]) = size;
                    s.Write(buffer.Slice(0, sizeof(int)));

                    int partDataSize = 0;
                    int offsetDestination = 0;
                    unsafe
                    {
                        fixed (byte* pDest = &buffer[0])
                        fixed (char* pSource = current.Data)
                        {
                            while (size > 0)
                            {
                                partDataSize = size > buffer.Length ? buffer.Length : size;
                                Buffer.MemoryCopy(pSource + (offsetDestination / sizeof(char)), pDest, buffer.Length, partDataSize);
                                s.Write(buffer.Slice(0, partDataSize));

                                offsetDestination += partDataSize;
                                size -= partDataSize;
                            }
                        }
                    }
                }
            }
            while (current.Next != null);

            //write comma between packages and links
            WriteNullRefferenceValue(in s);

            var uniqueNodesCount = globalLinkId;
            globalLinkId = -1;
            //write uniqueNodesCount
            long tempPosition = s.Position;
            s.Position = 0;
            Unsafe.As<byte, int>(ref buffer[0]) = uniqueNodesCount;
            s.Write(buffer.Slice(0, sizeof(int)));
            s.Position = tempPosition;

            //write links
            current = null;

            int tenPercent = (int)Math.Round(Environment.ProcessorCount * 0.1, 0);
            var logicalProcessors = tenPercent > 2 ? Environment.ProcessorCount - tenPercent : Environment.ProcessorCount;
            if (uniqueNodesCount <= 200)
            {
                do
                {
                    globalLinkId++;
                    if (current == null)
                    {
                        current = head;
                    }
                    else
                    {
                        current = current.Next;
                    }

                    if (current.Random == null)
                    {
                        WriteNullRefferenceValue(in s);
                        continue;
                    }

                    var randomLinkId = FindRealIdNode(in current, in head);
                    Unsafe.As<byte, int>(ref buffer[0]) = globalLinkId;
                    s.Write(buffer.Slice(0, sizeof(int)));

                    Unsafe.As<byte, int>(ref buffer[0]) = randomLinkId;
                    s.Write(buffer.Slice(0, sizeof(int)));
                }
                while (current.Next != null);
            }
            else
            {
                var itemsInPackage = ((int)(uniqueNodesCount / logicalProcessors));
                var threadsCount = (int)(uniqueNodesCount / itemsInPackage) > logicalProcessors ? logicalProcessors : (int)(uniqueNodesCount / itemsInPackage);

                globalLinkId = 0;
                current = head;
                var tasksFindingLinks = new List<Task<bool>>(threadsCount);
                var currentThread = 0;
                var skeepCount = 0;
                var lockStream = new object();

                while ((globalLinkId < uniqueNodesCount - 1) && currentThread < threadsCount)
                {
                    while (skeepCount > 0)
                    {
                        skeepCount--;
                        if (current == null)
                        {
                            current = head;
                        }
                        else
                        {
                            current = current.Next;
                        }
                        globalLinkId++;
                    }

                    currentThread++;

                    if (currentThread < threadsCount)
                    {
                        var parametrs = new FindLinksParam(current, head, itemsInPackage, globalLinkId, s, lockStream);
                        tasksFindingLinks.Add(Task.Factory.StartNew(FindLinks, parametrs));
                    }
                    else
                    {
                        var parametrs =
                            new FindLinksParam(
                                current,
                                head,
                                uniqueNodesCount - (itemsInPackage * (currentThread - 1)),
                                globalLinkId,
                                s,
                                lockStream
                                );
                        tasksFindingLinks.Add(Task.Factory.StartNew(FindLinks, parametrs));
                    }
                    skeepCount = itemsInPackage;
                }

                Task.WaitAll(tasksFindingLinks.ToArray());
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteNullRefferenceValue(in Stream s)
        {
            s.WriteByte(byte.MaxValue);
            s.WriteByte(byte.MaxValue);
            s.WriteByte(byte.MaxValue);
            s.WriteByte(byte.MaxValue);
        }

        [SkipLocalsInit]
        private class FindLinksParam
        {
            public FindLinksParam(
                ListNode node,
                ListNode head,
                int steps,
                int currentId,
                Stream s,
                object lockStream
                )
            {
                Head = head;
                Node = node;
                Steps = steps;
                CurrentId = currentId;
                Stream = s;
                LockStream = lockStream;
            }

            public ListNode Node;
            public ListNode Head;
            public int Steps;
            public int CurrentId;
            public Stream Stream;
            public object LockStream;
        }

        [SkipLocalsInit]
        bool FindLinks(object paramObj)
        {
            var param = (FindLinksParam)paramObj;
            Span<byte> buffer = stackalloc byte[sizeof(int) * 2 * 10];
            byte bufferOffset = 0;

            ListNode current = null;
            int randomLinkId = -1;
            do
            {
                if (current == null)
                {
                    current = param.Node;
                }
                else
                {
                    current = current.Next;
                }

                if (current.Random == null)
                {
                    Unsafe.As<byte, int>(ref buffer[bufferOffset]) = param.CurrentId;
                    Unsafe.As<byte, int>(ref buffer[bufferOffset + sizeof(int)]) = -1;
                    bufferOffset += sizeof(int) * 2;

                    if (bufferOffset == buffer.Length)
                    {
                        bufferOffset = 0;

                        int i = 0;
                        while (true)
                        {
                            if (Monitor.TryEnter(param.LockStream))
                            {
                                try
                                {
                                    param.Stream.Write(buffer);
                                    break;
                                }
                                finally
                                {
                                    Monitor.Exit(param.LockStream);
                                }
                            }
                            else
                            {
                                if (i++ == 5000)
                                {
                                    lock (param.LockStream)
                                    {
                                        param.Stream.Write(buffer);
                                    }
                                    break;
                                }
                            }
                        }
                    }

                    param.CurrentId++;
                    param.Steps--;
                    continue;
                }

                randomLinkId = FindRealIdNode(in current, in param.Head);

                Unsafe.As<byte, int>(ref buffer[bufferOffset]) = param.CurrentId;
                Unsafe.As<byte, int>(ref buffer[bufferOffset + sizeof(int)]) = randomLinkId;
                bufferOffset += sizeof(int) * 2;

                if (bufferOffset == buffer.Length)
                {
                    bufferOffset = 0;
                    int i = 0;
                    while (true)
                    {
                        if (Monitor.TryEnter(param.LockStream))
                        {
                            try
                            {
                                param.Stream.Write(buffer);
                                break;
                            }
                            finally
                            {
                                Monitor.Exit(param.LockStream);
                            }
                        }
                        else
                        {
                            if (i++ == 5000)
                            {
                                lock (param.LockStream)
                                {
                                    param.Stream.Write(buffer);
                                }
                                break;
                            }
                        }
                    }
                }

                param.CurrentId++;
                param.Steps--;
            }
            while (param.Steps > 0);

            if (bufferOffset != 0)
            {
                int i = 0;
                while (true)
                {
                    if (Monitor.TryEnter(param.LockStream))
                    {
                        try
                        {
                            param.Stream.Write(buffer.Slice(0, bufferOffset));
                            break;
                        }
                        finally
                        {
                            Monitor.Exit(param.LockStream);
                        }
                    }
                    else
                    {
                        if (i++ == 5000)
                        {
                            lock (param.LockStream)
                            {
                                param.Stream.Write(buffer.Slice(0, bufferOffset));
                            }
                            break;
                        }
                    }
                }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int FindRealIdNode(in ListNode node, in ListNode head)
        {
            int currentLoopId = 0;
            ListNode current = null;
            do
            {
                if (current == null)
                {
                    current = head;
                }
                else
                {
                    current = current.Next;
                    currentLoopId++;
                }

                if (!ReferenceEquals(node.Random, current))
                    continue;

                return currentLoopId;
            }
            while (current.Next != null);

            return -1;
        }

        /// <summary>
        /// Deserializes the list from the stream, returns the head node of the list
        /// </summary>
        /// <exception cref="System.ArgumentException">Thrown when a stream has invalid data</exception>
        public Task<ListNode> Deserialize(Stream s)
        {
            return Task<ListNode>.Factory.StartNew(DeserializeInternal, s);
        }

        [SkipLocalsInit]
        public ListNode DeserializeInternal(object obj)
        {
            var s = (Stream)obj;
            s.Position = 0;
            ListNode head = null;
            Span<byte> buffer = stackalloc byte[1000];

            if (s.Read(buffer.Slice(0, sizeof(int))) != sizeof(int))
            {
                throw new ArgumentException("Unexpected end of stream, expect four bytes");
            }

            ListNode current = null;
            ListNode previous = null;

            while (s.Read(buffer.Slice(0, sizeof(int))) == sizeof(int))
            {
                var linkId = BitConverter.ToInt32(buffer.Slice(0, sizeof(int)));
                if (linkId == -1)//end unique nodes
                    break;

                current = new ListNode();
                if (previous != null)
                {
                    previous.Next = current;
                    current.Previous = previous;
                }
                else
                {
                    head = current;
                }

                if (s.Read(buffer.Slice(0, sizeof(int))) < sizeof(int))
                {
                    throw new ArgumentException("Unexpected end of stream, expect four bytes");
                }

                var length = BitConverter.ToInt32(buffer.Slice(0, sizeof(int)));
                if (length != -1)
                {
                    int partDataSize = 0;
                    int offsetDestination = 0;
                    current.Data = new string(' ', length / sizeof(char));

                    unsafe
                    {
                        fixed (byte* pSource = &buffer[0])
                        fixed (char* pDest = current.Data)
                        {
                            while (length > 0)
                            {
                                partDataSize = length > buffer.Length ? buffer.Length : length;
                                if (s.Read(buffer.Slice(0, partDataSize)) != partDataSize)
                                {
                                    throw new ArgumentException("Unexpected end of stream, expect bytes represent string data");
                                }

                                Buffer.MemoryCopy(pSource, pDest + (offsetDestination / sizeof(char)), length, partDataSize);

                                offsetDestination += partDataSize;
                                length -= partDataSize;
                            }
                        }
                    }
                }

                previous = current;
            }

            while (s.Read(buffer.Slice(0, sizeof(int))) == sizeof(int))
            {
                int index = BitConverter.ToInt32(buffer.Slice(0, sizeof(int)));

                if (s.Read(buffer.Slice(0, sizeof(int))) != sizeof(int))
                    throw new ArgumentException("Unexpected end of stream, expect four bytes");

                int linkIndex = BitConverter.ToInt32(buffer.Slice(0, sizeof(int)));
                if (linkIndex != -1)
                    GetByIndex(in head, index).Random = GetByIndex(in head, linkIndex);
            }

            return head;
        }

        private ListNode GetByIndex(in ListNode head, int index)
        {
            var currentIndx = 0;
            ListNode current = null;
            do
            {
                if (current == null)
                {
                    current = head;
                }
                else
                {
                    current = current.Next;
                    currentIndx++;
                }

                if(currentIndx == index)
                {
                    return current;
                }
            }
            while (current.Next != null);

            throw new Exception("Out of range");
        }

        /// <summary>
        /// Makes a deep copy of the list, returns the head node of the list 
        /// </summary>
        public Task<ListNode> DeepCopy(ListNode head)
        {
            return Task<ListNode>.Factory.StartNew(DeepCopyInternal, head);
        }

        private ListNode DeepCopyInternal(object obj)
        {
            var head = (ListNode)obj;
            using (var stream = new MemoryStream())
            {
                var taskSerialize = Serialize(head, stream);
                taskSerialize.Wait();
                var taskDeserialize = Deserialize(stream);
                taskDeserialize.Wait();
                return taskDeserialize.Result;
            }
        }
    }
}