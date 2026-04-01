using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;

namespace CraterClaw.Core;

internal sealed class TeeHttpContent : HttpContent
{
    private readonly HttpContent _inner;
    private readonly ILogger _logger;
    private readonly MemoryStream _accumulator = new();

    public TeeHttpContent(HttpContent inner, ILogger logger)
    {
        _inner = inner;
        _logger = logger;
        foreach (var header in inner.Headers)
            Headers.TryAddWithoutValidation(header.Key, header.Value);
    }

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        => SerializeToStreamAsync(stream, context, CancellationToken.None);

    protected override async Task SerializeToStreamAsync(
        Stream stream, TransportContext? context, CancellationToken cancellationToken)
    {
        var innerStream = await _inner.ReadAsStreamAsync(cancellationToken);
        var buffer = new byte[4096];
        int read;
        while ((read = await innerStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await stream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            await _accumulator.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
    }

    protected override async Task<Stream> CreateContentReadStreamAsync(CancellationToken cancellationToken)
    {
        var innerStream = await _inner.ReadAsStreamAsync(cancellationToken);
        return new TeeStream(innerStream, _accumulator);
    }

    protected override bool TryComputeLength(out long length)
    {
        length = 0;
        return false;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_accumulator.Length > 0)
            {
                var text = Encoding.UTF8.GetString(_accumulator.ToArray());
                _logger.LogDebug("[RESPONSE] {Body}", text);
            }
            _accumulator.Dispose();
            _inner.Dispose();
        }
        base.Dispose(disposing);
    }
}

internal sealed class TeeStream : Stream
{
    private readonly Stream _inner;
    private readonly MemoryStream _accumulator;

    public TeeStream(Stream inner, MemoryStream accumulator)
    {
        _inner = inner;
        _accumulator = accumulator;
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var n = _inner.Read(buffer, offset, count);
        if (n > 0) _accumulator.Write(buffer, offset, n);
        return n;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var n = await _inner.ReadAsync(buffer, offset, count, cancellationToken);
        if (n > 0) await _accumulator.WriteAsync(buffer.AsMemory(offset, n), cancellationToken);
        return n;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var n = await _inner.ReadAsync(buffer, cancellationToken);
        if (n > 0) await _accumulator.WriteAsync(buffer[..n], cancellationToken);
        return n;
    }

    public override void Flush() => _inner.Flush();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _inner.Dispose();
        base.Dispose(disposing);
    }
}
