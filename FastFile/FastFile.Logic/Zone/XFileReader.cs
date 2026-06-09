using System.Buffers.Binary;
using FastFile.Models.Assets;
using FastFile.Models.Assets.Localize;
using FastFile.Models.Assets.Menu;
using FastFile.Models.Assets.Menufile;
using FastFile.Models.Assets.Eboot;
using FastFile.Models.Assets.RawFiles;
using FastFile.Models.Assets.Weapons;
using FastFile.Models.Assets.StringTables;
using FastFile.Models.Assets.StructuredData;
using FastFile.Models.Assets.TechniqueSet;
using FastFile.Models.Data;
using FastFile.Models.Zone;

namespace FastFile.Logic.Zone;

public sealed class XFileReader
{
	private const int PointerSize = 4;
	private const int XAssetListSize = 0x10;
	private const int XAssetSize = 0x08;
	private const int LocalizeEntrySize = 0x08;
	private const int RawFileSize = 0x10;
	private const int StringTableSize = 0x10;
	private const int StringTableCellSize = 0x08;
	private const int MenuFileSize = 0x0c;
	private const int MenuDefSize = 0x2f0;
		private const int MenuWindowSize = 0xb0;
		private const int MenuStatementSize = 0x18;
		private const int MenuStatementUnionEntrySize = 0x08;
		private const int MenuStatementKind1Size = 0x08;
		private const int MenuStatementStringHandlerSize = 0x08;
		private const int MenuExpressionEntrySize = 0x0c;
		private const int MenuStringListSize = 0x08;
		private const int MenuEventHandlerSize = 0x18;
		private const int MenuEventHandlerChildSize = 0x08;
		private const int MenuEventHandlerStringEntrySize = 0x08;
		private const int MenuItemDefSize = 0x1cc;
	private const int MenuItemConditionSize = 0x08;
		private const int MenuExpressionSupportingDataSize = 0x0c;
		private const int MenuExpressionSupportingDataChildSize = 0x0c;
		private const int MaterialRootSize = 0xa8;
		private const int MaterialConstantDefSize = 0x0c;
		private const int MaterialStateMapSize = 0x20;
		private const int MaterialWaterDefSize = 0x08;
		private const int MaterialConstantAssetSize = 0x50;
		private const int MaterialConstantLiteralSize = 0x48;
		private const int SoundAliasListSize = 0x0c;
		private const int SoundAliasEntrySize = 0x64;
		private const int SoundAliasNestedEntrySize = 0x10;
		private const int SoundAliasLoadSpecSize = 0x88;
		private const int SoundAliasLimitSize = 0x198;
		private const int MenuItemType6DataSize = 0x158;
		private const int MenuItemType12DataSize = 0x188;
		private const int MenuItemCommonDataSize = 0x20;
		private const int MenuItemType20DataSize = 0x1c;
		private const int MenuItemType21DataSize = 0x04;
			private const int MaterialTechniqueSetSize = 0x9c;
	private const int MaterialTechniqueSetTechniqueCount = 37;
	private const int MaterialTechniqueRecordSize = 0x44;
	private const int MaterialTechniqueSubRecordSize = 0x24;
	private const int MaterialTechniqueChildEntrySize = 0x08;
	private const int MaterialTechniqueArray20EntrySize = 0x14;
		private const int MaterialTechniqueByteEntrySize = 0x01;
		private const int MaterialTechniqueSize = 0x08;
		private const int MaterialPassSize = 0x18;
		private const int MaterialVertexDeclarationSize = 0x1c;
		private const int MaterialShaderArgumentSize = 0x08;
		private const int WeaponRootSize = 0x74;
		private const int WeaponDefRootSize = 0x684;
		private const int WeaponAnimCount = 37;
		private const int WeaponGunModelCount = 16;
		private const int WeaponHideTagCount = 32;
		private const int WeaponSurfaceCount = 31;
		private const int WeaponSoundAliasCount = 47;
		private const int WeaponHitLocationCount = 20;
		private const int WeaponVec2Size = 0x08;
		private const int XModelRootSize = 0x120;
		private const int FxEffectRootSize = 0x20;
		private const int TracerRootSize = 0x70;
		private const int WeaponEmbeddedSubrootSize = 0x70;
		private const int WeaponAttachmentEntrySize = 0x2c;
		private const int WeaponStringPairObjectSize = 0x2c;
		private const int StructuredDataDefSetSize = 0x0c;

	private readonly ReadOnlyMemory<byte> _memory;
	private readonly List<MaterializedSpan> _materializedSpans = [];
	private readonly List<Pointer> _offsetPointers = [];
	private readonly Dictionary<(int Block, int Offset), InsertAliasTarget> _insertAliasTargets = [];
	private readonly Dictionary<(int Block, int Offset, Type ResultType), LoadedObjectResult> _objectResults = [];
	private readonly Dictionary<(int Block, int Offset), List<LoadedObjectResult>> _objectResultsByAddress = [];
	private readonly Action<int, int>? _assetReadProgress;
	private readonly Stack<StreamBlockFrame> _streamBlockStack = new();
	private byte[][] _streamBlocks = [];
	private int[] _streamBlockOffsets = [];
	private XFILE_BLOCK _activeStreamBlock = XFILE_BLOCK.LARGE;
			private XFile? _header;
		private int _position;
		private string? _currentAssetFieldPath;
		private string? _lastCompletedAssetFieldPath;
		private string? _currentMaterializeFieldPath;
		private int _assetTraceBeforeIndex = -1;
		private int _assetTraceBeforeSource = -1;
		private int[]? _assetTraceBeforeCursors;

	private ReadOnlySpan<byte> Span => _memory.Span;
	private XFILE_BLOCK CurrentStreamBlock => _activeStreamBlock;
	public IReadOnlyList<byte[]> StreamBlocks => _streamBlocks;
	public int Position => _position;

	public XFileReader(byte[] buffer, Action<int, int>? assetReadProgress = null)
	{
		_memory = buffer.AsMemory();
		_assetReadProgress = assetReadProgress;
	}

	public XFile ParseHeader()
	{
		var blockCount = (int)XFILE_BLOCK.MAX_XFILE_COUNT;
		var blockSizes = new int[blockCount];

		var header = new XFile
		{
			Size = ReadInt32(),
			ExternalSize = ReadInt32(),
			BlockSize = blockSizes
		};

		for (var i = 0; i < blockSizes.Length; i++)
		{
			var blockSize = ReadInt32();
			if (blockSize < 0)
				throw new InvalidDataException($"Invalid negative XFILE block size {blockSize} for block {(XFILE_BLOCK)i}.");

			blockSizes[i] = blockSize;
		}

		_header = header;
			_streamBlocks = blockSizes
				.Select((size, index) =>
				{
					if (index == (int)XFILE_BLOCK.LARGE
						&& blockSizes.Length > (int)XFILE_BLOCK.XFILE_BLOCK_VERTEX)
					{
						return new byte[checked(size + blockSizes[(int)XFILE_BLOCK.XFILE_BLOCK_VERTEX])];
					}

					return new byte[size];
				})
				.ToArray();
			_streamBlockOffsets = new int[blockSizes.Length];
			_materializedSpans.Clear();
			_offsetPointers.Clear();
			_insertAliasTargets.Clear();
			_objectResults.Clear();
			_objectResultsByAddress.Clear();
			_streamBlockStack.Clear();
			_activeStreamBlock = XFILE_BLOCK.LARGE;

		return header;
	}

	public XAssetList Load_XAssetList()
	{
		EnsureHeaderParsed();

		PushStreamBlock(XFILE_BLOCK.TEMP);
		try
		{
			var rootSourceOffset = _position;
			var rootStreamOffset = MaterializeCurrent(XFILE_BLOCK.TEMP, XAssetListSize, PointerSize);
			var rootOffset = 0;
			var rootSpan = _streamBlocks[(int)XFILE_BLOCK.TEMP].AsSpan(rootStreamOffset, XAssetListSize);

		var scriptStringCount = ReadInt32(rootSpan, ref rootOffset);
		var scriptStringsPtr = ReadDirectPointer<ZonePointer<string?>[]>(
			rootSpan,
			ref rootOffset,
			rootSourceOffset + 0x04,
			XFILE_BLOCK.TEMP,
			rootStreamOffset + 0x04,
			"XAssetList.scriptStrings");

		var assetCount = ReadInt32(rootSpan, ref rootOffset);
		var assetsPtr = ReadDirectPointer<XAsset[]>(
			rootSpan,
			ref rootOffset,
			rootSourceOffset + 0x0c,
			XFILE_BLOCK.TEMP,
			rootStreamOffset + 0x0c,
			"XAssetList.assets");

		var assetList = new XAssetList
		{
			ScriptStringCount = scriptStringCount,
			ScriptStringsPtr = scriptStringsPtr,
			AssetCount = assetCount,
			AssetsPtr = assetsPtr
		};

			PushStreamBlock(XFILE_BLOCK.LARGE);
		try
		{
			LoadScriptStringList(assetList);
			LoadXAssetArray(assetList);
		}
		finally
		{
			PopStreamBlock();
		}

			ResolveOffsetPointers(assetList.AssetCount);
			return assetList;
		}
		finally
		{
			PopStreamBlock();
		}
	}

	private void LoadScriptStringList(XAssetList assetList)
	{
		var pointer = assetList.ScriptStringsPtr;
		var count = assetList.ScriptStringCount;

		if (count < 0)
			throw new InvalidDataException($"Invalid script string count {count}.");

		if (pointer.Kind == PointerKind.Null)
		{
			SetPointerResult(pointer, []);
			return;
		}

		if (pointer.Kind == PointerKind.Offset)
		{
			_offsetPointers.Add(pointer);
			RegisterLoadedOffsetTarget(pointer, checked(count * PointerSize));
			SetPointerResult(pointer, []);
			return;
		}

		var arrayLength = checked(count * PointerSize);
		var arraySpan = LoadPointerPayload(
			pointer, CurrentStreamBlock,
			arrayLength,
			PointerSize,
			"XAssetList.scriptStrings[]");
		RegisterArrayElementStarts(arraySpan, count, PointerSize);

		var scriptStrings = new ZonePointer<string?>[count];
		var arrayReader = 0;

		for (var i = 0; i < scriptStrings.Length; i++)
		{
			var elementSourceOffset = arraySpan.SourceOffset + arrayReader;
			var elementStreamOffset = arraySpan.StreamOffset + arrayReader;
			var elementPointer = ReadZonePointer<string?>(
				arraySpan.Bytes,
				ref arrayReader,
				elementSourceOffset,
				arraySpan.Block,
				elementStreamOffset,
				$"XAssetList.scriptStrings[{i}]");

			LoadXString(elementPointer, CurrentStreamBlock, $"XAssetList.scriptStrings[{i}]");
			scriptStrings[i] = elementPointer;
		}

		SetPointerResult(pointer, scriptStrings);
	}

	private void LoadXAssetArray(XAssetList assetList)
	{
		var pointer = assetList.AssetsPtr;
		var count = assetList.AssetCount;

		if (count < 0)
			throw new InvalidDataException($"Invalid asset count {count}.");

		if (pointer.Kind == PointerKind.Null)
		{
			SetPointerResult(pointer, []);
			return;
		}

		if (pointer.Kind == PointerKind.Offset)
		{
			_offsetPointers.Add(pointer);
			RegisterLoadedOffsetTarget(pointer, checked(count * XAssetSize));
			SetPointerResult(pointer, []);
			return;
		}

		var arrayLength = checked(count * XAssetSize);
		var arraySpan = LoadPointerPayload(
			pointer, CurrentStreamBlock,
			arrayLength,
			PointerSize,
			"XAssetList.assets[]");
		RegisterArrayElementStarts(arraySpan, count, XAssetSize);

		var assets = new XAsset[count];
		var arrayReader = 0;

		for (var i = 0; i < assets.Length; i++)
		{
			var rowSourceOffset = arraySpan.SourceOffset + arrayReader;
			var rowStreamOffset = arraySpan.StreamOffset + arrayReader;
			var assetType = (XAssetType)ReadInt32(arraySpan.Bytes, ref arrayReader);
			var assetPointer = ReadAliasPointer<BaseAsset>(
				arraySpan.Bytes,
				ref arrayReader,
				rowSourceOffset + 0x04,
				arraySpan.Block,
				rowStreamOffset + 0x04,
				$"XAssetList.assets[{i}].header");

			TraceAssetBoundary(i, assetType, "before");
			if (assetPointer.Kind == PointerKind.Null)
				SetPointerResult(assetPointer, null);
			else if (assetPointer.Kind == PointerKind.Offset)
				_offsetPointers.Add(assetPointer);
			else
				{
					var previousAssetFieldPath = _currentAssetFieldPath;
					_currentAssetFieldPath = $"XAssetList.assets[{i}]({assetType})";
					try
					{
						LoadXAssetHeader(assetType, assetPointer);
					}
					finally
					{
						_currentAssetFieldPath = previousAssetFieldPath;
					}
				}

				assets[i] = new XAsset
				{
					Type = assetType,
					XAssetPtr = assetPointer
				};
				_lastCompletedAssetFieldPath = $"XAssetList.assets[{i}]({assetType})";
				TraceAssetBoundary(i, assetType, "after");
				_assetReadProgress?.Invoke(i + 1, checked(assets.Length + _offsetPointers.Count));
			}

		SetPointerResult(pointer, assets);
	}

	private void TraceAssetBoundary(int index, XAssetType assetType, string phase)
	{
		if (!ShouldTraceAssetBoundary(index))
			return;

		var delta = string.Empty;
		if (phase == "before")
		{
			_assetTraceBeforeIndex = index;
			_assetTraceBeforeSource = _position;
			_assetTraceBeforeCursors = _streamBlockOffsets.ToArray();
		}
		else if (_assetTraceBeforeIndex == index && _assetTraceBeforeCursors is not null)
		{
			delta = " " + FormatCursorDelta(_assetTraceBeforeSource, _assetTraceBeforeCursors);
		}

		Console.WriteLine(
			$"asset[{index}] {assetType} {phase} source=0x{_position:X} "
			+ FormatCursorSnapshot()
			+ delta);
	}

	private static bool ShouldTraceAssetBoundary(int index)
	{
		if (Environment.GetEnvironmentVariable("FF_TRACE_XFILE_ASSETS") != "1")
			return false;

		var range = Environment.GetEnvironmentVariable("FF_TRACE_XFILE_ASSET_RANGE");
		if (string.IsNullOrWhiteSpace(range))
			return true;

		var separator = range.IndexOf('-');
		if (separator < 0)
			return int.TryParse(range, out var singleIndex) && index == singleIndex;

		return int.TryParse(range[..separator], out var start)
			&& int.TryParse(range[(separator + 1)..], out var end)
			&& index >= start
			&& index <= end;
	}

	private string FormatCursorSnapshot()
	{
		var parts = new string[_streamBlockOffsets.Length];
		for (var i = 0; i < parts.Length; i++)
		{
			var blockName = ((XFILE_BLOCK)i).ToString();
			var blockLength = i < _streamBlocks.Length ? _streamBlocks[i].Length : -1;
			parts[i] = $"{blockName}=0x{_streamBlockOffsets[i]:X}/0x{blockLength:X}";
		}

		return string.Join(" ", parts);
	}

	private string FormatCursorDelta(int beforeSource, int[] beforeCursors)
	{
		var parts = new string[_streamBlockOffsets.Length + 1];
		parts[0] = $"sourceDelta=0x{(_position - beforeSource):X}";
		for (var i = 0; i < _streamBlockOffsets.Length; i++)
		{
			var blockName = ((XFILE_BLOCK)i).ToString();
			var before = i < beforeCursors.Length ? beforeCursors[i] : 0;
			parts[i + 1] = $"{blockName}Delta=0x{(_streamBlockOffsets[i] - before):X}";
		}

		return string.Join(" ", parts);
	}

	private void LoadXAssetHeader(XAssetType assetType, AliasPointer<BaseAsset> pointer)
	{
		PushStreamBlock(XFILE_BLOCK.TEMP);
		BaseAsset asset;
		try
		{
			asset = assetType switch
				{
					XAssetType.Techset => LoadMaterialTechniqueSet(pointer),
					XAssetType.MenuFile => LoadMenuFile(pointer),
					XAssetType.Menu => LoadMenuDefAsset(pointer),
					XAssetType.Localize => LoadLocalizeEntry(pointer),
					XAssetType.RawFile => LoadRawFile(pointer),
					XAssetType.StringTable => LoadStringTable(pointer),
					XAssetType.StructuredDataDef => LoadStructuredDataDefSet(pointer),
					XAssetType.Weapon => LoadWeapon(pointer),
					_ => LoadUnsupportedXAssetRoot(assetType, pointer)
				};
			}
		finally
		{
			PopStreamBlock();
		}

		SetPointerResult(pointer, asset);
	}

	private LocalizeEntry LoadLocalizeEntry(AliasPointer<BaseAsset> pointer)
	{
		var fieldPath = _currentAssetFieldPath is null
			? "LocalizeEntry"
			: $"{_currentAssetFieldPath}.LocalizeEntry";
		var root = LoadPointerPayload(
			pointer, CurrentStreamBlock,
			LocalizeEntrySize,
			PointerSize,
			fieldPath);

		var rootReader = 0;
		var valuePtr = ReadDirectPointer<string>(
			root.Bytes,
			ref rootReader,
			root.SourceOffset,
			root.Block,
			root.StreamOffset,
			$"{fieldPath}.value");

		var namePtr = ReadDirectPointer<string>(
			root.Bytes,
			ref rootReader,
			root.SourceOffset + 0x04,
			root.Block,
			root.StreamOffset + 0x04,
			$"{fieldPath}.name");

		PushStreamBlock(XFILE_BLOCK.LARGE);
		try
		{
			LoadXString(valuePtr, CurrentStreamBlock, $"{fieldPath}.value");
			LoadXString(namePtr, CurrentStreamBlock, $"{fieldPath}.name");
		}
		finally
		{
			PopStreamBlock();
		}

		return new LocalizeEntry
		{
			Offset = root.SourceOffset,
			ValuePtr = valuePtr,
			NamePtr = namePtr
		};
	}

		private RawFile LoadRawFile(AliasPointer<BaseAsset> pointer)
		{
			var fieldPath = _currentAssetFieldPath is null
				? "RawFile"
				: $"{_currentAssetFieldPath}.RawFile";
			var root = LoadPointerPayload(
				pointer, CurrentStreamBlock,
				RawFileSize,
				PointerSize,
				fieldPath);

		var rootReader = 0;
		var namePtr = ReadDirectPointer<string>(
			root.Bytes,
			ref rootReader,
				root.SourceOffset,
				root.Block,
				root.StreamOffset,
				$"{fieldPath}.name");
		var compressedLen = ReadInt32(root.Bytes, ref rootReader);
		var len = ReadInt32(root.Bytes, ref rootReader);
		var bufferPtr = ReadDirectPointer<byte[]>(
			root.Bytes,
			ref rootReader,
				root.SourceOffset + 0x0c,
				root.Block,
				root.StreamOffset + 0x0c,
				$"{fieldPath}.buffer");

			PushStreamBlock(XFILE_BLOCK.LARGE);
			try
			{
				LoadXString(namePtr, CurrentStreamBlock, $"{fieldPath}.name");

		if (bufferPtr.Kind == PointerKind.Null)
		{
			SetPointerResult(bufferPtr, []);
		}
		else
		{
				var bufferLength = compressedLen != 0
					? compressedLen
					: checked(len + 1);
				if (bufferLength < 0)
				{
					throw new InvalidDataException(
						$"Invalid RawFile buffer length {bufferLength} for {fieldPath}.buffer "
						+ $"at source 0x{root.SourceOffset:X}; compressedLen={compressedLen}, len={len}, "
						+ $"previousAsset={_lastCompletedAssetFieldPath ?? "<none>"}.");
				}
				LoadByteArray(bufferPtr, CurrentStreamBlock, bufferLength, $"{fieldPath}.buffer");
			}
			}
			finally
			{
				PopStreamBlock();
			}

		return new RawFile
		{
			Offset = root.SourceOffset,
			NamePtr = namePtr,
			CompressedLen = compressedLen,
			Len = len,
			BufferPtr = bufferPtr
		};
	}

