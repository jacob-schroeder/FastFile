using FastFile.LogicOLD.Zone;
using FastFile.ModelsOLD.Assets.SoundAliasList;
using FastFile.ModelsOLD.Data;
using FastFile.ModelsOLD.Zone;
using FastFile.ModelsOLD.Zone.Attributes;

namespace FastFile.LogicOLD.Assets.Readers;

public sealed class SoundAssetReader : XAssetReadHandler
{
    private static readonly XPointerFieldAttribute SndAliasArrayAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        Alignment = 4,
        CountMember = nameof(SndAliasList.Count)
    };

    private static readonly XPointerFieldAttribute SoundFileArrayAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        Alignment = 4,
        CountMember = nameof(SndAlias.SoundFileCount)
    };

    private static readonly XPointerFieldAttribute SndCurveWrapperAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Alias,
        Target = XPointerTarget.Object,
        PayloadBlock = XFILE_BLOCK.TEMP,
        UseCurrentStream = true,
        Alignment = 4,
        OffsetIsAliasCell = true
    };

    private static readonly XPointerFieldAttribute LoadedSoundWrapperAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Alias,
        Target = XPointerTarget.Object,
        PayloadBlock = XFILE_BLOCK.TEMP,
        UseCurrentStream = true,
        Alignment = 4,
        OffsetIsAliasCell = true
    };

    private static readonly XPointerFieldAttribute LoadedSoundSeekTableAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ByteArray,
        UseCurrentStream = true,
        Alignment = 4,
        CountMember = nameof(LoadedSound.SeekTableByteCount)
    };

    private static readonly XPointerFieldAttribute LoadedSoundPhysicalAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ByteArray,
        UseCurrentStream = true,
        Alignment = 64,
        CountMember = nameof(LoadedSound.PhysicalDataByteCount)
    };

    private static readonly XPointerFieldAttribute CStringAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.CString
    };

    public override bool TryResolveLoadedObjectPointers(
        object value,
        IXAssetReaderContext context)
    {
        switch (value)
        {
            case SndAliasList aliasList:
                Load_SndAliasList(aliasList, context);
                return true;

            case SndAlias alias:
                Load_SndAlias(alias, context);
                return true;

            case SoundFile soundFile:
                Load_SoundFile(soundFile, context);
                return true;

            case LoadedSound loadedSound:
                Load_LoadedSound(loadedSound, context);
                return true;

            case SndCurve sndCurve:
                Load_SndCurve(sndCurve, context);
                return true;

            default:
                return false;
        }
    }

    private static void Load_SndAliasList(
        SndAliasList aliasList,
        IXAssetReaderContext context)
    {
        context.WithStreamBlock(XFILE_BLOCK.LARGE, () =>
        {
            context.ResolvePointerProperty(aliasList, nameof(SndAliasList.AliasNamePtr));
            context.ResolvePointerValue(aliasList.Aliases, SndAliasArrayAttribute, aliasList);
        });
    }

    private static void Load_SndAlias(
        SndAlias alias,
        IXAssetReaderContext context)
    {
        context.ResolvePointerProperty(alias, nameof(SndAlias.AliasNamePtr));
        context.ResolvePointerProperty(alias, nameof(SndAlias.SubtitlePtr));
        context.ResolvePointerProperty(alias, nameof(SndAlias.SecondaryAliasNamePtr));
        context.ResolvePointerProperty(alias, nameof(SndAlias.ChainAliasNamePtr));
        context.ResolvePointerProperty(alias, nameof(SndAlias.MixerGroupPtr));

        // PS3 helper 0x116f08 defaults this inline array to a single 0x10 SoundFile entry.
        alias.SoundFileCount = 1;

        context.WithStreamBlock(XFILE_BLOCK.TEMP, () =>
        {
            context.ResolvePointerValue(alias.SoundFiles, SoundFileArrayAttribute, alias);
        });

        context.WithStreamBlock(XFILE_BLOCK.TEMP, () =>
        {
            context.ResolvePointerValue(alias.VolumeFalloffCurve, SndCurveWrapperAttribute, alias);
        });

        context.ResolvePointerProperty(alias, nameof(SndAlias.SpeakerMap));
    }

    private static void Load_SoundFile(
        SoundFile soundFile,
        IXAssetReaderContext context)
    {
        context.WithStreamBlock(XFILE_BLOCK.LARGE, () =>
        {
            if (soundFile.Type == SndAliasType.SAT_LOADED)
            {
                soundFile.LoadedSoundPtr = XPointerCodec.CreatePointer<LoadedSound>(
                    soundFile.UnionRaw0,
                    PointerResolutionKind.Alias,
                    new XBlockAddress(XFILE_BLOCK.TEMP, soundFile.Offset + 0x04));

                context.WithStreamBlock(XFILE_BLOCK.TEMP, () =>
                {
                    context.ResolvePointerValue(soundFile.LoadedSoundPtr, LoadedSoundWrapperAttribute, soundFile);
                });

                return;
            }

            var streamed = new StreamedSound
            {
                FileIndex = unchecked((uint)soundFile.UnionRaw0),
                RawInfoBytes = soundFile.UnionData.AsSpan(4, 8).ToArray()
            };

            if (streamed.FileIndex == 0)
            {
                streamed.DirPtr = XPointerCodec.CreatePointer<string>(
                    soundFile.UnionRaw1,
                    PointerResolutionKind.Direct,
                    new XBlockAddress(XFILE_BLOCK.TEMP, soundFile.Offset + 0x08));

                streamed.NamePtr = XPointerCodec.CreatePointer<string>(
                    soundFile.UnionRaw2,
                    PointerResolutionKind.Direct,
                    new XBlockAddress(XFILE_BLOCK.TEMP, soundFile.Offset + 0x0C));

                context.ResolvePointerValue(streamed.DirPtr, CStringAttribute, streamed);
                context.ResolvePointerValue(streamed.NamePtr, CStringAttribute, streamed);
            }

            soundFile.StreamedSound = streamed;
        });
    }

    private static void Load_LoadedSound(
        LoadedSound loadedSound,
        IXAssetReaderContext context)
    {
        context.WithStreamBlock(XFILE_BLOCK.LARGE, () =>
        {
            context.ResolvePointerProperty(loadedSound, nameof(LoadedSound.NamePtr));
            context.ResolvePointerValue(loadedSound.SeekTablePtr, LoadedSoundSeekTableAttribute, loadedSound);

            context.WithStreamBlock(XFILE_BLOCK.PHYSICAL, () =>
            {
                context.ResolvePointerValue(loadedSound.PhysicalDataPtr, LoadedSoundPhysicalAttribute, loadedSound);
            });
        });
    }

    private static void Load_SndCurve(
        SndCurve sndCurve,
        IXAssetReaderContext context)
    {
        context.WithStreamBlock(XFILE_BLOCK.LARGE, () =>
        {
            context.ResolvePointerProperty(sndCurve, nameof(SndCurve.FilenamePtr));
        });
    }
}
