using System.Runtime.InteropServices;
using System.Text;
using Irc.Helpers;
using SSPI.NTLM;

public class NtlmType3Message
{
    private readonly byte[] _byteData;

    private readonly string _data;
    private readonly Dictionary<NtlmFlag, bool> _flags = new();
    private string _lmResponseData = string.Empty;
    private NtlmShared.NTLMSSPMessageType3 _messageType3;
    private string _ntlmResponseData = string.Empty;
    private NtlmShared.NtlmssposVersion _osVersionInfo;
    private string _sessionKeyData = string.Empty;

    private NtlmShared.NtlmsspSecurityBuffer _sessionKeySecBuf;

    private string _targetNameData = string.Empty;
    private string _userNameData = string.Empty;
    private string _workstationNameData = string.Empty;

    public uint Flags;

    public NtlmType3Message(string data)
    {
        _data = data;
        _byteData = _data.ToByteArray();
        Parse(_byteData);
    }

    public string TargetName => Encoding.Unicode.GetString(_targetNameData.ToByteArray());
    public string UserName => Encoding.Unicode.GetString(_userNameData.ToByteArray());
    public string Workstation => Encoding.Unicode.GetString(_workstationNameData.ToByteArray());

    public void Parse(byte[] data)
    {
        _messageType3 = data.Deserialize<NtlmShared.NTLMSSPMessageType3>();
        CopySecurityBuffers();
        EnumerateFlags();
    }

    private void CopySecurityBuffers()
    {
        if (_messageType3.LMResponse.Length > 0)
            _lmResponseData = _data.Substring(_messageType3.LMResponse.Offset, _messageType3.LMResponse.Length);

        if (_messageType3.NTLMResponse.Length > 0)
            _ntlmResponseData = _data.Substring(_messageType3.NTLMResponse.Offset, _messageType3.NTLMResponse.Length);

        if (_messageType3.TargetName.Length > 0)
            _targetNameData = _data.Substring(_messageType3.TargetName.Offset, _messageType3.TargetName.Length);

        if (_messageType3.UserName.Length > 0)
            _userNameData = _data.Substring(_messageType3.UserName.Offset, _messageType3.UserName.Length);

        if (_messageType3.WorkstationName.Length > 0)
            _workstationNameData =
                _data.Substring(_messageType3.WorkstationName.Offset, _messageType3.WorkstationName.Length);

        var legacyNtlm = _messageType3.LMResponse.Offset == 52 || _messageType3.NTLMResponse.Offset == 52 ||
                         _messageType3.TargetName.Offset == 52 || _messageType3.UserName.Offset == 52 ||
                         _messageType3.WorkstationName.Offset == 52;

        if (legacyNtlm)
        {
            Flags = (uint)(NtlmFlag.NTLMSSP_NEGOTIATE_NTLM | NtlmFlag.NTLMSSP_NEGOTIATE_OEM);
        }
        else
        {
            _sessionKeySecBuf = _data.Substring(52, Marshal.SizeOf<NtlmShared.NtlmsspSecurityBuffer>()).ToByteArray()
                .Deserialize<NtlmShared.NtlmsspSecurityBuffer>();
            Flags = _data.Substring(60, sizeof(uint)).ToByteArray().Deserialize<uint>();
            _osVersionInfo = _data.Substring(64, Marshal.SizeOf<NtlmShared.NtlmssposVersion>()).ToByteArray()
                .Deserialize<NtlmShared.NtlmssposVersion>();

            if (_sessionKeySecBuf.Length > 0 &&
                _sessionKeySecBuf.Offset + _sessionKeySecBuf.Length <= _data.Length)
                _sessionKeyData = _data.Substring(_sessionKeySecBuf.Offset, _sessionKeySecBuf.Length);
        }
    }

    private void EnumerateFlags()
    {
        foreach (var flag in Enum.GetValues<NtlmFlag>()) _flags.Add(flag, ((uint)flag & Flags) != 0);
    }

    public bool VerifySecurityContext(string challenge, string password)
    {
        var response = new NtlmResponses();

        if (_flags[NtlmFlag.NTLMSSP_NEGOTIATE_NTLM2])
            try
            {
                var authenticated = _ntlmResponseData.StartsWith(response.NtlmV2Response(_userNameData,
                    password.ToUnicodeString(), challenge, _ntlmResponseData));
                if (!authenticated)
                    authenticated =
                        _ntlmResponseData.StartsWith(response.Ntlm2SessionResponse(password.ToUnicodeString(),
                            challenge,
                            _lmResponseData));

                return authenticated;
            }
            catch (Exception)
            {
            }
        else if (_flags[NtlmFlag.NTLMSSP_NEGOTIATE_NTLM])
            try
            {
                var authenticated = false;
                authenticated = _ntlmResponseData == response.NtlmResponse(password.ToUnicodeString(), challenge);
                if (!authenticated) return _lmResponseData == response.LmResponse(password, challenge);
                //if (_flags[NtlmFlag.NTLMSSP_NEGOTIATE_NTLM])
                //{
                //return 
                //}
                //else // (_flags[NtlmFlag.NTLMSSP_NEGOTIATE_LM_KEY])
                //{

                //}
            }
            catch (Exception)
            {
            }

        return false;
    }
}