	private StringTable LoadStringTable(AliasPointer<BaseAsset> pointer)
	{
		var root = LoadPointerPayload(
			pointer, CurrentStreamBlock,
			StringTableSize,
			PointerSize,
			"StringTable");

		var rootReader = 0;
		var namePtr = ReadDirectPointer<string>(
			root.Bytes,
			ref rootReader,
			root.SourceOffset,
			root.Block,
			root.StreamOffset,
			"StringTable.name");
		var columnCount = ReadInt32(root.Bytes, ref rootReader);
		var rowCount = ReadInt32(root.Bytes, ref rootReader);
		var stringsPtr = ReadDirectPointer<StringTableCell[]>(
			root.Bytes,
			ref rootReader,
			root.SourceOffset + 0x0c,
			root.Block,
			root.StreamOffset + 0x0c,
			"StringTable.cells");

		PushStreamBlock(XFILE_BLOCK.LARGE);
		try
		{
			LoadXString(namePtr, CurrentStreamBlock, "StringTable.name");
			LoadStringTableCells(stringsPtr, checked(columnCount * rowCount), "StringTable.cells");
		}
		finally
		{
			PopStreamBlock();
		}

		return new StringTable
		{
			Offset = root.SourceOffset,
			NamePtr = namePtr,
			ColumnCount = columnCount,
			RowCount = rowCount,
			StringsPtr = stringsPtr
		};
	}

		private MenuList LoadMenuFile(AliasPointer<BaseAsset> pointer)
		{
			var fieldPath = _currentAssetFieldPath is null
				? "MenuFile"
				: $"{_currentAssetFieldPath}.MenuFile";
			var root = LoadPointerPayload(
				pointer, CurrentStreamBlock,
				MenuFileSize,
				PointerSize,
				fieldPath);

		var rootReader = 0;
		var namePtr = ReadDirectPointer<string>(
			root.Bytes,
			ref rootReader,
				root.SourceOffset,
				root.Block,
				root.StreamOffset,
				$"{fieldPath}.name");
		var menuCount = ReadInt32(root.Bytes, ref rootReader);
		var menusPtr = ReadDirectPointer<AliasPointer<MenuDef>[]>(
			root.Bytes,
			ref rootReader,
				root.SourceOffset + 0x08,
				root.Block,
				root.StreamOffset + 0x08,
				$"{fieldPath}.menus");

			PushStreamBlock(XFILE_BLOCK.LARGE);
			try
			{
				LoadXString(namePtr, CurrentStreamBlock, $"{fieldPath}.name");
				LoadMenuPointerArray(menusPtr, menuCount, $"{fieldPath}.menus");
			}
			finally
			{
				PopStreamBlock();
			}

			return new MenuList
			{
				Offset = root.SourceOffset,
				NamePtr = namePtr,
				MenuCount = menuCount,
				Menus = menusPtr
			};
		}

		private MenuDef LoadMenuDefAsset(AliasPointer<BaseAsset> pointer)
		{
			var payload = LoadPointerPayload(pointer, CurrentStreamBlock, MenuDefSize, PointerSize, "MenuDef");
			var menuDef = new MenuDef
			{
				Offset = payload.SourceOffset
			};

			PushStreamBlock(XFILE_BLOCK.LARGE);
			try
			{
				ConsumeMenuDef(payload, "MenuDef");
			}
			finally
			{
				PopStreamBlock();
			}
			return menuDef;
		}

		private MaterialTechniqueSet LoadMaterialTechniqueSet(AliasPointer<BaseAsset> pointer)
	{
		var fieldPath = _currentAssetFieldPath is null
			? "MaterialTechniqueSet"
			: $"{_currentAssetFieldPath}.MaterialTechniqueSet";
		var root = LoadPointerPayload(
			pointer, CurrentStreamBlock,
			MaterialTechniqueSetSize,
			PointerSize,
			fieldPath);

		var rootReader = 0;
		var namePtr = ReadDirectPointer<string>(
			root.Bytes,
			ref rootReader,
			root.SourceOffset,
			root.Block,
			root.StreamOffset,
			$"{fieldPath}.name");

		var worldVertexFormat = (MaterialWorldVertexFormat)root.Bytes[rootReader++];
		var hasBeenUploaded = root.Bytes[rootReader++] != 0;
		var unused = root.Bytes.AsSpan(rootReader, 2).ToArray();
		rootReader += 2;

		DirectPointer<MaterialTechnique>[] techniques;
		PushStreamBlock(XFILE_BLOCK.LARGE);
		try
		{
			LoadXString(namePtr, CurrentStreamBlock, $"{fieldPath}.name");

			techniques = new DirectPointer<MaterialTechnique>[MaterialTechniqueSetTechniqueCount];
			for (var i = 0; i < techniques.Length; i++)
			{
				var techniquePointerOffset = rootReader;
				var techniquePtr = ReadDirectPointer<MaterialTechnique>(
					root.Bytes,
					ref rootReader,
					root.SourceOffset + techniquePointerOffset,
					root.Block,
					root.StreamOffset + techniquePointerOffset,
					$"{fieldPath}.techniques[{i}]");

				LoadMaterialTechniquePointer(techniquePtr, $"{fieldPath}.techniques[{i}]");
				techniques[i] = techniquePtr;
			}
		}
		finally
		{
			PopStreamBlock();
		}

		return new MaterialTechniqueSet
		{
			Offset = root.SourceOffset,
			NamePtr = namePtr,
			WorldVertexFormat = worldVertexFormat,
			HasBeenUploaded = hasBeenUploaded,
			Unused = unused,
			Techniques = techniques
		};
	}

	private BaseAsset LoadWeapon(AliasPointer<BaseAsset> pointer)
	{
		var fieldPath = _currentAssetFieldPath is null
			? "Weapon"
			: $"{_currentAssetFieldPath}.Weapon";
		var root = LoadPointerPayload(
			pointer,
			CurrentStreamBlock,
			WeaponRootSize,
			PointerSize,
			fieldPath);

		var weapon = new WeaponVariantDef
		{
			Offset = root.SourceOffset,
			WeaponDefPtr = new DirectPointer<WeaponDef>(0),
			InternalNamePtr = new DirectPointer<string>(0),
			DisplayNamePtr = new DirectPointer<string>(0),
			HideTags = new DirectPointer<ushort[]>(0),
			XAnims = new DirectPointer<ZonePointer<string>[]>(0),
			szAltWeaponName = new DirectPointer<string>(0)
		};

		var rootReader = 0;
		weapon.InternalNamePtr = ReadDirectPointer<string>(
			root.Bytes,
			ref rootReader,
			root.SourceOffset,
			root.Block,
			root.StreamOffset,
			$"{fieldPath}.name");
		weapon.WeaponDefPtr = ReadDirectPointer<WeaponDef>(
			root.Bytes,
			ref rootReader,
			root.SourceOffset + 0x04,
			root.Block,
			root.StreamOffset + 0x04,
			$"{fieldPath}.weaponDef");
		weapon.DisplayNamePtr = ReadDirectPointer<string>(
			root.Bytes,
			ref rootReader,
			root.SourceOffset + 0x08,
			root.Block,
			root.StreamOffset + 0x08,
			$"{fieldPath}.displayName");
		weapon.HideTags = ReadDirectPointer<ushort[]>(
			root.Bytes,
			ref rootReader,
			root.SourceOffset + 0x0c,
			root.Block,
			root.StreamOffset + 0x0c,
			$"{fieldPath}.hideTags");
		weapon.XAnims = ReadDirectPointer<ZonePointer<string>[]>(
			root.Bytes,
			ref rootReader,
			root.SourceOffset + 0x10,
			root.Block,
			root.StreamOffset + 0x10,
			$"{fieldPath}.xAnims");

		var altWeaponNamePtrReader = 0x3c;
		weapon.szAltWeaponName = ReadDirectPointer<string>(
			root.Bytes,
			ref altWeaponNamePtrReader,
			root.SourceOffset + 0x3c,
			root.Block,
			root.StreamOffset + 0x3c,
			$"{fieldPath}.altWeaponName");

		PushStreamBlock(XFILE_BLOCK.LARGE);
		try
		{
			LoadXString(weapon.InternalNamePtr, CurrentStreamBlock, $"{fieldPath}.name");
			ConsumeWeaponVariantRoot(root, fieldPath);
		}
		finally
		{
			PopStreamBlock();
		}

		return weapon;
	}

	private void ConsumeWeaponVariantRoot(PointerPayload payload, string fieldPath)
	{
		LoadDirectObject(
			payload,
			0x04,
			WeaponDefRootSize,
			(loaded, loadedPath) => ConsumeWeaponDefRoot(loaded, loadedPath, payload),
			$"{fieldPath}.weaponDef");
		LoadEmbeddedXString(payload, 0x08, $"{fieldPath}.displayName");
		LoadDirectArray(payload, 0x0c, WeaponHideTagCount, sizeof(ushort), $"{fieldPath}.hideTags", sizeof(ushort));
		LoadXStringArrayPointer(payload, 0x10, WeaponAnimCount, $"{fieldPath}.xAnims");
		LoadEmbeddedXString(payload, 0x3c, $"{fieldPath}.altWeaponName");
		LoadWeaponMaterialAlias(payload, 0x48, $"{fieldPath}.killIcon");
		LoadWeaponMaterialAlias(payload, 0x4c, $"{fieldPath}.dpadIcon");
		LoadDirectArray(
			payload,
			0x68,
			ReadUInt16At(payload.Bytes, 0x64),
			WeaponVec2Size,
			$"{fieldPath}.accuracyGraphKnots");
		LoadDirectArray(
			payload,
			0x6c,
			ReadUInt16At(payload.Bytes, 0x66),
			WeaponVec2Size,
			$"{fieldPath}.originalAccuracyGraphKnots");
	}

	private void ConsumeWeaponDefRoot(PointerPayload payload, string fieldPath, PointerPayload variantPayload)
	{
		LoadEmbeddedXString(payload, 0x00, $"{fieldPath}.internalName");
		LoadXModelAliasPointerArray(payload, 0x04, WeaponGunModelCount, $"{fieldPath}.gunXModel");
		LoadXModelAliasCell(payload, 0x08, $"{fieldPath}.handXModel");
		LoadXStringArrayPointer(payload, 0x0c, WeaponAnimCount, $"{fieldPath}.szXAnimsR");
		LoadXStringArrayPointer(payload, 0x10, WeaponAnimCount, $"{fieldPath}.szXAnimsL");
		LoadEmbeddedXString(payload, 0x14, $"{fieldPath}.modeName");

		LoadDirectArray(payload, 0x18, WeaponGunModelCount, sizeof(ushort), $"{fieldPath}.notetrackMap0", sizeof(ushort));
		LoadDirectArray(payload, 0x1c, WeaponGunModelCount, sizeof(ushort), $"{fieldPath}.notetrackMap1", sizeof(ushort));
		LoadDirectArray(payload, 0x20, WeaponGunModelCount, sizeof(ushort), $"{fieldPath}.notetrackMap2", sizeof(ushort));
		LoadDirectArray(payload, 0x24, WeaponGunModelCount, sizeof(ushort), $"{fieldPath}.notetrackMap3", sizeof(ushort));

		LoadFxEffectAliasCell(payload, 0x48, $"{fieldPath}.flashEffect0");
		LoadFxEffectAliasCell(payload, 0x4c, $"{fieldPath}.flashEffect1");
		for (var offset = 0x50; offset <= 0x108; offset += PointerSize)
			LoadEmbeddedXString(payload, offset, $"{fieldPath}.soundAliases[{(offset - 0x50) / PointerSize}]");

		LoadXStringArrayPointer(payload, 0x10c, WeaponSurfaceCount, $"{fieldPath}.bounceSound");
		LoadFxEffectAliasCell(payload, 0x110, $"{fieldPath}.effect110");
		LoadFxEffectAliasCell(payload, 0x114, $"{fieldPath}.effect114");
		LoadFxEffectAliasCell(payload, 0x118, $"{fieldPath}.effect118");
		LoadFxEffectAliasCell(payload, 0x11c, $"{fieldPath}.effect11c");
		LoadWeaponMaterialAlias(payload, 0x120, $"{fieldPath}.material120");
		LoadWeaponMaterialAlias(payload, 0x124, $"{fieldPath}.material124");

		LoadXModelAliasPointerArray(payload, 0x1d8, WeaponGunModelCount, $"{fieldPath}.worldGunXModel");
		LoadXModelAliasCell(payload, 0x1dc, $"{fieldPath}.worldModel0");
		LoadXModelAliasCell(payload, 0x1e0, $"{fieldPath}.worldModel1");
		LoadXModelAliasCell(payload, 0x1e4, $"{fieldPath}.worldModel2");
		LoadXModelAliasCell(payload, 0x1e8, $"{fieldPath}.worldModel3");
		LoadWeaponMaterialAlias(payload, 0x1ec, $"{fieldPath}.ammoCounterIcon");
		LoadWeaponMaterialAlias(payload, 0x1f4, $"{fieldPath}.compassIcon");
		LoadWeaponMaterialAlias(payload, 0x1fc, $"{fieldPath}.overlayMaterial");
		LoadEmbeddedXString(payload, 0x20c, $"{fieldPath}.overlayReticle");
		LoadEmbeddedXString(payload, 0x214, $"{fieldPath}.overlayInterface");
		LoadEmbeddedXString(payload, 0x224, $"{fieldPath}.modeNameAlt");

		LoadWeaponMaterialAlias(payload, 0x308, $"{fieldPath}.overlayMaterial0");
		LoadWeaponMaterialAlias(payload, 0x30c, $"{fieldPath}.overlayMaterial1");
		LoadWeaponMaterialAlias(payload, 0x310, $"{fieldPath}.overlayMaterial2");
		LoadWeaponMaterialAlias(payload, 0x314, $"{fieldPath}.overlayMaterial3");
		MarkAliasAssetPointerCell(payload, 0x3c8, $"{fieldPath}.physCollmap");
		LoadXModelAliasCell(payload, 0x420, $"{fieldPath}.projectileModel");
		LoadFxEffectAliasCell(payload, 0x428, $"{fieldPath}.projExplosionEffect");
		LoadFxEffectAliasCell(payload, 0x42c, $"{fieldPath}.projDudEffect");
		LoadEmbeddedXString(payload, 0x430, $"{fieldPath}.projExplosionSound");
		LoadEmbeddedXString(payload, 0x434, $"{fieldPath}.projDudSound");
		LoadDirectArray(payload, 0x444, WeaponSurfaceCount, sizeof(int), $"{fieldPath}.parallelBounce");
		LoadDirectArray(payload, 0x448, WeaponSurfaceCount, sizeof(int), $"{fieldPath}.perpendicularBounce");
		LoadFxEffectAliasCell(payload, 0x44c, $"{fieldPath}.projImpactEffect");
		LoadFxEffectAliasCell(payload, 0x450, $"{fieldPath}.projImpactDudEffect");
		LoadFxEffectAliasCell(payload, 0x46c, $"{fieldPath}.viewShellEjectEffect");
		LoadEmbeddedXString(payload, 0x470, $"{fieldPath}.viewShellEjectSound");

		LoadEmbeddedXString(payload, 0x50c, $"{fieldPath}.accuracyGraphName0");
		LoadDirectArray(
			payload,
			0x514,
			ReadUInt16At(variantPayload.Bytes, 0x64),
			WeaponVec2Size,
			$"{fieldPath}.accuracyGraphKnots");
		LoadEmbeddedXString(payload, 0x510, $"{fieldPath}.accuracyGraphName1");
		LoadDirectArray(
			payload,
			0x518,
			ReadUInt16At(variantPayload.Bytes, 0x66),
			WeaponVec2Size,
			$"{fieldPath}.originalAccuracyGraphKnots");
		LoadEmbeddedXString(payload, 0x568, $"{fieldPath}.useHintString");
		LoadEmbeddedXString(payload, 0x56c, $"{fieldPath}.dropHintString");
		LoadEmbeddedXString(payload, 0x58c, $"{fieldPath}.scriptName");
		LoadDirectArray(payload, 0x5b4, WeaponHitLocationCount, sizeof(int), $"{fieldPath}.locationDamageMultipliers");
		LoadEmbeddedXString(payload, 0x5b8, $"{fieldPath}.fireRumble");
		LoadEmbeddedXString(payload, 0x5bc, $"{fieldPath}.meleeImpactRumble");
		LoadTracerAliasCell(payload, 0x5c0, $"{fieldPath}.tracer");
		LoadEmbeddedXString(payload, 0x5dc, $"{fieldPath}.turretOverheatSound");
		LoadFxEffectAliasCell(payload, 0x5e0, $"{fieldPath}.turretOverheatEffect");
		LoadEmbeddedXString(payload, 0x5e4, $"{fieldPath}.turretBarrelSpinRumble");
		LoadEmbeddedXString(payload, 0x5f4, $"{fieldPath}.turretBarrelSpinMaxSnd");
		LoadXStringArrayRoot(payload, 0x5f8, 4, $"{fieldPath}.turretBarrelSpinUpSnd");
		LoadXStringArrayRoot(payload, 0x608, 4, $"{fieldPath}.turretBarrelSpinDownSnd");
		LoadEmbeddedXString(payload, 0x618, $"{fieldPath}.missileConeSoundAlias");
		LoadEmbeddedXString(payload, 0x61c, $"{fieldPath}.missileConeSoundAliasAtBase");
	}

	private void LoadFxEffectAliasCell(PointerPayload payload, int offset, string fieldPath)
	{
		LoadAliasInlineObjectInBlock(
			payload,
			offset,
			XFILE_BLOCK.TEMP,
			FxEffectRootSize,
			(loaded, loadedPath) =>
			{
				PushStreamBlock(XFILE_BLOCK.LARGE);
				try
				{
					ConsumeFxEffectRoot(loaded, loadedPath);
				}
				finally
				{
					PopStreamBlock();
				}
			},
			fieldPath);
	}

	private void LoadTracerAliasCell(PointerPayload payload, int offset, string fieldPath)
	{
		LoadAliasInlineObjectInBlock(
			payload,
			offset,
			XFILE_BLOCK.TEMP,
			TracerRootSize,
			(loaded, loadedPath) =>
			{
				PushStreamBlock(XFILE_BLOCK.LARGE);
				try
				{
					LoadEmbeddedXString(loaded, 0x00, $"{loadedPath}.name");
					LoadWeaponMaterialAlias(loaded, 0x04, $"{loadedPath}.material");
				}
				finally
				{
					PopStreamBlock();
				}
			},
			fieldPath);
	}

	private void ConsumeFxEffectRoot(PointerPayload payload, string fieldPath)
	{
		LoadEmbeddedXString(payload, 0x00, $"{fieldPath}.name");
		var childCount = checked(ReadInt32At(payload.Bytes, 0x10)
			+ ReadInt32At(payload.Bytes, 0x14)
			+ ReadInt32At(payload.Bytes, 0x18));
		var children = LoadDirectArray(payload, 0x1c, childCount, FxEffectRootSize, $"{fieldPath}.children");
		if (children is null)
			return;

		for (var i = 0; i < childCount; i++)
			ConsumeFxEffectRoot(
				SubPayload(children.Value, checked(i * FxEffectRootSize), FxEffectRootSize),
				$"{fieldPath}.children[{i}]");
	}

