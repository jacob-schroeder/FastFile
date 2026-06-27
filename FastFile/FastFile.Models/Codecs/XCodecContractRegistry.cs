namespace FastFile.Models.Codecs;

public sealed class XCodecContractRegistry
{
    private readonly Dictionary<string, IXCodecContract> _contracts = new(StringComparer.Ordinal);

    public IReadOnlyCollection<IXCodecContract> Contracts => _contracts.Values;

    public void Add(IXCodecContract contract)
    {
        if (!_contracts.TryAdd(contract.Name, contract))
            throw new InvalidOperationException($"A codec contract named '{contract.Name}' is already registered.");
    }

    public void AddRange(IEnumerable<IXCodecContract> contracts)
    {
        foreach (IXCodecContract contract in contracts)
            Add(contract);
    }

    public bool TryGet(string name, out IXCodecContract contract)
    {
        return _contracts.TryGetValue(name, out contract!);
    }

    public IXCodecContract Get(string name)
    {
        return _contracts.TryGetValue(name, out IXCodecContract? contract)
            ? contract
            : throw new KeyNotFoundException($"No codec contract named '{name}' is registered.");
    }
}
