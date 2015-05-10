namespace Persimmon.Quotations.Evaluator

open System
open System.Reflection
open System.Collections.Concurrent
open System.Linq.Expressions

[<StructuredFormatDisplay("{ToStr}")>]
type TestCaseTree =
    | Parameter of string * Type
    | LambdaExpr of TestCaseTree list * Type
    | Argument of TestCaseTree option * MethodInfo * int
    | MethodCall of TestCaseTree option * MethodInfo * TestCaseTree list
    | Value of obj
with
    member this.ToStr =
        match this with
        | Parameter(name, typ) -> sprintf "%s: %s" name typ.FullName
        | LambdaExpr(prms, typ) ->
            let prms =
                prms
                |> List.map (sprintf "(%A)")
                |> String.concat " "
            sprintf "fun %s -> %s" prms typ.FullName
        | Argument _ -> "" // dummy value
        | MethodCall(instance, info, _) ->
            let instance =
                match instance with
                | Some v -> v.ToStr + "."
                | None -> ""
            sprintf "%s%s" instance info.Name
        | Value o -> string o
    override this.ToString() = this.ToStr

module internal ExpressionHelper =

    let registerParameter (recorder: ConcurrentDictionary<TestCaseTree, obj>) (expr: ParameterExpression) =
        let arg = Parameter(expr.Name, expr.Type)
        let inner (value: obj) = recorder.GetOrAdd(arg, value) |> ignore
        let func = Func<obj, _>(inner)
        let args = [| Expression.Convert(expr, typeof<obj>) :> Expression |]
        (arg, Expression.Call(Expression.Constant(func.Target), func.Method, args))

    let addRegisterParameterExpressions recorder body parameters =
        parameters
        |> List.foldBack (fun x (xs, body) ->
            let x, f = registerParameter recorder x
            (x::xs, Expression.Block(f, body) :> Expression))
        <| ([], body)

    let wrapVoid (e:#Expression) =
        if e.Type <> typeof<System.Void> then e :> Expression
        else Expression.Block(e, Expression.Constant(null, typeof<Unit>)) :> Expression

    let private registerAndReturn (recorder: ConcurrentDictionary<TestCaseTree, obj>) key expr typ =
        let typ =
          if typ <> typeof<System.Void> then typ
          else typeof<unit>
        let inner (value: obj) =
          recorder.GetOrAdd(key, value) |> ignore
          value
        let func = Func<obj, _>(inner)
        let args = [| Expression.Convert(wrapVoid expr, typeof<obj>) :> Expression |]
        let register = Expression.Call(Expression.Constant(func.Target), func.Method, args) :> Expression
        Expression.Convert(register, typ) :> Expression

    let addRegisterLambdaExpression recorder body parameters =
        let prms, body = addRegisterParameterExpressions recorder body parameters
        registerAndReturn recorder (LambdaExpr(prms, body.Type)) body body.Type

    let registerArgument recorder instance info index expr =
        let arg = Argument(instance, info, index)
        (arg, registerAndReturn recorder arg expr (info.GetParameters().[index].ParameterType))

    let registerResult recorder instance info args expr =
        registerAndReturn recorder (MethodCall(instance, info, args)) expr info.ReturnType

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