	private void LoadXModelAliasPointerArray(PointerPayload payload, int offset, int count, string fieldPath)
	{
		var array = LoadDirectArray(payload, offset, count, PointerSize, fieldPath);
		if (array is null)
			return;

		for (var i = 0; i < count; i++)
			LoadXModelAliasCell(array.Value, checked(i * PointerSize), $"{fieldPath}[{i}]");
	}

	private void LoadXModelAliasCell(PointerPayload payload, int offset, string fieldPath)
	{
		LoadAliasInlineObjectInBlock(
			payload,
			offset,
			XFILE_BLOCK.TEMP,
			XModelRootSize,
			(loaded, loadedPath) =>
			{
				PushStreamBlock(XFILE_BLOCK.LARGE);
				try
				{
					ConsumeXModelRoot(loaded, loadedPath);
				}
				finally
				{
					PopStreamBlock();
				}
			},
			fieldPath);
	}

	private void ConsumeXModelRoot(PointerPayload payload, string fieldPath)
	{
		var lodCount = payload.Bytes[0x04];
		var collLodCount = payload.Bytes[0x05];
		var materialCount = payload.Bytes[0x06];
		var lodDelta = Math.Max(0, lodCount - collLodCount);

		LoadEmbeddedXString(payload, 0x00, $"{fieldPath}.name");
		LoadDirectArray(payload, 0x24, lodCount, sizeof(ushort), $"{fieldPath}.boneNames", sizeof(ushort));
		LoadDirectByteArray(payload, 0x28, lodDelta, $"{fieldPath}.parentList");
		LoadDirectArray(payload, 0x2c, checked(lodDelta * 4), sizeof(ushort), $"{fieldPath}.tagAngles", sizeof(ushort));
		LoadDirectArray(payload, 0x30, checked(lodDelta * 3), sizeof(int), $"{fieldPath}.tagPositions");
		LoadDirectByteArray(payload, 0x34, lodCount, $"{fieldPath}.partClassification");
		LoadDirectArray(payload, 0x38, lodCount, 0x20, $"{fieldPath}.baseMat");
		LoadMaterialAliasPointerArray(payload, 0x3c, materialCount, $"{fieldPath}.materials");
		LoadDirectArray(payload, 0x40, 4, 0x28, $"{fieldPath}.lodInfo");

		var collSurfCount = ReadInt32At(payload.Bytes, 0xe8);
		LoadDirectArray(payload, 0xe4, collSurfCount, 0x24, $"{fieldPath}.collSurfs");
		LoadDirectArray(payload, 0xf0, lodCount, 0x1c, $"{fieldPath}.surfs");
		LoadDirectArray(payload, 0x110, materialCount, sizeof(ushort), $"{fieldPath}.boneInfo", sizeof(ushort));
		LoadAliasInlineObjectInBlock(
			payload,
			0x118,
			XFILE_BLOCK.TEMP,
			WeaponStringPairObjectSize,
			(loaded, loadedPath) =>
			{
				PushStreamBlock(XFILE_BLOCK.LARGE);
				try
				{
					ConsumeWeaponStringPairObject(loaded, loadedPath);
				}
				finally
				{
					PopStreamBlock();
				}
			},
			$"{fieldPath}.stringPair");
		MarkAliasAssetPointerCell(payload, 0x11c, $"{fieldPath}.physPreset");
	}

	private void LoadMaterialAliasPointerArray(PointerPayload payload, int offset, int count, string fieldPath)
	{
		var array = LoadDirectArray(payload, offset, count, PointerSize, fieldPath);
		if (array is null)
			return;

		for (var i = 0; i < count; i++)
			LoadWeaponMaterialAlias(array.Value, checked(i * PointerSize), $"{fieldPath}[{i}]");
	}

	private void LoadAliasAssetPointerArray(PointerPayload payload, int offset, int count, string fieldPath)
	{
		var array = LoadDirectArray(payload, offset, count, PointerSize, fieldPath);
		if (array is null)
			return;

		for (var i = 0; i < count; i++)
			MarkAliasAssetPointerCell(array.Value, checked(i * PointerSize), $"{fieldPath}[{i}]");
	}

	private void MarkAliasAssetPointerCell(PointerPayload payload, int offset, string fieldPath)
	{
		var pointerReader = offset;
		var pointer = ReadAliasPointer<BaseAsset>(
			payload.Bytes,
			ref pointerReader,
			payload.SourceOffset + offset,
			payload.Block,
			payload.StreamOffset + offset,
			fieldPath);
		MarkAliasAssetPointer(pointer);
	}

	private void ConsumeWeaponSubroot(PointerPayload payload, string fieldPath)
	{
		var attachmentCount = ReadInt32At(payload.Bytes, 0x08);
		var runtimeCount = ReadInt32At(payload.Bytes, 0x0c);
		var sharedCount10 = ReadInt32At(payload.Bytes, 0x10);
		var sharedCount14 = ReadInt32At(payload.Bytes, 0x14);
		var sharedCount18 = ReadInt32At(payload.Bytes, 0x18);
		var runtimeCount24 = ReadInt32At(payload.Bytes, 0x24);
		var sharedCount2c = ReadInt32At(payload.Bytes, 0x2c);

		var attachments = LoadNonZeroInlineArray(
			payload,
			0x30,
			attachmentCount,
			WeaponAttachmentEntrySize,
			PointerSize,
			$"{fieldPath}.attachments");
		if (attachments is not null)
		{
			for (var i = 0; i < attachmentCount; i++)
				ConsumeWeaponAttachmentEntry(
					SubPayload(attachments.Value, checked(i * WeaponAttachmentEntrySize), WeaponAttachmentEntrySize),
					$"{fieldPath}.attachments[{i}]");
		}

		LoadWeaponRuntimeArray(payload, 0x34, runtimeCount, 0x20, $"{fieldPath}.runtime34");
		LoadWeaponRuntimeArray(payload, 0x38, runtimeCount, 0x20, $"{fieldPath}.runtime38");
		LoadWeaponRuntimeArray(payload, 0x3c, runtimeCount, 0x24, $"{fieldPath}.runtime3c");
		LoadWeaponRuntimeArray(payload, 0x40, runtimeCount24, 0x04, $"{fieldPath}.runtime40");
		LoadWeaponRuntimeArray(payload, 0x44, sharedCount10, 0x04, $"{fieldPath}.runtime44");
		LoadWeaponRuntimeArray(payload, 0x48, checked(sharedCount10 * sharedCount18), 0x04, $"{fieldPath}.runtime48");
		LoadWeaponRuntimeArray(payload, 0x4c, AlignCount(sharedCount14, 0x10), 0x01, $"{fieldPath}.runtime4c");
		LoadWeaponRuntimeArray(payload, 0x50, runtimeCount, 0x0c, $"{fieldPath}.runtime50");
		LoadWeaponRuntimeArray(payload, 0x54, AlignCount(runtimeCount, 0x04), 0x04, $"{fieldPath}.runtime54");
		LoadNonZeroInlineArray(payload, 0x58, sharedCount14, 0x02, PointerSize, $"{fieldPath}.array58");
		LoadNonZeroInlineArray(payload, 0x5c, sharedCount14, 0x34, PointerSize, $"{fieldPath}.array5c");
		LoadNonZeroInlineArray(payload, 0x60, sharedCount2c, 0x04, PointerSize, $"{fieldPath}.array60");
	}

	private void ConsumeWeaponAttachmentEntry(PointerPayload payload, string fieldPath)
	{
		LoadAliasInlineObjectInBlock(
			payload,
			0x20,
			XFILE_BLOCK.TEMP,
			WeaponStringPairObjectSize,
			(loaded, loadedPath) =>
			{
				PushStreamBlock(XFILE_BLOCK.LARGE);
				try
				{
					ConsumeWeaponStringPairObject(loaded, loadedPath);
				}
				finally
				{
					PopStreamBlock();
				}
			},
			$"{fieldPath}.stringPair");

		LoadWeaponMaterialAlias(payload, 0x18, $"{fieldPath}.material18");
		LoadWeaponMaterialAlias(payload, 0x1c, $"{fieldPath}.material1c");
	}

	private void LoadWeaponMaterialAlias(PointerPayload payload, int offset, string fieldPath)
	{
		LoadAliasInlineObjectInBlock(
			payload,
			offset,
			XFILE_BLOCK.TEMP,
			MaterialRootSize,
			(loaded, loadedPath) =>
			{
				PushStreamBlock(XFILE_BLOCK.LARGE);
				try
				{
					ConsumeMaterialRoot(loaded, loadedPath);
				}
				finally
				{
					PopStreamBlock();
				}
			},
			fieldPath);
	}

	private void ConsumeWeaponStringPairObject(PointerPayload payload, string fieldPath)
	{
		LoadEmbeddedXString(payload, 0x00, $"{fieldPath}.name");
		LoadEmbeddedXString(payload, 0x1c, $"{fieldPath}.displayName");
	}

	private void LoadWeaponRuntimeArray(
		PointerPayload payload,
		int offset,
		int count,
		int elementSize,
		string fieldPath)
	{
		PushStreamBlock(XFILE_BLOCK.RUNTIME);
		try
		{
			LoadNonZeroInlineArray(payload, offset, count, elementSize, PointerSize, fieldPath);
		}
		finally
		{
			PopStreamBlock();
		}
	}

	private static int AlignCount(int count, int alignment)
	{
		if (count < 0)
			return count;
		var mask = alignment - 1;
		return checked((count + mask) & ~mask);
	}

	private void LoadPs3MaterialTechniqueRecords(DirectPointer<byte[]> pointer, int count, string fieldPath)
	{
		var payload = LoadNonZeroInlineArray(
			pointer,
			count,
			MaterialTechniqueRecordSize,
			PointerSize,
			fieldPath);
		if (payload is null)
			return;

		for (var i = 0; i < count; i++)
			ConsumePs3MaterialTechniqueRecord(
				SubPayload(payload.Value, checked(i * MaterialTechniqueRecordSize), MaterialTechniqueRecordSize),
				$"{fieldPath}[{i}]");
	}

	private void ConsumePs3MaterialTechniqueRecord(PointerPayload payload, string fieldPath)
	{
		LoadNonZeroInlineObject(
			payload,
			0x00,
			MaterialTechniqueRecordSize,
			ConsumePs3MaterialTechniqueNestedRecord,
			$"{fieldPath}.nested");
	}

	private void ConsumePs3MaterialTechniqueNestedRecord(PointerPayload payload, string fieldPath)
	{
		var childCount = ReadUInt16At(payload.Bytes, 0x18);
		var byteCount = ReadInt32At(payload.Bytes, 0x3c);

		ConsumePs3MaterialTechniqueSubRecord(
			SubPayload(payload, 0x18, MaterialTechniqueSubRecordSize),
			byteCount,
			$"{fieldPath}.subRecord");

		LoadDirectArray(
			payload,
			0x40,
			childCount,
			MaterialTechniqueArray20EntrySize,
			$"{fieldPath}.array20");
	}

	private void ConsumePs3MaterialTechniqueSubRecord(PointerPayload payload, int byteCount, string fieldPath)
	{
		var childCount = ReadUInt16At(payload.Bytes, 0x00);
		var childArray = LoadNonZeroInlineArray(
			payload,
			0x04,
			childCount,
			MaterialTechniqueChildEntrySize,
			PointerSize,
			$"{fieldPath}.children");
		if (childArray is not null)
		{
			for (var i = 0; i < childCount; i++)
				ConsumePs3MaterialTechniqueChildEntry(
					SubPayload(childArray.Value, checked(i * MaterialTechniqueChildEntrySize), MaterialTechniqueChildEntrySize),
					$"{fieldPath}.children[{i}]");
		}

		LoadDirectArray(
			payload,
			0x08,
			byteCount,
			MaterialTechniqueByteEntrySize,
			$"{fieldPath}.bytes",
			1);
	}

	private void ConsumePs3MaterialTechniqueChildEntry(PointerPayload payload, string fieldPath)
	{
		var count = ReadInt32At(payload.Bytes, 0x04);
		LoadDirectArray(
			payload,
			0x00,
			count,
			MaterialTechniqueArray20EntrySize,
			$"{fieldPath}.array20");
	}

	private StructuredDataDefSet LoadStructuredDataDefSet(AliasPointer<BaseAsset> pointer)
	{
		var root = LoadPointerPayload(
			pointer, CurrentStreamBlock,
			StructuredDataDefSetSize,
			PointerSize,
			"StructuredDataDefSet");

		var rootReader = 0;
		var namePtr = ReadDirectPointer<string>(
			root.Bytes,
			ref rootReader,
			root.SourceOffset,
			root.Block,
			root.StreamOffset,
			"StructuredDataDefSet.name");
		var defCount = ReadInt32(root.Bytes, ref rootReader);
		var defsPtr = ReadDirectPointer<StructuredDataDef[]>(
			root.Bytes,
			ref rootReader,
			root.SourceOffset + 0x08,
			root.Block,
			root.StreamOffset + 0x08,
			"StructuredDataDefSet.defs");

		PushStreamBlock(XFILE_BLOCK.LARGE);
		try
		{
			LoadXString(namePtr, CurrentStreamBlock, "StructuredDataDefSet.name");
			LoadStructuredDataDefArray(defsPtr, defCount, "StructuredDataDefSet.defs");
		}
		finally
		{
			PopStreamBlock();
		}

		return new StructuredDataDefSet
		{
			Offset = root.SourceOffset,
			NamePtr = namePtr,
			DefCount = defCount,
			DefsPtr = defsPtr
		};
	}

	private void LoadByteArray(DirectPointer<byte[]> pointer, XFILE_BLOCK block, int length, string fieldPath)
	{
		if (length < 0)
			throw new InvalidDataException($"Invalid byte array length {length} for {fieldPath}.");

		if (pointer.Kind == PointerKind.Offset)
		{
			_offsetPointers.Add(pointer);
			RegisterLoadedOffsetTarget(pointer, length);
			SetPointerResult(pointer, []);
			return;
		}

		var payload = LoadPointerPayload(pointer, block, length, 1, fieldPath);
		SetPointerResult(pointer, payload.Bytes);
	}

	private void LoadDirectByteArray(PointerPayload payload, int offset, int length, string fieldPath)
	{
		if (length < 0)
			throw new InvalidDataException($"Invalid byte array length {length} for {fieldPath}.");

		var pointerReader = offset;
		var pointer = ReadDirectPointer<byte[]>(
			payload.Bytes,
			ref pointerReader,
			payload.SourceOffset + offset,
			payload.Block,
			payload.StreamOffset + offset,
			fieldPath);

		if (pointer.Kind == PointerKind.Null)
		{
			SetPointerResult(pointer, []);
			return;
		}

		if (pointer.Kind == PointerKind.Offset)
		{
			_offsetPointers.Add(pointer);
			RegisterLoadedOffsetTarget(pointer, length);
			SetPointerResult(pointer, []);
			return;
		}

		var loaded = LoadCurrentStreamPayload(pointer, CurrentStreamBlock, length, PointerSize, fieldPath);
		SetPointerResult(pointer, loaded.Bytes);
	}

	private void LoadStringTableCells(
		DirectPointer<StringTableCell[]> pointer,
		int count,
		string fieldPath)
	{
		if (count < 0)
			throw new InvalidDataException($"Invalid string table cell count {count} for {fieldPath}.");

		if (pointer.Kind == PointerKind.Null)
		{
			SetPointerResult(pointer, []);
			return;
		}

		if (pointer.Kind == PointerKind.Offset)
		{
			_offsetPointers.Add(pointer);
			SetPointerResult(pointer, []);
			return;
		}

		var payload = LoadPointerPayload(
			pointer, CurrentStreamBlock,
			checked(count * StringTableCellSize),
			PointerSize,
			fieldPath);
		RegisterArrayElementStarts(payload, count, StringTableCellSize);

		var cells = new StringTableCell[count];
		var cellReader = 0;
		for (var i = 0; i < cells.Length; i++)
		{
			var cellSourceOffset = payload.SourceOffset + cellReader;
			var cellStreamOffset = payload.StreamOffset + cellReader;
			var stringPtr = ReadDirectPointer<string>(
				payload.Bytes,
				ref cellReader,
				cellSourceOffset,
				payload.Block,
				cellStreamOffset,
				$"{fieldPath}[{i}].string");
			var hash = ReadInt32(payload.Bytes, ref cellReader);

			LoadXString(stringPtr, CurrentStreamBlock, $"{fieldPath}[{i}].string");

			cells[i] = new StringTableCell
			{
				StringPtr = stringPtr,
				Hash = hash
			};
		}

		StringTableCell.ApplyLogicalStringValues(cells);
		SetPointerResult(pointer, cells);
	}

	private void LoadMenuPointerArray(
		DirectPointer<AliasPointer<MenuDef>[]> pointer,
		int count,
		string fieldPath)
	{
		if (count < 0)
			throw new InvalidDataException($"Invalid menu pointer count {count} for {fieldPath}.");

		if (pointer.Kind == PointerKind.Null)
		{
			SetPointerResult(pointer, []);
			return;
		}

		if (pointer.Kind == PointerKind.Offset)
		{
			_offsetPointers.Add(pointer);
			SetPointerResult(pointer, []);
			return;
		}

		var payload = LoadPointerPayload(
			pointer, CurrentStreamBlock,
			checked(count * PointerSize),
			PointerSize,
			fieldPath);
		RegisterArrayElementStarts(payload, count, PointerSize);

		var menus = new AliasPointer<MenuDef>[count];
		var menuReader = 0;
		for (var i = 0; i < menus.Length; i++)
		{
			var menuPointerOffset = menuReader;
			var menuPtr = ReadAliasPointer<MenuDef>(
				payload.Bytes,
				ref menuReader,
				payload.SourceOffset + menuPointerOffset,
				payload.Block,
				payload.StreamOffset + menuPointerOffset,
				$"{fieldPath}[{i}]");

			LoadMenuDefPointer(menuPtr, $"{fieldPath}[{i}]");
			menus[i] = menuPtr;
		}

		SetPointerResult(pointer, menus);
	}

	private void LoadMenuDefPointer(AliasPointer<MenuDef> pointer, string fieldPath)
	{
		if (pointer.Kind == PointerKind.Null)
		{
			SetPointerResult(pointer, null);
			return;
		}

		if (pointer.Kind == PointerKind.Offset)
		{
			_offsetPointers.Add(pointer);
			RegisterLoadedOffsetTarget(pointer, MenuDefSize);
			return;
		}

		PushStreamBlock(XFILE_BLOCK.TEMP);
		try
		{
			var payload = LoadPointerPayload(pointer, CurrentStreamBlock, MenuDefSize, PointerSize, fieldPath);
			var menuDef = new MenuDef
			{
				Offset = payload.SourceOffset
			};

			PushStreamBlock(XFILE_BLOCK.LARGE);
			try
			{
				ConsumeMenuDef(payload, fieldPath);
			}
			finally
			{
				PopStreamBlock();
			}
			SetPointerResult(pointer, menuDef);
		}
		finally
		{
			PopStreamBlock();
		}
	}

