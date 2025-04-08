using System.Runtime.InteropServices;
using System.Text;
using Irc.Helpers;
using SSPI.NTLM;

public class NtlmType2Message
{
    private readonly NtlmShared.TargetInformation _targetInformation;
    private readonly string _targetName;
    private Dictionary<NtlmFlag, bool> _flags = new();
    private NtlmShared.NTLMSSPMessageType2 _messageType2;

    public NtlmType2Message(uint flags, string targetName, NtlmShared.TargetInformation targetInformation)
    {
        Flags = flags;
        _targetName = targetName;
        _targetInformation = targetInformation;

        EnumerateFlags(flags);
        Configure();
    }

    public uint Flags
    {
        get => _messageType2.Flags;
        set
        {
            _messageType2.Flags = value;
            EnumerateFlags(_messageType2.Flags);
        }
    }

    public byte[] Challenge => _messageType2.Challenge;

    private void Configure()
    {
        _messageType2.Type = 2;
        _messageType2.Signature = NtlmShared.NtlmSignature.ToByteArray();
        _messageType2.Challenge = "AAAAAAAA".ToByteArray();

        if (Flags == 0)
            Flags = (uint)(NtlmFlag.NTLMSSP_NEGOTIATE_UNICODE |
                           NtlmFlag.NTLMSSP_REQUEST_TARGET |
                           NtlmFlag.NTLMSSP_NEGOTIATE_NTLM2 |
                           NtlmFlag.NTLMSSP_NEGOTIATE_128);

        // If the choice has been given select NTLM2
        if (_flags[NtlmFlag.NTLMSSP_NEGOTIATE_NTLM] && _flags[NtlmFlag.NTLMSSP_NEGOTIATE_NTLM2])
            _flags[NtlmFlag.NTLMSSP_NEGOTIATE_NTLM] = false;
    }

    private string? Serialize()
    {
        var payload = new StringBuilder();
        var targetPayload = new StringBuilder();

        var payloadOffset = Marshal.SizeOf<NtlmShared.NTLMSSPMessageType2>();
        var targetOffset = 0;

        var requestTarget = _flags[NtlmFlag.NTLMSSP_REQUEST_TARGET];
        var requestTargetInformation = _flags[NtlmFlag.NTLMSSP_NEGOTIATE_TARGET_INFO] | true;

        if (requestTarget)
        {
            targetOffset = payloadOffset;

            _messageType2.TargetName.Offset = payloadOffset;
            _messageType2.TargetName.AllocatedSpace = (short)_targetName.Length;
            _messageType2.TargetName.Length = (short)_targetName.Length;
        }

        if (requestTargetInformation)
        {
            var targetInformationOffset = targetOffset > 0 ? targetOffset : payloadOffset;

            _messageType2.TargetInformation.Offset = targetInformationOffset;
            _messageType2.Flags |= (uint)NtlmFlag.NTLMSSP_NEGOTIATE_TARGET_INFO;

            var domainNameSubBlock = new NtlmShared.NtlmsspSubBlock(2, (short)_targetInformation.DomainName.Length);
            targetPayload.Append(domainNameSubBlock.Serialize<NtlmShared.NtlmsspSubBlock>().ToAsciiString());
            targetPayload.Append(_targetInformation.DomainName);

            var serverNameSubBlock = new NtlmShared.NtlmsspSubBlock(1, (short)_targetInformation.ServerName.Length);
            targetPayload.Append(serverNameSubBlock.Serialize<NtlmShared.NtlmsspSubBlock>().ToAsciiString());
            targetPayload.Append(_targetInformation.ServerName);

            var dnsDomainNameSubBlock =
                new NtlmShared.NtlmsspSubBlock(4, (short)_targetInformation.DnsDomainName.Length);
            targetPayload.Append(dnsDomainNameSubBlock.Serialize<NtlmShared.NtlmsspSubBlock>().ToAsciiString());
            targetPayload.Append(_targetInformation.DnsDomainName);

            var dnsServerNameSubBlock =
                new NtlmShared.NtlmsspSubBlock(3, (short)_targetInformation.DnsServerName.Length);
            targetPayload.Append(dnsServerNameSubBlock.Serialize<NtlmShared.NtlmsspSubBlock>().ToAsciiString());
            targetPayload.Append(_targetInformation.DnsServerName);

            var terminatorSubBlock = new NtlmShared.NtlmsspSubBlock(0, 0);
            targetPayload.Append(terminatorSubBlock.Serialize<NtlmShared.NtlmsspSubBlock>().ToAsciiString());

            _messageType2.TargetInformation.Length = (short)targetPayload.Length;
            _messageType2.TargetInformation.AllocatedSpace = (short)targetPayload.Length;
        }

        payload.Append(_messageType2.Serialize<NtlmShared.NTLMSSPMessageType2>().ToAsciiString());
        if (requestTarget) payload.Append(_targetName);
        if (requestTargetInformation) payload.Append(targetPayload.ToString());

        return payload.ToString();
    }

    private void EnumerateFlags(uint flags)
    {
        _flags = new Dictionary<NtlmFlag, bool>();
        foreach (var flag in Enum.GetValues<NtlmFlag>()) _flags.Add(flag, ((uint)flag & flags) != 0);
    }

    public override string? ToString()
    {
        return Serialize();
    }
}