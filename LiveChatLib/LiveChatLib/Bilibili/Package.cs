using LiveChatLib.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LiveChatLib.Bilibili
{
    public class Package
    {
        private byte[] _body;
        public int Length { get { return HeadLength + Body.Length; } }
        public short HeadLength { get; private set; }
        public short ProtoVer { get; private set; }
        public MsgType MessageType { get; private set; }
        public int Sequence { get; private set; }
        public byte[] Body { get => _body; set => _body = value; }
        public Encoding BodyEncoding { get; set; }
        public string Content { get => BodyEncoding.GetString(Body); set => _body = BodyEncoding.GetBytes(value); }

        public Package(MsgType msgType, byte[] body, Encoding encoding = default(Encoding), short protover = 0x1, short headlen = 0x10, int sequence = 0x1)
        {
            HeadLength = headlen;
            ProtoVer = protover;
            MessageType = msgType;
            Sequence = sequence;
            if (encoding == default(Encoding))
            {
                BodyEncoding = Encoding.UTF8;
            }
            else
            {
                BodyEncoding = encoding;
            }
            Body = body;
        }

        public Package(MsgType msgType, string content, Encoding encoding = default(Encoding), short protover = 0x1, short headlen = 0x10, int sequence = 0x1)
        {
            HeadLength = headlen;
            ProtoVer = protover;
            MessageType = msgType;
            Sequence = sequence;
            if (encoding == default(Encoding))
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
                writer.Write(((int)MessageType).ToByteArray(true));
                writer.Write(Sequence.ToByteArray(true));
                writer.Write(Body);
                writer.Flush();
            }
            var result = ms.ToArray();
            return result;
        }

        public void LoadFromByteArray(byte[] package)
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
                var body = reader.ReadBytes(int.MaxValue);
                var result = new Package((MsgType)command, body, Encoding.UTF8, protover, headlength, sequence);

                // Set the result to current instance.
                this.HeadLength = result.HeadLength;
                this.MessageType = result.MessageType;
                this.Sequence = result.Sequence;
                this.Body = result.Body;
            }
        }
    }
    public enum MsgType
    {
        ClientHeart = 0x02,
        Renqi = 0x03,
        Command = 0x05,
        Auth = 0x07,
        ServerHeart = 0x08
    }
}
