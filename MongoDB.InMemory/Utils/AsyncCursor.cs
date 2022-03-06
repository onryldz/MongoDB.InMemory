using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace MongoDB.InMemory.Utils
{
    internal class AsyncCursor<TDocument> : IAsyncCursor<TDocument>
    {
        public IEnumerator<IEnumerable<TDocument>> Cursor;
        public AsyncCursor(params IEnumerable<TDocument>[] cursor)
        {
            Cursor = cursor.AsEnumerable()?.GetEnumerator();
        }

        public IEnumerable<TDocument> Current => Cursor.Current;
        public void Dispose() => Cursor.Dispose();
        public bool MoveNext(CancellationToken cancellationToken = default) => Cursor.MoveNext();
        public Task<bool> MoveNextAsync(CancellationToken cancellationToken = default) => Task.FromResult(Cursor.MoveNext());

    }
}