module Persimmon.Pudding.Example

open Persimmon
open Persimmon.Pudding.Quotations
open UseTestNameByReflection

let ``return int`` = test {
  return 1
}

let ``fail test`` = test {
  let! a = ``return int``
  do! assertEquals 2 a
  return a
}
