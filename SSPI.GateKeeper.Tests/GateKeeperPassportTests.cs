namespace SSPI.GateKeeper.Tests;

internal class GateKeeperPassportTests
{
    //[Test, Ignore("Temporarily disabled")]
    //public void AcceptSecurityContext_V3_Auth_Succeeds_With_Cookies()
    //{
    //    var ip = "1.2.3.4";


    //    var passportProvider = new PassportProvider();

    //    var gateKeeperPassport = new GateKeeperPassport(passportProvider);

    //    var passportCredentials = new PassportCredentials();
    //    passportCredentials.Id = new byte[16];
    //    passportCredentials.Expiry = DateTime.UtcNow.AddDays(1);

    //    var ticketCookie = passportProvider.Encrypt(passportCredentials);

    //    var passportCredentials2 = new PassportCredentials();
    //    passportCredentials2.Ticket = passportCredentials.Ticket;
    //    passportCredentials2.Profile = passportCredentials.Profile;

    //    var decrypted = passportProvider.Decrypt(passportCredentials2);

    //    var failed = false;

    //    //for (int i = 0; i < 1000; i++)
    //    //{
    //    //    var newCookie = passportProvider.Encrypt(puid.ToAsciiString(), DateTime.UtcNow);
    //    //    if (newCookie[0] != ticketCookie[0]) failed = true;
    //    //}


    //    var profileCookie = ""; //;passportProvider.Encrypt(JsonConvert.SerializeObject()));

    //    gateKeeperPassport.Guest = false;

    //    var gateKeeperToken = new GateKeeperToken();
    //    gateKeeperToken.Signature = "GKSSP\0".ToByteArray();
    //    gateKeeperToken.Version = 3;
    //    gateKeeperToken.Sequence = (int) EnumSupportPackageSequence.SSP_INIT;

    //    var token = $"{gateKeeperToken.Serialize<GateKeeperToken>().ToAsciiString()}";

    //    Assert.AreEqual(EnumSupportPackageSequence.SSP_OK, gateKeeperPassport.InitializeSecurityContext(token, null));

    //    gateKeeperToken.Version = 3;
    //    gateKeeperToken.Sequence = (int) EnumSupportPackageSequence.SSP_EXT;

    //    // Below contains magical answer guid to null byte challenge with ip
    //    token =
    //        $"{gateKeeperToken.Serialize<GateKeeperToken>().ToAsciiString()}{Guid.Parse("a8b9a59e-bd4d-411d-7728-4ec15d29282b").ToByteArray().ToAsciiString()}{new Guid().ToByteArray().ToAsciiString()}";

    //    Assert.AreEqual(EnumSupportPackageSequence.SSP_CREDENTIALS,
    //        gateKeeperPassport.AcceptSecurityContext(token, ip));

    //    gateKeeperToken.Version = 3;
    //    gateKeeperToken.Sequence = (int) EnumSupportPackageSequence.SSP_CREDENTIALS;

    //    // Token with ticket & profile cookies
    //    token =
    //        $"{gateKeeperToken.Serialize<GateKeeperToken>().ToAsciiString()}{passportCredentials.Ticket.Length:X8}{passportCredentials.Ticket}{passportCredentials.Profile.Length:X8}{passportCredentials.Profile}";

    //    Assert.AreEqual(EnumSupportPackageSequence.SSP_CREDENTIALS,
    //        gateKeeperPassport.AcceptSecurityContext(token, ip));
    //}
}