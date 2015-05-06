.\.nuget\NuGet.exe Install Persimmon.Console -Pre -OutputDirectory packages -ExcludeVersion

$inputs = @(
  ".\tests\Persimmon.Quotations.Evaluator.Tests\bin\Debug\Persimmon.Quotations.Evaluator.Tests.dll"
  ".\tests\Persimmon.Pudding.Tests\bin\Debug\Persimmon.Pudding.Tests.dll"
)

.\packages\Persimmon.Console\tools\Persimmon.Console.exe --parallel $inputs
