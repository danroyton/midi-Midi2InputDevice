using MidiController.Infrastructure.Input;

namespace MidiController.Infrastructure.Tests;

public sealed class VirtualKeyMapperTests
{
    [Theory]
    [InlineData("ctrl",   0x11)]
    [InlineData("CTRL",   0x11)]
    [InlineData("shift",  0x10)]
    [InlineData("alt",    0x12)]
    [InlineData("enter",  0x0D)]
    [InlineData("escape", 0x1B)]
    [InlineData("esc",    0x1B)]
    [InlineData("space",  0x20)]
    [InlineData("f1",     0x70)]
    [InlineData("f12",    0x7B)]
    [InlineData("mute",   0xAD)]
    public void Resolve_KnownKeys_ReturnsCorrectVk(string key, ushort expected)
    {
        Assert.Equal(expected, VirtualKeyMapper.Resolve(key));
    }

    [Theory]
    [InlineData("a", (ushort)'A')]
    [InlineData("A", (ushort)'A')]
    [InlineData("z", (ushort)'Z')]
    [InlineData("0", (ushort)'0')]
    [InlineData("9", (ushort)'9')]
    public void Resolve_SingleCharAlphanumeric_ReturnsAsciiUpperVk(string key, ushort expected)
    {
        Assert.Equal(expected, VirtualKeyMapper.Resolve(key));
    }

    [Theory]
    [InlineData("xyz_unknown")]
    [InlineData("")]
    [InlineData("§")]
    public void Resolve_UnknownKey_ThrowsArgumentException(string key)
    {
        Assert.Throws<ArgumentException>(() => VirtualKeyMapper.Resolve(key));
    }

    [Fact]
    public void Resolve_IsCaseInsensitive_ForNamedKeys()
    {
        Assert.Equal(VirtualKeyMapper.Resolve("ctrl"), VirtualKeyMapper.Resolve("CTRL"));
        Assert.Equal(VirtualKeyMapper.Resolve("f5"),   VirtualKeyMapper.Resolve("F5"));
    }
}
