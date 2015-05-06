namespace Persimmon.Pudding.Tests

open Persimmon
open UseTestNameByReflection
open Helper

module internal NonQuotation =

  let ``return int`` = test {
    return 1
  }

module QuotationsTest =

  open Persimmon.Pudding.Quotations

  let ``bind test case`` =
    let msg = """[parameter]
  _arg1: System.Int32 -> 1
  _arg2: Microsoft.FSharp.Core.Unit -> <null>
  a: System.Int32 -> 1
[method call]
  Persimmon.TestBuilder.Return(1) -> TestCase<Int32>({Name = "";
 Parameters = [];})
  assertEquals(2, 1) -> NotPassed (Violated "Expect: 2
Actual: 1")
"""
    let msg2 = "Expect: 2\nActual: 1"
    test {
      let! a = NonQuotation.``return int``
      do! assertEquals 2 a
      return a
    }
    |> shouldNotPassed (msg, [ msg2 ])
