using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ListSerializer
{
    public class ListSerializerV1 : IListSerializer
    {
        /// <summary>
        /// Serializes all nodes in the list, including topology of the Random links, into stream
        /// </summary>
        public Task Serialize(ListNode head, Stream s)
        {
            return Task.Factory.StartNew(() =>
            {
                SerializeInternal(in head, in s);
            });
        }

        [SkipLocalsInit]
        private void SerializeInternal(in ListNode head, in Stream s)
        {
            var dic = new Dictionary<ListNode, int>();
            var globalLinkId = 0;
            Span<byte> buffer = stackalloc byte[500];

            //package [linkBytes 4byte][length 4byte][data][randomLink 4 byte or 0 if null]
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

                var currentLinkId = GetLinkId(in dic, in current, ref globalLinkId);
                Unsafe.As<byte, int>(ref buffer[0]) = currentLinkId;
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

                if (current.Random == null)
                {
                    WriteNullRefferenceValue(in s);
                }
                else
                {
                    var linkRandom = GetLinkId(in dic, in current.Random, ref globalLinkId);
                    Unsafe.As<byte, int>(ref buffer[0]) = linkRandom;
                    s.Write(buffer.Slice(0, sizeof(int)));
                }
            }
            while (current.Next != null);

            //count all unique nodes
            long tempPosition = s.Position;
            s.Position = 0;
            Unsafe.As<byte, int>(ref buffer[0]) = globalLinkId;
            s.Write(buffer.Slice(0, sizeof(int)));
            s.Position = tempPosition;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteNullRefferenceValue(in Stream s)
        {
            s.WriteByte(byte.MaxValue);
            s.WriteByte(byte.MaxValue);
            s.WriteByte(byte.MaxValue);
            s.WriteByte(byte.MaxValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetLinkId(
            in Dictionary<ListNode, int> dictionary,
            in ListNode node,
            ref int linkCounter)
        {
            if (dictionary.TryGetValue(node, out var linkId))
            {
                return linkId;
            }
            else
            {
                linkCounter++;
                dictionary.Add(node, linkCounter);

                return linkCounter;
            }
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
        private ListNode DeserializeInternal(object obj)
        {
            var s = (Stream)obj;
            s.Position = 0;
            ListNode head = null;
            Span<byte> buffer = stackalloc byte[500];
            if (s.Read(buffer.Slice(0, sizeof(int))) != sizeof(int))
            {
                throw new ArgumentException("Unexpected end of stream, expect four bytes");
            }

            var linkDictionary = new Dictionary<int, ListNode>(BitConverter.ToInt32(buffer.Slice(0, sizeof(int))));
            var listNeedSetRandom = new List<(int linkId, int randomLinkId)>();

            ListNode current = null;
            ListNode previous = null;

            while (s.Read(buffer.Slice(0, sizeof(int))) == sizeof(int))
            {
                var linkId = BitConverter.ToInt32(buffer.Slice(0, sizeof(int)));

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

                if (!linkDictionary.ContainsKey(linkId))
                {
                    linkDictionary.Add(linkId, current);
                }

                if (s.Read(buffer.Slice(0, sizeof(int))) != sizeof(int))
                {
                    throw new ArgumentException("not find length data in stream");
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

                if (s.Read(buffer.Slice(0, sizeof(int))) != sizeof(int))
                {
                    throw new ArgumentException();
                }

                var randomLink = BitConverter.ToInt32(buffer.Slice(0, sizeof(int)));
                if (randomLink != -1)//means not null
                {
                    if (linkDictionary.TryGetValue(randomLink, out var findNode))
                    {
                        current.Random = findNode;
                    }
                    else
                    {
                        listNeedSetRandom.Add((linkId, randomLink));
                    }
                }

                previous = current;
            }

            foreach (var item in listNeedSetRandom)
            {
                if (linkDictionary.TryGetValue(item.randomLinkId, out var findNodeRandom))
                {
                    linkDictionary[item.linkId].Random = findNodeRandom;
                }
                else
                {
                    throw new ArgumentException();
                }
            }

            return head;
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