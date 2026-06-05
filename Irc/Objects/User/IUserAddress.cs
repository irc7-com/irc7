namespace Irc.Objects.User;

public interface IUserAddress
{
    string Nickname { get; }
    string User { set; get; }
    string Host { set; get; }
    string Server { set; get; }
    string RealName { set; get; }
    string RemoteIp { get; }
    string MaskedIp { get; }
    void SetNickname(string nickname);
    void SetIp(string address);
    string GetUserHost();
    string GetAddress();
    string GetFullAddress();
    string GetIpFullAddress();
    bool IsAddressPopulated();
    string ToString();
    UserAddress.UserHostPair UserHost { get; set; }
}