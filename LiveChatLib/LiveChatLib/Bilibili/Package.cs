using LiteDB;
using LiveChatLib.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LiveChatLib.Bilibili
{
    public class Package
    {
        public Guid _id { get; set; }
        public int Length { get { return HeadLength + Body.Length; } }
        public short HeadLength { get; private set; }
        public short ProtoVer { get; private set; }
        public MsgType MessageType { get; private set; }
        public int Sequence { get; private set; }
        public byte[] Body { get; private set; }

        [BsonIgnore]
        public Encoding BodyEncoding { get; set; }
        public string Content { get => BodyEncoding.GetString(Body); set => Body = BodyEncoding.GetBytes(value); }
        public bool MultiMessage { get; private set; } = false;

        public Package()
        {
            BodyEncoding = Encoding.UTF8;
        }
        public static IEnumerable<Package> ExtractPackage(MsgType msgType, byte[] body, Encoding encoding = null, short protover = 0x1, short headlen = 0x10, int sequence = 0x1)
        {
            var list = new List<Package>();
            var package = new Package();
            package.HeadLength = headlen;
            package.ProtoVer = protover;
            package.MessageType = msgType;
            package.Sequence = sequence;
            if (encoding == null)
            {
                package.BodyEncoding = Encoding.UTF8;
            }
            else
            {
                package.BodyEncoding = encoding;
            }
            if (protover == 2 && msgType == MsgType.Command)
            {
                var deflateStream = new MemoryStream(body, 2, body.Length-2);
                var extracted = DeflateHelper.Extract(deflateStream);
                using(var ms = new MemoryStream(extracted))
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    using (var reader = new BinaryReader(ms))
                    {
                        do
                        {
                            var p = new Package()
                            {
                                HeadLength = headlen,
                                ProtoVer = protover,
                                Sequence = sequence
                            };
                            var msgLength = reader.ReadBytes(4).ByteToInt32(true);
                            var msgHeaderLength = reader.ReadBytes(2).ByteToInt16(true);
                            var msgVer = reader.ReadBytes(2).ByteToInt16(true);
                            var msgAc = reader.ReadBytes(4).ByteToInt32(true);
                            var msgParam = reader.ReadBytes(4).ByteToInt32(true);
                            switch (msgAc)
                            {
                                case 3:
                                    p.MessageType = MsgType.ServerHeart;
                                    break;

                                case 5:
                                    p.MessageType = MsgType.Command;
                                    break;

                                case 8:
                                    p.MessageType = MsgType.Auth;
                                    break;
                            }
                            p.Body = reader.ReadBytes((int)(msgLength - 16));
                            list.Add(p);
                        } while (ms.Length - ms.Position > 16);
                    }
                    return list;
                }
            }
            else
            {
                package.Body = body;
            }
            return new[] { package };
        }

        public Package(MsgType msgType, string content, Encoding encoding = null, short protover = 0x1, short headlen = 0x10, int sequence = 0x1)
        {
            HeadLength = headlen;
            ProtoVer = protover;
            MessageType = msgType;
            Sequence = sequence;
            if (encoding == null)
            {
                BodyEncoding = Encoding.UTF8;
            }
            else
            {
                BodyEncoding = encoding;
            }
            Content = content;
        }

        public byte[] ToByteArray()
        {
            var ms = new MemoryStream();
            using(var writer = new BinaryWriter(ms))
            {
                writer.Write(Length.ToByteArray(true));
                writer.Write(HeadLength.ToByteArray(true));
                writer.Write(ProtoVer.ToByteArray(true));
                writer.Write(((int)MessageType).ToByteArray(true));
                writer.Write(Sequence.ToByteArray(true));
                writer.Write(Body);
                writer.Flush();
            }
            var result = ms.ToArray();
            return result;
        }

        public static IEnumerable<Package> LoadFromByteArray(byte[] package)
        {
            var ms = new MemoryStream(package);
            using(var reader = new BinaryReader(ms))
            {
                var length = reader.ReadBytes(4).ByteToInt32(true);
                if(length != package.Length)
                {
                    throw new Exception("LOADING PACKAGE FAILED: The length of the package does not match with its header.");
                }
                var headlength = reader.ReadBytes(2).ByteToInt16(true);
                var protover = reader.ReadBytes(2).ByteToInt16(true);
                var command = reader.ReadBytes(4).ByteToInt32(true);
                var sequence = reader.ReadBytes(4).ByteToInt32(true);
                ms.Seek(headlength, SeekOrigin.Begin);
                var body = reader.ReadBytes(length - headlength);
                var result = ExtractPackage((MsgType)command, body, Encoding.UTF8, protover, headlength, sequence);

                return result;
            }
        }
    }
    public enum MsgType
    {
        ClientHeart = 0x02,
        Renqi = 0x03,
        Command = 0x05,
        Auth = 0x07,
        ServerHeart = 0x08,
    }
}
