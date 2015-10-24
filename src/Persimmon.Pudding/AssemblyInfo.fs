namespace System
open System.Reflection
open System.Runtime.CompilerServices
open System.Runtime.InteropServices

[<assembly: AssemblyTitleAttribute("Persimmon.Pudding")>]
[<assembly: AssemblyDescriptionAttribute("")>]
[<assembly: GuidAttribute("0765BE39-7F17-4C1F-9A3A-D4FAA2F775DD")>]
[<assembly: InternalsVisibleToAttribute("Persimmon.Pudding.Tests")>]
[<assembly: AssemblyProductAttribute("Persimmon.Pudding")>]
[<assembly: AssemblyVersionAttribute("0.1.0")>]
[<assembly: AssemblyFileVersionAttribute("0.1.0")>]
[<assembly: AssemblyInformationalVersionAttribute("0.1.0")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.1.0"
