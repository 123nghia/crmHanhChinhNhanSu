using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VS.Human.Business.Helpers
{
    internal sealed class AttendanceDeviceTcpClient : IAsyncDisposable
    {
        private const ushort CmdConnect = 1000;
        private const ushort CmdFreeData = 1502;
        private const ushort CmdDataWrrq = 1503;
        private const ushort CmdDataRdy = 1504;
        private const int MaxChunk = 65472;
        private static readonly byte[] EmptyPayload = Array.Empty<byte>();
        private static readonly byte[] AttendanceLogsRequest = new byte[] { 0x01, 0x0D, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        private readonly string _ipAddress;
        private readonly int _port;
        private readonly TimeSpan _timeout;
        private TcpClient? _client;
        private NetworkStream? _stream;
        private ushort _sessionId;
        private ushort _replyId;

        public AttendanceDeviceTcpClient(string ipAddress, int port, TimeSpan timeout)
        {
            _ipAddress = ipAddress ?? throw new ArgumentNullException(nameof(ipAddress));
            _port = port <= 0 ? 4370 : port;
            _timeout = timeout <= TimeSpan.Zero ? TimeSpan.FromSeconds(120) : timeout;
        }

        public async Task ReadAttendanceLogsAsync(Action<AttendanceDeviceLog> onLog, CancellationToken cancellationToken = default)
        {
            if (onLog == null)
            {
                throw new ArgumentNullException(nameof(onLog));
            }

            await ConnectAsync(cancellationToken);
            await ExecuteCommandAsync(CmdFreeData, EmptyPayload, cancellationToken);

            var totalSize = await PrepareDataAsync(AttendanceLogsRequest, cancellationToken);
            if (totalSize <= 4)
            {
                return;
            }

            var fullChunks = totalSize / MaxChunk;
            var remain = totalSize % MaxChunk;
            var totalChunks = fullChunks + (remain > 0 ? 1 : 0);
            var carry = Array.Empty<byte>();

            for (var chunkIndex = 0; chunkIndex < totalChunks; chunkIndex++)
            {
                var start = chunkIndex * MaxChunk;
                var size = (chunkIndex == totalChunks - 1 && remain > 0) ? remain : MaxChunk;
                var chunk = await ReadChunkAsync(start, size, cancellationToken);
                if (chunkIndex == 0)
                {
                    if (chunk.Length < 4)
                    {
                        throw new InvalidDataException("Attendance payload from device is shorter than expected.");
                    }

                    chunk = chunk[4..];
                }

                if (carry.Length > 0)
                {
                    var combined = new byte[carry.Length + chunk.Length];
                    Buffer.BlockCopy(carry, 0, combined, 0, carry.Length);
                    Buffer.BlockCopy(chunk, 0, combined, carry.Length, chunk.Length);
                    chunk = combined;
                }

                var offset = 0;
                while (offset + 40 <= chunk.Length)
                {
                    onLog(DecodeAttendanceLog(chunk.AsSpan(offset, 40)));
                    offset += 40;
                }

                carry = offset < chunk.Length
                    ? chunk[offset..]
                    : Array.Empty<byte>();
            }

            if (carry.Length > 0)
            {
                throw new InvalidDataException("Attendance payload from device ended with an incomplete record.");
            }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (_stream != null)
                {
                    await _stream.DisposeAsync();
                }
            }
            finally
            {
                _stream = null;
                _client?.Dispose();
                _client = null;
            }
        }

        private async Task ConnectAsync(CancellationToken cancellationToken)
        {
            if (_stream != null)
            {
                return;
            }

            _client = new TcpClient();
            using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            connectCts.CancelAfter(_timeout);
            await _client.ConnectAsync(_ipAddress, _port, connectCts.Token);
            _stream = _client.GetStream();
            _sessionId = 0;
            _replyId = 0;
            await ExecuteCommandAsync(CmdConnect, EmptyPayload, cancellationToken);
        }

        private async Task<byte[]> ExecuteCommandAsync(ushort command, byte[] payload, CancellationToken cancellationToken)
        {
            EnsureConnected();

            if (command == CmdConnect)
            {
                _sessionId = 0;
                _replyId = 0;
            }
            else
            {
                _replyId++;
            }

            var packet = CreateTcpPacket(command, _sessionId, _replyId, payload);
            await WriteAsync(packet, cancellationToken);
            var reply = await ReadPacketAsync(_timeout, cancellationToken);
            var payloadReply = RemoveTcpHeader(reply);

            if (command == CmdConnect && payloadReply.Length >= 6)
            {
                _sessionId = BinaryPrimitives.ReadUInt16LittleEndian(payloadReply.AsSpan(4, 2));
            }

            return payloadReply;
        }

        private async Task<int> PrepareDataAsync(byte[] requestData, CancellationToken cancellationToken)
        {
            EnsureConnected();

            _replyId++;
            var packet = CreateTcpPacket(CmdDataWrrq, _sessionId, _replyId, requestData);
            await WriteAsync(packet, cancellationToken);
            var reply = await ReadPacketAsync(_timeout, cancellationToken);
            if (reply.Length < 21)
            {
                throw new InvalidDataException("Attendance prepare response from device is invalid.");
            }

            return BinaryPrimitives.ReadInt32LittleEndian(reply.AsSpan(17, 4));
        }

        private async Task<byte[]> ReadChunkAsync(int start, int size, CancellationToken cancellationToken)
        {
            EnsureConnected();

            _replyId++;
            var request = new byte[8];
            BinaryPrimitives.WriteInt32LittleEndian(request.AsSpan(0, 4), start);
            BinaryPrimitives.WriteInt32LittleEndian(request.AsSpan(4, 4), size);

            var packet = CreateTcpPacket(CmdDataRdy, _sessionId, _replyId, request);
            await WriteAsync(packet, cancellationToken);

            var expectedLength = size + 8;
            using var output = new MemoryStream(expectedLength);
            while (output.Length < expectedLength)
            {
                var reply = await ReadPacketAsync(_timeout, cancellationToken);
                if (reply.Length <= 16)
                {
                    continue;
                }

                output.Write(reply, 16, reply.Length - 16);
            }

            var buffer = output.ToArray();
            if (buffer.Length < expectedLength)
            {
                throw new InvalidDataException("Attendance chunk payload from device is shorter than expected.");
            }

            var chunk = new byte[size];
            Buffer.BlockCopy(buffer, 8, chunk, 0, size);
            return chunk;
        }

        private async Task<byte[]> ReadPacketAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            EnsureConnected();

            var prefix = new byte[8];
            await ReadExactAsync(prefix, timeout, cancellationToken);
            var payloadSize = BinaryPrimitives.ReadUInt16LittleEndian(prefix.AsSpan(4, 2));
            if (payloadSize <= 0)
            {
                throw new InvalidDataException("Attendance packet length from device is invalid.");
            }

            var payload = new byte[payloadSize];
            await ReadExactAsync(payload, timeout, cancellationToken);

            var packet = new byte[prefix.Length + payload.Length];
            Buffer.BlockCopy(prefix, 0, packet, 0, prefix.Length);
            Buffer.BlockCopy(payload, 0, packet, prefix.Length, payload.Length);
            return packet;
        }

        private async Task ReadExactAsync(byte[] buffer, TimeSpan timeout, CancellationToken cancellationToken)
        {
            EnsureConnected();

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(timeout);

            var offset = 0;
            while (offset < buffer.Length)
            {
                var read = await _stream!.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), timeoutCts.Token);
                if (read <= 0)
                {
                    throw new IOException("Attendance device closed the TCP connection.");
                }

                offset += read;
            }
        }

        private async Task WriteAsync(byte[] packet, CancellationToken cancellationToken)
        {
            EnsureConnected();
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(_timeout);
            await _stream!.WriteAsync(packet.AsMemory(0, packet.Length), timeoutCts.Token);
            await _stream.FlushAsync(timeoutCts.Token);
        }

        private void EnsureConnected()
        {
            if (_client == null || _stream == null)
            {
                throw new InvalidOperationException("Attendance device TCP connection has not been established.");
            }
        }

        private static AttendanceDeviceLog DecodeAttendanceLog(ReadOnlySpan<byte> record)
        {
            var userId = Encoding.ASCII.GetString(record.Slice(2, 9)).TrimEnd('\0').Trim();
            var timeCode = BinaryPrimitives.ReadUInt32LittleEndian(record.Slice(27, 4));
            return new AttendanceDeviceLog
            {
                UserId = userId,
                RecordTime = ParseRecordTime(timeCode),
                Type = record[26],
                State = record[31]
            };
        }

        private static DateTime ParseRecordTime(uint timeCode)
        {
            var time = timeCode;
            var second = time % 60;
            time = (time - second) / 60;
            var minute = time % 60;
            time = (time - minute) / 60;
            var hour = time % 24;
            time = (time - hour) / 24;
            var day = time % 31 + 1;
            time = (time - (day - 1)) / 31;
            var month = time % 12 + 1;
            time = (time - (month - 1)) / 12;
            var year = time + 2000;
            return new DateTime((int)year, (int)month, (int)day, (int)hour, (int)minute, (int)second);
        }

        private static byte[] RemoveTcpHeader(byte[] packet)
        {
            if (packet.Length >= 8 &&
                packet[0] == 0x50 &&
                packet[1] == 0x50 &&
                packet[2] == 0x82 &&
                packet[3] == 0x7D)
            {
                var payload = new byte[packet.Length - 8];
                Buffer.BlockCopy(packet, 8, payload, 0, payload.Length);
                return payload;
            }

            return packet;
        }

        private static byte[] CreateTcpPacket(ushort command, ushort sessionId, ushort replyId, byte[] data)
        {
            var payload = new byte[8 + data.Length];
            BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(0, 2), command);
            BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(2, 2), 0);
            BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(4, 2), sessionId);
            BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(6, 2), replyId);
            Buffer.BlockCopy(data, 0, payload, 8, data.Length);

            var checksum = CreateChecksum(payload);
            BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(2, 2), checksum);
            var nextReplyId = (ushort)((replyId + 1) % ushort.MaxValue);
            BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(6, 2), nextReplyId);

            var packet = new byte[8 + payload.Length];
            packet[0] = 0x50;
            packet[1] = 0x50;
            packet[2] = 0x82;
            packet[3] = 0x7D;
            BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(4, 2), (ushort)payload.Length);
            Buffer.BlockCopy(payload, 0, packet, 8, payload.Length);
            return packet;
        }

        private static ushort CreateChecksum(ReadOnlySpan<byte> buffer)
        {
            uint checksum = 0;
            for (var index = 0; index < buffer.Length; index += 2)
            {
                if (index == buffer.Length - 1)
                {
                    checksum += buffer[index];
                }
                else
                {
                    checksum += BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(index, 2));
                }

                checksum %= ushort.MaxValue;
            }

            checksum = ushort.MaxValue - checksum - 1;
            return (ushort)checksum;
        }
    }

    internal sealed class AttendanceDeviceLog
    {
        public string UserId { get; set; } = string.Empty;
        public DateTime RecordTime { get; set; }
        public byte Type { get; set; }
        public byte State { get; set; }
    }
}
