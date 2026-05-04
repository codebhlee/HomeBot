using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HomeBot.Models;

namespace HomeBot.Services;

internal static class OllamaStreamer
{
    public static async IAsyncEnumerable<OllamaChunk> StreamAsync(
        HttpResponseMessage response,
        [EnumeratorCancellation] CancellationToken ct)
    {
        using var stream = await response.Content.ReadAsStreamAsync(ct);
        var pipe = PipeReader.Create(stream);

        try
        {
            while (true)
            {
                var result = await pipe.ReadAsync(ct);
                var buffer = result.Buffer;

                while (TryReadLine(ref buffer, out var line))
                {
                    var chunk = ParseChunk(line);
                    if (chunk is not null)
                        yield return chunk;
                }

                pipe.AdvanceTo(buffer.Start, buffer.End);

                if (result.IsCompleted) break;
            }
        }
        finally
        {
            await pipe.CompleteAsync();
        }
    }

    private static bool TryReadLine(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> line)
    {
        var reader = new SequenceReader<byte>(buffer);
        if (reader.TryReadTo(out line, (byte)'\n'))
        {
            buffer = buffer.Slice(reader.Position);
            return true;
        }
        line = default;
        return false;
    }

    private static OllamaChunk? ParseChunk(ReadOnlySequence<byte> utf8Json)
    {
        if (utf8Json.IsEmpty) return null;

        string? responseText = null;
        bool    done         = false;

        var reader = new Utf8JsonReader(utf8Json);
        while (reader.Read())
        {
            if (reader.TokenType != JsonTokenType.PropertyName) continue;

            if (reader.ValueTextEquals("response"u8))
            {
                reader.Read();
                responseText = reader.GetString();
            }
            else if (reader.ValueTextEquals("done"u8))
            {
                reader.Read();
                done = reader.GetBoolean();
            }
            else
            {
                reader.Skip();
            }
        }

        return new OllamaChunk(responseText, done);
    }
}