		private void ConsumeMenuDef(PointerPayload payload, string fieldPath)
		{
			LoadInlineOnlyDirectObject(payload, 0x2ec, MenuEventHandlerSize, ConsumeMenuEventHandlerSet, $"{fieldPath}.legacyEventHandler");
		ConsumeMenuWindow(SubPayload(payload, 0x00, MenuWindowSize), $"{fieldPath}.window");
		LoadEmbeddedXString(payload, 0xb0, $"{fieldPath}.font");

		LoadMenuStatementPointer(payload, 0xe4, $"{fieldPath}.onOpen");
		LoadMenuStatementPointer(payload, 0xec, $"{fieldPath}.onRequestClose");
		LoadMenuStatementPointer(payload, 0xe8, $"{fieldPath}.onClose");
		LoadMenuStatementPointer(payload, 0xf0, $"{fieldPath}.onEsc");
		LoadExpressionSupportingDataPointer(payload, 0xf4, $"{fieldPath}.expressionData");
		LoadInlineOnlyDirectObject(payload, 0xf8, MenuStatementSize, ConsumeMenuStatement, $"{fieldPath}.visibleExp");
		LoadEmbeddedXString(payload, 0xfc, $"{fieldPath}.allowedBinding");
		LoadEmbeddedXString(payload, 0x100, $"{fieldPath}.soundName");

		LoadInlineOnlyDirectObject(payload, 0x118, MenuStatementSize, ConsumeMenuStatement, $"{fieldPath}.rectXExp");
		LoadInlineOnlyDirectObject(payload, 0x11c, MenuStatementSize, ConsumeMenuStatement, $"{fieldPath}.rectYExp");
		LoadInlineOnlyDirectObject(payload, 0x120, MenuStatementSize, ConsumeMenuStatement, $"{fieldPath}.rectHExp");
		LoadInlineOnlyDirectObject(payload, 0x124, MenuStatementSize, ConsumeMenuStatement, $"{fieldPath}.rectWExp");

		var itemCount = ReadInt32At(payload.Bytes, 0xb8);
		LoadMenuItemPointerArray(payload, 0x128, itemCount, $"{fieldPath}.items");
	}

	private void ConsumeMenuWindow(PointerPayload payload, string fieldPath)
	{
		LoadEmbeddedXString(payload, 0x00, $"{fieldPath}.name");
		LoadEmbeddedXString(payload, 0x2c, $"{fieldPath}.group");
		LoadWeaponMaterialAlias(payload, 0xac, $"{fieldPath}.background");
	}

	private void ConsumeMaterialRoot(PointerPayload payload, string fieldPath)
	{
		LoadEmbeddedXString(payload, 0x00, $"{fieldPath}.info.name");
		LoadMaterialNonZeroInlineArray(payload, 0x90, 0x25, 0x02, XFILE_BLOCK.RUNTIME, 0x02, $"{fieldPath}.textureTable");

		var techniqueSetReader = 0x94;
		var techniqueSetPtr = ReadAliasPointer<BaseAsset>(
			payload.Bytes,
			ref techniqueSetReader,
			payload.SourceOffset + 0x94,
			payload.Block,
			payload.StreamOffset + 0x94,
			$"{fieldPath}.techniqueSet");
		if (techniqueSetPtr.Kind == PointerKind.Null)
			SetPointerResult(techniqueSetPtr, null);
		else if (techniqueSetPtr.Kind == PointerKind.Offset)
			_offsetPointers.Add(techniqueSetPtr);
		else
			LoadXAssetHeader(XAssetType.Techset, techniqueSetPtr);

		LoadMaterialCountedDirectArray(
			payload,
			0x98,
			payload.Bytes[0x3d],
			MaterialConstantDefSize,
			ConsumeMaterialConstantDef,
			$"{fieldPath}.constantTable");
		LoadMaterialCountedDirectArray(
			payload,
			0x9c,
			payload.Bytes[0x3e],
			MaterialStateMapSize,
			null,
			$"{fieldPath}.stateMap");
		LoadMaterialCountedDirectArray(
			payload,
			0xa0,
			payload.Bytes[0x3f],
			MaterialWaterDefSize,
			ConsumeMaterialWaterDef,
			$"{fieldPath}.water");

		var stringCount = payload.Bytes[0x42];
		LoadMaterialStateBitsPointer(payload, 0xa4, stringCount, $"{fieldPath}.stateBits");
	}

	private void LoadMaterialCountedDirectArray(
		PointerPayload payload,
		int offset,
		int count,
		int elementSize,
		Action<PointerPayload, string>? consume,
		string fieldPath)
	{
		var array = LoadDirectArray(payload, offset, count, elementSize, fieldPath);
		if (array is null || consume is null)
			return;

		for (var i = 0; i < count; i++)
			consume(SubPayload(array.Value, checked(i * elementSize), elementSize), $"{fieldPath}[{i}]");
	}

	private void ConsumeMaterialConstantDef(PointerPayload payload, string fieldPath)
	{
		if (payload.Bytes[0x07] == 0x0b)
		{
			LoadDirectObject(
				payload,
				0x08,
				MaterialConstantLiteralSize,
				ConsumeMaterialConstantLiteral,
				$"{fieldPath}.literal");
			return;
		}

		LoadAliasInlineObjectInBlock(
			payload,
			0x08,
			XFILE_BLOCK.TEMP,
			MaterialConstantAssetSize,
			ConsumeMaterialConstantAsset,
			$"{fieldPath}.asset");
	}

	private void ConsumeMaterialConstantAsset(PointerPayload payload, string fieldPath)
	{
		PushStreamBlock(XFILE_BLOCK.LARGE);
		try
		{
			LoadEmbeddedXString(payload, 0x4c, $"{fieldPath}.name");
		}
		finally
		{
			PopStreamBlock();
		}

		RegisterPayloadStart(payload, 0x28, checked(payload.Bytes.Length - 0x28));
	}

	private void ConsumeMaterialConstantLiteral(PointerPayload payload, string fieldPath)
	{
		var width = ReadInt32At(payload.Bytes, 0x10);
		var height = ReadInt32At(payload.Bytes, 0x14);
		var byteCount = checked(width * height);

		LoadDirectByteArray(payload, 0x04, byteCount, $"{fieldPath}.data0");
		LoadDirectByteArray(payload, 0x08, byteCount, $"{fieldPath}.data1");
		LoadDirectByteArray(payload, 0x0c, byteCount, $"{fieldPath}.data2");
		LoadAliasInlineObject(payload, 0x44, MaterialConstantAssetSize, ConsumeMaterialConstantAsset, $"{fieldPath}.fallback");
	}

	private void ConsumeMaterialWaterDef(PointerPayload payload, string fieldPath)
	{
		LoadAliasInlineObjectInBlock(payload, 0x00, XFILE_BLOCK.TEMP, 0x08, null, $"{fieldPath}.image");
		RegisterPayloadStart(payload, 0x04, 0x04);
	}

	private void LoadMaterialNonZeroInlineArray(
		PointerPayload payload,
		int offset,
		int count,
		int elementSize,
		XFILE_BLOCK block,
		int alignment,
		string fieldPath)
	{
		var pointerReader = offset;
		var pointer = ReadDirectPointer<byte[]>(
			payload.Bytes,
			ref pointerReader,
			payload.SourceOffset + offset,
			payload.Block,
			payload.StreamOffset + offset,
			fieldPath);

		if (pointer.Kind == PointerKind.Null)
		{
			SetPointerResult(pointer, []);
			return;
		}

		if (pointer.Kind == PointerKind.Offset)
		{
			_offsetPointers.Add(pointer);
			RegisterLoadedOffsetTarget(pointer, checked(count * elementSize));
			SetPointerResult(pointer, []);
			return;
		}

		var array = LoadCurrentStreamPayload(
			pointer,
			block,
			checked(count * elementSize),
			alignment,
			fieldPath);
		SetPointerResult(pointer, array.Bytes);
	}

	private void LoadMaterialStateBitsPointer(PointerPayload payload, int offset, int count, string fieldPath)
	{
		var pointerReader = offset;
		var pointer = ReadDirectPointer<byte[]>(
			payload.Bytes,
			ref pointerReader,
			payload.SourceOffset + offset,
			payload.Block,
			payload.StreamOffset + offset,
			fieldPath);

		if (pointer.Kind == PointerKind.Null)
		{
			SetPointerResult(pointer, []);
			return;
		}

		if (pointer.Kind == PointerKind.Offset)
		{
			_offsetPointers.Add(pointer);
			RegisterLoadedOffsetTarget(pointer, checked(count * PointerSize));
			SetPointerResult(pointer, []);
			return;
		}

		var array = LoadCurrentStreamPayload(
			pointer, CurrentStreamBlock,
			checked(count * PointerSize),
			PointerSize,
			fieldPath);
		SetPointerResult(pointer, array.Bytes);

		for (var i = 0; i < count; i++)
			LoadEmbeddedXString(array, checked(i * PointerSize), $"{fieldPath}[{i}]");
	}

		private void LoadMenuStatementPointer(PointerPayload payload, int offset, string fieldPath)
		{
			TraceMenuStatementPointer(payload, offset, fieldPath, "before");
			LoadDirectObject(payload, offset, 0x08, ConsumeMenuStatementReference, fieldPath);
			TraceMenuStatementPointer(payload, offset, fieldPath, "after");
		}

		private void TraceMenuStatementPointer(PointerPayload payload, int offset, string fieldPath, string phase)
		{
			if (!ShouldTraceMenuField(fieldPath))
				return;

			var raw = ReadInt32At(payload.Bytes, offset);
			Console.WriteLine($"trace {phase} {fieldPath} raw=0x{raw:X8} pos=0x{_position:X} large=0x{_streamBlockOffsets[(int)XFILE_BLOCK.LARGE]:X}");
		}

		private static bool ShouldTraceMenuField(string fieldPath)
		{
			var assetFilter = Environment.GetEnvironmentVariable("FF_TRACE_XFILE_ASSET");
			if (string.IsNullOrWhiteSpace(assetFilter))
				assetFilter = "XAssetList.assets[49]";

			var fieldFilter = Environment.GetEnvironmentVariable("FF_TRACE_XFILE_FIELD");
			if (string.IsNullOrWhiteSpace(fieldFilter))
				fieldFilter = ".items[0].";

			return Environment.GetEnvironmentVariable("FF_TRACE_XFILE") == "1"
				&& fieldPath.Contains(assetFilter, StringComparison.Ordinal)
				&& fieldPath.Contains(fieldFilter, StringComparison.Ordinal);
		}

	private void ConsumeMenuStatementReference(PointerPayload payload, string fieldPath)
	{
		var count = ReadInt32At(payload.Bytes, 0x00);
		LoadMenuExpressionPointerArray(payload, 0x04, count, $"{fieldPath}.entries");
	}

	private void ConsumeMenuStatement(PointerPayload payload, string fieldPath)
	{
			var count = ReadInt32At(payload.Bytes, 0x00);
			LoadMenuExpressionEntryArray(payload, 0x04, count, $"{fieldPath}.entries");
			LoadInlineOnlyDirectObject(payload, 0x08, MenuEventHandlerSize, ConsumeMenuEventHandlerSet, $"{fieldPath}.eventHandler");
		}

	private void LoadMenuExpressionEntryArray(PointerPayload payload, int offset, int count, string fieldPath)
	{
		var array = LoadDirectArray(payload, offset, count, MenuExpressionEntrySize, fieldPath);
		if (array is null)
			return;

		for (var i = 0; i < count; i++)
			ConsumeMenuExpressionEntry(SubPayload(array.Value, checked(i * MenuExpressionEntrySize), MenuExpressionEntrySize), $"{fieldPath}[{i}]");
	}

	private void LoadMenuExpressionPointerArray(PointerPayload payload, int offset, int count, string fieldPath)
	{
		var array = LoadDirectArray(payload, offset, count, PointerSize, fieldPath);
			if (array is null)
				return;

			for (var i = 0; i < count; i++)
				LoadDirectObject(array.Value, checked(i * PointerSize), MenuStatementUnionEntrySize, ConsumeMenuStatementUnionEntry, $"{fieldPath}[{i}]");
		}

		private void ConsumeMenuStatementUnionEntry(PointerPayload payload, string fieldPath)
		{
			var kind = payload.Bytes[0x04];
			switch (kind)
			{
				case 0:
					LoadEmbeddedXString(payload, 0x00, $"{fieldPath}.string");
					break;
				case 1:
					LoadDirectObject(payload, 0x00, MenuStatementKind1Size, ConsumeMenuStatementKind1, $"{fieldPath}.operand");
					break;
				case 2:
					LoadMenuStatementPointer(payload, 0x00, $"{fieldPath}.statement");
					break;
				case >= 3 and <= 6:
					LoadDirectObject(payload, 0x00, MenuStatementStringHandlerSize, ConsumeMenuStatementStringHandler, $"{fieldPath}.stringHandler");
					break;
			}
		}

		private void ConsumeMenuStatementKind1(PointerPayload payload, string fieldPath)
		{
			LoadInlineOnlyDirectObject(payload, 0x04, MenuStatementSize, ConsumeMenuStatement, $"{fieldPath}.handler");
			LoadMenuStatementPointer(payload, 0x00, $"{fieldPath}.statement");
		}

		private void ConsumeMenuStatementStringHandler(PointerPayload payload, string fieldPath)
		{
			LoadEmbeddedXString(payload, 0x00, $"{fieldPath}.string");
			LoadInlineOnlyDirectObject(payload, 0x04, MenuStatementSize, ConsumeMenuStatement, $"{fieldPath}.handler");
		}

		private void ConsumeMenuExpressionEntry(PointerPayload payload, string fieldPath)
		{
			var kind = ReadInt32At(payload.Bytes, 0x00);
			if (kind == 0)
				return;

			ConsumeMenuExpressionEntryChild(SubPayload(payload, 0x04, 0x08), $"{fieldPath}.child");
		}

		private void ConsumeMenuExpressionEntryChild(PointerPayload payload, string fieldPath)
		{
			var kind = ReadInt32At(payload.Bytes, 0x00);
			switch (kind)
			{
				case 2:
					LoadEmbeddedXString(payload, 0x04, $"{fieldPath}.string");
					break;
				case 3:
					LoadInlineOnlyDirectObject(payload, 0x04, MenuStatementSize, ConsumeMenuStatement, $"{fieldPath}.statement");
					break;
			}
		}

		private void ConsumeMenuEventHandler(PointerPayload payload, string fieldPath)
		{
			LoadMenuStringListRoot(SubPayload(payload, 0x10, MenuStringListSize), $"{fieldPath}.strings");
		}

		private void ConsumeMenuEventHandlerSet(PointerPayload payload, string fieldPath)
		{
			ConsumeMenuEventHandlerStatementArrayRoot(SubPayload(payload, 0x00, MenuEventHandlerChildSize), $"{fieldPath}.statements");
			ConsumeMenuEventHandlerStringEntryArrayRoot(SubPayload(payload, 0x08, MenuEventHandlerChildSize), $"{fieldPath}.stringEntries");
			LoadMenuStringListRoot(SubPayload(payload, 0x10, MenuStringListSize), $"{fieldPath}.strings");
		}

		private void ConsumeMenuEventHandlerStatementArrayRoot(PointerPayload payload, string fieldPath)
		{
			var count = ReadInt32At(payload.Bytes, 0x00);
			var array = LoadDirectArray(payload, 0x04, count, PointerSize, $"{fieldPath}.values");
			if (array is null)
				return;

			for (var i = 0; i < count; i++)
				LoadInlineOnlyDirectObject(array.Value, checked(i * PointerSize), MenuStatementSize, ConsumeMenuStatement, $"{fieldPath}.values[{i}]");
		}

		private void ConsumeMenuEventHandlerStringEntryArrayRoot(PointerPayload payload, string fieldPath)
		{
			var count = ReadInt32At(payload.Bytes, 0x00);
			var array = LoadDirectArray(payload, 0x04, count, PointerSize, $"{fieldPath}.values");
			if (array is null)
				return;

			for (var i = 0; i < count; i++)
				LoadDirectObject(array.Value, checked(i * PointerSize), MenuEventHandlerStringEntrySize, ConsumeMenuEventHandlerStringEntry, $"{fieldPath}.values[{i}]");
		}

		private void ConsumeMenuEventHandlerStringEntry(PointerPayload payload, string fieldPath)
		{
			LoadEmbeddedXString(payload, 0x04, $"{fieldPath}.string");
		}

	private void LoadMenuStringListRoot(PointerPayload payload, string fieldPath)
	{
		var count = ReadInt32At(payload.Bytes, 0x00);
		LoadXStringArrayPointer(payload, 0x04, count, $"{fieldPath}.values");
	}

	private void LoadXStringArrayPointer(PointerPayload payload, int offset, int count, string fieldPath)
	{
		var array = LoadDirectArray(payload, offset, count, PointerSize, fieldPath);
		if (array is null)
			return;

		for (var i = 0; i < count; i++)
			LoadEmbeddedXString(array.Value, checked(i * PointerSize), $"{fieldPath}[{i}]");
	}

	private void LoadExpressionSupportingDataPointer(PointerPayload payload, int offset, string fieldPath)
	{
		LoadDirectObject(payload, offset, MenuExpressionSupportingDataSize, ConsumeExpressionSupportingData, fieldPath);
	}

	private void ConsumeExpressionSupportingData(PointerPayload payload, string fieldPath)
	{
		LoadMenuStatementPointer(payload, 0x04, $"{fieldPath}.statement");
		LoadDirectObject(payload, 0x08, MenuExpressionSupportingDataChildSize, ConsumeExpressionSupportingData, $"{fieldPath}.child");
	}

	private void LoadMenuItemPointerArray(PointerPayload payload, int offset, int count, string fieldPath)
	{
		var array = LoadDirectArray(payload, offset, count, PointerSize, fieldPath);
		if (array is null)
			return;

		for (var i = 0; i < count; i++)
		{
			var itemPath = $"{fieldPath}[{i}]";
			TraceItemBoundary(itemPath, "before");
			LoadDirectObject(array.Value, checked(i * PointerSize), MenuItemDefSize, ConsumeMenuItemDef, itemPath);
			TraceItemBoundary(itemPath, "after");
		}
	}

	private void TraceItemBoundary(string fieldPath, string phase)
	{
		var filter = Environment.GetEnvironmentVariable("FF_TRACE_XFILE_ITEMS");
		if (string.IsNullOrWhiteSpace(filter) || !fieldPath.Contains(filter, StringComparison.Ordinal))
			return;

		Console.WriteLine(
			$"trace item {phase} {fieldPath} pos=0x{_position:X} large=0x{_streamBlockOffsets[(int)XFILE_BLOCK.LARGE]:X}");
	}

	private void ConsumeMenuItemDef(PointerPayload payload, string fieldPath)
	{
		ConsumeMenuWindow(SubPayload(payload, 0x00, MenuWindowSize), $"{fieldPath}.window");
		LoadEmbeddedXString(payload, 0x12c, $"{fieldPath}.text");

		LoadMenuStatementPointer(payload, 0x138, $"{fieldPath}.mouseEnterText");
		LoadMenuStatementPointer(payload, 0x13c, $"{fieldPath}.mouseExitText");
		LoadMenuStatementPointer(payload, 0x140, $"{fieldPath}.mouseEnter");
		LoadMenuStatementPointer(payload, 0x144, $"{fieldPath}.mouseExit");
		LoadMenuStatementPointer(payload, 0x148, $"{fieldPath}.action");
		LoadMenuStatementPointer(payload, 0x14c, $"{fieldPath}.accept");
		LoadMenuStatementPointer(payload, 0x150, $"{fieldPath}.onFocus");
		LoadMenuStatementPointer(payload, 0x154, $"{fieldPath}.leaveFocus");

		LoadEmbeddedXString(payload, 0x158, $"{fieldPath}.dvar");
		LoadEmbeddedXString(payload, 0x15c, $"{fieldPath}.dvarTest");
			LoadExpressionSupportingDataPointer(payload, 0x160, $"{fieldPath}.expressionData");
			LoadEmbeddedXString(payload, 0x164, $"{fieldPath}.enableDvar");
			LoadItemSoundAliasPointer(payload, 0x16c, $"{fieldPath}.soundAlias");
			LoadMenuItemTypeData(payload, $"{fieldPath}.typeData");

			var conditionCount = ReadInt32At(payload.Bytes, 0x18c);
			LoadMenuItemConditionArray(payload, 0x190, conditionCount, $"{fieldPath}.conditions");

		LoadInlineOnlyDirectObject(payload, 0x194, MenuStatementSize, ConsumeMenuStatement, $"{fieldPath}.visibleExp");
		LoadInlineOnlyDirectObject(payload, 0x198, MenuStatementSize, ConsumeMenuStatement, $"{fieldPath}.disabledExp");
		LoadInlineOnlyDirectObject(payload, 0x19c, MenuStatementSize, ConsumeMenuStatement, $"{fieldPath}.textExp");
			LoadInlineOnlyDirectObject(payload, 0x1a0, MenuStatementSize, ConsumeMenuStatement, $"{fieldPath}.materialExp");
		}

