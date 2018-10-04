﻿// <copyright file="PipelinedXor32Encryptor.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Network.Xor
{
    using System;
    using System.Buffers;
    using System.IO.Pipelines;

    /// <summary>
    /// Pipelined encryptor which uses a 32 byte key for a xor encryption.
    /// It's typically used to encrypt packet sent by the client to the server.
    /// <seealso cref="Xor32Decryptor"/> for the non-pipelined version.
    /// </summary>
    public class PipelinedXor32Encryptor : PacketPipeReaderBase, IPipelinedEncryptor
    {
        private readonly PipeWriter target;
        private readonly Pipe pipe = new Pipe();
        private readonly byte[] xor32Key;

        /// <summary>
        /// Initializes a new instance of the <see cref="PipelinedXor32Encryptor"/> class.
        /// </summary>
        /// <param name="target">The target.</param>
        public PipelinedXor32Encryptor(PipeWriter target)
            : this(target, DefaultKeys.Xor32Key)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PipelinedXor32Encryptor"/> class.
        /// </summary>
        /// <param name="target">The target.</param>
        /// /// <param name="xor32Key">The xor32 key.</param>
        public PipelinedXor32Encryptor(PipeWriter target, byte[] xor32Key)
        {
            if (xor32Key.Length != 32)
            {
                throw new ArgumentException($"Xor32key must have a size of 32 bytes, but is {xor32Key.Length} bytes long.");
            }

            this.Source = this.pipe.Reader;
            this.xor32Key = xor32Key;
            this.ReadSource().ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public PipeWriter Writer => this.pipe.Writer;

        /// <summary>
        /// Reads the mu online packet.
        /// Encrypts the packet and writes it into the target.
        /// </summary>
        /// <param name="packet">The mu online packet</param>
        protected override void ReadPacket(ReadOnlySequence<byte> packet)
        {
            var span = this.pipe.Writer.GetSpan((int)packet.Length);
            var target = span.Slice(0, (int)packet.Length);
            packet.CopyTo(target);

            var headerSize = target.GetPacketHeaderSize();
            for (int i = headerSize + 1; i < packet.Length; i++)
            {
                target[i] = (byte)(target[i] ^ target[i - 1] ^ this.xor32Key[i % 32]);
            }

            this.target.Write(target);
            this.target.FlushAsync();
        }
    }
}
