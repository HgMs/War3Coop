using System.Text;
using War3Net.IO.Compression;

namespace Wc3Inspect;

internal static class GameCacheInspector
{
    private static readonly byte[] PackedSignature = Encoding.ASCII.GetBytes("Warcraft III recorded game\u001a\0");

    private const uint MaxPackedBytes = 64 * 1024 * 1024;
    private const uint MaxCollectionCount = 100_000;
    private const int MaxStringBytes = 1024 * 1024;

    public static object Inspect(string inputPath)
    {
        var path = Path.GetFullPath(inputPath);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Game cache file was not found.", path);
        }

        var file = new FileInfo(path);
        using var stream = File.OpenRead(path);
        using var packedReader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: false);
        var packed = ReadPackedFile(packedReader);
        using var cacheReader = new BinaryReader(packed.Data, Encoding.UTF8, leaveOpen: false);
        var cache = ReadGameCache(cacheReader);

        if (cacheReader.BaseStream.Position != cacheReader.BaseStream.Length)
        {
            throw new InvalidDataException(
                $"Game cache has {cacheReader.BaseStream.Length - cacheReader.BaseStream.Position} unread decompressed bytes.");
        }

        return new
        {
            schemaVersion = 1,
            kind = "game-cache",
            source = new
            {
                path,
                size = file.Length,
                sha256 = Hashing.Sha256(path),
            },
            packed = new
            {
                packed.DataOffset,
                packed.CompressedSize,
                packed.HeaderVersion,
                packed.DecompressedSize,
                packed.BlockCount,
                packed.GameIdentifier,
                packed.GameVersion,
                packed.Build,
                packed.Flags,
                packed.GameLength,
                packed.HeaderChecksum,
            },
            cache = new
            {
                cache.Version,
                gameCacheCount = cache.GameCaches.Count,
                gameCaches = cache.GameCaches,
            },
            warnings = new[]
            {
                "Packed-file block and header checksums are reported but not validated.",
                "Unknown unit-state fields are intentionally omitted from this read-only report.",
            },
        };
    }

    private static PackedGameCache ReadPackedFile(BinaryReader reader)
    {
        var signature = ReadExactly(reader, PackedSignature.Length);
        if (!signature.SequenceEqual(PackedSignature))
        {
            throw new InvalidDataException("Invalid Warcraft III packed-file signature.");
        }

        var dataOffset = reader.ReadUInt32();
        var compressedSize = reader.ReadUInt32();
        var headerVersion = reader.ReadUInt32();
        var decompressedSize = reader.ReadUInt32();
        var blockCount = reader.ReadUInt32();

        if (decompressedSize > MaxPackedBytes)
        {
            throw new InvalidDataException($"Decompressed game cache exceeds the {MaxPackedBytes}-byte limit.");
        }

        string gameIdentifier;
        uint gameVersion;
        if (headerVersion == 0)
        {
            if (dataOffset != 0x40)
            {
                throw new InvalidDataException($"Header version 0 requires data offset 0x40, found 0x{dataOffset:X}.");
            }

            _ = reader.ReadUInt16();
            gameVersion = reader.ReadUInt16();
            gameIdentifier = string.Empty;
        }
        else if (headerVersion == 1)
        {
            if (dataOffset != 0x44)
            {
                throw new InvalidDataException($"Header version 1 requires data offset 0x44, found 0x{dataOffset:X}.");
            }

            gameIdentifier = FormatFourCc(ReadExactly(reader, 4).Reverse().ToArray());
            gameVersion = reader.ReadUInt32();
        }
        else
        {
            throw new NotSupportedException($"Packed-file header version {headerVersion} is not supported.");
        }

        var build = reader.ReadUInt16();
        var flags = reader.ReadUInt16();
        var gameLength = reader.ReadUInt32();
        var headerChecksum = reader.ReadUInt32();
        if (reader.BaseStream.Position != dataOffset)
        {
            throw new InvalidDataException(
                $"Packed-file header ended at offset {reader.BaseStream.Position}, expected {dataOffset}.");
        }

        var output = new MemoryStream((int)decompressedSize);
        for (var block = 0U; block < blockCount; block++)
        {
            var compressedBlockSize = reader.ReadUInt16();
            var decompressedBlockSize = reader.ReadUInt16();
            _ = reader.ReadUInt32();
            var compressedBlock = ReadExactly(reader, compressedBlockSize);
            var decompressedBlock = ZLibCompression.Decompress(compressedBlock, decompressedBlockSize);
            var remaining = checked((int)(decompressedSize - output.Length));
            output.Write(decompressedBlock, 0, Math.Min(decompressedBlock.Length, remaining));
        }

        if (output.Length != decompressedSize)
        {
            throw new InvalidDataException(
                $"Decompressed {output.Length} bytes, expected {decompressedSize}.");
        }

        output.Position = 0;
        return new PackedGameCache(
            dataOffset,
            compressedSize,
            headerVersion,
            decompressedSize,
            blockCount,
            gameIdentifier,
            gameVersion,
            build,
            flags,
            gameLength,
            headerChecksum,
            output);
    }

    private static ParsedGameCache ReadGameCache(BinaryReader reader)
    {
        var version = reader.ReadUInt32();
        var gameCacheCount = ReadCount(reader, "game cache");
        var gameCaches = new List<object>(checked((int)gameCacheCount));
        for (var cacheIndex = 0U; cacheIndex < gameCacheCount; cacheIndex++)
        {
            var name = ReadNullTerminatedString(reader);
            var reserved = reader.ReadUInt32();
            var categoryCount = ReadCount(reader, "category");
            var categories = new List<object>(checked((int)categoryCount));
            for (var categoryIndex = 0U; categoryIndex < categoryCount; categoryIndex++)
            {
                categories.Add(ReadCategory(reader));
            }

            gameCaches.Add(new { name, reserved, categoryCount, categories });
        }

        return new ParsedGameCache(version, gameCaches);
    }

    private static object ReadCategory(BinaryReader reader)
    {
        var name = ReadNullTerminatedString(reader);
        var reservedVariableTypes = Convert.ToHexString(ReadExactly(reader, 5));
        var filler = Convert.ToHexString(ReadExactly(reader, 15));

        var integerCount = ReadCount(reader, "integer entry");
        var integers = ReadEntries(integerCount, () => new
        {
            name = ReadNullTerminatedString(reader),
            value = reader.ReadUInt32(),
        });

        var realCount = ReadCount(reader, "real entry");
        var reals = ReadEntries(realCount, () => new
        {
            name = ReadNullTerminatedString(reader),
            value = reader.ReadSingle(),
        });

        var booleanCount = ReadCount(reader, "boolean entry");
        var booleans = ReadEntries(booleanCount, () => new
        {
            name = ReadNullTerminatedString(reader),
            value = reader.ReadUInt32() != 0,
        });

        var unitCount = ReadCount(reader, "unit entry");
        var units = ReadEntries(unitCount, () => ReadUnit(reader));

        var stringCount = ReadCount(reader, "string entry");
        var strings = ReadEntries(stringCount, () => new
        {
            name = ReadNullTerminatedString(reader),
            value = ReadNullTerminatedString(reader),
        });

        return new
        {
            name,
            reservedVariableTypes,
            filler,
            integerCount,
            integers,
            realCount,
            reals,
            booleanCount,
            booleans,
            unitCount,
            units,
            stringCount,
            strings,
        };
    }

    private static object ReadUnit(BinaryReader reader)
    {
        var name = ReadNullTerminatedString(reader);
        var unitId = ReadFourCc(reader);
        var inventorySlotCount = ReadCount(reader, "inventory slot");
        if (inventorySlotCount > 64)
        {
            throw new InvalidDataException($"Inventory slot count {inventorySlotCount} exceeds the safety limit.");
        }

        var inventory = ReadEntries(inventorySlotCount, () =>
        {
            var itemId = ReadFourCc(reader);
            var charges = reader.ReadUInt32();
            var flags = ReadExactly(reader, 4);
            return new
            {
                itemId,
                charges,
                flags = Convert.ToHexString(flags),
                empty = itemId == "\\0\\0\\0\\0",
            };
        });

        var experience = reader.ReadUInt32();
        var levelMinusOne = reader.ReadUInt32();
        var unusedSkillPoints = reader.ReadUInt32();
        var heroProperNameIndex = reader.ReadUInt16();
        var unknownHeroProperNameIndex2 = reader.ReadUInt16();
        var strength = reader.ReadUInt32();
        var strengthBonus = reader.ReadSingle();
        var agility = reader.ReadUInt32();
        var moveSpeedIncrementFromAgility = reader.ReadSingle();
        var attackSpeedIncrementFromAgility = reader.ReadSingle();
        var agilityBonus = reader.ReadSingle();
        var intelligence = reader.ReadUInt32();
        var intelligenceBonus = reader.ReadSingle();

        var heroSkillCount = ReadCount(reader, "hero skill");
        if (heroSkillCount > 1_000)
        {
            throw new InvalidDataException($"Hero skill count {heroSkillCount} exceeds the safety limit.");
        }

        var heroSkills = ReadEntries(heroSkillCount, () => new
        {
            abilityId = ReadFourCc(reader),
            currentLevel = reader.ReadUInt32(),
        });

        var lifeIncrement = reader.ReadSingle();
        var manaIncrement = reader.ReadSingle();
        var sightRangeDay = reader.ReadSingle();
        var unknownState = Convert.ToHexString(ReadExactly(reader, 4));
        var attackIncrement1 = reader.ReadUInt32();
        var attackIncrement2 = reader.ReadUInt32();
        var armorIncrement = reader.ReadSingle();
        var unknownTail = Convert.ToHexString(ReadExactly(reader, 2));

        return new
        {
            name,
            unitId,
            inventorySlotCount,
            inventory,
            experience,
            level = checked(levelMinusOne + 1),
            unusedSkillPoints,
            heroProperNameIndex,
            unknownHeroProperNameIndex2,
            attributes = new
            {
                strength,
                strengthBonus,
                agility,
                agilityBonus,
                intelligence,
                intelligenceBonus,
            },
            agilityDerived = new { moveSpeedIncrementFromAgility, attackSpeedIncrementFromAgility },
            heroSkillCount,
            heroSkills,
            stateDeltas = new
            {
                lifeIncrement,
                manaIncrement,
                sightRangeDay,
                attackIncrement1,
                attackIncrement2,
                armorIncrement,
            },
            unknownState,
            unknownTail,
        };
    }

    private static List<T> ReadEntries<T>(uint count, Func<T> read)
    {
        var entries = new List<T>(checked((int)count));
        for (var index = 0U; index < count; index++)
        {
            entries.Add(read());
        }

        return entries;
    }

    private static uint ReadCount(BinaryReader reader, string label)
    {
        var count = reader.ReadUInt32();
        if (count > MaxCollectionCount)
        {
            throw new InvalidDataException($"{label} count {count} exceeds the safety limit.");
        }

        return count;
    }

    private static string ReadNullTerminatedString(BinaryReader reader)
    {
        using var bytes = new MemoryStream();
        while (true)
        {
            if (reader.BaseStream.Position >= reader.BaseStream.Length)
            {
                throw new EndOfStreamException("Unterminated game-cache string.");
            }

            var value = reader.ReadByte();
            if (value == 0)
            {
                return Encoding.UTF8.GetString(bytes.ToArray());
            }

            bytes.WriteByte(value);
            if (bytes.Length > MaxStringBytes)
            {
                throw new InvalidDataException("Game-cache string exceeds the safety limit.");
            }
        }
    }

    private static string ReadFourCc(BinaryReader reader)
    {
        var bytes = ReadExactly(reader, 4);
        Array.Reverse(bytes);
        return FormatFourCc(bytes);
    }

    private static string FormatFourCc(byte[] bytes)
    {
        return string.Concat(bytes.Select(value => value is >= 0x20 and <= 0x7E ? ((char)value).ToString() : "\\0"));
    }

    private static byte[] ReadExactly(BinaryReader reader, int count)
    {
        var bytes = reader.ReadBytes(count);
        if (bytes.Length != count)
        {
            throw new EndOfStreamException($"Expected {count} bytes, found {bytes.Length}.");
        }

        return bytes;
    }

    private sealed record PackedGameCache(
        uint DataOffset,
        uint CompressedSize,
        uint HeaderVersion,
        uint DecompressedSize,
        uint BlockCount,
        string GameIdentifier,
        uint GameVersion,
        ushort Build,
        ushort Flags,
        uint GameLength,
        uint HeaderChecksum,
        MemoryStream Data);

    private sealed record ParsedGameCache(uint Version, List<object> GameCaches);
}