	private void LoadItemSoundAliasPointer(PointerPayload payload, int offset, string fieldPath)
	{
		var pointerReader = offset;
		var pointer = ReadDirectPointer<byte[]>(
				payload.Bytes,
				ref pointerReader,
				payload.SourceOffset + offset,
				payload.Block,
				payload.StreamOffset + offset,
				fieldPath);

			if (pointer.Kind == PointerKind.Null)
			{
				SetPointerResult(pointer, []);
				return;
			}

		if (pointer.Kind == PointerKind.Offset)
		{
			_offsetPointers.Add(pointer);
			RegisterLoadedOffsetTarget(pointer, SoundAliasListSize);
			return;
		}

		PushStreamBlock(XFILE_BLOCK.TEMP);
		try
		{
			var loaded = LoadCurrentStreamPayload(pointer, CurrentStreamBlock, SoundAliasListSize, PointerSize, fieldPath);
			SetPointerResult(pointer, loaded.Bytes);
			PushStreamBlock(XFILE_BLOCK.LARGE);
			try
			{
				ConsumeSoundAliasList(loaded, fieldPath);
			}
			finally
			{
				PopStreamBlock();
			}
		}
		finally
		{
			PopStreamBlock();
		}
		}

		private void ConsumeSoundAliasList(PointerPayload payload, string fieldPath)
		{
			LoadEmbeddedXString(payload, 0x00, $"{fieldPath}.name");

			var aliasCount = ReadInt32At(payload.Bytes, 0x08);
			LoadSoundAliasEntryArray(payload, 0x04, aliasCount, $"{fieldPath}.aliases");
		}

		private void LoadSoundAliasEntryArray(PointerPayload payload, int offset, int count, string fieldPath)
		{
			var array = LoadInlineOnlyDirectArray(payload, offset, count, SoundAliasEntrySize, fieldPath);
			if (array is null)
				return;

			for (var i = 0; i < count; i++)
				ConsumeSoundAliasEntry(SubPayload(array.Value, checked(i * SoundAliasEntrySize), SoundAliasEntrySize), $"{fieldPath}[{i}]");
		}

		private void ConsumeSoundAliasEntry(PointerPayload payload, string fieldPath)
		{
			LoadEmbeddedXString(payload, 0x00, $"{fieldPath}.aliasName");
			LoadEmbeddedXString(payload, 0x04, $"{fieldPath}.subtitle");
			LoadEmbeddedXString(payload, 0x08, $"{fieldPath}.secondaryAliasName");
			LoadEmbeddedXString(payload, 0x0c, $"{fieldPath}.chainAliasName");
			LoadEmbeddedXString(payload, 0x10, $"{fieldPath}.mixerGroup");

			LoadSoundAliasNestedEntryArray(payload, 0x14, 1, $"{fieldPath}.nested");
			LoadInlineOnlyDirectObject(payload, 0x50, SoundAliasLoadSpecSize, ConsumeSoundAliasLoadSpec, $"{fieldPath}.loadSpec");
			LoadInlineOnlyDirectObject(payload, 0x60, SoundAliasLimitSize, ConsumeSoundAliasLimit, $"{fieldPath}.limit");
		}

		private void LoadSoundAliasNestedEntryArray(PointerPayload payload, int offset, int count, string fieldPath)
		{
			var array = LoadInlineOnlyDirectArray(payload, offset, count, SoundAliasNestedEntrySize, fieldPath);
			if (array is null)
				return;

			for (var i = 0; i < count; i++)
				ConsumeSoundAliasNestedEntry(SubPayload(array.Value, checked(i * SoundAliasNestedEntrySize), SoundAliasNestedEntrySize), $"{fieldPath}[{i}]");
		}

		private void ConsumeSoundAliasNestedEntry(PointerPayload payload, string fieldPath)
		{
			var kind = payload.Bytes[0x00];
			if (kind == 1)
				LoadInlineOnlyDirectObject(payload, 0x04, 0x04, null, $"{fieldPath}.inline");
			else
				LoadInlineOnlyDirectObject(payload, 0x04, 0x0c, ConsumeSoundAliasNestedVariant, $"{fieldPath}.inline");
		}

		private void ConsumeSoundAliasNestedVariant(PointerPayload payload, string fieldPath)
		{
			LoadDirectObject(payload, 0x04, 0x08, ConsumeSoundAliasNestedStringPair, $"{fieldPath}.value");
		}

		private void ConsumeSoundAliasNestedStringPair(PointerPayload payload, string fieldPath)
		{
			LoadEmbeddedXString(payload, 0x00, $"{fieldPath}.first");
			LoadEmbeddedXString(payload, 0x04, $"{fieldPath}.second");
		}

		private void ConsumeSoundAliasLoadSpec(PointerPayload payload, string fieldPath)
		{
			LoadEmbeddedXString(payload, 0x00, $"{fieldPath}.name");
		}

		private void ConsumeSoundAliasLimit(PointerPayload payload, string fieldPath)
		{
			LoadEmbeddedXString(payload, 0x04, $"{fieldPath}.name");
		}

		private void LoadMenuItemTypeData(PointerPayload payload, string fieldPath)
		{
			var itemType = ReadInt32At(payload.Bytes, 0x100);
			switch (itemType)
			{
				case 6:
					LoadDirectObject(payload, 0x184, MenuItemType6DataSize, ConsumeMenuItemType6Data, fieldPath);
					break;
				case 12:
					LoadDirectObject(payload, 0x184, MenuItemType12DataSize, ConsumeMenuItemType12Data, fieldPath);
					break;
				case 13:
					LoadEmbeddedXString(payload, 0x184, fieldPath);
					break;
				case 20:
					LoadDirectObject(payload, 0x184, MenuItemType20DataSize, null, fieldPath);
					break;
				case 21:
					LoadDirectObject(payload, 0x184, MenuItemType21DataSize, null, fieldPath);
					break;
				case 0:
				case 4:
				case 9:
				case 10:
				case 11:
				case 14:
				case 16:
				case 17:
				case 18:
				case 22:
				case 23:
					LoadDirectObject(payload, 0x184, MenuItemCommonDataSize, null, fieldPath);
					break;
			}
		}

			private void ConsumeMenuItemType6Data(PointerPayload payload, string fieldPath)
			{
				LoadMenuStatementPointer(payload, 0x134, $"{fieldPath}.statement");
				LoadWeaponMaterialAlias(payload, 0x154, $"{fieldPath}.material");
			}

		private void ConsumeMenuItemType12Data(PointerPayload payload, string fieldPath)
		{
			LoadXStringArrayRoot(payload, 0x00, 32, $"{fieldPath}.dvarList");
			LoadXStringArrayRoot(payload, 0x80, 32, $"{fieldPath}.dvarStr");
		}

		private void LoadXStringArrayRoot(PointerPayload payload, int offset, int count, string fieldPath)
		{
			for (var i = 0; i < count; i++)
				LoadEmbeddedXString(payload, checked(offset + (i * PointerSize)), $"{fieldPath}[{i}]");
		}

		private void LoadMenuItemConditionArray(PointerPayload payload, int offset, int count, string fieldPath)
		{
			var array = LoadDirectArray(payload, offset, count, MenuItemConditionSize, fieldPath);
		if (array is null)
			return;

		for (var i = 0; i < count; i++)
			LoadInlineOnlyDirectObject(array.Value, checked((i * MenuItemConditionSize) + 0x04), MenuStatementSize, ConsumeMenuStatement, $"{fieldPath}[{i}].statement");
	}

	private PointerPayload SubPayload(PointerPayload payload, int offset, int length)
	{
		if (offset < 0 || length < 0 || offset + length > payload.Bytes.Length)
			throw new InvalidDataException($"Invalid embedded payload span 0x{offset:X}+0x{length:X}.");

		RegisterPayloadStart(payload, offset, length);
		return new PointerPayload(
			payload.Block,
			payload.StreamOffset + offset,
			payload.SourceOffset < 0 ? -1 : payload.SourceOffset + offset,
			payload.Bytes.AsSpan(offset, length).ToArray());
	}

	private void RegisterArrayElementStarts(PointerPayload payload, int count, int elementSize)
	{
		if (count <= 0 || elementSize <= 0)
			return;

		for (var i = 0; i < count; i++)
			RegisterPayloadStart(payload, checked(i * elementSize), elementSize);
	}

	private void RegisterPayloadStart(PointerPayload payload, int offset, int length)
	{
		if (offset < 0 || length < 0 || offset + length > payload.Bytes.Length)
			throw new InvalidDataException($"Invalid registered payload span 0x{offset:X}+0x{length:X}.");

		_materializedSpans.Add(new MaterializedSpan(
			(int)payload.Block,
			payload.StreamOffset + offset,
			payload.SourceOffset < 0 ? -1 : payload.SourceOffset + offset,
			length));
	}

	private void RegisterLoadedOffsetTarget(Pointer pointer, int length)
	{
		if (pointer.Kind != PointerKind.Offset || length < 0)
			return;

		var blockIndex = pointer.StreamBlockIndex;
		var streamOffset = pointer.Offset;
		if (blockIndex < 0 || blockIndex >= _streamBlocks.Length || streamOffset < 0)
			return;

		var endOffset = checked(streamOffset + length);
		if (endOffset > _streamBlocks[blockIndex].Length || endOffset > _streamBlockOffsets[blockIndex])
			return;

		_materializedSpans.Add(new MaterializedSpan(blockIndex, streamOffset, -1, length));
	}

	private void LoadEmbeddedXString(PointerPayload payload, int offset, string fieldPath)
	{
		var pointerReader = offset;
		var pointer = ReadZonePointer<string?>(
			payload.Bytes,
			ref pointerReader,
			payload.SourceOffset + offset,
			payload.Block,
			payload.StreamOffset + offset,
			fieldPath);

		LoadXString(pointer, CurrentStreamBlock, fieldPath);
	}

	private PointerPayload? LoadNonZeroInlineObject(
		PointerPayload payload,
		int offset,
		int size,
		Action<PointerPayload, string>? consume,
		string fieldPath)
	{
		var pointerReader = offset;
		var pointer = ReadDirectPointer<byte[]>(
			payload.Bytes,
			ref pointerReader,
			payload.SourceOffset + offset,
			payload.Block,
			payload.StreamOffset + offset,
			fieldPath);

		if (pointer.Kind == PointerKind.Null)
		{
			SetPointerResult(pointer, []);
			return null;
		}

		var loaded = LoadNonZeroInlinePayload(pointer, size, PointerSize, fieldPath);
		SetPointerResult(pointer, loaded.Bytes);
		consume?.Invoke(loaded, fieldPath);
		return loaded;
	}

	private PointerPayload? LoadNonZeroInlineArray(
		PointerPayload payload,
		int offset,
		int count,
		int elementSize,
		int alignment,
		string fieldPath)
	{
		var pointerReader = offset;
		var pointer = ReadDirectPointer<byte[]>(
			payload.Bytes,
			ref pointerReader,
			payload.SourceOffset + offset,
			payload.Block,
			payload.StreamOffset + offset,
			fieldPath);

		return LoadNonZeroInlineArray(pointer, count, elementSize, alignment, fieldPath);
	}

	private PointerPayload? LoadNonZeroInlineArray(
		DirectPointer<byte[]> pointer,
		int count,
		int elementSize,
		int alignment,
		string fieldPath)
	{
		if (pointer.Kind == PointerKind.Null)
		{
			SetPointerResult(pointer, []);
			return null;
		}

		if (count < 0)
			throw new InvalidDataException($"Invalid array count {count} for {fieldPath}; previousAsset={_lastCompletedAssetFieldPath ?? "<none>"}.");
		if (elementSize != 0 && count > int.MaxValue / elementSize)
			throw new InvalidDataException($"Invalid array count {count} for {fieldPath}; previousAsset={_lastCompletedAssetFieldPath ?? "<none>"}.");

		var array = LoadNonZeroInlinePayload(
			pointer,
			checked(count * elementSize),
			alignment,
			fieldPath);
		RegisterArrayElementStarts(array, count, elementSize);
		SetPointerResult(pointer, array.Bytes);
		return array;
	}

	private PointerPayload LoadNonZeroInlinePayload(
		Pointer pointer,
		int length,
		int alignment,
		string fieldPath)
	{
		var loaded = LoadNonInsertedCurrentStreamPayload(pointer, CurrentStreamBlock, length, alignment, fieldPath);
		if (pointer.Kind == PointerKind.Insert)
			WritePointerFieldTarget(pointer, (int)loaded.Block, loaded.StreamOffset);
		return loaded;
	}

	private PointerPayload? LoadDirectArray(
		PointerPayload payload,
		int offset,
		int count,
		int elementSize,
		string fieldPath,
		int alignment = PointerSize)
	{
			if (count < 0)
				throw new InvalidDataException($"Invalid array count {count} for {fieldPath}; previousAsset={_lastCompletedAssetFieldPath ?? "<none>"}.");
			if (elementSize != 0 && count > int.MaxValue / elementSize)
				throw new InvalidDataException($"Invalid array count {count} for {fieldPath}; previousAsset={_lastCompletedAssetFieldPath ?? "<none>"}.");

			var pointerReader = offset;
		var pointer = ReadDirectPointer<byte[]>(
			payload.Bytes,
			ref pointerReader,
			payload.SourceOffset + offset,
			payload.Block,
			payload.StreamOffset + offset,
			fieldPath);

			if (ShouldTraceMenuField(fieldPath))
				Console.WriteLine($"trace array {fieldPath} raw=0x{pointer.Raw:X8} count={count} elementSize=0x{elementSize:X} pos=0x{_position:X} large=0x{_streamBlockOffsets[(int)XFILE_BLOCK.LARGE]:X}");

		if (pointer.Kind == PointerKind.Null)
		{
			SetPointerResult(pointer, []);
			return null;
		}

		if (pointer.Kind == PointerKind.Offset)
		{
			_offsetPointers.Add(pointer);
			RegisterLoadedOffsetTarget(pointer, checked(count * elementSize));
			return null;
		}

		var array = LoadCurrentStreamPayload(
			pointer, CurrentStreamBlock,
			checked(count * elementSize),
			alignment,
			fieldPath);
			RegisterArrayElementStarts(array, count, elementSize);
			SetPointerResult(pointer, array.Bytes);
			return array;
		}

		private PointerPayload? LoadInlineOnlyDirectArray(
			PointerPayload payload,
			int offset,
			int count,
			int elementSize,
			string fieldPath)
		{
			if (count < 0)
				throw new InvalidDataException($"Invalid array count {count} for {fieldPath}.");
			if (elementSize != 0 && count > int.MaxValue / elementSize)
				throw new InvalidDataException($"Invalid array count {count} for {fieldPath}.");

			var pointerReader = offset;
		var pointer = ReadDirectPointer<byte[]>(
			payload.Bytes,
			ref pointerReader,
			payload.SourceOffset + offset,
			payload.Block,
			payload.StreamOffset + offset,
			fieldPath);

			if (ShouldTraceMenuField(fieldPath))
				Console.WriteLine($"trace inline-array {fieldPath} raw=0x{pointer.Raw:X8} count={count} elementSize=0x{elementSize:X} pos=0x{_position:X} large=0x{_streamBlockOffsets[(int)XFILE_BLOCK.LARGE]:X}");

			if (pointer.Kind == PointerKind.Null)
			{
				SetPointerResult(pointer, []);
				return null;
			}

		if (pointer.Kind == PointerKind.Offset)
		{
			_offsetPointers.Add(pointer);
			RegisterLoadedOffsetTarget(pointer, checked(count * elementSize));
			return null;
		}

			var array = LoadCurrentStreamPayload(
				pointer, CurrentStreamBlock,
				checked(count * elementSize),
				PointerSize,
				fieldPath);
			RegisterArrayElementStarts(array, count, elementSize);
			SetPointerResult(pointer, array.Bytes);
			return array;
		}

		private void LoadDirectObject(
			PointerPayload payload,
			int offset,
		int size,
		Action<PointerPayload, string>? consume,
		string fieldPath)
	{
		var pointerReader = offset;
		var pointer = ReadDirectPointer<byte[]>(
			payload.Bytes,
			ref pointerReader,
			payload.SourceOffset + offset,
			payload.Block,
			payload.StreamOffset + offset,
			fieldPath);

		if (ShouldTraceMenuField(fieldPath))
			Console.WriteLine($"trace object before {fieldPath} raw=0x{pointer.Raw:X8} size=0x{size:X} pos=0x{_position:X} large=0x{_streamBlockOffsets[(int)XFILE_BLOCK.LARGE]:X}");

		if (pointer.Kind == PointerKind.Null)
		{
			SetPointerResult(pointer, []);
			return;
		}

		if (pointer.Kind == PointerKind.Offset)
		{
			_offsetPointers.Add(pointer);
			RegisterLoadedOffsetTarget(pointer, size);
			return;
		}

		var loaded = LoadCurrentStreamPayload(pointer, CurrentStreamBlock, size, PointerSize, fieldPath);
		SetPointerResult(pointer, loaded.Bytes);
		consume?.Invoke(loaded, fieldPath);
		if (ShouldTraceMenuField(fieldPath))
			Console.WriteLine($"trace object after {fieldPath} raw=0x{pointer.Raw:X8} size=0x{size:X} pos=0x{_position:X} large=0x{_streamBlockOffsets[(int)XFILE_BLOCK.LARGE]:X}");
	}

	private void LoadInlineOnlyDirectObject(
		PointerPayload payload,
		int offset,
		int size,
		Action<PointerPayload, string>? consume,
		string fieldPath)
	{
		var pointerReader = offset;
		var pointer = ReadDirectPointer<byte[]>(
			payload.Bytes,
			ref pointerReader,
			payload.SourceOffset + offset,
			payload.Block,
			payload.StreamOffset + offset,
			fieldPath);

		if (ShouldTraceMenuField(fieldPath))
			Console.WriteLine($"trace inline-object before {fieldPath} raw=0x{pointer.Raw:X8} size=0x{size:X} pos=0x{_position:X} large=0x{_streamBlockOffsets[(int)XFILE_BLOCK.LARGE]:X}");

		if (pointer.Kind == PointerKind.Null)
		{
			SetPointerResult(pointer, []);
			return;
		}

		if (pointer.Kind == PointerKind.Offset)
		{
			_offsetPointers.Add(pointer);
			RegisterLoadedOffsetTarget(pointer, size);
			return;
		}

		var loaded = LoadCurrentStreamPayload(pointer, CurrentStreamBlock, size, PointerSize, fieldPath);
		SetPointerResult(pointer, loaded.Bytes);
		consume?.Invoke(loaded, fieldPath);
		if (ShouldTraceMenuField(fieldPath))
			Console.WriteLine($"trace inline-object after {fieldPath} raw=0x{pointer.Raw:X8} size=0x{size:X} pos=0x{_position:X} large=0x{_streamBlockOffsets[(int)XFILE_BLOCK.LARGE]:X}");
	}

