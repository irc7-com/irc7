using System;
using Irc.Enumerations;
using Irc.Helpers;
using Irc.Security.Credentials;
using NUnit.Framework;

namespace SSPI.GateKeeper.Tests;

public class GateKeeperTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void AcceptSecurityContext_V1_Auth_Fails_If_Guid_Exists()
    {
        var gateKeeper = new Irc.Security.Packages.GateKeeper(new DefaultProvider());
        var gateKeeperToken = new GateKeeperToken();
        gateKeeperToken.Signature = "GKSSP\0".ToByteArray();
        gateKeeperToken.Version = 1;
        gateKeeperToken.Sequence = (int)EnumSupportPackageSequence.SSP_INIT;

        var token = $"{gateKeeperToken.Serialize<GateKeeperToken>().ToAsciiString()}";

        Assert.That(EnumSupportPackageSequence.SSP_OK,
            Is.EqualTo(gateKeeper.InitializeSecurityContext(token, string.Empty)));

        gateKeeperToken.Version = 1;
        gateKeeperToken.Sequence = (int)EnumSupportPackageSequence.SSP_EXT;

        token =
            $"{gateKeeperToken.Serialize<GateKeeperToken>().ToAsciiString()}{new Guid().ToByteArray().ToAsciiString()}{new Guid().ToByteArray().ToAsciiString()}";
        gateKeeper.SetChallenge(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 });
        Assert.That(EnumSupportPackageSequence.SSP_FAILED,
            Is.EqualTo(gateKeeper.AcceptSecurityContext(token, string.Empty)));
    }

    [Test]
    public void AcceptSecurityContext_Auth_Fails_If_Guest_And_Guid_Blank()
    {
        var gateKeeper = new Irc.Security.Packages.GateKeeper(new DefaultProvider());
        gateKeeper.Guest = true;

        var gateKeeperToken = new GateKeeperToken();
        gateKeeperToken.Signature = "GKSSP\0".ToByteArray();
        gateKeeperToken.Version = 2;
        gateKeeperToken.Sequence = (int)EnumSupportPackageSequence.SSP_INIT;

        var token = $"{gateKeeperToken.Serialize<GateKeeperToken>().ToAsciiString()}";

        Assert.That(EnumSupportPackageSequence.SSP_OK,
            Is.EqualTo(gateKeeper.InitializeSecurityContext(token, string.Empty)));

        gateKeeperToken.Version = 2;
        gateKeeperToken.Sequence = (int)EnumSupportPackageSequence.SSP_EXT;

        // Below contains magical answer guid to null byte challenge
        token =
            $"{gateKeeperToken.Serialize<GateKeeperToken>().ToAsciiString()}{Guid.Parse("e23a3251-b322-8b2b-a34c-c4d0be30c5dd").ToByteArray().ToAsciiString()}{new Guid().ToByteArray().ToAsciiString()}";
        gateKeeper.SetChallenge(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 });
        gateKeeper.CreateSecurityChallenge();
        Assert.That(EnumSupportPackageSequence.SSP_FAILED,
            Is.EqualTo(gateKeeper.AcceptSecurityContext(token, string.Empty)));
    }

    [Test]
    public void AcceptSecurityContext_Auth_Succeeds_If_Not_Guest_And_Guid_Blank()
    {
        var gateKeeper = new Irc.Security.Packages.GateKeeper(new DefaultProvider());
        gateKeeper.Guest = false;

        var gateKeeperToken = new GateKeeperToken();
        gateKeeperToken.Signature = "GKSSP\0".ToByteArray();
        gateKeeperToken.Version = 2;
        gateKeeperToken.Sequence = (int)EnumSupportPackageSequence.SSP_INIT;

        var token = $"{gateKeeperToken.Serialize<GateKeeperToken>().ToAsciiString()}";

        Assert.That(EnumSupportPackageSequence.SSP_OK,
            Is.EqualTo(gateKeeper.InitializeSecurityContext(token, string.Empty)));

        gateKeeperToken.Version = 2;
        gateKeeperToken.Sequence = (int)EnumSupportPackageSequence.SSP_EXT;

        // Below contains magical answer guid to null byte challenge
        token =
            $"{gateKeeperToken.Serialize<GateKeeperToken>().ToAsciiString()}{Guid.Parse("e23a3251-b322-8b2b-a34c-c4d0be30c5dd").ToByteArray().ToAsciiString()}{new Guid().ToByteArray().ToAsciiString()}";
        gateKeeper.SetChallenge(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 });
        gateKeeper.CreateSecurityChallenge();
        Assert.That(EnumSupportPackageSequence.SSP_OK,
            Is.EqualTo(gateKeeper.AcceptSecurityContext(token, string.Empty)));
    }

    [Test]
    public void AcceptSecurityContext_V3_Auth_Succeeds_With_IP()
    {
        var ip = "1.2.3.4";

        var gateKeeper = new Irc.Security.Packages.GateKeeper(new DefaultProvider());
        gateKeeper.Guest = false;

        var gateKeeperToken = new GateKeeperToken();
        gateKeeperToken.Signature = "GKSSP\0".ToByteArray();
        gateKeeperToken.Version = 3;
        gateKeeperToken.Sequence = (int)EnumSupportPackageSequence.SSP_INIT;

        var token = $"{gateKeeperToken.Serialize<GateKeeperToken>().ToAsciiString()}";

        Assert.That(EnumSupportPackageSequence.SSP_OK,
            Is.EqualTo(gateKeeper.InitializeSecurityContext(token, string.Empty)));

        gateKeeperToken.Version = 3;
        gateKeeperToken.Sequence = (int)EnumSupportPackageSequence.SSP_EXT;

        // Below contains magical answer guid to null byte challenge with ip
        token =
            $"{gateKeeperToken.Serialize<GateKeeperToken>().ToAsciiString()}{Guid.Parse("a8b9a59e-bd4d-411d-7728-4ec15d29282b").ToByteArray().ToAsciiString()}{new Guid().ToByteArray().ToAsciiString()}";
        gateKeeper.SetChallenge(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 });
        gateKeeper.CreateSecurityChallenge();
        Assert.That(EnumSupportPackageSequence.SSP_OK, Is.EqualTo(gateKeeper.AcceptSecurityContext(token, ip)));
    }
}