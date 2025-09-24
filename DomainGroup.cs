using System.Collections.ObjectModel;

namespace ProxyGuy;

public class DomainGroup : ObservableCollection<RequestInfo>
{
    public string Domain { get; }

    public DomainGroup(string domain)
    {
        Domain = domain;
    }
}