	private void LoadAliasInlineObject(
		PointerPayload payload,
		int offset,
		int size,
		Action<PointerPayload, string>? consume,
		string fieldPath)
	{
		var pointerReader = offset;
		var pointer = ReadAliasPointer<byte[]>(
			payload.Bytes,
			ref pointerReader,
			payload.SourceOffset + offset,
			payload.Block,
			payload.StreamOffset + offset,
			fieldPath);

		if (pointer.Kind == PointerKind.Null)
		{
			SetPointerResult(pointer, []);
			return;
		}

		if (pointer.Kind == PointerKind.Offset)
		{
			_offsetPointers.Add(pointer);
			RegisterLoadedOffsetTarget(pointer, size);
			return;
		}

		var loaded = LoadCurrentStreamPayload(pointer, CurrentStreamBlock, size, PointerSize, fieldPath);
		SetPointerResult(pointer, loaded.Bytes);
		consume?.Invoke(loaded, fieldPath);
	}

	private void LoadAliasInlineObjectInBlock(
		PointerPayload payload,
		int offset,
		XFILE_BLOCK block,
		int size,
		Action<PointerPayload, string>? consume,
		string fieldPath)
	{
		PushStreamBlock(block);
		try
		{
			LoadAliasInlineObject(payload, offset, size, consume, fieldPath);
		}
		finally
		{
			PopStreamBlock();
		}
	}

