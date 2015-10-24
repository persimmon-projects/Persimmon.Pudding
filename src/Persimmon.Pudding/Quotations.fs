namespace Persimmon.Pudding

open Persimmon
open System
open System.Text
open System.Collections.Concurrent
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns
open Persimmon.Quotations.Evaluator

module Quotations =

  let private cast<'T> (expr: Expr) : Expr<'T> = expr |> Expr.Cast

  let private unwrapDelay (expr: Expr<unit -> TestCase<'T>>) =
    let rec inner = function
    | Call(_, info, [expr]) when info.Name = "Delay" -> inner expr
    | Lambda(arg, expr) when arg.Type = typeof<unit> ->
      expr
      |> cast<TestCase<'T>>
    | _ -> failwith "If this pattern was called, you should doubt Persimmon.Pudding.Quotations."
    inner expr

  let internal genParameterMessages (builder: StringBuilder) (values: ConcurrentDictionary<TestCaseTree, obj>) =
    let values =
      values
      |> Seq.filter (fun (KeyValue(k, _)) -> match k with | Parameter _ -> true | _ -> false)
    if not <| Seq.isEmpty values then builder.AppendLine("[parameter]") |> ignore
    values
    |> Seq.iter (fun (KeyValue(k, v)) ->
      Printf.bprintf builder "  %A -> %A%s" k v Environment.NewLine)

  let private ignoreMethodNames = [
    "Bind"
    "Source"
  ]

  let internal genMethodCallMessage (builder: StringBuilder) (values: ConcurrentDictionary<TestCaseTree, obj>) =

    let formatArguments args =
      args
      |> List.sortBy (function
      | Argument(_, _, i) -> i
      | _ -> failwith "oops!") // TODO: improve message
      |> List.map (fun x ->
        match values.TryGetValue(x) with
        | true, o -> string o
        | false, _ -> "oops!")
      |> String.concat ", "

    let calls =
      values
      |> Seq.filter (fun (KeyValue(k, _)) ->
        match k with
        | MethodCall(_, info, _) when ignoreMethodNames |> List.exists ((=) info.Name) -> false
        | MethodCall _ -> true
        | _ -> false)

    if not <| Seq.isEmpty calls then builder.AppendLine("[method call]") |> ignore

    calls
    |> Seq.iter (fun (KeyValue(k, v)) ->
      match k with
      | MethodCall(_, _, args) ->
        Printf.bprintf builder "  %A(%s) -> %A%s" k (formatArguments args) v Environment.NewLine
      | _ -> ())

  let internal genLambdaMessage (builder: StringBuilder) (values: ConcurrentDictionary<TestCaseTree, obj>) =

    let formatParameter = function
    | Parameter(_, typ) as x ->
      match values.TryGetValue(x) with
      | true, _ when typ = typeof<unit> -> "()"
      | true, _ when typ.FullName.StartsWith("Microsoft.FSharp.Core.FSharpOption`1") -> "None"
      | true, o -> string o
      | false, _ -> failwith "oops!"
    | _ -> failwith "oops!"

    let lambdas =
      values
      |> Seq.filter (fun (KeyValue(k, _)) ->
        match k with
        | LambdaExpr _ -> true
        | _ -> false)

    if not <| Seq.isEmpty lambdas then builder.AppendLine("[lambda expression]") |> ignore

    lambdas
    |> Seq.iter (fun (KeyValue(k, v)) ->
      match k with
      | LambdaExpr(args, _) ->
        let args =
          args
          |> List.map formatParameter
          |> String.concat " "
        Printf.bprintf builder "  fun %s -> %A%s" args v Environment.NewLine
      | _ -> ())

  type TestBuilder with
    member __.Quote() = ()
    member this.Run(expr: Expr<unit -> TestCase<'T>>) =
      try
        let vs, f = expr |> unwrapDelay |> QuotationEvaluator.Compile
        let test = this.Run(f)
        let body () =
          match test.Run() with
          | Done(_, (Passed _, []), _) as res -> res
          | res ->
            let builder = StringBuilder()
            genParameterMessages builder vs
            genMethodCallMessage builder vs
            genLambdaMessage builder vs
            let msg = builder.ToString()
            if msg = "" then res
            else res |> TestResult.addAssertionResult (NotPassed (Violated msg))
        TestCase(test.Name, test.Parameters, body)
      // TODO: get name value
      with e -> TestCase.makeError None [] e
