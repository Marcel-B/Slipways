using System.Net;
using Slipways.Infrastructure.Websites;

namespace Slipways.Api.Tests;

public class PublicAddressGuardTests
{
    [Theory]
    [InlineData("93.184.215.14", true)]
    [InlineData("2a01:4f8:c17:1234::1", true)]
    [InlineData("127.0.0.1", false)]
    [InlineData("10.1.2.3", false)]
    [InlineData("172.20.0.5", false)]
    [InlineData("192.168.178.1", false)]
    [InlineData("100.73.171.118", false)]
    [InlineData("169.254.169.254", false)]
    [InlineData("0.0.0.0", false)]
    [InlineData("::1", false)]
    [InlineData("fd7a:115c:a1e0::1", false)]
    [InlineData("fe80::1", false)]
    [InlineData("::ffff:192.168.0.1", false)]
    [InlineData("2001:db8::1", false)]
    public void Tells_public_addresses_from_private_ones(string address, bool isPublic) =>
        Assert.Equal(isPublic, PublicAddressGuard.IsPublic(IPAddress.Parse(address)));
}