		private static int ReadInt32At(byte[] bytes, int offset)
		{
			return BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(offset, sizeof(int)));
		}

		private static ushort ReadUInt16At(byte[] bytes, int offset)
		{
			return BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset, sizeof(ushort)));
		}

		private void LoadMaterialTechniquePointer(DirectPointer<MaterialTechnique> pointer, string fieldPath)
		{
		if (pointer.Kind == PointerKind.Null)
		{
			SetPointerResult(pointer, null);
			return;
		}

		if (pointer.Kind == PointerKind.Offset)
		{
			_offsetPointers.Add(pointer);
			RegisterLoadedOffsetTarget(pointer, MaterialTechniqueSize);
			return;
		}

		var root = LoadPointerPayload(pointer, CurrentStreamBlock, MaterialTechniqueSize, PointerSize, fieldPath);
		var rootReader = 0;
		var namePtr = ReadDirectPointer<string>(
			root.Bytes,
			ref rootReader,
			root.SourceOffset,
			root.Block,
			root.StreamOffset,
			$"{fieldPath}.name");
		var flags = ReadUInt16(root.Bytes, ref rootReader);
		var passCount = ReadUInt16(root.Bytes, ref rootReader);
		var passes = LoadMaterialPassArray(passCount, $"{fieldPath}.passes");

		LoadXString(namePtr, CurrentStreamBlock, $"{fieldPath}.name");

		SetPointerResult(pointer, new MaterialTechnique
		{
			Offset = root.SourceOffset,
			NamePtr = namePtr,
			Flags = flags,
			PassCount = passCount,
			Passes = passes
		});
	}

	private MaterialPass[] LoadMaterialPassArray(int count, string fieldPath)
	{
		if (count < 0)
			throw new InvalidDataException($"Invalid material pass count {count} for {fieldPath}.");

		if (count == 0)
			return [];

		var length = checked(count * MaterialPassSize);
		var block = CurrentStreamBlock;
		var sourceOffset = _position;
		int streamOffset;
		try
		{
			_currentMaterializeFieldPath = fieldPath;
			streamOffset = MaterializeCurrent(block, length, PointerSize);
		}
		finally
		{
			_currentMaterializeFieldPath = null;
		}
		var bytes = _streamBlocks[(int)block].AsSpan(streamOffset, length).ToArray();
		RegisterArrayElementStarts(
			new PointerPayload(block, streamOffset, sourceOffset, bytes),
			count,
			MaterialPassSize);

		var passes = new MaterialPass[count];
		var passReader = 0;
		for (var i = 0; i < passes.Length; i++)
		{
			var passSourceOffset = sourceOffset + passReader;
			var passStreamOffset = streamOffset + passReader;
			var vertexDecl = ReadDirectPointer<MaterialVertexDeclaration>(
					bytes,
					ref passReader,
					passSourceOffset,
					block,
					passStreamOffset,
					$"{fieldPath}[{i}].vertexDecl");
			var vertexShader = ReadAliasPointer<MaterialVertexShader>(
					bytes,
					ref passReader,
					passSourceOffset + 0x04,
					block,
					passStreamOffset + 0x04,
					$"{fieldPath}[{i}].vertexShader");
			var pixelShader = ReadAliasPointer<MaterialPixelShader>(
					bytes,
					ref passReader,
					passSourceOffset + 0x08,
					block,
					passStreamOffset + 0x08,
					$"{fieldPath}[{i}].pixelShader");

			var perPrimArgCount = bytes[passReader++];
			var perObjArgCount = bytes[passReader++];
			var stableArgCount = bytes[passReader++];
			var customSamplerFlags = bytes[passReader++];
			var precompiledIndex = bytes[passReader++];
			var padding = bytes.AsSpan(passReader, 3).ToArray();
			passReader += 3;

			var args = ReadDirectPointer<MaterialShaderArgument[]>(
					bytes,
					ref passReader,
					passSourceOffset + 0x14,
					block,
					passStreamOffset + 0x14,
					$"{fieldPath}[{i}].args");

			LoadMaterialVertexDeclarationPointer(vertexDecl, $"{fieldPath}[{i}].vertexDecl");
			MarkAliasAssetPointer(vertexShader);
			MarkAliasAssetPointer(pixelShader);
			LoadMaterialShaderArguments(
				args,
				perPrimArgCount + perObjArgCount + stableArgCount,
				$"{fieldPath}[{i}].args");

			passes[i] = new MaterialPass
			{
				Offset = passSourceOffset,
				VertexDecl = vertexDecl,
				VertexShader = vertexShader,
				PixelShader = pixelShader,
				PerPrimArgCount = perPrimArgCount,
				PerObjArgCount = perObjArgCount,
				StableArgCount = stableArgCount,
				CustomSamplerFlags = customSamplerFlags,
				PrecompiledIndex = precompiledIndex,
				Padding = padding,
				Args = args
			};
		}

		return passes;
	}

	private void LoadMaterialVertexDeclarationPointer(
		DirectPointer<MaterialVertexDeclaration> pointer,
		string fieldPath)
	{
		if (pointer.Kind == PointerKind.Null)
		{
			SetPointerResult(pointer, null);
			return;
		}

		if (pointer.Kind == PointerKind.Offset)
		{
			_offsetPointers.Add(pointer);
			RegisterLoadedOffsetTarget(pointer, MaterialVertexDeclarationSize);
			return;
		}

		var payload = LoadPointerPayload(
			pointer,
			GetMaterialVertexDeclarationBlock(),
			MaterialVertexDeclarationSize,
			PointerSize,
			fieldPath);

		SetPointerResult(pointer, new MaterialVertexDeclaration
		{
			Offset = payload.SourceOffset,
			Raw = payload.Bytes
		});
	}

	private void LoadMaterialShaderArguments(
		DirectPointer<MaterialShaderArgument[]> pointer,
		int count,
		string fieldPath)
	{
		if (count < 0)
			throw new InvalidDataException($"Invalid material shader argument count {count} for {fieldPath}.");

		if (pointer.Kind == PointerKind.Null)
		{
			SetPointerResult(pointer, []);
			return;
		}
		if (pointer.Kind == PointerKind.Offset)
		{
			_offsetPointers.Add(pointer);
			RegisterLoadedOffsetTarget(pointer, checked(count * MaterialShaderArgumentSize));
			return;
		}

		var payload = LoadCurrentStreamPayload(
			pointer, CurrentStreamBlock,
			checked(count * MaterialShaderArgumentSize),
			PointerSize,
			fieldPath);
		RegisterArrayElementStarts(payload, count, MaterialShaderArgumentSize);

		var args = new MaterialShaderArgument[count];
		var argReader = 0;
		for (var i = 0; i < args.Length; i++)
		{
			var type = (MaterialShaderArgumentType)ReadUInt16(payload.Bytes, ref argReader);
			var dest = ReadUInt16(payload.Bytes, ref argReader);
			var raw = ReadInt32(payload.Bytes, ref argReader);

			args[i] = new MaterialShaderArgument
			{
				Type = type,
				Dest = dest,
				Argument = new MaterialArgumentDef
				{
					Raw = raw
				}
			};
		}

		SetPointerResult(pointer, args);
	}

	private PointerPayload LoadNonInsertedCurrentStreamPayload(
		Pointer pointer,
		XFILE_BLOCK block,
		int length,
		int alignment,
		string fieldPath)
	{
		if (length < 0)
			throw new InvalidDataException($"Invalid current-stream payload length {length} for {fieldPath}.");

		var sourceOffset = IsRuntimeZeroFillBlock(block) ? -1 : _position;
		int streamOffset;
		try
		{
			_currentMaterializeFieldPath = fieldPath;
			streamOffset = MaterializeCurrent(block, length, alignment);
		}
		catch (InvalidDataException ex)
		{
			throw new InvalidDataException(
				$"{ex.Message} fieldPath={fieldPath}; source=0x{sourceOffset:X}; length=0x{length:X}; previousAsset={_lastCompletedAssetFieldPath ?? "<none>"}.",
				ex);
		}
		finally
		{
			_currentMaterializeFieldPath = null;
		}
		SetPointerTarget(pointer, block, sourceOffset, length, streamOffset);

		return new PointerPayload(
			block,
			streamOffset,
			sourceOffset,
			_streamBlocks[(int)block].AsSpan(streamOffset, length).ToArray());
	}

	private void LoadStructuredDataDefArray(
		DirectPointer<StructuredDataDef[]> pointer,
		int count,
		string fieldPath)
	{
		if (count < 0)
			throw new InvalidDataException($"Invalid structured data def count {count} for {fieldPath}.");

		if (pointer.Kind == PointerKind.Null)
		{
			SetPointerResult(pointer, []);
			return;
		}

		if (pointer.Kind == PointerKind.Offset)
		{
			_offsetPointers.Add(pointer);
			RegisterLoadedOffsetTarget(pointer, checked(count * 0x34));
			SetPointerResult(pointer, []);
			return;
		}

			var payload = LoadCurrentStreamPayload(pointer, CurrentStreamBlock, checked(count * 0x34), PointerSize, fieldPath);
			RegisterArrayElementStarts(payload, count, 0x34);
			var defs = new StructuredDataDef[count];
			var defReader = 0;
			for (var i = 0; i < defs.Length; i++)
			{
				var defOffset = defReader;
				var version = ReadInt32(payload.Bytes, ref defReader);
				var formatChecksum = unchecked((uint)ReadInt32(payload.Bytes, ref defReader));
				var enumCount = ReadInt32(payload.Bytes, ref defReader);
			var enumsPtrOffset = defReader;
			var enumsPtr = ReadDirectPointer<StructuredDataEnum[]>(
				payload.Bytes,
				ref defReader,
				payload.SourceOffset + enumsPtrOffset,
				payload.Block,
				payload.StreamOffset + enumsPtrOffset,
				$"{fieldPath}[{i}].enums");
			var structCount = ReadInt32(payload.Bytes, ref defReader);
			var structsPtrOffset = defReader;
			var structsPtr = ReadDirectPointer<StructuredDataStruct[]>(
				payload.Bytes,
				ref defReader,
				payload.SourceOffset + structsPtrOffset,
				payload.Block,
				payload.StreamOffset + structsPtrOffset,
				$"{fieldPath}[{i}].structs");
			var indexedArrayCount = ReadInt32(payload.Bytes, ref defReader);
			var indexedArraysPtrOffset = defReader;
			var indexedArraysPtr = ReadDirectPointer<StructuredDataIndexedArray[]>(
				payload.Bytes,
				ref defReader,
				payload.SourceOffset + indexedArraysPtrOffset,
				payload.Block,
				payload.StreamOffset + indexedArraysPtrOffset,
				$"{fieldPath}[{i}].indexedArrays");
			var enumedArrayCount = ReadInt32(payload.Bytes, ref defReader);
			var enumedArraysPtrOffset = defReader;
			var enumedArraysPtr = ReadDirectPointer<StructuredDataEnumedArray[]>(
				payload.Bytes,
				ref defReader,
				payload.SourceOffset + enumedArraysPtrOffset,
				payload.Block,
				payload.StreamOffset + enumedArraysPtrOffset,
				$"{fieldPath}[{i}].enumedArrays");
				var rootType = ReadStructuredDataType(payload.Bytes, ref defReader);
				var size = unchecked((uint)ReadInt32(payload.Bytes, ref defReader));

				ConsumeStructuredDataDef(SubPayload(payload, defOffset, 0x34), $"{fieldPath}[{i}]");

				defs[i] = new StructuredDataDef
				{
				Version = version,
				FormatChecksum = formatChecksum,
				EnumCount = enumCount,
				EnumsPtr = enumsPtr,
				StructCount = structCount,
				StructsPtr = structsPtr,
				IndexedArrayCount = indexedArrayCount,
				IndexedArraysPtr = indexedArraysPtr,
				EnumedArrayCount = enumedArrayCount,
				EnumedArraysPtr = enumedArraysPtr,
				RootType = rootType,
				Size = size
			};
		}

			SetPointerResult(pointer, defs);
		}

		private void ConsumeStructuredDataDef(PointerPayload payload, string fieldPath)
		{
			var enumCount = ReadInt32At(payload.Bytes, 0x08);
			var structCount = ReadInt32At(payload.Bytes, 0x10);
			var indexedArrayCount = ReadInt32At(payload.Bytes, 0x18);
			var enumedArrayCount = ReadInt32At(payload.Bytes, 0x20);

			LoadStructuredDataEnumArray(payload, 0x0c, enumCount, $"{fieldPath}.enums");
			LoadStructuredDataStructArray(payload, 0x14, structCount, $"{fieldPath}.structs");
			LoadStructuredDataRawArray(payload, 0x1c, indexedArrayCount, 0x10, $"{fieldPath}.indexedArrays");
			LoadStructuredDataRawArray(payload, 0x24, enumedArrayCount, 0x10, $"{fieldPath}.enumedArrays");
		}

		private void LoadStructuredDataEnumArray(PointerPayload payload, int offset, int count, string fieldPath)
		{
			var array = LoadDirectArray(payload, offset, count, 0x0c, fieldPath);
			if (array is null)
				return;

			for (var i = 0; i < count; i++)
				ConsumeStructuredDataEnum(SubPayload(array.Value, checked(i * 0x0c), 0x0c), $"{fieldPath}[{i}]");
		}

		private void ConsumeStructuredDataEnum(PointerPayload payload, string fieldPath)
		{
			var entryCount = ReadInt32At(payload.Bytes, 0x00);
			var array = LoadDirectArray(payload, 0x08, entryCount, 0x08, $"{fieldPath}.entries");
			if (array is null)
				return;

			for (var i = 0; i < entryCount; i++)
				LoadEmbeddedXString(array.Value, checked(i * 0x08), $"{fieldPath}.entries[{i}].name");
		}

		private void LoadStructuredDataStructArray(PointerPayload payload, int offset, int count, string fieldPath)
		{
			var array = LoadDirectArray(payload, offset, count, 0x10, fieldPath);
			if (array is null)
				return;

			for (var i = 0; i < count; i++)
				ConsumeStructuredDataStruct(SubPayload(array.Value, checked(i * 0x10), 0x10), $"{fieldPath}[{i}]");
		}

		private void ConsumeStructuredDataStruct(PointerPayload payload, string fieldPath)
		{
			var propertyCount = ReadInt32At(payload.Bytes, 0x00);
			var array = LoadDirectArray(payload, 0x04, propertyCount, 0x10, $"{fieldPath}.properties");
			if (array is null)
				return;

			for (var i = 0; i < propertyCount; i++)
				LoadEmbeddedXString(array.Value, checked(i * 0x10), $"{fieldPath}.properties[{i}].name");
		}

		private void LoadStructuredDataRawArray(PointerPayload payload, int offset, int count, int elementSize, string fieldPath)
		{
			LoadDirectArray(payload, offset, count, elementSize, fieldPath);
		}

	private StructuredDataType ReadStructuredDataType(byte[] bytes, ref int offset)
	{
		return new StructuredDataType
		{
			Type = (StructuredDataTypeCategory)ReadInt32(bytes, ref offset),
			UnionValue = ReadInt32(bytes, ref offset)
		};
	}

	private static ushort ReadUInt16(ReadOnlySpan<byte> bytes, ref int offset)
	{
		var value = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(offset, sizeof(ushort)));
		offset += sizeof(ushort);
		return value;
	}

	private XFILE_BLOCK GetMaterialVertexDeclarationBlock()
	{
		var vertexIndex = (int)XFILE_BLOCK.XFILE_BLOCK_VERTEX;
		if (_streamBlocks.Length > vertexIndex && _streamBlocks[vertexIndex].Length > 0)
			return XFILE_BLOCK.XFILE_BLOCK_VERTEX;

		return XFILE_BLOCK.LARGE;
	}

	private void MarkAliasAssetPointer<T>(AliasPointer<T> pointer)
	{
		if (pointer.Kind == PointerKind.Null)
		{
			SetPointerResult(pointer, default);
			return;
		}

		if (pointer.Kind == PointerKind.Offset)
		{
			_offsetPointers.Add(pointer);
			return;
		}

		if (pointer.Kind == PointerKind.Inline)
		{
			throw new InvalidDataException(
				$"Inline alias asset pointer for {pointer.FieldPath} is not implemented; consuming it as null would desync stream cursors.");
		}

		ReserveInsertCell(pointer, XFILE_BLOCK.LARGE);
		SetPointerResult(pointer, default);
	}

	private void MarkOffsetOrNull<T>(DirectPointer<T[]> pointer)
	{
		if (pointer.Kind == PointerKind.Null)
		{
			SetPointerResult(pointer, []);
			return;
		}

		if (pointer.Kind == PointerKind.Offset)
			_offsetPointers.Add(pointer);
	}

	private BaseAsset LoadUnsupportedXAssetRoot(XAssetType assetType, AliasPointer<BaseAsset> pointer)
	{
		if (!EbootXAssetDispatch.Assets.TryGetValue(assetType, out var dispatch)
			|| !dispatch.HasDispatchHandler
			|| dispatch.RootSize is not { } rootSize)
		{
			throw new InvalidDataException(
				$"Unsupported XAsset type {assetType} at {pointer.FieldPath} has no verified EBOOT root-size skip loader.");
		}

		var payload = LoadPointerPayload(
			pointer,
			CurrentStreamBlock,
			rootSize,
			PointerSize,
			$"{assetType}.rawRoot");

		return new SkippedEbootAssetRoot(assetType, rootSize)
		{
			Offset = payload.SourceOffset,
			RawRoot = payload.Bytes
		};
	}

	private void LoadXString<T>(ZonePointer<T> pointer, XFILE_BLOCK block, string fieldPath)
	{
		if (ShouldTraceMenuField(fieldPath))
			Console.WriteLine($"trace xstring before {fieldPath} raw=0x{pointer.Raw:X8} pos=0x{_position:X} large=0x{_streamBlockOffsets[(int)XFILE_BLOCK.LARGE]:X}");

		RejectUnsupportedInsertPointer(pointer, fieldPath);

		if (pointer.Kind == PointerKind.Null)
		{
			SetPointerResult(pointer, default);
			if (ShouldTraceMenuField(fieldPath))
				Console.WriteLine($"trace xstring null {fieldPath} raw=0x{pointer.Raw:X8} pos=0x{_position:X} large=0x{_streamBlockOffsets[(int)XFILE_BLOCK.LARGE]:X}");
			return;
		}

		if (pointer.Kind == PointerKind.Offset)
		{
			_offsetPointers.Add(pointer);
			if (TryResolveStringPointer(pointer))
				return;

			SetPointerResult(pointer, default);
			if (ShouldTraceMenuField(fieldPath))
				Console.WriteLine($"trace xstring offset {fieldPath} raw=0x{pointer.Raw:X8} pos=0x{_position:X} large=0x{_streamBlockOffsets[(int)XFILE_BLOCK.LARGE]:X}");
			return;
		}

		var sourceOffset = IsRuntimeZeroFillBlock(block) ? -1 : _position;
		var value = ReadCString();
		var byteLength = _position - sourceOffset;
		TraceSizedPayload(block, byteLength, fieldPath, sourceOffset);
		TraceInsertPointer(pointer, block, byteLength, fieldPath);
		ReserveInsertCell(pointer, block);
		int streamOffset;
		try
		{
			_currentMaterializeFieldPath = fieldPath;
			streamOffset = Materialize(sourceOffset, block, byteLength, 1);
		}
		catch (InvalidDataException ex)
		{
			throw new InvalidDataException(
				$"{ex.Message} fieldPath={fieldPath}; source=0x{sourceOffset:X}; length=0x{byteLength:X}; previousAsset={_lastCompletedAssetFieldPath ?? "<none>"}.",
				ex);
		}
		finally
		{
			_currentMaterializeFieldPath = null;
		}

		SetPointerTarget(pointer, block, sourceOffset, byteLength, streamOffset);
		pointer.SetResolutionKind(PointerResolutionKind.Direct, fieldPath);
		SetPointerResult(pointer, (T)(object)value);
		if (ShouldTraceMenuField(fieldPath))
			Console.WriteLine($"trace xstring after {fieldPath} raw=0x{pointer.Raw:X8} source=0x{sourceOffset:X} length=0x{byteLength:X} pos=0x{_position:X} large=0x{_streamBlockOffsets[(int)XFILE_BLOCK.LARGE]:X} value=\"{TrimTraceString(value)}\"");
	}

	private static string TrimTraceString(string value)
	{
		var sanitized = value.Replace("\r", "\\r", StringComparison.Ordinal).Replace("\n", "\\n", StringComparison.Ordinal);
		return sanitized.Length <= 80 ? sanitized : sanitized[..80] + "...";
	}

	private PointerPayload LoadPointerPayload(
		Pointer pointer,
		XFILE_BLOCK block,
		int length,
		int alignment,
		string fieldPath)
	{
		if (length < 0)
			throw new InvalidDataException($"Invalid pointer payload length {length} for {fieldPath}.");
		RejectUnsupportedInsertPointer(pointer, fieldPath);

		if (pointer.Kind == PointerKind.Offset)
		{
			_offsetPointers.Add(pointer);
			throw new InvalidDataException(
				$"Offset pointer for {fieldPath} must resolve to existing g_streamBlocks data and must not consume current stream bytes.");
		}

		TraceSizedPayload(block, length, fieldPath);
		TraceInsertPointer(pointer, block, length, fieldPath);
		ReserveInsertCell(pointer, block);

		var sourceOffset = IsRuntimeZeroFillBlock(block) ? -1 : _position;
		int streamOffset;
		try
		{
			_currentMaterializeFieldPath = fieldPath;
			streamOffset = MaterializeCurrent(block, length, alignment);
		}
		catch (InvalidDataException ex)
		{
			throw new InvalidDataException(
				$"{ex.Message} fieldPath={fieldPath}; source=0x{sourceOffset:X}; length=0x{length:X}; previousAsset={_lastCompletedAssetFieldPath ?? "<none>"}.",
				ex);
		}
		finally
		{
			_currentMaterializeFieldPath = null;
		}
		SetPointerTarget(pointer, block, sourceOffset, length, streamOffset);

		return new PointerPayload(
			block,
			streamOffset,
			sourceOffset,
			_streamBlocks[(int)block].AsSpan(streamOffset, length).ToArray());
	}

	private PointerPayload LoadCurrentStreamPayload(
		Pointer pointer,
		XFILE_BLOCK block,
		int length,
		int alignment,
		string fieldPath)
	{
		if (length < 0)
			throw new InvalidDataException($"Invalid current-stream payload length {length} for {fieldPath}.");
		RejectUnsupportedInsertPointer(pointer, fieldPath);

		TraceSizedPayload(block, length, fieldPath);
		TraceInsertPointer(pointer, block, length, fieldPath);
		ReserveInsertCell(pointer, block);

		var sourceOffset = IsRuntimeZeroFillBlock(block) ? -1 : _position;
		int streamOffset;
		try
		{
			_currentMaterializeFieldPath = fieldPath;
			streamOffset = MaterializeCurrent(block, length, alignment);
		}
		catch (InvalidDataException ex)
		{
			throw new InvalidDataException(
				$"{ex.Message} fieldPath={fieldPath}; source=0x{sourceOffset:X}; length=0x{length:X}; previousAsset={_lastCompletedAssetFieldPath ?? "<none>"}.",
				ex);
		}
		finally
		{
			_currentMaterializeFieldPath = null;
		}
		SetPointerTarget(pointer, block, sourceOffset, length, streamOffset);

		return new PointerPayload(
			block,
			streamOffset,
			sourceOffset,
			_streamBlocks[(int)block].AsSpan(streamOffset, length).ToArray());
	}

	private static void RejectUnsupportedInsertPointer(Pointer pointer, string fieldPath)
	{
		if (pointer.Kind != PointerKind.Insert || pointer.ResolutionKind == PointerResolutionKind.Alias)
			return;

		throw new InvalidDataException(
			$"Direct pointer {fieldPath} has raw -2 insert value, but the verified EBOOT branch for direct pointers "
			+ "does not call DB_InsertPointer. Treating it as inline would desync stream cursors.");
	}

	private void TraceInsertPointer(Pointer pointer, XFILE_BLOCK block, int length, string fieldPath)
	{
		if (pointer.Kind != PointerKind.Insert)
			return;
		if (Environment.GetEnvironmentVariable("FF_TRACE_XFILE_INSERTS") != "1")
			return;

		Console.WriteLine(
			$"insert {fieldPath} raw=0x{pointer.Raw:X8} block={block} length=0x{length:X} "
			+ $"source=0x{_position:X} large=0x{_streamBlockOffsets[(int)XFILE_BLOCK.LARGE]:X}");
	}

	private void TraceSizedPayload(XFILE_BLOCK block, int length, string fieldPath, int? sourceOffset = null)
	{
		var lengthFilter = Environment.GetEnvironmentVariable("FF_TRACE_XFILE_LENGTH");
		if (string.IsNullOrWhiteSpace(lengthFilter))
			return;
		if (!int.TryParse(lengthFilter, System.Globalization.NumberStyles.HexNumber, null, out var wantedLength)
			&& !int.TryParse(lengthFilter, out wantedLength))
			return;
		if (length != wantedLength)
			return;

		Console.WriteLine(
			$"payload length=0x{length:X} fieldPath={fieldPath} block={block} "
			+ $"source=0x{(sourceOffset ?? _position):X} large=0x{_streamBlockOffsets[(int)XFILE_BLOCK.LARGE]:X}");
	}

	private void MarkInlineAssetHeaderPending(AliasPointer<BaseAsset> pointer)
	{
		ReserveInsertCell(pointer, XFILE_BLOCK.LARGE);
		SetPointerResult(pointer, null);
	}

	private DirectPointer<T> ReadDirectPointer<T>(
		ReadOnlySpan<byte> span,
		ref int offset,
		int pointerSourceOffset,
		XFILE_BLOCK pointerBlock,
		int pointerStreamOffset,
		string fieldPath)
	{
		var pointer = new DirectPointer<T>(ReadInt32(span, ref offset));
		SetPointerField(pointer, PointerResolutionKind.Direct, pointerSourceOffset, pointerBlock, pointerStreamOffset, fieldPath);
		return pointer;
	}

	private AliasPointer<T> ReadAliasPointer<T>(
		ReadOnlySpan<byte> span,
		ref int offset,
		int pointerSourceOffset,
		XFILE_BLOCK pointerBlock,
		int pointerStreamOffset,
		string fieldPath)
	{
		var pointer = new AliasPointer<T>(ReadInt32(span, ref offset));
		SetPointerField(pointer, PointerResolutionKind.Alias, pointerSourceOffset, pointerBlock, pointerStreamOffset, fieldPath);
		return pointer;
	}

	private ZonePointer<T> ReadZonePointer<T>(
		ReadOnlySpan<byte> span,
		ref int offset,
		int pointerSourceOffset,
		XFILE_BLOCK pointerBlock,
		int pointerStreamOffset,
		string fieldPath)
	{
		var pointer = new ZonePointer<T>(ReadInt32(span, ref offset));
		SetPointerField(pointer, PointerResolutionKind.Direct, pointerSourceOffset, pointerBlock, pointerStreamOffset, fieldPath);
		return pointer;
	}

	private void SetPointerField(
		Pointer pointer,
		PointerResolutionKind kind,
		int pointerSourceOffset,
		XFILE_BLOCK pointerBlock,
		int pointerStreamOffset,
		string fieldPath)
	{
		pointer.SetResolutionKind(kind, fieldPath);
		pointer.SetPointerFieldSourceSpan(pointerSourceOffset, PointerSize, (int)pointerBlock, pointerStreamOffset);
	}

	private void SetPointerTarget(
		Pointer pointer,
		XFILE_BLOCK block,
		int sourceOffset,
		int length,
		int streamOffset)
	{
		pointer.SetSourceSpan(sourceOffset, length);
		pointer.SetTargetSpan(sourceOffset, length, (int)block, streamOffset);
		pointer.SetStreamAddress((int)block, streamOffset);
		pointer.SetResolvedAddress((int)block, streamOffset);
		if (!pointer.IsInsertPointer)
			WritePointerFieldTarget(pointer, (int)block, streamOffset);
		RegisterInsertAliasTarget(pointer, block, sourceOffset, length, streamOffset);
	}

	private void SetPointerResult<T>(ZonePointer<T> pointer, T? result)
	{
		pointer.SetResult(result);

		if (!pointer.HasResolvedAddress)
			return;

		RegisterObjectResult(
			pointer.ResolvedStreamBlockIndex,
			pointer.ResolvedStreamOffset,
			pointer.TargetSpanOffset,
			pointer.TargetSpanLength,
			typeof(T),
			result,
			pointer.FieldPath);
	}

	private void RegisterObjectResult(
		int blockIndex,
		int streamOffset,
		int sourceOffset,
		int length,
		Type resultType,
		object? result,
		string fieldPath)
	{
		if (blockIndex < 0 || streamOffset < 0 || length < 0)
			return;

		var entry = new LoadedObjectResult(
			blockIndex,
			streamOffset,
			sourceOffset,
			length,
			resultType,
			result,
			fieldPath);

		_objectResults[(blockIndex, streamOffset, resultType)] = entry;

		var addressKey = (blockIndex, streamOffset);
		if (!_objectResultsByAddress.TryGetValue(addressKey, out var entries))
		{
			entries = [];
			_objectResultsByAddress[addressKey] = entries;
		}

		entries.RemoveAll(existing => existing.ResultType == resultType);
		entries.Add(entry);
	}

	private bool TryGetRegisteredObjectResult(
		int blockIndex,
		int streamOffset,
		Type resultType,
		out LoadedObjectResult result)
	{
		if (_objectResults.TryGetValue((blockIndex, streamOffset, resultType), out result))
			return true;

		if (!_objectResultsByAddress.TryGetValue((blockIndex, streamOffset), out var entries))
			return false;

		foreach (var entry in entries)
		{
			if (resultType.IsAssignableFrom(entry.ResultType)
				|| entry.Result is not null && resultType.IsInstanceOfType(entry.Result))
			{
				result = entry;
				return true;
			}
		}

		result = default;
		return false;
	}

	private void ReserveInsertCell(Pointer pointer, XFILE_BLOCK block)
	{
		if (!pointer.IsInsertPointer)
			return;

		if (pointer.HasAliasCellStreamAddress)
			return;

		var aliasBlock = XFILE_BLOCK.LARGE;
		var streamOffset = Allocate(aliasBlock, PointerSize, PointerSize);
		pointer.SetAliasCellStreamAddress((int)aliasBlock, streamOffset);
		WritePointerFieldTarget(pointer, (int)aliasBlock, streamOffset);
		if (Environment.GetEnvironmentVariable("FF_TRACE_INSERTS") == "1")
			Console.WriteLine($"trace insert {pointer.FieldPath} block={aliasBlock} cell=0x{streamOffset:X} pos=0x{_position:X}");
	}

	private void RegisterInsertAliasTarget(
		Pointer pointer,
		XFILE_BLOCK targetBlock,
		int sourceOffset,
		int length,
		int targetStreamOffset)
	{
		if (!pointer.HasAliasCellStreamAddress)
			return;

		var alias = new InsertAliasTarget(
			pointer.AliasCellStreamBlockIndex,
			pointer.AliasCellStreamOffset,
			(int)targetBlock,
			targetStreamOffset,
			sourceOffset,
			length,
			pointer.FieldPath);

		_insertAliasTargets[(alias.AliasBlockIndex, alias.AliasStreamOffset)] = alias;

		var encodedTarget = Pointer.EncodeOffset((int)targetBlock, targetStreamOffset);
		BinaryPrimitives.WriteInt32BigEndian(
			_streamBlocks[alias.AliasBlockIndex].AsSpan(alias.AliasStreamOffset, PointerSize),
			encodedTarget);
	}

	private void WritePointerFieldTarget(Pointer pointer, int blockIndex, int streamOffset)
	{
		WritePointerFieldValue(pointer, Pointer.EncodeOffset(blockIndex, streamOffset));
	}

	private void WritePointerFieldValue(Pointer pointer, int value)
	{
		if (!pointer.HasPointerFieldSourceSpan)
			return;

		var blockIndex = pointer.PointerFieldStreamBlockIndex;
		var streamOffset = pointer.PointerFieldStreamOffset;
		if (blockIndex < 0 || blockIndex >= _streamBlocks.Length)
			throw new InvalidDataException($"Pointer field for {pointer.FieldPath} references invalid stream block {blockIndex}.");

		if (streamOffset < 0 || streamOffset + PointerSize > _streamBlocks[blockIndex].Length)
		{
			throw new InvalidDataException(
				$"Pointer field for {pointer.FieldPath} references {FormatStreamAddress(blockIndex, streamOffset)}, "
				+ $"but block length is 0x{_streamBlocks[blockIndex].Length:X}.");
		}

		BinaryPrimitives.WriteInt32BigEndian(
			_streamBlocks[blockIndex].AsSpan(streamOffset, PointerSize),
			value);
	}

	private void PushStreamBlock(XFILE_BLOCK block)
	{
		EnsureValidStreamBlock(block);
		var blockIndex = (int)block;
		_streamBlockStack.Push(new StreamBlockFrame(
			_activeStreamBlock,
			block,
			_streamBlockOffsets[blockIndex]));
		_activeStreamBlock = block;
	}

	private void PopStreamBlock()
	{
		if (!_streamBlockStack.TryPop(out var frame))
			throw new InvalidOperationException("Cannot pop XFILE stream block; the stream block stack is empty.");

		if (_activeStreamBlock == XFILE_BLOCK.TEMP)
			_streamBlockOffsets[(int)XFILE_BLOCK.TEMP] = frame.ActiveBlockOffsetOnPush;

		_activeStreamBlock = frame.PreviousBlock;
	}

	private void EnsureValidStreamBlock(XFILE_BLOCK block)
	{
		var blockIndex = (int)block;
		if (blockIndex < 0 || blockIndex >= _streamBlocks.Length)
			throw new InvalidDataException($"Invalid stream block {block}.");
	}

	private int MaterializeCurrent(XFILE_BLOCK block, int length, int alignment)
	{
		var sourceOffset = IsRuntimeZeroFillBlock(block) ? -1 : _position;
		var streamOffset = Materialize(sourceOffset, block, length, alignment);
		if (!IsRuntimeZeroFillBlock(block))
			_position += length;
		return streamOffset;
	}

	private int Materialize(int sourceOffset, XFILE_BLOCK block, int length, int alignment)
	{
		var blockIndex = (int)block;
		if (length == 0)
			return AlignBlock(block, alignment);

		var streamOffset = Allocate(block, length, alignment);
		if (IsRuntimeZeroFillBlock(block))
		{
			_streamBlocks[blockIndex].AsSpan(streamOffset, length).Clear();
		}
		else
		{
			EnsureReadable(sourceOffset, length);
			Span.Slice(sourceOffset, length).CopyTo(_streamBlocks[blockIndex].AsSpan(streamOffset, length));
		}
		_materializedSpans.Add(new MaterializedSpan(blockIndex, streamOffset, sourceOffset, length));
		return streamOffset;
	}

	private static bool IsRuntimeZeroFillBlock(XFILE_BLOCK block)
	{
		return block == XFILE_BLOCK.RUNTIME;
	}

	private void MaterializeAtCurrentPointer(Pointer pointer, int length, string fieldPath)
	{
		var blockIndex = pointer.StreamBlockIndex;
		var streamOffset = pointer.Offset;
		var endOffset = checked(streamOffset + length);

		if (blockIndex < 0 || blockIndex >= _streamBlocks.Length)
			throw new InvalidDataException($"Invalid stream block {blockIndex} for {fieldPath}.");

		if (streamOffset < 0 || endOffset > _streamBlocks[blockIndex].Length)
		{
			throw new InvalidDataException(
				$"Stream block {(XFILE_BLOCK)blockIndex} overflow while loading {fieldPath}. "
				+ $"Need 0x{endOffset:X} bytes, block has 0x{_streamBlocks[blockIndex].Length:X}.");
		}

		if (length == 0)
			return;

		var sourceOffset = _position;
		EnsureReadable(sourceOffset, length);
		Span.Slice(sourceOffset, length).CopyTo(_streamBlocks[blockIndex].AsSpan(streamOffset, length));
		_position += length;
		_streamBlockOffsets[blockIndex] = Math.Max(_streamBlockOffsets[blockIndex], endOffset);
		_materializedSpans.Add(new MaterializedSpan(blockIndex, streamOffset, sourceOffset, length));
	}

	private int Allocate(XFILE_BLOCK block, int length, int alignment)
	{
		var blockIndex = (int)block;
		var beforeAlign = blockIndex >= 0 && blockIndex < _streamBlockOffsets.Length
			? _streamBlockOffsets[blockIndex]
			: 0;
		var streamOffset = AlignBlock(block, alignment);
		TraceAlignmentPadding(block, beforeAlign, streamOffset);
		var endOffset = checked(streamOffset + length);

		if (blockIndex < 0 || blockIndex >= _streamBlocks.Length)
			throw new InvalidDataException($"Invalid stream block {block}.");

		if (endOffset > _streamBlocks[blockIndex].Length)
		{
			throw new InvalidDataException(
				$"Stream block {block} overflow while loading XFILE data. "
				+ $"Need 0x{endOffset:X} bytes, block has 0x{_streamBlocks[blockIndex].Length:X}.");
		}

		_streamBlockOffsets[blockIndex] = endOffset;
		return streamOffset;
	}

	private void TraceAlignmentPadding(XFILE_BLOCK block, int beforeAlign, int streamOffset)
	{
		var padding = streamOffset - beforeAlign;
		if (padding <= 0 || Environment.GetEnvironmentVariable("FF_TRACE_XFILE_ALIGN") != "1")
			return;

		var minText = Environment.GetEnvironmentVariable("FF_TRACE_XFILE_ALIGN_MIN");
		if (!string.IsNullOrWhiteSpace(minText)
			&& int.TryParse(minText, System.Globalization.NumberStyles.HexNumber, null, out var minPadding)
			&& padding < minPadding)
		{
			return;
		}

		Console.WriteLine(
			$"align block={block} pad=0x{padding:X} before=0x{beforeAlign:X} after=0x{streamOffset:X} "
			+ $"source=0x{_position:X} fieldPath={_currentMaterializeFieldPath ?? "<unknown>"}");
	}

	private int AlignBlock(XFILE_BLOCK block, int alignment)
	{
		var blockIndex = (int)block;
		if (blockIndex < 0 || blockIndex >= _streamBlockOffsets.Length)
			throw new InvalidDataException($"Invalid stream block {block}.");

		if (alignment <= 1)
			return _streamBlockOffsets[blockIndex];

		var mask = alignment - 1;
		if ((alignment & mask) != 0)
			throw new InvalidDataException($"Alignment {alignment} is not a power of two.");

		var aligned = (_streamBlockOffsets[blockIndex] + mask) & ~mask;
		_streamBlockOffsets[blockIndex] = aligned;
		return aligned;
	}

	private void ResolveOffsetPointers(int totalAssetCount)
	{
		var resolvedOffsetPointers = 0;
		var totalWorkUnits = checked(totalAssetCount + _offsetPointers.Count);

		foreach (var pointer in _offsetPointers.Where(pointer => pointer.ResolutionKind != PointerResolutionKind.Alias))
		{
			ValidateOffsetPointerAddress(pointer);

			if (GetPointerResultType(pointer) == typeof(string)
				&& TryResolveStringOffsetPointer(pointer))
			{
				_assetReadProgress?.Invoke(++resolvedOffsetPointers + totalAssetCount, totalWorkUnits);
				continue;
			}

			if (!TryFindResolvedTarget(pointer.StreamBlockIndex, pointer.Offset, 0, out var target))
				ThrowUnresolvedOffsetPointer(pointer, pointer.StreamBlockIndex, pointer.Offset);

			ApplyResolvedTarget(pointer, target);
			ApplyResolvedPointerResult(pointer, target);
			_assetReadProgress?.Invoke(++resolvedOffsetPointers + totalAssetCount, totalWorkUnits);
		}

		foreach (var pointer in _offsetPointers.Where(pointer => pointer.ResolutionKind == PointerResolutionKind.Alias))
		{
			ValidateOffsetPointerAddress(pointer);
			var resolvedAlias = TryResolveAliasOffsetPointer(pointer, out var aliasTarget);
			_assetReadProgress?.Invoke(++resolvedOffsetPointers + totalAssetCount, totalWorkUnits);
			if (resolvedAlias)
				ApplyResolvedPointerResult(pointer, aliasTarget);
		}
	}

	private bool TryResolveAliasOffsetPointer(Pointer pointer, out ResolvedPointerTarget target)
	{
		if (!TryReadAliasPointerCell(pointer, out var cellValue, out var pointerCellSpan))
		{
			throw new InvalidDataException(
				$"Alias offset pointer for {pointer.FieldPath} at source 0x{pointer.PointerFieldSourceOffset:X} "
				+ $"resolved to pointer cell {FormatStreamAddress(pointer.StreamBlockIndex, pointer.Offset)}, "
				+ "but that address is not a loaded pointer cell or insert alias cell"
				+ (pointerCellSpan is null
					? "."
					: $"; enclosing span starts at {FormatStreamAddress(pointerCellSpan.Value.BlockIndex, pointerCellSpan.Value.StreamOffset)} "
						+ $"source=0x{pointerCellSpan.Value.SourceOffset:X} length=0x{pointerCellSpan.Value.Length:X}."));
		}

		WritePointerFieldValue(pointer, cellValue);

		if (cellValue == 0)
		{
			SetResolvedNullPointerResult(pointer);
			target = default;
			return false;
		}

		var targetPointer = new Pointer(cellValue);
		if (targetPointer.Kind != PointerKind.Offset)
		{
			throw new InvalidDataException(
				$"Alias offset pointer for {pointer.FieldPath} at source 0x{pointer.PointerFieldSourceOffset:X} "
				+ $"resolved to pointer cell {FormatStreamAddress(pointer.StreamBlockIndex, pointer.Offset)} "
				+ $"with unresolved value 0x{cellValue:X8}; alias cells must contain null or encoded offset values after loading.");
		}

		ValidateStreamAddress(pointer, targetPointer.StreamBlockIndex, targetPointer.Offset);

		if (!TryFindResolvedTarget(targetPointer.StreamBlockIndex, targetPointer.Offset, 0, out target))
		{
			var enclosingSpan = FindContainingMaterializedSpan(targetPointer.StreamBlockIndex, targetPointer.Offset);
			throw new InvalidDataException(
				$"Alias offset pointer for {pointer.FieldPath} at source 0x{pointer.PointerFieldSourceOffset:X} "
				+ $"read pointer cell {FormatStreamAddress(pointer.StreamBlockIndex, pointer.Offset)} "
				+ $"value 0x{cellValue:X8}, but final target {FormatStreamAddress(targetPointer.StreamBlockIndex, targetPointer.Offset)} "
				+ "is not a loaded object/span start or insert alias cell"
				+ (enclosingSpan is null
					? "."
					: $"; enclosing span starts at {FormatStreamAddress(enclosingSpan.Value.BlockIndex, enclosingSpan.Value.StreamOffset)} "
						+ $"source=0x{enclosingSpan.Value.SourceOffset:X} length=0x{enclosingSpan.Value.Length:X}."));
		}

		ApplyResolvedTarget(pointer, target, writePointerField: false);
		return true;
	}

	private bool TryReadAliasPointerCell(
		Pointer pointer,
		out int cellValue,
		out MaterializedSpan? pointerCellSpan)
	{
		var blockIndex = pointer.StreamBlockIndex;
		var streamOffset = pointer.Offset;
		if (streamOffset < 0 || streamOffset + PointerSize > _streamBlocks[blockIndex].Length)
		{
			cellValue = 0;
			pointerCellSpan = null;
			return false;
		}

		pointerCellSpan = FindContainingMaterializedSpan(blockIndex, streamOffset, PointerSize);
		var isInsertAliasCell = _insertAliasTargets.ContainsKey((blockIndex, streamOffset));
		if (pointerCellSpan is null && !isInsertAliasCell)
		{
			cellValue = 0;
			return false;
		}

		cellValue = BinaryPrimitives.ReadInt32BigEndian(
			_streamBlocks[blockIndex].AsSpan(streamOffset, PointerSize));
		return true;
	}

	private void ThrowUnresolvedOffsetPointer(Pointer pointer, int blockIndex, int streamOffset)
	{
		var enclosingSpan = FindContainingMaterializedSpan(blockIndex, streamOffset);
		throw new InvalidDataException(
			$"Offset pointer for {pointer.FieldPath} at source 0x{pointer.PointerFieldSourceOffset:X} "
			+ $"resolved to {FormatStreamAddress(blockIndex, streamOffset)}, "
			+ "but that address is not a loaded object/span start or insert alias cell"
			+ (enclosingSpan is null
				? "."
				: $"; enclosing span starts at {FormatStreamAddress(enclosingSpan.Value.BlockIndex, enclosingSpan.Value.StreamOffset)} "
					+ $"source=0x{enclosingSpan.Value.SourceOffset:X} length=0x{enclosingSpan.Value.Length:X}."));
	}

	private void SetResolvedNullPointerResult(Pointer pointer)
	{
		var resultType = GetPointerResultType(pointer);
		if (resultType is not null)
			SetPointerResultByType(pointer, resultType, null);
	}

	private bool TryResolveStringPointer<T>(ZonePointer<T> pointer)
	{
		if (!TryFindStringTarget(pointer.StreamBlockIndex, pointer.Offset, out var target, out var value))
			return false;

		ApplyResolvedTarget(pointer, target);
		SetPointerResult(pointer, (T)(object)value);
		return true;
	}

	private bool TryResolveStringOffsetPointer(Pointer pointer)
	{
		if (!TryFindStringTarget(pointer.StreamBlockIndex, pointer.Offset, out var target, out var value))
			return false;

		ApplyResolvedTarget(pointer, target);
		SetPointerResultByType(pointer, typeof(string), value);
		return true;
	}

	private bool TryFindStringTarget(
		int blockIndex,
		int streamOffset,
		out ResolvedPointerTarget target,
		out string value)
	{
		target = default;

		if (TryFindResolvedTarget(blockIndex, streamOffset, 1, out target)
			&& TryReadCStringFromStreamSpan(target.BlockIndex, target.StreamOffset, target.Length, out value, out var exactLength))
		{
			target = new ResolvedPointerTarget(
				target.BlockIndex,
				target.StreamOffset,
				target.SourceOffset,
				exactLength);
			return true;
		}

		var containingSpan = FindContainingMaterializedSpan(blockIndex, streamOffset, 1);
		if (containingSpan is null)
		{
			value = string.Empty;
			return false;
		}

		var maxLength = containingSpan.Value.StreamOffset + containingSpan.Value.Length - streamOffset;
		if (!TryReadCStringFromStreamSpan(blockIndex, streamOffset, maxLength, out value, out var length))
		{
			var loadedLength = blockIndex >= 0 && blockIndex < _streamBlockOffsets.Length
				? _streamBlockOffsets[blockIndex] - streamOffset
				: 0;
			if (loadedLength <= maxLength
				|| !TryReadCStringFromStreamSpan(blockIndex, streamOffset, loadedLength, out value, out length))
			{
				value = string.Empty;
				return false;
			}
		}

		var sourceOffset = containingSpan.Value.SourceOffset < 0
			? -1
			: containingSpan.Value.SourceOffset + (streamOffset - containingSpan.Value.StreamOffset);
		target = new ResolvedPointerTarget(blockIndex, streamOffset, sourceOffset, length);
		RegisterLoadedSpanStart(blockIndex, streamOffset, sourceOffset, length);
		return true;
	}

	private void RegisterLoadedSpanStart(int blockIndex, int streamOffset, int sourceOffset, int length)
	{
		if (blockIndex < 0 || blockIndex >= _streamBlocks.Length || streamOffset < 0 || length < 0)
			return;

		var endOffset = checked(streamOffset + length);
		if (endOffset > _streamBlocks[blockIndex].Length || endOffset > _streamBlockOffsets[blockIndex])
			return;

		if (_materializedSpans.Any(span =>
			    span.BlockIndex == blockIndex
			    && span.StreamOffset == streamOffset
			    && span.Length == length))
		{
			return;
		}

		_materializedSpans.Add(new MaterializedSpan(blockIndex, streamOffset, sourceOffset, length));
	}

		private MaterializedSpan? FindMaterializedSpan(int blockIndex, int streamOffset, int minimumLength)
		{
			MaterializedSpan? best = null;

		foreach (var span in _materializedSpans)
		{
			if (span.BlockIndex != blockIndex
				|| streamOffset != span.StreamOffset
				|| streamOffset + minimumLength > span.StreamOffset + span.Length)
			{
				continue;
			}

			if (best is null
				|| span.Length < best.Value.Length
				|| span.Length == best.Value.Length && span.StreamOffset < best.Value.StreamOffset)
			{
				best = span;
			}
		}

			return best;
		}

		private MaterializedSpan? FindContainingMaterializedSpan(int blockIndex, int streamOffset)
		{
			return FindContainingMaterializedSpan(blockIndex, streamOffset, 1);
		}

		private MaterializedSpan? FindContainingMaterializedSpan(int blockIndex, int streamOffset, int minimumLength)
		{
			MaterializedSpan? best = null;

			foreach (var span in _materializedSpans)
			{
				if (span.BlockIndex != blockIndex
					|| streamOffset < span.StreamOffset
					|| streamOffset + minimumLength > span.StreamOffset + span.Length)
				{
					continue;
				}

				if (best is null
					|| span.Length < best.Value.Length
					|| span.Length == best.Value.Length && span.StreamOffset > best.Value.StreamOffset)
				{
					best = span;
				}
			}

			return best;
		}

		private bool TryFindResolvedTarget(
		int blockIndex,
		int streamOffset,
		int minimumLength,
		out ResolvedPointerTarget target)
	{
		if (_insertAliasTargets.TryGetValue((blockIndex, streamOffset), out var aliasTarget)
			&& minimumLength <= aliasTarget.Length)
		{
			target = new ResolvedPointerTarget(
				aliasTarget.TargetBlockIndex,
				aliasTarget.TargetStreamOffset,
				aliasTarget.SourceOffset,
				aliasTarget.Length);
			return true;
		}

		var span = FindMaterializedSpan(blockIndex, streamOffset, minimumLength);
		if (span is not null)
		{
			target = new ResolvedPointerTarget(
				span.Value.BlockIndex,
				span.Value.StreamOffset,
				span.Value.SourceOffset,
				span.Value.Length);
			return true;
		}

		target = default;
		return false;
	}

	private void ApplyResolvedTarget(Pointer pointer, ResolvedPointerTarget target, bool writePointerField = true)
	{
		pointer.SetTargetSpan(
			target.SourceOffset,
			target.Length,
			target.BlockIndex,
			target.StreamOffset);
		pointer.SetResolvedAddress(target.BlockIndex, target.StreamOffset);
		if (writePointerField)
			WritePointerFieldTarget(pointer, target.BlockIndex, target.StreamOffset);
	}

	private void ApplyResolvedPointerResult(Pointer pointer, ResolvedPointerTarget target)
	{
		var resultType = GetPointerResultType(pointer);
		if (resultType is null)
			return;

		if (TryGetRegisteredObjectResult(target.BlockIndex, target.StreamOffset, resultType, out var registered))
		{
			SetPointerResultByType(pointer, resultType, registered.Result);
			return;
		}

		if (resultType == typeof(byte[]))
		{
			var bytes = _streamBlocks[target.BlockIndex]
				.AsSpan(target.StreamOffset, target.Length)
				.ToArray();
			SetPointerResultByType(pointer, resultType, bytes);
			return;
		}

		if (resultType == typeof(string))
		{
			var value = target.SourceOffset >= 0
				? ReadCStringAtOffset(target.SourceOffset)
				: ReadCStringFromStream(target.BlockIndex, target.StreamOffset);
			SetPointerResultByType(pointer, resultType, value);
			return;
		}

		throw new InvalidDataException(
			$"Offset pointer for {pointer.FieldPath} resolved to {FormatStreamAddress(target.BlockIndex, target.StreamOffset)}, "
			+ $"but no registered {resultType.Name} result exists at that address.");
	}

	private static Type? GetPointerResultType(Pointer pointer)
	{
		for (var type = pointer.GetType(); type is not null; type = type.BaseType)
		{
			if (!type.IsGenericType)
				continue;

			var definition = type.GetGenericTypeDefinition();
			if (definition == typeof(ZonePointer<>)
				|| definition == typeof(DirectPointer<>)
				|| definition == typeof(AliasPointer<>))
			{
				return type.GetGenericArguments()[0];
			}
		}

		return null;
	}

	private static void SetPointerResultByType(Pointer pointer, Type resultType, object? result)
	{
		var zonePointerType = typeof(ZonePointer<>).MakeGenericType(resultType);
		if (!zonePointerType.IsInstanceOfType(pointer))
			return;

		var setResult = zonePointerType.GetMethod(nameof(ZonePointer<object>.SetResult))
			?? throw new MissingMethodException(zonePointerType.FullName, nameof(ZonePointer<object>.SetResult));
		setResult.Invoke(pointer, [result]);
	}

	private void ValidateOffsetPointerAddress(Pointer pointer)
	{
		ValidateStreamAddress(pointer, pointer.StreamBlockIndex, pointer.Offset);
	}

	private void ValidateStreamAddress(Pointer pointer, int blockIndex, int streamOffset)
	{
		if (blockIndex < 0 || blockIndex >= _streamBlocks.Length)
			throw new InvalidDataException($"Offset pointer for {pointer.FieldPath} references invalid stream block {blockIndex}.");

		if (streamOffset < 0 || streamOffset >= _streamBlocks[blockIndex].Length)
		{
			throw new InvalidDataException(
				$"Offset pointer for {pointer.FieldPath} references {FormatStreamAddress(blockIndex, streamOffset)}, "
				+ $"but block length is 0x{_streamBlocks[blockIndex].Length:X}.");
		}
	}

	private static string FormatStreamAddress(int blockIndex, int streamOffset)
	{
		return $"block {blockIndex} offset 0x{streamOffset:X}";
	}

	private int ReadInt32()
	{
		EnsureReadable(_position, sizeof(int));
		var value = BinaryPrimitives.ReadInt32BigEndian(Span.Slice(_position, sizeof(int)));
		_position += sizeof(int);
		return value;
	}

	private static int ReadInt32(ReadOnlySpan<byte> span, ref int offset)
	{
		var value = BinaryPrimitives.ReadInt32BigEndian(span.Slice(offset, sizeof(int)));
		offset += sizeof(int);
		return value;
	}

	private string ReadCString()
	{
		var value = ReadCStringAt(ref _position);
		return value;
	}

	private string ReadCStringAt(ref int offset)
	{
		var slice = Span.Slice(offset);
		var end = slice.IndexOf((byte)0);
		if (end < 0)
			throw new InvalidDataException($"Missing null terminator at XFILE offset 0x{offset:X8}.");

		var value = System.Text.Encoding.Latin1.GetString(slice.Slice(0, end));
		offset += end + 1;
		return value;
	}

	private string ReadCStringAtOffset(int offset)
	{
		var readOffset = offset;
		return ReadCStringAt(ref readOffset);
	}

	private string ReadCStringFromStream(int blockIndex, int streamOffset)
	{
		var slice = _streamBlocks[blockIndex].AsSpan(streamOffset);
		var end = slice.IndexOf((byte)0);
		if (end < 0)
			throw new InvalidDataException($"Missing null terminator at {FormatStreamAddress(blockIndex, streamOffset)}.");

		return System.Text.Encoding.Latin1.GetString(slice.Slice(0, end));
	}

	private bool TryReadCStringFromStreamSpan(
		int blockIndex,
		int streamOffset,
		int maxLength,
		out string value,
		out int byteLength)
	{
		if (maxLength <= 0)
		{
			value = string.Empty;
			byteLength = 0;
			return false;
		}

		var slice = _streamBlocks[blockIndex].AsSpan(streamOffset, maxLength);
		var end = slice.IndexOf((byte)0);
		if (end < 0)
		{
			value = string.Empty;
			byteLength = 0;
			return false;
		}

		value = System.Text.Encoding.Latin1.GetString(slice.Slice(0, end));
		byteLength = end + 1;
		return true;
	}

	private void EnsureHeaderParsed()
	{
		if (_header is null || _streamBlocks.Length == 0)
			throw new InvalidOperationException("ParseHeader must be called before loading XFILE data.");
	}

	private void EnsureReadable(int offset, int length)
	{
		if (offset < 0 || length < 0 || offset + length > Span.Length)
			throw new InvalidDataException(
				$"Attempted to read 0x{length:X} bytes at XFILE offset 0x{offset:X8}, "
				+ $"but the buffer is 0x{Span.Length:X} bytes.");
	}

	private readonly record struct MaterializedSpan(
		int BlockIndex,
		int StreamOffset,
		int SourceOffset,
		int Length);

	private readonly record struct InsertAliasTarget(
		int AliasBlockIndex,
		int AliasStreamOffset,
		int TargetBlockIndex,
		int TargetStreamOffset,
		int SourceOffset,
		int Length,
		string FieldPath);

	private readonly record struct ResolvedPointerTarget(
		int BlockIndex,
		int StreamOffset,
		int SourceOffset,
		int Length);

	private readonly record struct LoadedObjectResult(
		int BlockIndex,
		int StreamOffset,
		int SourceOffset,
		int Length,
		Type ResultType,
		object? Result,
		string FieldPath);

	private readonly record struct StreamBlockFrame(
		XFILE_BLOCK PreviousBlock,
		XFILE_BLOCK ActiveBlock,
		int ActiveBlockOffsetOnPush);

	private sealed class SkippedEbootAssetRoot(XAssetType type, int rootSize) : EbootAssetRoot(type)
	{
		public override int? EbootRootSize => rootSize;
		public override bool IsHandledByEbootDispatch => true;
	}

	private readonly record struct PointerPayload(
		XFILE_BLOCK Block,
		int StreamOffset,
		int SourceOffset,
		byte[] Bytes);
}
