
using FastFile.Models.Zone;

namespace FastFile.Logic.Zone;

public sealed class XFileReader
{
	private readonly ReadOnlyMemory<byte> _memory;
	private int _position;
	private ReadOnlySpan<byte> Span => _memory.Span;

	public XFileReader(byte[] buffer)
	{
		_memory = buffer.AsMemory();
	}

	public XFile ParseHeader()
	{
		throw new NotImplementedException();
	}

	public XAssetList Load_XAssetList()
	{
		throw new NotImplementedException();
	}
}
