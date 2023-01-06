using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ListSerializer
{
    file class SerializeParametrs
    {
        public SerializeParametrs(
            ListNode head,
            Stream stream,
            List<ListNode> allUniqueNodes
            )
        {
            Stream = stream;
            AllUniqueNodes = allUniqueNodes;
            Head= head;
        }

        public ListNode Head;
        public Stream Stream;
        public List<ListNode> AllUniqueNodes;
    }

    file class DeserializeParams
    {
        public DeserializeParams(
            Stream stream,
            List<ListNode> allUniqueNodes
            )
        {
            Stream = stream;
            AllUniqueNodes = allUniqueNodes;
        }

        public Stream Stream;
        public List<ListNode> AllUniqueNodes;
    }

    public class ListSerializerV2 : IListSerializer
    {
        /// <summary>
        /// Serializes all nodes in the list, including topology of the Random links, into stream
        /// </summary>
        public Task Serialize(ListNode head, Stream s)
        {
            return Task.Factory.StartNew(SerializeInternal, new SerializeParametrs(head, s, null));
        }

        [SkipLocalsInit]
        private void SerializeInternal(object obj)
        {
            var param = (SerializeParametrs)obj;
            var globalLinkId = 0;
            Span<byte> buffer = stackalloc byte[500];

            //package [linkBytes 4byte][length 4byte][data]
            //All stream packages...link datas 'idLinkNodeHead'...'idLinkNodeTail'
            param.Stream.Position = 0;
            param.Stream.Write(buffer.Slice(0, sizeof(int)));//reserved for count all unique nodes
            if(param.AllUniqueNodes == null)
            {
                param.AllUniqueNodes = new List<ListNode>();
            }
            else
            {
                param.AllUniqueNodes.Clear();
            }

            ListNode current = null;
            do
            {
                if (current == null)
                {
                    current = param.Head;
                }
                else
                {
                    current = current.Next;
                }

                //write id unique Node
                Unsafe.As<byte, int>(ref buffer[0]) = globalLinkId++;
                param.Stream.Write(buffer.Slice(0, sizeof(int)));

                if (current.Data == null)
                {
                    WriteNullRefferenceValue(in param.Stream);
                }
                else
                {
                    var size = current.Data.Length * sizeof(char);
                    Unsafe.As<byte, int>(ref buffer[0]) = size;
                    param.Stream.Write(buffer.Slice(0, sizeof(int)));

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
                                Buffer.MemoryCopy(pSource + offsetDestination, pDest, buffer.Length, partDataSize);
                                param.Stream.Write(buffer.Slice(0, partDataSize));

                                offsetDestination += partDataSize;
                                size -= partDataSize;
                            }
                        }
                    }
                }

                param.AllUniqueNodes.Add(current);
            }
            while (current.Next != null);

            //count all unique nodes
            long tempPosition = param.Stream.Position;
            param.Stream.Position = 0;
            Unsafe.As<byte, int>(ref buffer[0]) = param.AllUniqueNodes.Count;
            param.Stream.Write(buffer.Slice(0, sizeof(int)));
            param.Stream.Position = tempPosition;

            //write comma between packages and links
            WriteNullRefferenceValue(in param.Stream);

            //write links
            for (int i = 0; i < param.AllUniqueNodes.Count; i++)
            {
                ListNode item = param.AllUniqueNodes[i];
                if (item.Random == null)
                {
                    WriteNullRefferenceValue(in param.Stream);
                    continue;
                }

                int randomLinkId = EnumerationSearch(in param.AllUniqueNodes, in item);
                if (randomLinkId < 0)
                {
                    throw new ArgumentException("Algorithm error");
                }

                Unsafe.As<byte, int>(ref buffer[0]) = randomLinkId;
                param.Stream.Write(buffer.Slice(0, sizeof(int)));
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

        private int EnumerationSearch(in List<ListNode> uniqueNodes, in ListNode node)
        {
            for (int i = 0; i < uniqueNodes.Count; i++)
            {
                var currentNode = uniqueNodes[i];
                if (ReferenceEquals(node.Random, currentNode))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Deserializes the list from the stream, returns the head node of the list
        /// </summary>
        /// <exception cref="System.ArgumentException">Thrown when a stream has invalid data</exception>
        public Task<ListNode> Deserialize(Stream s)
        {
            return Task<ListNode>.Factory.StartNew(DeserializeInternal, new DeserializeParams(s, null));
        }

        private class DeserializeParams
        {
            public DeserializeParams(
                Stream stream,
                List<ListNode> allUniqueNodes
                )
            {
                Stream = stream;
                AllUniqueNodes = allUniqueNodes;
            }

            public Stream Stream;
            public List<ListNode> AllUniqueNodes;
        }

        [SkipLocalsInit]
        private ListNode DeserializeInternal(object obj)
        {
            var param = (DeserializeParams)obj;
            param.Stream.Position = 0;
            ListNode head = null;
            Span<byte> buffer = stackalloc byte[500];
            if (param.Stream.Read(buffer.Slice(0, sizeof(int))) != sizeof(int))
            {
                throw new ArgumentException("Unexpected end of stream, expect four bytes");
            }

            if(param.AllUniqueNodes == null)
            {
                param.AllUniqueNodes = new List<ListNode>(BitConverter.ToInt32(buffer.Slice(0, sizeof(int))));
            }
            else
            {
                param.AllUniqueNodes.Clear();
            }

            ListNode current = null;
            ListNode previous = null;

            while (param.Stream.Read(buffer.Slice(0, sizeof(int))) == sizeof(int))
            {
                var linkId = BitConverter.ToInt32(buffer.Slice(0, sizeof(int)));
                if (linkId == -1)//end unique nodes
                    break;

                current = new ListNode();
                param.AllUniqueNodes.Add(current);
                if (previous != null)
                {
                    previous.Next = current;
                    current.Previous = previous;
                }
                else
                {
                    head = current;
                }

                if (param.Stream.Read(buffer.Slice(0, sizeof(int))) != sizeof(int))
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
                                if (param.Stream.Read(buffer.Slice(0, partDataSize)) != partDataSize)
                                {
                                    throw new ArgumentException("Unexpected end of stream, expect bytes represent string data");
                                }

                                Buffer.MemoryCopy(pSource, pDest + offsetDestination, length, partDataSize);

                                offsetDestination += partDataSize;
                                length -= partDataSize;
                            }
                        }
                    }
                }

                previous = current;
            }

            int uniqueNodesIndex = 0;
            while (param.Stream.Read(buffer.Slice(0, sizeof(int))) == sizeof(int))
            {
                int linkIndex = BitConverter.ToInt32(buffer.Slice(0, sizeof(int)));
                if (linkIndex != -1)
                    param.AllUniqueNodes[uniqueNodesIndex].Random = param.AllUniqueNodes[linkIndex];

                uniqueNodesIndex++;
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
                var nodes = new List<ListNode>();
                var taskSerialize = Task.Factory.StartNew(SerializeInternal, new SerializeParametrs(head, stream, nodes));
                taskSerialize.Wait();
                var taskDeserialize = Task<ListNode>.Factory.StartNew(DeserializeInternal, new DeserializeParams(stream, nodes));
                taskDeserialize.Wait();
                return taskDeserialize.Result;
            }
        }
    }
}