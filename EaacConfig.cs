using System.Buffers.Binary;
using System.Text;

namespace EAACDummyStub;

/// <summary>
///     A launch profile from EAAntiCheat.cfg
/// </summary>
/// <example>
///     "default", "GW2.Main_Win64_Retail.exe", "pvzgw2"
/// </example>
internal sealed record GameProfile(string Name, string Executable, string GameId);

/// <summary>
///     EAAntiCheat.cfg is a resource-only PE image (signed by EA using Authenticode).
///     The launch profiles live in an RT_RCDATA resource as a protobuf (Google Protocol Buffers) message like this:
///     <code>
///         message Config { repeated Profile profiles = 1; ... }
///         message Profile { string name = 1; string executable = 2; string game_id = 3; }
///     </code>
/// </summary>
internal static class EaacConfig
{
    private const uint RtRcData = 10;

    public static List<GameProfile> Read(string path)
    {
        return Parse(File.ReadAllBytes(path));
    }

    public static List<GameProfile> Parse(ReadOnlySpan<byte> image)
    {
        List<GameProfile> profiles = [];
        ProtoReader reader = new(FindRcData(image));
        while (reader.TryReadTag(out int field, out int wireType))
        {
            if (field == 1 && wireType == ProtoReader.LengthDelimited)
            {
                profiles.Add(ParseProfile(reader.ReadBytes()));
            }
            else
            {
                reader.Skip(wireType);
            }
        }
        return profiles;
    }

    private static GameProfile ParseProfile(ReadOnlySpan<byte> data)
    {
        string name = "", executable = "", gameId = "";
        ProtoReader reader = new(data);
        while (reader.TryReadTag(out int field, out int wireType))
        {
            switch (field, wireType)
            {
                case (1, ProtoReader.LengthDelimited): name = Encoding.UTF8.GetString(reader.ReadBytes()); break;
                case (2, ProtoReader.LengthDelimited): executable = Encoding.UTF8.GetString(reader.ReadBytes()); break;
                case (3, ProtoReader.LengthDelimited): gameId = Encoding.UTF8.GetString(reader.ReadBytes()); break;
                default: reader.Skip(wireType); break;
            }
        }
        return new GameProfile(name, executable, gameId);
    }

    /// <summary>Walks the PE resource tree: RT_RCDATA -> first name -> first language -> data.</summary>
    private static ReadOnlySpan<byte> FindRcData(ReadOnlySpan<byte> image)
    {
        if (U16(image, 0) != 0x5A4D)
        {
            throw new InvalidDataException("Missing MZ header.");
        }
        int pe = (int)U32(image, 0x3C);
        if (U32(image, pe) != 0x00004550)
        {
            throw new InvalidDataException("Missing PE signature.");
        }

        int sectionCount = U16(image, pe + 6);
        int optionalHeader = pe + 24;
        int sectionTable = optionalHeader + U16(image, pe + 20);
        int dataDirectories = U16(image, optionalHeader) switch
        {
            0x20B => optionalHeader + 112, // PE32+
            0x10B => optionalHeader + 96,  // PE32
            ushort magic => throw new InvalidDataException($"Unknown optional header magic 0x{magic:X}."),
        };

        uint resourceRva = U32(image, dataDirectories + (2 * 8)); // IMAGE_DIRECTORY_ENTRY_RESOURCE
        if (resourceRva == 0)
        {
            throw new InvalidDataException("Image has no resource directory.");
        }

        int root = RvaToOffset(image, sectionTable, sectionCount, resourceRva);
        int SubDirectory(uint entry) => root + (int)(entry & 0x7FFFFFFF);

        uint typeEntry = FindEntry(image, root, RtRcData) ?? throw new InvalidDataException("No RT_RCDATA resource.");
        uint nameEntry = FindEntry(image, SubDirectory(typeEntry), null) ?? throw new InvalidDataException("Empty RT_RCDATA directory.");
        uint langEntry = FindEntry(image, SubDirectory(nameEntry), null) ?? throw new InvalidDataException("Empty RT_RCDATA language directory.");

        int dataEntry = root + (int)langEntry;
        int offset = RvaToOffset(image, sectionTable, sectionCount, U32(image, dataEntry));
        int length = (int)U32(image, dataEntry + 4);
        return image.Slice(offset, length);
    }

    private static int RvaToOffset(ReadOnlySpan<byte> image, int sectionTable, int sectionCount, uint rva)
    {
        for (int i = 0; i < sectionCount; i++)
        {
            int section = sectionTable + (i * 40);
            uint virtualAddress = U32(image, section + 12);
            uint size = Math.Max(U32(image, section + 8), U32(image, section + 16));
            if (rva >= virtualAddress && rva < virtualAddress + size)
            {
                return (int)(U32(image, section + 20) + (rva - virtualAddress));
            }
        }
        throw new InvalidDataException($"RVA 0x{rva:X} is outside every section.");
    }

    /// <summary>Returns OffsetToData of the entry with the given ID (or the first entry when ID is null).</summary>
    private static uint? FindEntry(ReadOnlySpan<byte> image, int directory, uint? id)
    {
        int count = U16(image, directory + 12) + U16(image, directory + 14);
        for (int i = 0; i < count; i++)
        {
            int entry = directory + 16 + (i * 8);
            uint name = U32(image, entry);
            if (id is null || name == id)
            {
                return U32(image, entry + 4);
            }
        }
        return null;
    }

    private static ushort U16(ReadOnlySpan<byte> data, int offset) => BinaryPrimitives.ReadUInt16LittleEndian(data[offset..]);

    private static uint U32(ReadOnlySpan<byte> data, int offset) => BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);

    /// <summary>Minimal protobuf wire format reader that is enough for reading the config.</summary>
    private ref struct ProtoReader(ReadOnlySpan<byte> data)
    {
        public const int Varint = 0, Fixed64 = 1, LengthDelimited = 2, Fixed32 = 5;

        private readonly ReadOnlySpan<byte> _data = data;
        private int _position;

        public bool TryReadTag(out int field, out int wireType)
        {
            if (_position >= _data.Length)
            {
                field = wireType = 0;
                return false;
            }
            ulong tag = ReadVarint();
            field = (int)(tag >> 3);
            wireType = (int)(tag & 7);
            return true;
        }

        public ReadOnlySpan<byte> ReadBytes()
        {
            int length = (int)ReadVarint();
            ReadOnlySpan<byte> bytes = _data.Slice(_position, length);
            _position += length;
            return bytes;
        }

        public void Skip(int wireType)
        {
            switch (wireType)
            {
                case Varint: ReadVarint(); break;
                case Fixed64: _position += 8; break;
                case LengthDelimited: ReadBytes(); break;
                case Fixed32: _position += 4; break;
                default: throw new InvalidDataException($"Unsupported protobuf wire type {wireType}.");
            }
        }

        private ulong ReadVarint()
        {
            ulong result = 0;
            for (int shift = 0; shift < 64; shift += 7)
            {
                byte b = _data[_position++];
                result |= (ulong)(b & 0x7F) << shift;
                if ((b & 0x80) == 0)
                {
                    return result;
                }
            }
            throw new InvalidDataException("Malformed protobuf varint.");
        }
    }
}