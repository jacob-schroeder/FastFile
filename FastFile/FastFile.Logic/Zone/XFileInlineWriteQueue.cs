
namespace FastFile.Logic.Zone;

internal sealed class XFileInlineWriteQueue
{
    private readonly List<Action> _inlineWriters = new();

    public void Add(Action writer)
    {
        _inlineWriters.Add(writer);
    }

    public void Resolve()
    {
        var resolvedCount = 0;
        while (_inlineWriters.Count > 0)
        {
            if (++resolvedCount > 1_000_000)
                throw new InvalidDataException($"Stopped writing inline XFile pointers after {resolvedCount:N0} entries.");

            var writer = _inlineWriters[0];
            _inlineWriters.RemoveAt(0);

            var olderSiblingCount = _inlineWriters.Count;
            writer();

            var nestedCount = _inlineWriters.Count - olderSiblingCount;
            if (nestedCount <= 0 || olderSiblingCount <= 0)
                continue;

            var nestedWriters = _inlineWriters.GetRange(olderSiblingCount, nestedCount);
            _inlineWriters.RemoveRange(olderSiblingCount, nestedCount);
            _inlineWriters.InsertRange(0, nestedWriters);
        }
    }
}
