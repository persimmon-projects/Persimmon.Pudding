namespace Persimmon.Quotations.Evaluator

open System
open System.Reflection
open System.Collections.Concurrent
open System.Linq.Expressions

[<StructuredFormatDisplay("{ToStr}")>]
type TestCaseTree =
    | Parameter of string * Type
    | LambdaExpr of TestCaseTree list * TestCaseTree
    | Argument of TestCaseTree option * MethodInfo * int
    | MethodCall of TestCaseTree option * MethodInfo * TestCaseTree list
    | Value of obj
with
    member this.ToStr =
        match this with
        | Parameter(name, typ) -> sprintf "%s: %s" name typ.FullName
        | LambdaExpr(prms, body) ->
            prms
            |> List.map (sprintf "(%A)")
            |> String.concat " "
            |> sprintf "fun %s ->%s%A" Environment.NewLine
            <| string body
        | Argument _ -> "" // dummy value
        | MethodCall(instance, info, args) ->
            let instance =
                match instance with
                | Some v -> v.ToStr + "."
                | None -> ""
            sprintf "%s%s" instance info.Name
        | Value o -> string o
    override this.ToString() = this.ToStr

module internal ExpressionHelper =

    let registerParameter (recorder: ConcurrentDictionary<TestCaseTree, obj>) (expr: ParameterExpression) =
        let inner (value: obj) = recorder.GetOrAdd(Parameter(expr.Name, expr.Type), value) |> ignore
        let func = Func<obj, _>(inner) 
        let args = [| Expression.Convert(expr, typeof<obj>) :> Expression |]
        Expression.Call(Expression.Constant(func.Target), func.Method, args)

    let addRegisterParameterExpressions recorder body parameters =
        parameters
        |> List.foldBack (fun x body ->
            Expression.Block(registerParameter recorder x, body)
            :> Expression)
        <| body

    let registerArgument (recorder: ConcurrentDictionary<TestCaseTree, obj>) instance info index expr =
        let arg = Argument(instance, info, index)
        let inner (value: obj) =
            recorder.GetOrAdd(arg, value) |> ignore
            value
        let func = Func<obj, obj>(inner) 
        let args = [| Expression.Convert(expr, typeof<obj>) :> Expression |]
        let register = Expression.Call(Expression.Constant(func.Target), func.Method, args) :> Expression
        let ret = Expression.Convert(register, info.GetParameters().[index].ParameterType) :> Expression
        (arg, ret)

    let registerResult (recorder: ConcurrentDictionary<TestCaseTree, obj>) instance info args expr =
        let inner (value: obj) =
            recorder.GetOrAdd(MethodCall(instance, info, args), box value) |> ignore
            value
        let func = Func<obj, obj>(inner) 
        let args = [| Expression.Convert(expr, typeof<obj>) :> Expression |]
        let register = Expression.Call(Expression.Constant(func.Target), func.Method, args) :> Expression
        Expression.Convert(register, info.ReturnType) :> Expression

    let callAndRegisterResult recorder (instance: Expression) info args =
        let instanceTree =
            match instance with
            | null -> None
            | :? ConstantExpression as instance -> Some (Value instance.Value)
            | _ -> raise (NotSupportedException("Unsupported expression."))
        let tree, args =
            args
            |> Array.mapi (registerArgument recorder instanceTree info)
            |> Array.unzip
        Expression.Call(instance, info, args)
        |> registerResult recorder instanceTree info (List.ofArray tree)
