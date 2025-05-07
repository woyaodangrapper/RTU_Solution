using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TcpTest")]
[assembly: InternalsVisibleTo("Tcp")]
[assembly: InternalsVisibleTo("RTU.Infrastructures")] // needed by NSubstitute
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")] // needed by NSubstitute